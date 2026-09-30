# Take-home: collect a reservation

Time budget: 3 to 4 hours. Work in the BorrowBox repository as it stands. Do not add packages, do not introduce a database, do not refactor the stores.

## Background

`Reservation.Collect()` (`src/BorrowBox.Core/Reservations/Reservation.cs:30-34`) and `EquipmentItem.CollectReservation()` (`src/BorrowBox.Core/Equipment/Equipment.cs:55-59`) both exist and neither is called anywhere in `src/` or `tests/`. The lifecycle they were written for is missing: a borrower reserves an item, walks to the desk, and converts the reservation into a loan. Today the only route from `Reserved` back to anything is `CancelAsync` or the expiry worker.

## What to build

A collect workflow, end to end.

Add `ReservationService.CollectAsync(Guid reservationId, DateOnly dueOn, CancellationToken cancellationToken)` returning the created `Loan`, and expose it as `POST /api/v1/reservations/{id:guid}/collect` alongside the existing routes in `src/BorrowBox.Api/Reservations/ReservationEndpoints.cs`.

Behaviour:

1. Load the reservation. Not found is `reservation.not_found`; not `Active` is `reservation.not_active`. `GetActiveAsync` at `src/BorrowBox.Core/Reservations/Reservation.cs:95-102` already does both.
2. Load the equipment. Missing is `equipment.not_found`. If its status is not `Reserved`, throw `equipment.not_reserved`, matching `CancelAsync` at line 87-88.
3. Load the borrower named on the reservation. Missing is `borrower.not_found`; inactive is `borrower.inactive`.
4. Enforce the borrower loan limit through the existing `BorrowerLoanPolicy` (`src/BorrowBox.Core/Loans/Loan.cs:45-56`), which throws `borrower.loan_limit`.
5. Refuse a reservation that is already past its deadline: if `reservation.ExpiresAt <= clock.UtcNow`, throw a new code `reservation.expired`. Use the same inclusive comparison the expiry loop uses at `src/BorrowBox.Core/Reservations/Reservation.cs:121`, and say in your notes why you matched it.
6. Construct the `Loan` with the captured clock instant and the requested `dueOn`, so an invalid due date throws `loan.due_date_invalid` from `src/BorrowBox.Core/Loans/Loan.cs:13-14`.
7. Only then mutate: `reservation.Collect()`, `equipment.CollectReservation()`, `equipmentStore.SaveAsync`, `loanStore.AddAsync`.

The endpoint returns 201 with a `Location` of `/api/v1/loans/{loan.Id}` and a `LoanResponse` body (`src/BorrowBox.Api/Loans/LoanEndpoints.cs:6`). It requires authorization; decide whether a bare `RequireAuthorization()` like the sibling reservation routes or the `StaffPolicy` used by inventory writes is correct, and defend the choice in your notes.

`ReservationService` does not currently take an `ILoanStore` or a `BorrowerLoanPolicy`. Extend its primary constructor (`src/BorrowBox.Core/Reservations/Reservation.cs:62-66`) and update the DI registration if needed (`src/BorrowBox.Api/Program.cs:44`). Keep every existing call site compiling, including `new ReservationService(equipmentStore, borrowerStore, reservationStore)` at `tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs:69-70`.

## Acceptance criteria

- Every rejection path above leaves **both** entities unchanged and adds nothing to the loan store. Assert equipment status, reservation status and the loan count, not only the exception code.
- The happy path leaves the reservation `Collected`, the equipment `CheckedOut`, and one active loan for that borrower.
- All existing tests still pass: 29 across the three projects, 19 of them in `BorrowBox.Infrastructure.Tests`.
- No `DateTimeOffset.UtcNow` appears in your new service code. Use the injected `IClock` (`src/BorrowBox.Core/Common/Time.cs:3-6`).
- No new NuGet references.

## What to submit

- A single branch with your commits, plus a `NOTES.md` of at most one page covering: the ordering you chose and why; your authorization decision; the boundary decision on `reservation.expired`; and what you did **not** do.
- New tests in `tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs`, using the `FixedClock` record already declared at line 76 of that file.
- The output of your final run.

Run the focused suite with:

```powershell
dotnet test tests/BorrowBox.Infrastructure.Tests/BorrowBox.Infrastructure.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReservationTests"
```

and the whole solution with `dotnet test BorrowBox.slnx --configuration Release`. In bash, single-quote the `--filter` value.

## Grading rubric

| Criterion | Strong | Weak |
|---|---|---|
| Ordering | Every precondition, including the loan-limit read and the `new Loan(...)` construction, runs before the first call to `Collect()` or `CollectReservation()`. A blank line or comment marks the boundary | Mutates the reservation, then discovers the due date is invalid and leaves the item `Reserved` with a `Collected` reservation |
| Test assertions | Rejection tests assert surviving `equipment.Status`, `reservation.Status` and an empty `ListActiveByBorrowerAsync` result, in the style of `tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:144-145` | Only `Assert.ThrowsAsync<BorrowBoxException>`, which passes against the buggy ordering too |
| Boundary handling | A test for `ExpiresAt` exactly equal to the clock instant, with the chosen direction stated | "Near now" values, or no expiry test at all |
| Error contract | New code `reservation.expired` reaches the client as a 422 with `code` in the Problem Details extensions, via the existing handler (`src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs:12-21`); no new exception type | A raw `InvalidOperationException` or a hand-written `Results.BadRequest`, bypassing the handler and changing the status code family |
| Compatibility | Optional or reordered constructor parameters keep the three-argument `ReservationService` construction in the existing tests compiling | Breaks `tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs:69` and "fixes" it by editing that test |
| Honesty in NOTES.md | States plainly that there is still no rollback if `loanStore.AddAsync` fails after `SaveAsync`, and that the loan-limit read is not atomic | Claims the workflow is transactional or race-free |
| Scope | Touches `Reservation.cs`, `ReservationEndpoints.cs`, `Program.cs` if required, and the reservation test file | Rewrites the stores, adds a repository abstraction, or swaps in EF Core |

## Reviewer's notes

The first thing read is the diff of `ReservationService`, looking for one thing: where the first mutation sits relative to the last throw. Everything else is secondary.

Second is the new tests. A submission with three rejection tests that assert only the exception code scores lower than one with two tests that assert surviving state, because this repository's whole thesis is that the exception fires either way; restoring the pre-fix sources fails 5 of 19 tests and every one of them fails on the state assertion, not the throw.

Third is `NOTES.md`. A candidate who writes "there is still no rollback between `SaveAsync` and `AddAsync`, and the loan-limit read at `src/BorrowBox.Core/Loans/Loan.cs:73-74` is not atomic across different equipment" is telling the truth about this codebase, and that is the answer wanted. A candidate who says the collect workflow is atomic has not read the store interfaces.

Fourth is whether the existing suite still passes untouched. Editing a shipped test to make new code compile is the single most common way this exercise is failed.
