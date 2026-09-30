# 3. Worked change

Begin with the reservation path. `ReserveAsync` resolves equipment and borrower and rejects missing or inactive borrowers. The repaired method constructs a `Reservation` with one `(clock ?? new SystemClock()).UtcNow` read and the requested expiry. Construction checks identity and `expiresAt > reservedAt`. If the request is invalid, the exception occurs while no equipment method has run. For valid input, `equipment.Reserve()` runs, then the equipment store saves and the reservation store adds. This preserves the old success sequence while moving the fallible constructor earlier.

The focused regression uses a fixed clock at 2026-01-02 10:00 UTC and expiry at 09:00. It asserts `BorrowBoxException`, `EquipmentStatus.Available`, and an empty reservation store. Checking only the exception would miss the original leak. The existing cancel test still creates a valid reservation, cancels it, and asserts Cancelled plus Available. Cancel now checks `equipment.Status == Reserved` before `reservation.Cancel()`.

Checkout follows the same candidate isolation. The service captures `checkedOutAt`, loads equipment and borrower, applies the borrower loan policy, and constructs `Loan` before calling `TryCheckOutAsync`. A due date before the captured checkout date throws `loan.due_date_invalid` while equipment stays Available and no loan is added. For valid input, the store’s existing per-item lock changes equipment and returns the checked-out entity, then the loan store adds the candidate. A failure to acquire equipment therefore does not create a loan.

The return defect was a less obvious ordering issue. `ReturnService` loaded both entities, called `loan.Return(returnedAt)`, and then called `equipment.Return()`. The domain method previously accepted a timestamp before checkout. It now checks that time first. The service first preserves the `loan.already_returned` rejection, then checks `equipment.Status == CheckedOut` before changing the active loan. A mismatched equipment state or early timestamp leaves the loan active and equipment unchanged. The API no longer supplies an uncoordinated `UtcNow`; the service’s injected clock supplies the default.

Expiry captures `clock.UtcNow` once and selects active reservations with `ExpiresAt <= now`, so equality is due. Before expiring a record it requires an existing Reserved equipment item. A missing or mismatched item is skipped without changing reservation status. This is a deliberate recovery policy for inconsistent legacy state: the worker does not claim an expiry that it could not complete.

The design does not add keyed semaphores, clone-on-read, SQLite schema changes, or a transaction abstraction. Those would be larger changes with different failure semantics. The tests establish rejected-input safety and state-machine boundaries; the README and verification limits state what remains outside the claim.

The optional clock parameter is a compatibility choice. Existing unit tests constructed services with the original three or four arguments, so making the `IClock` nullable and defaulting to `SystemClock` preserves those call sites. The API’s dependency injection container sees the registered `IClock` and supplies it. New deterministic tests pass `FixedClock` explicitly. This keeps the change focused on time consistency without a broad test fixture rewrite.

The return endpoint is part of the time repair. Before the change it called `DateTimeOffset.UtcNow` while the expiry service used an injected clock. That made it impossible to run all lifecycle paths under one deterministic clock. The endpoint now calls the overload taking only ID and cancellation token; `ReturnService` captures its clock. The explicit timestamp overload remains useful for tests and any caller that already has an authoritative event time.

The worker mismatch policy is intentionally conservative. If a reservation says Active but its equipment is missing or Available, expiring the reservation alone would hide the fact that equipment was never released from Reserved state. Skipping leaves the inconsistency observable for repair and avoids a false success count. A production system could record a repair event or dead-letter the record, but that would require a durable audit contract absent from this demo.

## The repaired reservation path

This is the method the walkthrough starts from, `src/BorrowBox.Core/Reservations/Reservation.cs:68`. The `Reservation` constructor — the fallible step — now runs before `equipment.Reserve()`, so an invalid expiry throws while the equipment is still untouched.

```csharp
    public async Task<Reservation> ReserveAsync(Guid equipmentId, Guid borrowerId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var equipment = await equipmentStore.GetAsync(equipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("equipment.not_found", "Equipment was not found.");
        var borrower = await borrowerStore.GetAsync(borrowerId, cancellationToken).ConfigureAwait(false)
            ?? throw new BorrowBoxException("borrower.not_found", "Borrower was not found.");
        if (!borrower.IsActive) throw new BorrowBoxException("borrower.inactive", "Borrower is inactive.");

        var reservation = new Reservation(Guid.NewGuid(), equipment.Id, borrower.Id, (clock ?? new SystemClock()).UtcNow, expiresAt);
        equipment.Reserve();
        await equipmentStore.SaveAsync(equipment, cancellationToken).ConfigureAwait(false);
        await reservationStore.AddAsync(reservation, cancellationToken).ConfigureAwait(false);
        return reservation;
    }
```

