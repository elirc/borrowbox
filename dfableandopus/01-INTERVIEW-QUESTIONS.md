# Interview questions: BorrowBox

Twenty-eight questions with model answers. Every technical answer names a file and line you can open.

## Screening

### Q1 (junior). Walk me through the data model.

Four aggregates, all plain C# classes in `src/BorrowBox.Core`. `EquipmentItem` (`src/BorrowBox.Core/Equipment/Equipment.cs:7`) owns `Status`, a five-value enum declared at `src/BorrowBox.Core/Equipment/Equipment.cs:5`: `Available, CheckedOut, Reserved, Inspection, Retired`. `Borrower` (`src/BorrowBox.Core/Borrowers/Borrower.cs:5`) holds a normalised `CollegeId` and an `IsActive` flag. `Reservation` (`src/BorrowBox.Core/Reservations/Reservation.cs:9`) has its own status enum at line 7: `Active, Collected, Cancelled, Expired`. `Loan` (`src/BorrowBox.Core/Loans/Loan.cs:7`) is active while `ReturnedAt is null` (`src/BorrowBox.Core/Loans/Loan.cs:28`). There are no navigation properties: `Reservation` and `Loan` hold `EquipmentId` and `BorrowerId` as `Guid`s, so every workflow does two store reads.

### Q2 (junior). Where does a request enter and where does it stop?

Minimal APIs. `src/BorrowBox.Api/Program.cs:72-77` maps six endpoint groups. A reservation POST binds `CreateReservationRequest` (`src/BorrowBox.Api/Reservations/ReservationEndpoints.cs:5`) and calls `ReservationService.ReserveAsync` at `src/BorrowBox.Api/Reservations/ReservationEndpoints.cs:14`, returning 201 with a Location header on success. The endpoint contains no validation at all: it does not check the GUIDs or the expiry.

### Q3 (junior). So what validates?

The domain constructors. `Reservation`'s constructor rejects empty identities with `reservation.identity_required` (`src/BorrowBox.Core/Reservations/Reservation.cs:13-14`) and rejects `expiresAt <= reservedAt` with `reservation.expiry_invalid` (line 15). `Loan`'s constructor rejects `dueOn < DateOnly.FromDateTime(checkedOutAt.UtcDateTime)` with `loan.due_date_invalid` (`src/BorrowBox.Core/Loans/Loan.cs:13-14`). The boundary is the aggregate, not the HTTP edge.

### Q4 (junior). What status code does a rule violation produce?

422. `BorrowBoxExceptionHandler` (`src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs:11-12`) checks `exception is not BorrowBoxException` and returns `false` if so, otherwise sets `StatusCodes.Status422UnprocessableEntity` and writes RFC 7807 Problem Details with the machine-readable `code` and the `traceId` in `Extensions` (line 21). `BorrowBoxException` carries that `Code` string (`src/BorrowBox.Core/Common/BorrowBoxException.cs:5`). Anything that is not a `BorrowBoxException` falls through to the framework's 500.

### Q5 (mid). How is authentication done, and would you ship it?

No, and the code says so. `DemoHeaderAuthenticationHandler` reads an `X-Demo-Role` header, accepts only the literals `Student` or `Staff`, and mints a `ClaimsPrincipal` from it (`src/BorrowBox.Api/Security/DemoHeaderAuthenticationHandler.cs:21-31`). Missing header returns `AuthenticateResult.NoResult()`, which becomes 401; a `Student` hitting a `StaffOnly` route becomes 403. Both are asserted in `AuthorizationTests.Retire_WithoutIdentity_ReturnsUnauthorized` and `Retire_StudentRole_ReturnsForbidden` (`tests/BorrowBox.Api.Tests/Security/AuthorizationTests.cs:14`, `:21`). It is a teaching stand-in for a bearer token, and there is no borrower-to-actor check anywhere: any `Student` header can return any loan ID.

### Q6 (junior). Which routes are public?

Reads. `GET /api/v1/equipment` and `GET /api/v1/equipment/{id}` carry no `RequireAuthorization` (`src/BorrowBox.Api/Equipment/EquipmentEndpoints.cs:21`, `:26`), while create, update and retire take `BorrowBoxSecurity.StaffPolicy` (lines 20, 35, 38). Health endpoints at `src/BorrowBox.Api/Program.cs:64-71` are public too.

### Q7 (mid). How is state stored?

By default in process. `src/BorrowBox.Api/Program.cs:32-38` registers `InMemoryEquipmentStore`, `InMemoryBorrowerStore`, `InMemoryLoanStore` and `InMemoryReservationStore` as singletons. Each wraps a `ConcurrentDictionary` and hands back the live instance, for example `InMemoryEquipmentStore.GetAsync` at `src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs:17-22`. `/health/ready` reports `durable = false` and names the reason (`src/BorrowBox.Api/Program.cs:65-71`), and `HealthTests.Ready_ExplainsNonDurableDefault_AndReturnsCorrelationId` asserts it.

## Deep dive

### Q8 (mid). Describe the bug you fixed.

`ReservationService.ReserveAsync` called `equipment.Reserve()` before `new Reservation(...)`. `Reserve()` at `src/BorrowBox.Core/Equipment/Equipment.cs:49-53` sets `Status = Reserved` immediately. If the caller passed an expiry already in the past, the `Reservation` constructor then threw at `src/BorrowBox.Core/Reservations/Reservation.cs:15`, the request returned 422, and the equipment stayed `Reserved` forever with no reservation row for the expiry worker to find. The repaired order is at `src/BorrowBox.Core/Reservations/Reservation.cs:76-79`: construct, `Reserve()`, `SaveAsync`, `AddAsync`.

### Q9 (mid). And the checkout half?

Same shape, different mechanism. `CheckoutService.CheckOutAsync` now captures `checkedOutAt` on its first line (`src/BorrowBox.Core/Loans/Loan.cs:67`), loads both entities, applies `BorrowerLoanPolicy.EnsureCanBorrow`, constructs the `Loan` at line 76 and only then calls `equipmentStore.TryCheckOutAsync` at line 77. Before the fix the store call came first, so `loan.due_date_invalid` left an item `CheckedOut` with no loan to return it. `CheckoutTests.InvalidDueDate_DoesNotCheckOutEquipment` pins it: a fixed clock at 2026-02-10 with `dueOn` 2026-02-09 must leave `EquipmentStatus.Available` and an empty loan list (`tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:144-145`).

### Q10 (mid). Why does asserting only the exception miss the bug?

Because the exception fires in both versions. When the pre-fix sources are restored, five of the nineteen infrastructure tests fail, and in every one the `Assert.ThrowsAsync<BorrowBoxException>` still passes. The failing assertion is the one after it, on `equipment.Status` or the store count. That is why `ReservationTests.InvalidExpiry_DoesNotReserveEquipment` asserts both `EquipmentStatus.Available` and `Assert.Empty(await reservationStore.ListAsync(...))` (`tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs:50-51`).

### Q11 (mid). What breaks if I delete line 97 of Loan.cs?

`src/BorrowBox.Core/Loans/Loan.cs:97-98` is the workflow precondition `equipment.Status != EquipmentStatus.CheckedOut` throwing `equipment.not_checked_out`. Delete it and `equipment.Return()` runs unconditionally, and `Return()` at `src/BorrowBox.Core/Equipment/Equipment.cs:67` is the one unguarded transition in the aggregate: `public void Return() => Status = EquipmentStatus.Available;`. So returning a loan whose item is currently `Inspection` or `Retired` would silently flip it to `Available`. The guard lives in the service because the `Loan` aggregate cannot see the `EquipmentItem`.

### Q12 (mid). Why is there also a check inside `Loan.Return`?

Two different concerns. `Loan.Return` (`src/BorrowBox.Core/Loans/Loan.cs:30-35`) rejects a second return with `loan.already_returned` and a return instant before checkout with `loan.return_time_invalid`. Those are invariants of the loan record itself and hold no matter who calls. The service-level check is about the other entity. `CheckoutTests.ReturnBeforeCheckout_DoesNotChangeEitherEntity` proves the pair: returning one minute before `checkedOutAt` leaves `loan.IsActive` true, `loan.ReturnedAt` null, and equipment still `CheckedOut` (`tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:162-164`).

### Q13 (mid). Time: why inject a clock instead of calling `DateTimeOffset.UtcNow`?

Because a boundary you cannot pin is a boundary you cannot test. `IClock` is two lines (`src/BorrowBox.Core/Common/Time.cs:3-6`), DI registers `SystemClock` as a singleton (`src/BorrowBox.Api/Program.cs:36`), and tests pass a one-line `FixedClock` record (`tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:198`). The return endpoint used to supply its own `UtcNow`; it now calls the two-argument overload and lets `ReturnService` capture its own clock (`src/BorrowBox.Api/Loans/LoanEndpoints.cs:20`, `src/BorrowBox.Core/Loans/Loan.cs:86-87`).

### Q14 (mid). Why is the clock parameter optional?

`IClock? clock = null` on `ReservationService` (`src/BorrowBox.Core/Reservations/Reservation.cs:66`), `CheckoutService` (`src/BorrowBox.Core/Loans/Loan.cs:63`) and `ReturnService` (line 84), with `(clock ?? new SystemClock()).UtcNow` at the call sites. It kept every existing three-argument test construction compiling, for example `new ReturnService(equipmentStore, loanStore)` at `tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:99`. The cost is honest: a caller who forgets the clock silently gets wall time instead of a compile error. In a service with real reviewers I would make it required and fix the call sites.

### Q15 (mid). Expiry uses `<=` but creation uses `<`. Deliberate?

Yes, and they are opposite questions. Creation asks "is this expiry in the future", so `expiresAt <= reservedAt` is rejected (`src/BorrowBox.Core/Reservations/Reservation.cs:15`); a reservation that is already due must never become `Active`. Expiry asks "is this deadline reached", so the filter is `item.ExpiresAt <= now` (`src/BorrowBox.Core/Reservations/Reservation.cs:120-121`); equality means due. Loan due dates are a third case: a `DateOnly` compared against the UTC checkout *date* (`src/BorrowBox.Core/Loans/Loan.cs:13`), so a 23:45 UTC checkout due the same calendar day is valid.

### Q16 (mid). The expiry worker skips some reservations. Why not expire them?

`src/BorrowBox.Core/Reservations/Reservation.cs:123-124` loads the item and `continue`s when it is null or not `Reserved`. Expiring the reservation alone would set it `Expired` and hide the fact that the equipment was never released, and it would inflate the returned count that the worker logs (`src/BorrowBox.Api/Reservations/ReservationExpiryWorker.cs:17`). Skipping leaves the inconsistency visible for repair. `ReservationExpiryTests.ExpireDue_MismatchedEquipment_DoesNotChangeReservation` asserts count 0, reservation still `Active`, equipment still `Available` (`tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationExpiryTests.cs:69-71`). The honest limit: nothing records the anomaly, so it is only observable by inspection.

### Q17 (mid). How does the code stop two people checking out the same camera?

`InMemoryEquipmentStore.TryCheckOutAsync` takes `lock (item)`, re-reads `item.Status` inside the lock and returns `null` if it is not `Available` (`src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs:41-46`). The service turns `null` into `equipment.not_available` (`src/BorrowBox.Core/Loans/Loan.cs:77-78`). `CheckoutTests.CheckOut_TwoCompetingRequests_OnlyOneSucceeds` runs two `CheckOutAsync` calls through a `RacingEquipmentStore` that blocks both reads until both have arrived, then asserts exactly one success (`tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:125`).

### Q18 (mid). What does that lock not protect?

The borrower loan limit. `policy.EnsureCanBorrow(activeLoans.Count)` at `src/BorrowBox.Core/Loans/Loan.cs:73-74` reads a count outside any lock, and the lock is per equipment item. Two simultaneous checkouts of *different* items by a borrower at 2 of 3 loans can both pass the check and both succeed. It is a documented race, not a fixed one. `ConcurrentDictionary` makes each dictionary operation safe; it does not make "loan row plus equipment status" one transaction.

### Q19 (mid). Is there rollback if `AddAsync` fails after the equipment is saved?

No. `IReservationStore` and `ILoanStore` expose only `AddAsync`, `GetAsync` and a list method (`src/BorrowBox.Core/Reservations/Reservation.cs:55-60`, `src/BorrowBox.Core/Loans/Loan.cs:38-43`). There is no `SaveAsync` and no transaction abstraction. Validate-before-mutate removes the damage from *rejected input*, which is the common case; it does not make a mid-sequence store failure atomic. I would not claim otherwise in a review.

### Q20 (mid). Is there a place in this repo that still has the old ordering?

Yes, and I would flag it. `InspectionService.ReportDamageAsync` calls `equipment.MarkForInspection()` at `src/BorrowBox.Core/Inspections/Inspection.cs:52` and constructs the `Inspection` at line 53, where a blank damage string throws `inspection.damage_required` (`src/BorrowBox.Core/Inspections/Inspection.cs:13`). That leaves the item in `Inspection` status with no inspection record: the exact bug class the reservation repair removed. It also uses `DateTimeOffset.UtcNow` directly rather than `IClock`, at lines 53 and 65.

### Q21 (mid). Anything else about `CompleteAsync`?

`src/BorrowBox.Core/Inspections/Inspection.cs:66-67` only branches on `Passed` and `Retired`. An outcome of `Repair` completes the inspection record but leaves the equipment in `Inspection` status with no open inspection to complete. The `InspectionTests.Complete_AppliesOutcomeToEquipment` theory (`tests/BorrowBox.Infrastructure.Tests/Inspections/InspectionTests.cs:14`) is parameterised over outcomes, so the current behaviour is pinned.

### Q22 (mid). Compare the in-memory store with the SQLite one.

`SqliteEquipmentStore.TryCheckOutAsync` replaces the lock with a conditional update: `.Where(row => row.Id == id && row.Status == nameof(EquipmentStatus.Available)).ExecuteUpdateAsync(...)` and then `changed == 1` decides the winner (`src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs:40-45`). Its `GetAsync` uses `AsNoTracking()` and maps a row to a fresh domain object (lines 17-19), so mutating the returned `EquipmentItem` no longer writes anything; you must call `SaveAsync`. `SqliteEquipmentStoreTests.Save_PersistsChangesAcrossContexts` verifies the write by reading through a second `BorrowBoxDbContext` (`tests/BorrowBox.Infrastructure.Tests/Equipment/SqliteEquipmentStoreTests.cs:28-33`). The adapter is not wired into `Program.cs`.

## Behavioural

### Q23 (junior). Tell me about a bug you found and fixed.

**Situation.** BorrowBox reservations and checkouts were leaving equipment in states no user could clear. **Task.** Find the cause and make it impossible to regress. **Action.** I traced both to ordering: a fallible constructor running after a live mutation, in `ReserveAsync` and `CheckOutAsync`. I moved the constructor first, added the missing workflow preconditions in `CancelAsync` (`src/BorrowBox.Core/Reservations/Reservation.cs:87-88`), `ReturnAsync` (`src/BorrowBox.Core/Loans/Loan.cs:97-98`) and the expiry loop, and injected a clock so the boundaries were testable. **Result.** 29 tests pass across three projects, 19 of them in the infrastructure suite. Restoring the pre-fix sources fails exactly 5 of those 19.

### Q24 (mid). Tell me about a time you disagreed with a proposed fix.

**Situation.** The obvious reading of "equipment left reserved" is a missing transaction. **Task.** Decide whether to add one. **Action.** I checked what the stores actually offer: no `SaveAsync` on the reservation or loan store, no transaction abstraction, and the default registration is four in-memory singletons (`src/BorrowBox.Api/Program.cs:32-38`). A transaction abstraction would have been a rewrite with new failure semantics, and it would not have fixed the real cause, which was a rejected request, not a partial commit. **Result.** I shipped the ordering fix plus state preconditions, and wrote the residual limits down rather than implying they were covered.

### Q25 (mid). Tell me about a time you made your own work harder on purpose.

**Situation.** I had a green suite and no proof it meant anything. **Task.** Prove the tests catch the original defect. **Action.** I restored the pre-fix `Reservation.cs` and `Loan.cs` byte snapshots. The build broke first, because the repair had also changed two constructor signatures, so I re-added only the signature parts and kept the old ordering. **Result.** 5 failed, 14 passed of 19, and I could name which surviving state each assertion caught. Reverted byte-for-byte; the final run was 19 passed again.

### Q26 (junior). How do you decide a change is done?

When a rerun from a clean state reproduces the number. The commands are recorded: `dotnet restore BorrowBox.slnx`, then `dotnet test BorrowBox.slnx --configuration Release --no-restore` with a TRX logger, then a Release build. I check the TRX counters rather than the exit code, because a test command run with missing SDK assets can exit zero without running anything.

## Follow-ups after your answer

### F1. You said the endpoint does no validation. Is that a problem?

It costs a round trip through the domain for input that could be rejected at the edge, and the 422 carries a domain code rather than a field name, so a client cannot map the error to a form field. The trade is that there is exactly one place a rule lives. If I added edge validation I would keep the domain check and treat the edge one as a fast path, never as the authority.

### F2. You moved the constructor first. Does that fix everything?

No. It fixes rejected input. It does nothing for a failure between `equipmentStore.SaveAsync` at `src/BorrowBox.Core/Reservations/Reservation.cs:78` and `reservationStore.AddAsync` at line 79. Ordering is not atomicity.

### F3. If you put this behind EF Core, does the ordering rule go away?

No. A rolled-back transaction leaves the tracked C# entity mutated, and the next `SaveChangesAsync` on a still-live context would write it. Ordering is what makes transactions unnecessary for the rejection path.

### F4. You said the loan limit races. How would you prove it?

The same way `CheckOut_TwoCompetingRequests_OnlyOneSucceeds` proves the item lock: a fake `ILoanStore` whose `ListActiveByBorrowerAsync` blocks until two callers have read, then asserting that both checkouts on different items succeed while the policy limit is 2. It would pass today, which is the point.

### F5. Which of the residual limits would you fix first?

`InspectionService.ReportDamageAsync` at `src/BorrowBox.Core/Inspections/Inspection.cs:52-53`. It is the same defect class, it is one line move, and the test costs five minutes.

### F6. What would you tell an on-call engineer about `/health/ready`?

That `durable: false` is the truth, not a placeholder: a restart loses every loan and reservation. Readiness returning 200 means the process can serve requests, not that data survived.
