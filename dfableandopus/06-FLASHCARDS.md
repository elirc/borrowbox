# Flashcards: BorrowBox

Thirty-six pairs. Cover the right column.

| Question | Answer |
|---|---|
| The five `EquipmentStatus` values, in declaration order | `Available, CheckedOut, Reserved, Inspection, Retired` (`src/BorrowBox.Core/Equipment/Equipment.cs:5`) |
| The four `ReservationStatus` values | `Active, Collected, Cancelled, Expired` (`src/BorrowBox.Core/Reservations/Reservation.cs:7`) |
| Which comparison rejects a new reservation | `expiresAt <= reservedAt`, code `reservation.expiry_invalid` (`src/BorrowBox.Core/Reservations/Reservation.cs:15`) |
| Which comparison selects a reservation for expiry | `item.ExpiresAt <= now`, inclusive, so equality is due (`src/BorrowBox.Core/Reservations/Reservation.cs:121`) |
| Why the two are opposite directions | Creation asks "is it still in the future", expiry asks "has the deadline been reached" |
| Which comparison rejects a loan due date | `dueOn < DateOnly.FromDateTime(checkedOutAt.UtcDateTime)`, code `loan.due_date_invalid` (`src/BorrowBox.Core/Loans/Loan.cs:13-14`) |
| So a 23:45 UTC checkout due the same calendar day is | Valid, because the comparison is date to date, not instant to instant |
| The two guards inside `Loan.Return` | `loan.already_returned` then `loan.return_time_invalid` (`src/BorrowBox.Core/Loans/Loan.cs:32-33`) |
| What makes a `Loan` active | `ReturnedAt is null` (`src/BorrowBox.Core/Loans/Loan.cs:28`) |
| The workflow precondition in `ReturnService` | `equipment.Status != EquipmentStatus.CheckedOut` throws `equipment.not_checked_out` (`src/BorrowBox.Core/Loans/Loan.cs:97-98`) |
| Why that guard is in the service, not in `Loan` | `Loan` holds only an `EquipmentId`; it cannot see the item |
| The only unguarded transition on `EquipmentItem` | `public void Return() => Status = EquipmentStatus.Available;` (`src/BorrowBox.Core/Equipment/Equipment.cs:67`) |
| The precondition added to `CancelAsync` | `equipment.Status != EquipmentStatus.Reserved` throws `equipment.not_reserved` (`src/BorrowBox.Core/Reservations/Reservation.cs:87-88`) |
| Line order in the repaired `ReserveAsync` | `new Reservation(...)` at :76, `equipment.Reserve()` at :77, `SaveAsync` :78, `AddAsync` :79 |
| Line order in the repaired `CheckOutAsync` | clock read :67, `new Loan(...)` :76, `TryCheckOutAsync` :77, `loanStore.AddAsync` :79 (`src/BorrowBox.Core/Loans/Loan.cs`) |
| The original defect, in one sentence | A fallible constructor ran after a live aggregate mutation, so a rejected request stranded the equipment |
| What the expiry loop does on a status mismatch | `continue`, changing nothing and not counting it (`src/BorrowBox.Core/Reservations/Reservation.cs:124`) |
| Why skip rather than expire the reservation alone | Expiring it would hide equipment never released and inflate the reported count |
| HTTP status for any `BorrowBoxException` | 422 Unprocessable Entity (`src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs:12`) |
| What the handler returns for a non-`BorrowBoxException` | `false`, so the framework produces a 500 (`src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs:11`) |
| The two Problem Details extensions | `code` and `traceId` (`src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs:21`) |
| Status code for `GET /api/v1/equipment?page=0` | 422, code `equipment.page_invalid` (`src/BorrowBox.Core/Equipment/Equipment.cs:112`) |
| Maximum allowed `pageSize` | 50 (`src/BorrowBox.Core/Equipment/Equipment.cs:112`) |
| Default `page` and `pageSize` at the endpoint | 1 and 20 (`src/BorrowBox.Api/Equipment/EquipmentEndpoints.cs:23`) |
| The two accepted `X-Demo-Role` values | `Student` and `Staff` (`src/BorrowBox.Api/Security/DemoHeaderAuthenticationHandler.cs:25`) |
| Missing header versus wrong role | `NoResult()` then 401; authenticated `Student` on a Staff route then 403 |
| Which equipment routes are public | The two GETs (`src/BorrowBox.Api/Equipment/EquipmentEndpoints.cs:21`, `:26`) |
| The policy name and its requirement | `StaffOnly`, `RequireRole("Staff")` (`src/BorrowBox.Api/Program.cs:31`, `src/BorrowBox.Api/Security/DemoHeaderAuthenticationHandler.cs:11`) |
| Borrower loan limit registered in DI | `new BorrowerLoanPolicy(maximumActiveLoans: 3)` (`src/BorrowBox.Api/Program.cs:35`) |
| Which race the item lock does not cover | The borrower loan-limit count, read outside any lock at `src/BorrowBox.Core/Loans/Loan.cs:73-74` |
| How `InMemoryEquipmentStore` picks one checkout winner | `lock (item)`, re-read `Status`, return `null` if not `Available` (`src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs:41-46`) |
| The SQLite equivalent of that lock | `Where(... Status == Available).ExecuteUpdateAsync(...)`, win when `changed == 1` (`src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs:40-45`) |
| Why mutating a `SqliteEquipmentStore` result writes nothing | `GetAsync` uses `AsNoTracking()` and maps to a fresh object (`src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs:17-19`) |
| The only unique index in the EF model | `AssetTag` (`src/BorrowBox.Infrastructure/Data/BorrowBoxDbContext.cs:14`), unenforced by the in-memory default |
| Why `IClock? clock = null` rather than required | Source compatibility with existing three-argument constructions; the cost is silent wall time if forgotten |
| Worker tick interval | One minute, `PeriodicTimer` (`src/BorrowBox.Api/Reservations/ReservationExpiryWorker.cs:11`) |
| What `/health/ready` reports | `persistence = "in-memory"`, `durable = false` (`src/BorrowBox.Api/Program.cs:68-69`) |
| Total test count and split | 29: 6 Core, 19 Infrastructure, 4 API |
| Result of restoring the pre-fix sources | `Failed: 5, Passed: 14, Total: 19` |
| Which assertion caught the bug in all five | The surviving-state assertion, not `Assert.ThrowsAsync` |
| The test pinning the reservation fix | `ReservationTests.InvalidExpiry_DoesNotReserveEquipment` (`tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs:35`) |
| The test pinning the checkout fix | `CheckoutTests.InvalidDueDate_DoesNotCheckOutEquipment` (`tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:129`) |
| The test pinning the return-ordering fix | `CheckoutTests.ReturnBeforeCheckout_DoesNotChangeEitherEntity` (`tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:149`) |
| How the competing-checkout test forces a race | `RacingEquipmentStore` blocks both `GetAsync` calls until both arrive (`tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:180-185`) |
| Which file still has the pre-fix ordering | `InspectionService.ReportDamageAsync`, mutate at :52 then construct at :53 (`src/BorrowBox.Core/Inspections/Inspection.cs`) |
| What `InspectionOutcome.Repair` leaves behind | Equipment still in `Inspection`; only `Passed` and `Retired` are branched (`src/BorrowBox.Core/Inspections/Inspection.cs:66-67`) |
| Two domain transitions with no caller anywhere | `Reservation.Collect()` and `EquipmentItem.CollectReservation()` |
| What validate-before-mutate does not give you | Rollback. There is no save or transaction contract on `IReservationStore` or `ILoanStore` |
