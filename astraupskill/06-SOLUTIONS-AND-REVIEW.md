# 6. Solutions and review

The examples below are method-body excerpts or complete test methods as labeled. They use the existing test imports and store contracts; no fictional repository or clock API is introduced.

## Solution 0: the disposable red run

The current [advanced failure lab](advanced/03-FAILURE-LAB.md) replaces the old manual snapshot-restoration exercise. Its starter preserves today's signatures and introduces two ordering defects in copied source. The expected result is two failed state-preservation checks and four passing checks; the reference passes all six.

Move candidate construction before the visible equipment transition in each generated service. Keep the state assertions. The persistence-failure and cross-item borrower-limit observations remain green because they describe known limitations, not desired transactional guarantees. Historical snapshots remain available for comparison and should not overwrite application files.

## Solutions 1–2: construct first

> These fixtures are deliberately **not** the ones Astra committed. `ReservationTests.InvalidExpiry_DoesNotReserveEquipment` in the repo uses an expiry one hour in the past; the solution below uses the harder equality boundary (`expiresAt == reservedAt`), which is the case juniors get wrong. Compare both once you have written your own.

The reservation solution is a complete method body:

```csharp
[Fact]
public async Task ExpiryEqualToNow_DoesNotReserveEquipment()
{
    var equipmentStore = new InMemoryEquipmentStore();
    var borrowerStore = new InMemoryBorrowerStore();
    var reservationStore = new InMemoryReservationStore();
    var equipment = new EquipmentItem(Guid.NewGuid(), "TRI-7", "Tripod", "Media");
    var borrower = new Borrower(Guid.NewGuid(), "S701", "Nadia");
    await equipmentStore.AddAsync(equipment, CancellationToken.None);
    await borrowerStore.AddAsync(borrower, CancellationToken.None);
    var clock = new FixedClock(new DateTimeOffset(2026, 4, 6, 9, 30, 0, TimeSpan.Zero));
    var service = new ReservationService(equipmentStore, borrowerStore, reservationStore, clock);

    var error = await Assert.ThrowsAsync<BorrowBoxException>(() => service.ReserveAsync(
        equipment.Id, borrower.Id, clock.UtcNow, CancellationToken.None));

    Assert.Equal("reservation.expiry_invalid", error.Code);
    Assert.Equal(EquipmentStatus.Available, equipment.Status);
    Assert.Empty(await reservationStore.ListAsync(CancellationToken.None));
}
```

Equality fails because `Reservation`'s constructor tests `expiresAt <= reservedAt` (`Reservation.cs:15`), not `<`. A reservation that is already due the instant it is created must never reach `Active`. Note the `error.Code` assertion: naming the code is what turns "it threw" into "it threw for the reason I predicted".

Checkout uses the mirror-image boundary — the one that must **succeed**:

```csharp
[Fact]
public async Task DueDateEqualToCheckoutDate_CheckoutSucceeds()
{
    var equipmentStore = new InMemoryEquipmentStore();
    var borrowerStore = new InMemoryBorrowerStore();
    var loanStore = new InMemoryLoanStore();
    var equipment = new EquipmentItem(Guid.NewGuid(), "CAM-31", "Camera", "Media");
    var borrower = new Borrower(Guid.NewGuid(), "S702", "Omar");
    await equipmentStore.AddAsync(equipment, CancellationToken.None);
    await borrowerStore.AddAsync(borrower, CancellationToken.None);
    var clock = new FixedClock(new DateTimeOffset(2026, 4, 6, 23, 45, 0, TimeSpan.Zero));
    var service = new CheckoutService(equipmentStore, borrowerStore, loanStore, new BorrowerLoanPolicy(3), clock);

    var loan = await service.CheckOutAsync(equipment.Id, borrower.Id, new DateOnly(2026, 4, 6), CancellationToken.None);

    Assert.True(loan.IsActive);
    Assert.Equal(EquipmentStatus.CheckedOut, equipment.Status);
    Assert.Single(await loanStore.ListActiveByBorrowerAsync(borrower.Id, CancellationToken.None));
}
```

The 23:45 clock is the interesting part: `Loan`'s constructor compares `dueOn` against `DateOnly.FromDateTime(checkedOutAt.UtcDateTime)` (`Loan.cs:13`), so a same-day due date is legal even fifteen minutes before midnight UTC — and would be *illegal* fifteen minutes later. If your local time zone is behind UTC, "today" for the learner and "today" for the constructor are different days. That is why the course insists on a fixed clock rather than `DateTimeOffset.UtcNow`.

For the rejection direction, the critical production line is `var loan = new Loan(..., checkedOutAt, dueOn);` **before** `TryCheckOutAsync`, and the assertions are equipment Available plus an empty `ListActiveByBorrowerAsync`.

## Solutions 3–5: state boundaries

The constructor’s strict reservation comparison means equal expiry fails; expiry worker selection uses `<=`, so an already-created reservation at equal `now` is due. A due date equal to `DateOnly.FromDateTime(checkedOutAt.UtcDateTime)` succeeds. Keep these rules separate in tests.

The complete return assertion is:

```csharp
await Assert.ThrowsAsync<BorrowBoxException>(() => new ReturnService(equipmentStore, loanStore)
    .ReturnAsync(loan.Id, checkedOutAt.AddMinutes(-1), CancellationToken.None));
Assert.True(loan.IsActive);
Assert.Null(loan.ReturnedAt);
Assert.Equal(EquipmentStatus.CheckedOut, equipment.Status);
```

For a mismatched reservation, set equipment Available and call `CancelAsync`; the service checks status before `reservation.Cancel()`. For a mismatched loan, set equipment Available and call `ReturnAsync`; the service returns `equipment.not_checked_out` before `loan.Return`.

## Solutions 6–7: transitions and worker

Domain `EnsureActive` makes repeated reservation cancel/expire calls fail. `Loan.Return` checks `IsActive` first. The test should assert the first status and then the second exception code. The expiry equality solution uses `new Reservation(..., reservedAt, reservedAt.AddHours(1))` and a fixed clock at that expiry. It asserts one expiration and Available equipment. A future reservation remains Active.

## Solution 8: concurrency scope

The racing equipment store in the existing checkout test coordinates two readers, then lets one `TryCheckOutAsync` win. The assertion `attempts.Count(success => success) == 1` is meaningful because both tasks are forced into the race and one success is required. This proves the per-item checkout operation is guarded. It does not prove that two services atomically observe a borrower’s active-loan count when they check out different equipment. A shared keyed coordinator or database constraint would be a separate design.

## Solution 9: proposed extension

A real command would load actor, reservation, equipment, and expected version; reject unauthorized actors, stale versions, missing references, and illegal transitions; then update both records and append an activity event inside one transaction. On failure, neither record should change. BorrowBox has no version field, reservation `SaveAsync`, or cross-store transaction, so this is a review design only. Do not add it to the current evidence.

Review every rejection for side effects. Invalid construction must precede `Reserve` or `TryCheckOut`. Return-time rejection must precede `ReturnedAt`. Equipment mismatch must precede reservation or loan transition. Worker mismatch must not create a false expiration count. This ordering is the actual lesson.

One useful mutation exercise is to move `equipment.Reserve()` above the `new Reservation` line. The invalid-expiry test should then fail on Available status. Move `TryCheckOutAsync` above `new Loan` and the invalid-due-date test should fail on CheckedOut status. Remove the `returnedAt < CheckedOutAt` guard and the return-before-checkout test should fail because `ReturnedAt` becomes non-null. These mutations connect each assertion to one line of production behavior.

Another review pass asks what happens when a later store call fails. The current `SaveAsync` and `AddAsync` methods can throw cancellation or an implementation exception after an entity was changed. There is no reservation or loan save contract to restore a prior object, and the stores hold live references. Therefore the implementation claims validation-before-mutate and precondition ordering only. A durable implementation would need a transaction boundary or an explicit command coordinator, plus failure tests against that boundary.

## Solution 10: the same repair against SQLite

The store to compose is the one that already exists, `SqliteEquipmentStore(BorrowBoxDbContext)`. Arrange it the way `SqliteEquipmentStoreTests` does — an open `SqliteConnection("Data Source=:memory:")` kept alive for the duration of the test, `EnsureCreated()`, and a **second** `BorrowBoxDbContext` over the same connection for the read-back:

```csharp
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<BorrowBoxDbContext>().UseSqlite(connection).Options;
await using (var setup = new BorrowBoxDbContext(options))
{
    await setup.Database.EnsureCreatedAsync();
}

await using var writeContext = new BorrowBoxDbContext(options);
var equipmentStore = new SqliteEquipmentStore(writeContext);
var equipment = new EquipmentItem(Guid.NewGuid(), "SQL-1", "Projector", "AV");
await equipmentStore.AddAsync(equipment, CancellationToken.None);
var clock = new FixedClock(new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero));
var service = new ReservationService(equipmentStore, borrowerStore, reservationStore, clock);

await Assert.ThrowsAsync<BorrowBoxException>(() => service.ReserveAsync(
    equipment.Id, borrower.Id, clock.UtcNow.AddMinutes(-1), CancellationToken.None));

await using var readContext = new BorrowBoxDbContext(options);
var stored = await new SqliteEquipmentStore(readContext).GetAsync(equipment.Id, CancellationToken.None);
Assert.Equal(EquipmentStatus.Available, stored!.Status);
```

Three things change, and they are the point of the exercise.

**1. The read-back is a different object.** `SqliteEquipmentStore.GetAsync` uses `AsNoTracking()` and maps a row to a fresh `EquipmentItem`. The in-memory store hands you the live instance, so `Assert.Equal(EquipmentStatus.Available, equipment.Status)` on your local variable proves something about *storage*. Against SQLite that same assertion proves nothing at all — your local `equipment` was never persisted after the mutation. You must assert on a value read back through a new context.

**2. The save boundary hides part of the bug.** In the reordered (broken) version, `equipment.Reserve()` mutates the in-memory instance and the constructor then throws, so `SaveAsync` never runs — the database still reads `Available`. The in-memory store is *more* revealing here, because its live reference makes the partial mutation visible. Do not conclude that the database made the bug go away: it made it invisible until something later saves the same object. A service that catches the exception, logs it and continues to save the equipment for an unrelated reason writes the corruption for real.

**3. A transaction does not subsume the repairs.** Wrapping `ReserveAsync` in `BeginTransactionAsync` would roll back the equipment row and the reservation row together, which handles the *storage* half of the split brain. It does nothing about three of the four repairs in this course:
- `Loan.Return`'s `returnedAt < CheckedOutAt` check is a domain invariant. No isolation level rejects a nonsensical timestamp.
- `ReturnService`'s "equipment must still be CheckedOut" check is a workflow precondition across two aggregates; a transaction makes the write atomic, not the decision correct.
- The expiry loop's status check prevents an *unwanted but individually valid* write. A transaction commits it happily.

Only the reservation/checkout ordering defect is genuinely a storage-atomicity problem, and even there the ordering fix is cheaper, testable without a database, and leaves the in-process object graph consistent — which a rollback never does, because EF Core rolls back the database, not your C# objects. That last sentence is the one worth memorising.

## Complete reservation fixture

This is the complete current `tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs`, including imports and its fixed clock. The cases already exist in the delivered project: read or modify them there rather than adding duplicates. They demonstrate the successful cancel path, invalid expiry before equipment mutation, and a mismatched equipment state. The return excerpt above belongs inside `CheckoutTests` with the setup shown in `ReturnBeforeCheckout_DoesNotChangeEitherEntity`; it is an assertion excerpt, not a standalone test.

```csharp
using BorrowBox.Core.Borrowers;
using BorrowBox.Core.Common;
using BorrowBox.Core.Equipment;
using BorrowBox.Core.Reservations;
using BorrowBox.Infrastructure.Borrowers;
using BorrowBox.Infrastructure.Equipment;
using BorrowBox.Infrastructure.Reservations;

namespace BorrowBox.Infrastructure.Tests.Reservations;

public sealed class ReservationTests
{
    [Fact]
    public async Task Cancel_ReleasesEquipment()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var reservationStore = new InMemoryReservationStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "MIC-1", "Microphone", "Audio");
        var borrower = new Borrower(Guid.NewGuid(), "student@example.edu", "Student");
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);
        var service = new ReservationService(equipmentStore, borrowerStore, reservationStore,
            new FixedClock(new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero)));

        var reservation = await service.ReserveAsync(equipment.Id, borrower.Id,
            new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero), CancellationToken.None);
        await service.CancelAsync(reservation.Id, CancellationToken.None);

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }

    [Fact]
    public async Task InvalidExpiry_DoesNotReserveEquipment()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var reservationStore = new InMemoryReservationStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "MIC-2", "Microphone", "Audio");
        var borrower = new Borrower(Guid.NewGuid(), "student2@example.edu", "Student Two");
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);
        var service = new ReservationService(equipmentStore, borrowerStore, reservationStore,
            new FixedClock(new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero)));

        await Assert.ThrowsAsync<BorrowBoxException>(() => service.ReserveAsync(
            equipment.Id, borrower.Id, new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.Zero), CancellationToken.None));

        Assert.Equal(EquipmentStatus.Available, equipment.Status);
        Assert.Empty(await reservationStore.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_MismatchedEquipment_DoesNotChangeReservation()
    {
        var equipmentStore = new InMemoryEquipmentStore();
        var borrowerStore = new InMemoryBorrowerStore();
        var reservationStore = new InMemoryReservationStore();
        var equipment = new EquipmentItem(Guid.NewGuid(), "MIC-3", "Microphone", "Audio");
        var borrower = new Borrower(Guid.NewGuid(), "student3@example.edu", "Student Three");
        var reservation = new Reservation(Guid.NewGuid(), equipment.Id, borrower.Id,
            new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero));
        await equipmentStore.AddAsync(equipment, CancellationToken.None);
        await borrowerStore.AddAsync(borrower, CancellationToken.None);
        await reservationStore.AddAsync(reservation, CancellationToken.None);

        await Assert.ThrowsAsync<BorrowBoxException>(() => new ReservationService(
            equipmentStore, borrowerStore, reservationStore).CancelAsync(reservation.Id, CancellationToken.None));

        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
    }

    private sealed record FixedClock(DateTimeOffset UtcNow) : IClock;
}
```
