# BorrowBox runbook

## Start and verify

1. Restore locked packages: `dotnet restore BorrowBox.slnx --locked-mode`.
2. Start: `dotnet run --project src/BorrowBox.Api`.
3. Check `/health/live` for process liveness.
4. Check `/health/ready` and confirm its persistence description matches the intended environment.
5. Send or capture `X-Correlation-ID` when tracing a request.

## Common symptoms

| Symptom | First checks | Safe response |
| --- | --- | --- |
| API will not start | SDK version, locked restore, port conflict | Capture logs; correct configuration; restart |
| Data disappears after restart | `/health/ready` says `durable: false` | Select and migrate a durable adapter before real use |
| Reservations remain active | worker logs, host uptime, clock configuration | Run the expiry use case once; investigate before retry automation |
| Checkout reports unavailable | equipment status and active reservation | Do not edit storage directly; resolve the owning workflow |
| 401 or 403 | identity header/token and required policy | Do not weaken policy; correct caller identity/role |

## Escalation information

Include UTC time, correlation ID, route, status code, release SHA, persistence mode, sanitized logs, and steps already attempted. Never paste real borrower identifiers into public issues.
