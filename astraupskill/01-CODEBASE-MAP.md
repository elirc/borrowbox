# 1. Codebase map

## Where the application starts

BorrowBox begins at `src/BorrowBox.Api/Program.cs`. The API registers singleton in-memory equipment, borrower, loan, and reservation stores. It registers `SystemClock` as `IClock`, scoped services for checkout, return, reservation, and expiry, and a hosted `ReservationExpiryWorker`. Authentication is a demo header handler with a Staff policy. The startup readiness response says persistence is in-memory and durable storage is false.

## Core: the invariants

The Core project owns invariants. `EquipmentItem` has states Available, CheckedOut, Reserved, Inspection, and Retired, with methods such as `Reserve`, `CheckOut`, `ReleaseReservation`, and `Return`. `Reservation` has Active, Collected, Cancelled, and Expired statuses. Its constructor requires nonempty identities and an expiry after reservation time. `Loan` requires nonempty identities and a due date on or after checkout date. `Loan.Return` changes `ReturnedAt` only while active.

## Reservation workflows

`ReservationService.ReserveAsync` loads equipment and borrower, checks borrower activity, constructs a reservation, reserves the equipment, saves it, and adds the reservation. `CancelAsync` loads an active reservation and its equipment, cancels, releases, and saves. `ReservationExpiryService` captures its clock, lists active due reservations, releases equipment, and counts expirations. The repair makes each state precondition visible before the first mutation.

## Checkout and return workflows

`CheckoutService.CheckOutAsync` loads equipment and borrower, checks the borrower loan limit, asks the equipment store for an atomic per-item checkout, then adds a loan. `InMemoryEquipmentStore.TryCheckOutAsync` locks the item and changes it only if Available. The repair validates a `Loan` with a captured clock before that call. `ReturnService` loads loan and equipment, validates the equipment state and return instant, then changes both.

## Infrastructure and where the tests live

Infrastructure stores are `ConcurrentDictionary` wrappers that retain entity references. `AddAsync` and `SaveAsync` store the same object, not a clone. That is useful for demonstrating side effects but does not provide multi-record durability. The SQLite adapter is available only for equipment; the teaching default does not select it. Tests live in Core, Infrastructure, and API projects. The focused lifecycle tests belong in `tests/BorrowBox.Infrastructure.Tests`, where all three in-memory stores can be composed.

The API endpoints bind `CreateReservationRequest`, `CheckOutRequest`, and route IDs, then let the exception handler translate `BorrowBoxException` to Problem Details. `LoanEndpoints` now asks `ReturnService` for its injected clock rather than calling `DateTimeOffset.UtcNow` at the edge. This keeps reservation expiry, checkout, and return time semantics consistent.
