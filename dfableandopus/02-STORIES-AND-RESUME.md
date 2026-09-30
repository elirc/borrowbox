# Stories and resume lines: BorrowBox

## Story 1: the reservation that could not be cancelled

**Situation.** BorrowBox is a campus equipment lending API. Cameras and laptops were sitting in `Reserved` status with nothing on the reservations list to cancel, so the items were unlendable until the process restarted.

**Task.** Find the cause, fix it, and leave a test that fails if anyone reintroduces it.

**Action.** I traced the reservation path. `ReservationService.ReserveAsync` called `equipment.Reserve()` before constructing the `Reservation`. `Reserve()` at `src/BorrowBox.Core/Equipment/Equipment.cs:49-53` sets `Status = Reserved`, with no way back except `ReleaseReservation()`, which the expiry worker only reaches through a reservation row. An expiry already past then threw `reservation.expiry_invalid` from the constructor at `src/BorrowBox.Core/Reservations/Reservation.cs:15`, the caller got a 422, and the item was stranded. I inverted the order so the constructor runs first (lines 76-77) and passed one `IClock.UtcNow` read into it instead of a bare `DateTimeOffset.UtcNow`.

**Result.** `ReservationTests.InvalidExpiry_DoesNotReserveEquipment` asserts the exception *and* `EquipmentStatus.Available` *and* an empty reservation store (`tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs:50-51`). The suite is 29 passing tests: 6 Core, 19 Infrastructure, 4 API.

## Story 2: proving the tests were worth having

**Situation.** After the repair the suite was green, which proves nothing about whether it would have caught the defect.

**Task.** Reproduce the pre-fix behaviour and record what fails.

**Action.** I restored the byte-exact pre-change copies of `Reservation.cs` and `Loan.cs` from `astraupskill/snapshots/`. The build broke immediately, because the repair had also added the optional `IClock` parameters and the `ReturnAsync(Guid, CancellationToken)` overload at `src/BorrowBox.Core/Loans/Loan.cs:86-87`. That was itself informative: a behavioural fix that changes a public signature is a source-compatibility decision. I re-applied only the signature parts, left the old ordering and missing guards, then ran the infrastructure project.

**Result.** `Failed: 5, Passed: 14, Skipped: 0, Total: 19`: `CheckoutTests.InvalidDueDate_DoesNotCheckOutEquipment`, `CheckoutTests.ReturnBeforeCheckout_DoesNotChangeEitherEntity`, `ReservationTests.InvalidExpiry_DoesNotReserveEquipment`, `ReservationTests.Cancel_MismatchedEquipment_DoesNotChangeReservation` and `ReservationExpiryTests.ExpireDue_MismatchedEquipment_DoesNotChangeReservation`. In all five the `Assert.ThrowsAsync<BorrowBoxException>` still passed; what failed was the surviving-state assertion after it. Sources restored byte-for-byte, back to 19 passed.

## Story 3: saying no to the transaction

**Situation.** The obvious prescription for "two records disagree" is a database transaction, and it was the first thing suggested.

**Task.** Decide whether adding one was right for this codebase.

**Action.** I read what the storage layer offers. `IReservationStore` and `ILoanStore` expose only `AddAsync`, `GetAsync` and a list method (`src/BorrowBox.Core/Reservations/Reservation.cs:55-60`); no save or transaction contract, and the API registers four in-memory singletons (`src/BorrowBox.Api/Program.cs:32-38`). A transaction abstraction meant a storage rewrite, and it would not have addressed the cause: a *rejected* request, not a partial commit. I shipped the ordering fix plus three missing preconditions: `equipment.Status != Reserved` in `CancelAsync` (`src/BorrowBox.Core/Reservations/Reservation.cs:87-88`), `equipment.Status != CheckedOut` in `ReturnAsync` (`src/BorrowBox.Core/Loans/Loan.cs:97-98`), and the status-mismatch skip in the expiry loop (line 124).

**Result.** Four workflows covered by 19 infrastructure tests, plus a limits section stating what is not: no rollback after a store write fails, and a loan limit that still races across different equipment because `TryCheckOutAsync` locks one item.

## Resume bullets

- Fixed a state-leak defect in a .NET 10 lending API where a rejected reservation left equipment permanently `Reserved`, by moving fallible domain construction ahead of aggregate mutation across four workflows (reserve, checkout, return, background expiry).
- Added 8 regression tests asserting surviving entity state rather than only the thrown exception, growing the xUnit suite to 29 passing tests across Core, Infrastructure and API projects.
- Verified the repair by restoring pre-change sources and recording an exact 5-failed / 14-passed baseline, then confirming a byte-for-byte revert returned the suite to 19 passed.
- Introduced an injected `IClock` through three services and the loan return endpoint, replacing uncoordinated `DateTimeOffset.UtcNow` calls so expiry, checkout and return ran under one deterministic clock in tests.
- Documented the remaining concurrency limits, including the borrower loan-limit race and the absent rollback contract on the reservation and loan stores, instead of implying transactional coverage.

## 60-second spoken summary

BorrowBox is a .NET 10 minimal-API service for lending campus equipment. Four workflows each touch two objects: an equipment item and either a reservation or a loan. I fixed an ordering defect running through all four. `ReserveAsync` set the item to `Reserved` before constructing the `Reservation`, so an expiry in the past threw from the constructor, returned a 422, and left the item stranded with nothing to cancel. Checkout had the same shape with a bad due date. The fix is validate-before-mutate: read every reference, capture the clock once, build the fallible object, check every precondition, then change live state. I added the missing cross-entity checks too: cancel refuses unless the item is `Reserved`, return unless it is `CheckedOut`. Nineteen infrastructure tests cover it, and I proved they matter by putting the old code back and watching exactly five fail, all on the state assertion rather than the exception. What I did not do is add transactions: the stores have no save contract, so I fixed the rejected-input case and wrote down what is still uncovered.

## What I would do next

1. Fix `InspectionService.ReportDamageAsync`: it calls `equipment.MarkForInspection()` at `src/BorrowBox.Core/Inspections/Inspection.cs:52` before constructing the `Inspection` at line 53, so blank damage text strands an item in `Inspection` with no record. Same defect class, one line move.
2. Handle `InspectionOutcome.Repair`, which completes the record but leaves equipment in `Inspection`: lines 66-67 branch only on `Passed` and `Retired`.
3. Port the repair onto `SqliteEquipmentStore`, where `GetAsync` is `AsNoTracking()` so mutation writes nothing (`src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs:17-19`).
4. Make `IClock` required rather than `IClock? clock = null`, so a forgotten clock is a compile error.
