# 2. Concepts

## The rule

Validate-before-mutate is an ordering rule for workflows that touch more than one object. Read all references, capture the operation time once, construct the candidate domain object, and check all state preconditions. Only then mutate the live equipment and persist the related record. If candidate construction fails, a live object must still have its original state.

The original reservation defect was:

```csharp
equipment.Reserve();
var reservation = new Reservation(Guid.NewGuid(), equipment.Id, borrower.Id,
    DateTimeOffset.UtcNow, expiresAt);
```

An expiry before the current time makes the constructor throw after `equipment.Status` has become Reserved. The repaired source captures `IClock.UtcNow`, constructs `Reservation`, and only then calls `equipment.Reserve()`. The same shape fixes checkout: a bad due date used to run `TryCheckOutAsync` before `new Loan`, leaving equipment CheckedOut. A valid loan candidate is now built before the equipment operation.

## Time is an input

Time is an input. Using `DateTimeOffset.UtcNow` in one service and an injected clock in another means tests can observe inconsistent boundaries. `ReservationService` and `CheckoutService` accept an optional `IClock`; dependency injection supplies the registered `SystemClock`, while tests pass a fixed implementation. `ReturnService` has an overload that captures its clock and retains the explicit timestamp overload for existing callers. One captured instant per operation prevents a clock tick between validation and construction.

## Domain preconditions and workflow preconditions

State machines have both domain and workflow preconditions. `Loan.Return` first rejects repeated returns, then rejects `returnedAt < CheckedOutAt`, and only then sets `ReturnedAt`. `ReturnService` checks equipment status is CheckedOut before invoking the loan transition. Cancellation and expiry check Reserved before changing a reservation. A method that changes one entity and lets a later method throw creates a split-brain state.

## What an in-memory lock does and does not buy you

`ConcurrentDictionary` makes individual dictionary operations safe; it does not make “reservation status plus equipment status” one atomic transaction. `TryCheckOutAsync` uses a lock on the equipment item, so two checkout attempts for one item still have one winner. That lock does not protect borrower-limit checks across distinct equipment. A shared coordinator or database transaction could address that future concern, but no such mechanism is added here. The course calls this out instead of claiming an in-memory lock is rollback.

## Live references: why a test can pass and still be blind

The storage reference model matters. `GetAsync` returns the entity held in the dictionary. A test that compares the same reference before and after cannot detect mutation. Capture scalar state, status, counts, and independent arrays before the operation. Since reservation and loan stores have no `SaveAsync`, a store failure after equipment mutation cannot currently be rolled back by a service-level contract. Validation ordering reduces rejected-operation damage but is not durable atomicity.

## Candidate objects versus live aggregates

There are two different kinds of candidate in this design. `new Reservation(...)` and `new Loan(...)` are domain candidates: they have immutable IDs and dates and can fail validation. `EquipmentItem` is a live aggregate reference retrieved from a store. Constructing the candidate first creates a safe boundary between fallible input and a mutable reference. Calling `Reserve`, `CheckOut`, or `Return` only after all expected exceptions have been evaluated makes the operation easier to reason about and easier to retry.

## Boundary equality, case by case

Equality deserves explicit language. Reservation creation requires a strict future expiry because an item that is already due should never enter Active state. Expiry processing uses an inclusive comparison because a reservation whose deadline is exactly now is due. Loan due dates are calendar dates, so the constructor compares against the UTC checkout date rather than a local clock time. Return timestamps are instants and may equal checkout, but never precede it. Tests should name these boundary decisions rather than relying on “near now” values.

## Bridge: what each of these becomes against a database

Everything above is taught on `ConcurrentDictionary` and one `lock`. Your next job will almost certainly use EF Core against SQL Server, Postgres or SQLite. This table is the translation; `src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs` already shows two of the rows for real.

| In this course (in memory) | EF Core / SQL equivalent | What changes |
|---|---|---|
| `store.GetAsync(id)` returns the live instance; mutating it *is* the write | `dbContext.Equipment.FirstAsync(...)` returns a **tracked** entity; mutating it stages a change that only lands on `SaveChangesAsync` | Your assertions must read back through a *new* `DbContext` (or `AsNoTracking()`), or you are asserting against the change tracker, not the database |
| `await equipmentStore.SaveAsync(equipment, ct)` | `await dbContext.SaveChangesAsync(ct)` — one call that writes every tracked change | Two separate `SaveAsync` calls become one atomic `SaveChangesAsync`, which is *better* than what this project has |
| Equipment write and reservation write are two independent store calls that can half-succeed | `using var tx = await dbContext.Database.BeginTransactionAsync(ct); ...; await tx.CommitAsync(ct);` | The database half of the split brain disappears. The in-process half does not: a rolled-back transaction leaves your C# objects mutated. Dispose the scope after a failed save |
| `lock (_gate)` inside `InMemoryEquipmentStore.TryCheckOutAsync` | `UPDATE Equipment SET Status='CheckedOut' WHERE Id=@id AND Status='Available'` — in EF Core, `ExecuteUpdateAsync` with the status in the `Where`, checking the affected-row count (`SqliteEquipmentStore.cs:38-46` does exactly this) | The lock is replaced by a conditional update. `changed == 1` means you won; `changed == 0` means somebody else did, and you must **not** treat it as an error to retry blindly |
| "only one winner per equipment item" proven by a racing fake store | A `RowVersion`/`[Timestamp]` column, or `[ConcurrencyCheck]`, producing `DbUpdateConcurrencyException` on a lost update | You now have a concrete exception type to catch and a decision to make: reload and retry, or return 409 to the caller |
| No rollback contract for reservations and loans | `DbUpdateException` / transaction rollback, plus a unique index to make "one active reservation per item" a database rule rather than a service convention | Invariants you can push into the schema stop depending on every service remembering to check them |

Two warnings that survive the translation.

First, **ordering still matters**. A transaction makes writes atomic; it does not make a constructor that throws harmless. If you call `equipment.Reserve()` before `new Reservation(...)` inside a transaction, the rollback saves the database and leaves your tracked entity mutated, and EF Core will happily write that mutation the next time anything calls `SaveChangesAsync` on the same still-alive context. Validate-before-mutate is not a workaround for missing transactions; it is the rule that makes transactions unnecessary for most rejections.

Second, **a domain invariant is not a storage concern**. `Loan.Return`'s "return time cannot precede checkout" and `ReturnService`'s "equipment must still be CheckedOut" have no database equivalent. Isolation levels, unique indexes and `RowVersion` protect you from *concurrent* damage; they never protect you from a well-formed request that should have been refused. Exercise 10 and its solution in chapter 06 make you demonstrate this.
