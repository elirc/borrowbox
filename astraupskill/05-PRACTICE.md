# 5. Practice

Every exercise has a matching solution in chapter 06. Use the real namespaces and existing store implementations. Put focused tests in the infrastructure test projects, where the Core types and in-memory stores are already referenced.

> **Do not open `tests/BorrowBox.Infrastructure.Tests/` until you have written your own version of exercises 1–4.** Astra committed the finished tests for those cases to the repo, so the answer key is sitting next to the question. Chapter 06 deliberately uses *different* fixtures from the shipped tests so you can still check yourself afterwards.

## Exercise 0: run the signature-compatible disposable starter

Use the [advanced failure lab](advanced/03-FAILURE-LAB.md) before studying the worked repair. Its generator copies current source into a new absolute output directory outside the project and introduces bounded behavioral defects without changing constructor or interface signatures.

Do not overwrite live source with the historical snapshots. They remain useful for reading the original change, but are not a compiling exercise baseline. The generator rejects existing output directories and changed source anchors. Edit only the generated starter, record its failing checks, repair one defect at a time, and compare with a separately generated reference lab.

The new harness has its own documented result counts; they are not counts from the application's old full test suite. [Follow the complete advanced route](advanced/README.md) for persistence failure, lifecycle, concurrency, exercises and a separate solution rubric.

## Predict before you run

For every exercise below, fill in this table on paper **before** you run anything, then run the single focused test rather than the whole suite.

| # | (a) expected `BorrowBoxException` code | (b) expected `equipment.Status` after | (c) expected store count after | Focused command |
|---|---|---|---|---|
| 1 | `reservation.expiry_invalid` | ? | ? | `dotnet test tests/BorrowBox.Infrastructure.Tests -c Release --filter "FullyQualifiedName~ReservationTests.InvalidExpiry"` |
| 2 | ? | ? | ? | `--filter "FullyQualifiedName~CheckoutTests.InvalidDueDate"` |
| 3 | (two cases: one succeeds, one throws) | ? | ? | `--filter "FullyQualifiedName~ReservationTests"` |
| 4 | ? | ? | ? | `--filter "FullyQualifiedName~CheckoutTests.ReturnBeforeCheckout"` |
| 5 | ? (two different codes) | ? | ? | `--filter "FullyQualifiedName~MismatchedEquipment"` |
| 6 | ? | ? | ? | `--filter "FullyQualifiedName~Return_SameLoanTwice"` |
| 7 | none (the happy path) | ? | ? | `--filter "FullyQualifiedName~ReservationExpiryTests"` |
| 8 | none | ? | ? | `--filter "FullyQualifiedName~CheckOut_TwoCompetingRequests"` |
| 9 | design only | — | — | — |
| 10 | ? | ? | ? | `--filter "FullyQualifiedName~SqliteEquipmentStoreTests"` |

Columns (b) and (c) are the ones that matter. A prediction of "it throws" is not a prediction; "it throws `loan.due_date_invalid` **and** the item is still `Available` **and** `ListActiveByBorrowerAsync` returns zero rows" is. In PowerShell keep the `--filter` value in double quotes so the shell does not interpret `|`; in bash use single quotes.

## Exercise 1: reservation candidate

Create Available equipment and an active borrower. Inject a clock at 10:00 and call `ReserveAsync` with expiry 09:00. Assert the domain exception, Available equipment, and zero reservations.

## Exercise 2: checkout candidate

Create Available equipment and active borrower. Inject a clock at 10 February and call checkout with due date 9 February. Assert `loan.due_date_invalid`, Available equipment, and no active loan for the borrower.

## Exercise 3: valid boundaries

Prove an expiry one hour after reservation succeeds, while an expiry equal to reservation time fails. Prove a due date equal to checkout’s UTC date succeeds. Use one fixed clock and avoid `UtcNow` in the test setup.

## Exercise 4: return ordering

Create a CheckedOut equipment item and a Loan. Return it with `checkedOutAt - 1 minute`. Assert the loan remains active, `ReturnedAt` is null, and equipment remains CheckedOut.

## Exercise 5: mismatched lifecycle state

Create an active reservation whose equipment is Available and call `CancelAsync`. Create an active loan whose equipment is Available and call `ReturnAsync`. Assert both operations throw before changing the entity that was still valid.

## Exercise 6: repeated operations

Cancel a reservation twice, expire a reservation twice, and return a loan twice. Assert the first operation changes state and the second reports the domain code without a new transition.

## Exercise 7: expiry equality

Seed a Reserved equipment item and reservation with `ExpiresAt` exactly equal to a fixed clock. Run `ExpireDueAsync` and assert count one, Expired reservation, and Available equipment. Add a future reservation and assert it remains Active.

## Exercise 8: checkout competition

Reuse `CheckoutTests.CheckOut_TwoCompetingRequests_OnlyOneSucceeds`. Explain what the equipment lock proves and why it does not prove borrower-limit atomicity when two different items are involved.

## Exercise 9: proposed command

Design a future `ChangeReservationStatus` command with actor authorization, expected version, legal transitions, and a transaction that updates reservation and equipment together. Mark it proposed: this project has no versions, coordinator, or reservation save contract.

## Exercise 10: port one repair onto real persistence

Everything above happens in memory, where "save" is a dictionary write and a rejected operation damages nothing but a reference you still hold. This exercise moves one repair onto the SQLite adapter that already exists, so you can see what changes when storage is a database.

`src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs` implements the same `IEquipmentStore` contract against `BorrowBoxDbContext`. It is not selected by `Program.cs`; `tests/BorrowBox.Infrastructure.Tests/Equipment/SqliteEquipmentStoreTests.cs` is the only thing that exercises it.

Do this:

1. Write a test that composes `ReservationService` with `SqliteEquipmentStore` (over a SQLite connection, as `SqliteEquipmentStoreTests.Save_PersistsChangesAcrossContexts` does) plus the in-memory borrower and reservation stores, and calls `ReserveAsync` with an expiry in the past.
2. Assert the same three things you have been asserting all course: the domain exception, the equipment row still `Available` **when read back from a new `DbContext`**, and zero reservations.
3. Now deliberately break it: move `equipment.Reserve()` back above the constructor in a local copy and watch what the *database* ends up holding. Note the difference from the in-memory case — the in-memory store leaves a mutated live object, the SQLite store leaves nothing until `SaveAsync` runs, so the ordering bug is *partly* masked by the save boundary.
4. Write down which of the four repairs in this course are still needed once persistence is real, and which are subsumed by a transaction. **Acceptance:** a test that passes against the repaired code, fails against the reordered code, and a paragraph naming at least one repair that a transaction would *not* have saved you from.

The answer to (4) is in chapter 06; the honest short version is that a transaction rolls back writes, not the in-process object graph, and `Loan.Return`'s "return time cannot precede checkout" rule is a domain invariant no isolation level will enforce for you.

For each exercise, write the failure code you expect before running. This makes a broad `Assert.ThrowsAsync<BorrowBoxException>` more useful: `reservation.expiry_invalid`, `loan.due_date_invalid`, `loan.return_time_invalid`, `equipment.not_reserved`, and `equipment.not_checked_out` describe different boundaries. Then assert the state that must remain unchanged. A test that names only the exception cannot catch a mutation leak.

When creating fixtures, use different IDs for equipment and borrower records and add every record to its store before invoking the service. The stores return references, so retain the same `equipment` and `loan` variables for after-state checks. For a store-level count, call the actual `ListAsync` or `ListActiveByBorrowerAsync` method; do not assume a private dictionary or invent a `Save` method. This practice mirrors the production contracts and prevents a green test from proving the wrong thing.
