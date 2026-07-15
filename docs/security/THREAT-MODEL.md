# Lightweight threat model

## Assets

Equipment availability, borrower data, loan history, staff-only changes, and report exports.

## Trust boundaries

- Network callers cross into the HTTP API.
- The API crosses into configured stores and SQLite.
- A deployment crosses into an external identity provider when the demo handler is replaced.

## Important threats and controls

| Threat | Current control | Production follow-up |
| --- | --- | --- |
| Caller changes inventory | Staff authorization policy | Validate signed institutional identity tokens |
| Caller forges `X-Demo-Role` | Explicit documentation warning only | Remove demo handler before deployment |
| Double checkout | Atomic conditional checkout in each adapter | Add a database constraint/transaction around loan creation too |
| CSV formula injection | Not yet controlled | Prefix cells beginning with `=`, `+`, `-`, or `@` for spreadsheet consumers |
| Sensitive data in logs | No borrower details are intentionally logged | Add structured redaction rules and review telemetry |
| Dependency vulnerability | CI audits direct and transitive packages | Enable automated dependency updates and response ownership |

## Out of scope for this teaching release

Public internet deployment, payment data, multi-campus tenancy, and regulatory certification. These require deployment-specific analysis rather than speculative framework code.
