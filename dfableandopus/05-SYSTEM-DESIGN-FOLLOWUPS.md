# Now scale it: BorrowBox design follow-ups

Seven questions an interviewer asks once you have explained the lending workflows. Each answer starts from what the code does today.

## Today's shape

```text
  HTTP                     Program.cs                        Core                      Infrastructure
  ----                     ----------                        ----                      --------------
  POST /api/v1/loans  -->  MapLoanEndpoints  -->  CheckoutService.CheckOutAsync  --+-> IEquipmentStore ---+
  POST /reservations  -->  MapReservation... -->  ReservationService.ReserveAsync +-> IBorrowerStore     |  ConcurrentDictionary
  POST /{id}/return   -->  MapLoanEndpoints  -->  ReturnService.ReturnAsync      +-> ILoanStore         |  singletons, one process
                                                                                  +-> IReservationStore -+
        |                                              ^                                  ^
        |  BorrowBoxException                          |                                  |
        +--> BorrowBoxExceptionHandler --> 422         |  IClock (SystemClock singleton)  |
                                                       |                                  |
  BackgroundService: ReservationExpiryWorker --1 min-->+  ExpireDueAsync -----------------+

  Not registered:  SqliteEquipmentStore --> BorrowBoxDbContext --> Equipment table (unique index on AssetTag)
```

## 1. Make it multi-tenant, two campuses that must not see each other's kit

Today there is no tenant anywhere. `EquipmentItem` has `Id`, `AssetTag`, `Name`, `Category`, `Status` and nothing else (`src/BorrowBox.Core/Equipment/Equipment.cs:22-26`), and `EquipmentService.SearchAsync` filters only on query text and category (`src/BorrowBox.Core/Equipment/Equipment.cs:114-116`). Identity carries a role and nothing else: `DemoHeaderAuthenticationHandler` issues exactly two claims, name and role (`src/BorrowBox.Api/Security/DemoHeaderAuthenticationHandler.cs:28`).

I would add `CampusId` to `EquipmentItem` and `Borrower`, add a campus claim, and resolve it once per request into a scoped `ITenantContext` rather than threading a parameter through every method. The filter belongs in the store, not the service, so a forgotten `Where` cannot leak: `IEquipmentStore.ListAsync` becomes tenant-scoped by construction. The trade-off is that the stores stop being dumb dictionaries and start holding policy, which makes them harder to fake in tests. Under EF Core I would use a global query filter plus a composite unique index on `(CampusId, AssetTag)` instead of the current single-column index at `src/BorrowBox.Infrastructure/Data/BorrowBoxDbContext.cs:14`, so the database enforces it even if a service forgets.

## 2. Two staff members check out the same camera at the same millisecond

Handled for one item, not for the borrower. `InMemoryEquipmentStore.TryCheckOutAsync` re-reads the status inside `lock (item)` and returns null to the loser (`src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs:41-46`), which becomes `equipment.not_available` and a 422. `CheckoutTests.CheckOut_TwoCompetingRequests_OnlyOneSucceeds` proves exactly one winner through a fake store that blocks both reads until both have arrived (`tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs:121-125`).

What is not handled: the borrower limit. `policy.EnsureCanBorrow(activeLoans.Count)` reads a count outside any lock (`src/BorrowBox.Core/Loans/Loan.cs:73-74`), and the lock is per item, so two checkouts of *different* cameras by a borrower at 2 of 3 loans both pass. The in-process fix is a keyed semaphore on borrower ID, which costs a second lock and a lock-ordering rule against the item lock. The durable fix is a database constraint or a serializable transaction over the loan count. The honest ranking: the item race is the one users notice, the borrower race overspends a soft policy, so I would ship the item fix first and document the other, which is what this repository did.

## 3. Move to a real database. What changes and what does not?

The adapter already exists for one aggregate. `SqliteEquipmentStore.TryCheckOutAsync` swaps the lock for a conditional update, `Where(row => row.Id == id && row.Status == nameof(EquipmentStatus.Available))` then `ExecuteUpdateAsync`, treating `changed == 1` as the win (`src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs:40-45`). That is the same "one winner" semantic expressed as a row count.

Two things change sharply. First, reads stop being writes: `GetAsync` uses `AsNoTracking()` and maps to a fresh object (lines 17-19), so every test that today mutates a store-returned `EquipmentItem` and asserts on the same reference must instead read back through a second context, which is what `SqliteEquipmentStoreTests.Save_PersistsChangesAcrossContexts` does (`tests/BorrowBox.Infrastructure.Tests/Equipment/SqliteEquipmentStoreTests.cs:28-33`). Second, the two independent store calls at `src/BorrowBox.Core/Reservations/Reservation.cs:78-79` become one `SaveChangesAsync` inside a transaction, which removes the half-written case entirely.

What does not change: ordering. A rolled-back transaction leaves the tracked entity mutated in memory, and a still-live `DbContext` will write it on the next save. And a domain rule such as `Loan.Return`'s "return time cannot precede checkout" (`src/BorrowBox.Core/Loans/Loan.cs:33`) has no database equivalent; isolation levels protect against concurrent damage, never against a well-formed request that should have been refused. There is also a new failure mode: `BorrowBoxExceptionHandler` only maps `BorrowBoxException` (`src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs:11`), so a `DbUpdateException` from the `AssetTag` unique index would surface as an unhandled 500 until I map it to a 409.

## 4. Paginate a million pieces of equipment

Today's search loads everything and pages in memory: `store.ListAsync(...)` then `Where`, `OrderBy(item => item.AssetTag)`, `.ToArray()`, then `Skip((page - 1) * pageSize).Take(pageSize)` (`src/BorrowBox.Core/Equipment/Equipment.cs:113-119`). The page-size cap of 50 at line 112 bounds the response, not the work. At a million rows that is a full materialisation per request, and the SQLite adapter's `ListAsync` has the same shape (`src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs:24`).

Step one is pushing the predicate, sort and paging into the store so they become SQL. Step two, once page numbers get large, is keyset pagination: `WHERE AssetTag > @lastSeen ORDER BY AssetTag LIMIT @size`, which is stable under concurrent inserts and costs an index seek rather than an offset scan. The price is a contract change. `EquipmentPage` currently returns `Page, PageSize, Total` (`src/BorrowBox.Core/Equipment/Equipment.cs:97`), and keyset paging gives up jumping to page 500 and makes `Total` an extra count query most callers do not need. For a staff console that scrolls, that is the right trade; for one that renders numbered pages, it is not.

## 5. The expiry worker at scale

`ReservationExpiryWorker` ticks a `PeriodicTimer` every minute, creates a DI scope and calls `ExpireDueAsync` (`src/BorrowBox.Api/Reservations/ReservationExpiryWorker.cs:11-17`). `ExpireDueAsync` lists *every* reservation and filters in memory (`src/BorrowBox.Core/Reservations/Reservation.cs:117-121`). Three problems appear as soon as there is more than one process: every instance runs the sweep, the sweep is O(all reservations), and a throw inside `ExecuteAsync` silently ends the loop for the lifetime of the process.

Fixes in order of value: query for due rows instead of listing all; take a lease so one instance sweeps, via a row lock or a distributed lock; wrap the body in try/catch so one bad reservation does not kill the worker. The equality boundary must survive all of it, `ExpiresAt <= now` with a single captured `now` (line 116), because a sweep that reads the clock per row has no consistent cutoff.

Note the deliberate policy that should not be "optimised" away: the loop skips a reservation whose equipment is missing or not `Reserved` (line 124) instead of expiring it. Skipping keeps an inconsistency visible and keeps the reported count honest. At scale I would add what is missing today, an emitted anomaly event, because nothing currently records the skip.

## 6. Real authentication and per-borrower authorization

The header handler is a stand-in and the code says so in the README and in `/health/ready`. Swapping `DemoHeaderAuthenticationHandler` for JWT bearer changes one registration (`src/BorrowBox.Api/Program.cs:28-29`); the `StaffOnly` policy at line 31 and every `RequireAuthorization` call site stay as they are, which is the payoff of having used policies rather than inline role checks.

The larger gap is that authorization is role-only. Nothing links the authenticated principal to a `Borrower`: `POST /api/v1/loans/{id}/return` takes only a loan ID (`src/BorrowBox.Api/Loans/LoanEndpoints.cs:18`), so any `Student` header can return any loan. That needs a borrower ID claim and a resource check comparing it to `loan.BorrowerId`, with staff allowed to override. That check cannot live in a policy alone because it needs the loaded loan, so it belongs in the service, next to the existing preconditions.

## 7. Observability

There is one deliberate piece today: correlation. Middleware reads `X-Correlation-ID`, falls back to `TraceIdentifier`, rejects values over 100 characters, echoes it on the response and opens a logging scope around the rest of the pipeline (`src/BorrowBox.Api/Program.cs:52-60`), and `HealthTests.Ready_ExplainsNonDurableDefault_AndReturnsCorrelationId` asserts the echo. The same trace ID goes into every 422 body as `traceId` (`src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs:21`), so a user-reported error maps to a log line. Beyond that there is one `LogInformation` in the whole app, the expiry count at `src/BorrowBox.Api/Reservations/ReservationExpiryWorker.cs:17`.

What I would add, in order: a counter keyed on `BorrowBoxException.Code`, because `reservation.expiry_invalid` spiking means a broken client while `equipment.not_available` spiking means contention, and today both are indistinguishable 422s; a gauge of equipment by status, which the existing `InventoryReportService.SummarizeAsync` already computes (`src/BorrowBox.Core/Reporting/InventoryReportService.cs:16-19`), so a stranded-item leak would show as a rising `Reserved` count; and worker liveness, since a silently dead `ExecuteAsync` loop is invisible while `/health/ready` keeps returning 200. That last one is the real operational lesson here: readiness reports that the process can serve requests, not that the background work is happening or that the data survives a restart, which is why `durable` is hard-coded `false` at `src/BorrowBox.Api/Program.cs:69`.
