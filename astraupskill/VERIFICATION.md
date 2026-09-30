# Verification and limits

Run from the BorrowBox project root with the .NET 10 SDK installed. The checked-in `global.json` selects SDK 10.0.302 with patch roll-forward. The project target is net10.0; the recorded tests used the installed .NET 10 runtime, so the explicit `DOTNET_ROLL_FORWARD=Major` in the command record did not require a newer target runtime.

```powershell
dotnet restore BorrowBox.slnx
dotnet test BorrowBox.slnx --configuration Release --no-restore --logger "trx;LogFileName=local-tests.trx"
dotnet build BorrowBox.slnx --configuration Release --no-restore
```

Restore must finish before testing. A test command issued with missing test SDK assets can return zero without running tests. Require an actual test summary and inspect the TRX counters. The authoritative [test output](evidence/luna-test-04.log) and [results summary](evidence/results.json) establish **29 passed tests**: six Core tests, nineteen Infrastructure tests and four API tests. Each suite's TRX file is included beside the output. The [final Release build](evidence/root-build-02.log) also passed. Earlier empty-output runs do not establish a passing test count.

The infrastructure cases exercise invalid expiry, invalid due date, early return, repeated return, mismatched cancellation/expiry state, positive lifecycle paths, and existing checkout competition. The competition test uses a coordinated fake equipment-store implementation; it verifies that the service respects a single winner from `TryCheckOutAsync`. It does not establish database serialization or borrower-limit atomicity across different equipment.

The four API tests use `WebApplicationFactory` and an in-process test server. They exercise equipment create/get, readiness metadata, and unauthorized/forbidden retirement. They do start the application test host, whose registered hosted services participate in its lifecycle, but do not establish periodic expiry-worker reliability. No external listener, live account or production service was used.

For a narrower repeat after restore, quote the filter so PowerShell does not interpret its pipe:

```powershell
dotnet test tests/BorrowBox.Infrastructure.Tests/BorrowBox.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Reservations|FullyQualifiedName~Loans"
```

The default stores hold mutable references and lose state on restart. Validation ordering prevents the expected rejected inputs from changing equipment first; it does not roll back a later `SaveAsync` or `AddAsync` failure. SQLite equipment persistence is not selected. Reservation and loan stores have no transaction/save contract. Real authentication, borrower-to-actor authorization, tenant scoping, multi-record durability and a shared coordinator remain follow-up work. The future versioned command in the exercises is a design proposal, not an implemented feature.
