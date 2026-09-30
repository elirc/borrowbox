# Code reading drill: BorrowBox

Six drills for a shared-screen round. Talk continuously. Give yourself the stated time, then open the answer.

## Drill 1: explain this method (5 minutes)

From `src/BorrowBox.Core/Loans/Loan.cs:65-81`.

```csharp
    public async Task<Loan> CheckOutAsync(Guid equipmentId, Guid borrowerId, DateOnly dueOn, CancellationToken cancellationToken)
    {
        var checkedOutAt = (clock ?? new SystemClock()).UtcNow;
        var equipment = await equipmentStore.GetAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        var borrower = await borrowerStore.GetAsync(borrowerId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("borrower.not_found", "Borrower was not found.");
        if (!borrower.IsActive) throw new BorrowBoxException("borrower.inactive", "Borrower is inactive.");
        var activeLoans = await loanStore.ListActiveByBorrowerAsync(borrower.Id, cancellationToken).ConfigureAwait(false);
        policy.EnsureCanBorrow(activeLoans.Count);

        var loan = new Loan(Guid.NewGuid(), equipment.Id, borrower.Id, checkedOutAt, dueOn);
        equipment = await equipmentStore.TryCheckOutAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_available", "Equipment is not available.");
        await loanStore.AddAsync(loan, cancellationToken).ConfigureAwait(false);
        return loan;
    }
```

<details>
<summary>Answer</summary>

Five phases in a deliberate order. One clock read on the first line, so validation and construction share an instant. Two reads with null-coalescing throws, each carrying a machine code the exception handler turns into a 422. A policy check through `BorrowerLoanPolicy.EnsureCanBorrow` (`src/BorrowBox.Core/Loans/Loan.cs:51-55`), which throws `borrower.loan_limit` when the active count has reached the maximum; DI registers the maximum as 3 (`src/BorrowBox.Api/Program.cs:35`). Then the fallible construction: `new Loan(...)` throws `loan.due_date_invalid` if `dueOn` is before the UTC checkout date. Only after that does the single mutating call happen, `TryCheckOutAsync`, which returns null when the item is not `Available`.

Two things to say out loud. First, the blank line before `new Loan` is the boundary: everything above it can throw with nothing mutated. Second, the local `equipment` is reassigned to whatever the store returns, so the checked-out instance is the store's, not the one read earlier.

Weaknesses worth naming: `activeLoans.Count` is read outside any lock, so the limit races across distinct items; and if `loanStore.AddAsync` threw, the item would already be `CheckedOut` with no loan, because there is no rollback contract.
</details>

## Drill 2: spot the bug (4 minutes)

A mutated copy of `src/BorrowBox.Core/Reservations/Reservation.cs:68-81`. One behaviour differs from the repository.

```csharp
    public async Task<Reservation> ReserveAsync(Guid equipmentId, Guid borrowerId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var equipment = await equipmentStore.GetAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        var borrower = await borrowerStore.GetAsync(borrowerId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("borrower.not_found", "Borrower was not found.");
        if (!borrower.IsActive) throw new BorrowBoxException("borrower.inactive", "Borrower is inactive.");

        equipment.Reserve();
        var reservation = new Reservation(Guid.NewGuid(), equipment.Id, borrower.Id, (clock ?? new SystemClock()).UtcNow, expiresAt);
        await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
        await reservationStore.AddAsync(reservation, cancellationToken).ConfigureAwait(false);
        return reservation;
    }
```

<details>
<summary>Answer</summary>

`equipment.Reserve()` has been moved ahead of `new Reservation(...)`. In the repository they are the other way round: construction at `src/BorrowBox.Core/Reservations/Reservation.cs:76`, `Reserve()` at line 77.

This is the original defect. `Reserve()` sets `Status = Reserved` (`src/BorrowBox.Core/Equipment/Equipment.cs:52`), then the constructor throws `reservation.expiry_invalid` for any `expiresAt <= reservedAt` (`src/BorrowBox.Core/Reservations/Reservation.cs:15`). The caller sees a 422, the equipment is `Reserved`, and there is no reservation row, so `ReservationExpiryService` will never release it: its loop iterates reservations, not equipment.

Note what does *not* change: the exception type and code are identical either way. The only observable difference is the surviving `equipment.Status`, which is exactly what `ReservationTests.InvalidExpiry_DoesNotReserveEquipment` asserts on line 50 of `tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs`.
</details>

## Drill 3: spot the bug (4 minutes)

A mutated copy of `src/BorrowBox.Core/Reservations/Reservation.cs:114-132`.

```csharp
    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken)
    {
        var reservations = await reservationStore.ListAsync(cancellationToken).ConfigureAwait(false);
        var expiredCount = 0;

        foreach (var reservation in reservations.Where(item =>
                     item.Status == ReservationStatus.Active && item.ExpiresAt < clock.UtcNow))
        {
            var equipment = await equipmentStore.GetAsync(reservation.EquipmentId, cancellationToken).ConfigureAwait(false);
            if (equipment is null || equipment.Status != EquipmentStatus.Reserved) continue;
            reservation.Expire();
            equipment.ReleaseReservation();
            await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
            expiredCount++;
        }

        return expiredCount;
    }
```

<details>
<summary>Answer</summary>

Two mutations, both in the `Where` predicate.

First, `var now = clock.UtcNow;` before the list read (`src/BorrowBox.Core/Reservations/Reservation.cs:116`) has been deleted and the clock is read inside the predicate instead. `Where` is lazy, so with a real `SystemClock` each element is evaluated against a different instant. One pass over the collection no longer has a single "now".

Second, `<=` has become `<`. The repository uses `item.ExpiresAt <= now` (`src/BorrowBox.Core/Reservations/Reservation.cs:121`) because a deadline reached exactly is due. With `<`, a reservation whose `ExpiresAt` equals the current tick is skipped for one worker cycle, and the cycle is one minute (`src/BorrowBox.Api/Reservations/ReservationExpiryWorker.cs:11`).

The equality case is not hypothetical: `ReservationExpiryTests.ExpireDue_MismatchedEquipment_DoesNotChangeReservation` uses an expiry of exactly 2026-01-02 10:00 with a clock of exactly 2026-01-02 10:00 (`tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationExpiryTests.cs:60-66`). It expects 0 only because the equipment is `Available`, not because the time comparison excluded it.
</details>

## Drill 4: predict the status code (6 minutes)

The API is running with the default registrations from `src/BorrowBox.Api/Program.cs`. Equipment item `E` exists and is `Available`; borrower `B` exists and is active. Give the status code and the resulting `equipment.Status` for each.

1. `POST /api/v1/equipment` with `X-Demo-Role: Student`, body `{"assetTag":"CAM-01","name":"Camera","category":"Media"}`
2. `POST /api/v1/reservations` with no `X-Demo-Role` header
3. `POST /api/v1/reservations` with `X-Demo-Role: Student`, `expiresAt` one hour in the past
4. `POST /api/v1/loans` with `X-Demo-Role: Student`, `dueOn` yesterday
5. `GET /api/v1/equipment?page=0`
6. `POST /api/v1/equipment` twice with `X-Demo-Role: Staff` and the same `assetTag`

<details>
<summary>Answers</summary>

1. **403.** The create route requires `BorrowBoxSecurity.StaffPolicy` (`src/BorrowBox.Api/Equipment/EquipmentEndpoints.cs:20`), and the policy requires the `Staff` role (`src/BorrowBox.Api/Program.cs:31`). The handler authenticated the `Student` successfully, so it is forbidden, not unauthorized.
2. **401.** `HandleAuthenticateAsync` returns `AuthenticateResult.NoResult()` when the header is absent (`src/BorrowBox.Api/Security/DemoHeaderAuthenticationHandler.cs:21-22`), and the route carries a bare `RequireAuthorization()` (`src/BorrowBox.Api/Reservations/ReservationEndpoints.cs:16`).
3. **422**, body code `reservation.expiry_invalid`, equipment stays **Available**. The constructor throws at `src/BorrowBox.Core/Reservations/Reservation.cs:15` before `equipment.Reserve()` on line 77. `BorrowBoxExceptionHandler` sets 422 (`src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs:12`).
4. **422**, code `loan.due_date_invalid`, equipment stays **Available** and no loan is stored. `new Loan(...)` at `src/BorrowBox.Core/Loans/Loan.cs:76` throws before `TryCheckOutAsync` on line 77.
5. **422**, code `equipment.page_invalid`. `EquipmentService.SearchAsync` rejects `page < 1 || pageSize is < 1 or > 50` (`src/BorrowBox.Core/Equipment/Equipment.cs:112`) by throwing a `BorrowBoxException`, so the read path returns 422 rather than 400.
6. **201 both times.** There is no uniqueness check in `EquipmentService.CreateAsync` (`src/BorrowBox.Core/Equipment/Equipment.cs:101-106`) and `InMemoryEquipmentStore.AddAsync` keys on `item.Id`, which is a fresh GUID. The unique index on `AssetTag` exists only in the EF model (`src/BorrowBox.Infrastructure/Data/BorrowBoxDbContext.cs:14`), and that store is not registered. Swapping to SQLite would turn the second call into an unhandled `DbUpdateException` and a 500, because the handler only maps `BorrowBoxException`.
</details>

## Drill 5: predict the state (5 minutes)

```csharp
var equipmentStore = new InMemoryEquipmentStore();
var loanStore = new InMemoryLoanStore();
var equipment = new EquipmentItem(Guid.NewGuid(), "LAP-4", "Laptop", "Computing");
equipment.MarkForInspection();
var loan = new Loan(Guid.NewGuid(), equipment.Id, Guid.NewGuid(),
    new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero), new DateOnly(2026, 5, 10));
await equipmentStore.AddAsync(equipment, CancellationToken.None);
await loanStore.AddAsync(loan, CancellationToken.None);

await new ReturnService(equipmentStore, loanStore)
    .ReturnAsync(loan.Id, new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero), CancellationToken.None);
```

What happens, and what would happen if `src/BorrowBox.Core/Loans/Loan.cs:97-98` were deleted?

<details>
<summary>Answer</summary>

As written it throws `BorrowBoxException` with code `equipment.not_checked_out`. `ReturnService.ReturnAsync` finds the loan, sees it is active, loads the equipment and finds `Status == Inspection`, which is not `CheckedOut` (`src/BorrowBox.Core/Loans/Loan.cs:97-98`). Nothing mutates: the loan stays active and the item stays in `Inspection`.

Delete those two lines and the call succeeds. `loan.Return(...)` passes both of its own guards, because the loan has not been returned and 2026-05-03 is after 2026-05-01. Then `equipment.Return()` runs, and it is the only transition in `EquipmentItem` with no precondition: `public void Return() => Status = EquipmentStatus.Available;` (`src/BorrowBox.Core/Equipment/Equipment.cs:67`). An item under inspection silently becomes lendable. The guard has to live in the service because `Loan` holds only an `EquipmentId` and cannot see the item.
</details>

## Drill 6: review this diff (8 minutes)

A colleague proposes this change to `src/BorrowBox.Core/Reservations/Reservation.cs`. Find the defects.

```diff
     public async Task<Reservation> CancelAsync(Guid id, CancellationToken cancellationToken)
     {
         var reservation = await GetActiveAsync(id, cancellationToken).ConfigureAwait(false);
         var equipment = await GetEquipmentAsync(reservation, cancellationToken).ConfigureAwait(false);
-        if (equipment.Status != EquipmentStatus.Reserved)
-            throw new BorrowBoxException("equipment.not_reserved", "Equipment is not reserved for this reservation.");
         reservation.Cancel();
-        equipment.ReleaseReservation();
-        await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
+        try
+        {
+            equipment.ReleaseReservation();
+            await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
+        }
+        catch (BorrowBoxException)
+        {
+            // equipment was already released elsewhere; the cancel still counts
+        }
         return reservation;
     }
```

<details>
<summary>Answer</summary>

Three defects.

**1. It reintroduces the split-brain state the repair removed.** The explicit precondition is gone, so `reservation.Cancel()` now runs first. If the item is not `Reserved`, `ReleaseReservation()` throws (`src/BorrowBox.Core/Equipment/Equipment.cs:63`), the catch swallows it, and the method returns a `Cancelled` reservation over an item in some other status. `ReservationTests.Cancel_MismatchedEquipment_DoesNotChangeReservation` asserts the opposite: reservation still `Active` and equipment still `Available` (`tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs:72-73`). This diff turns that test red.

**2. The catch is too wide.** It catches every `BorrowBoxException`, not just `equipment.not_reserved`. `SaveAsync` is inside the `try`, so a future store that validates on write would have its error silently discarded too. If a catch is genuinely wanted, it must filter on `ex.Code` (`src/BorrowBox.Core/Common/BorrowBoxException.cs:5`) and cover only the transition call.

**3. The comment is wrong about "already released elsewhere".** With the default `InMemoryEquipmentStore`, `GetAsync` returns the live instance (`src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs:17-22`), so the status read and the transition see the same object with no window between them. The scenario the code claims to handle is not the one it hits; what it actually hits is the inconsistent-legacy-state case, and the repository's answer to that is refuse and stay visible, the same policy as the expiry loop skip at line 124.

Say the alternative out loud: keep the precondition, and if a genuine "already released" outcome is wanted, return it as a distinct result rather than a swallowed exception.
</details>
