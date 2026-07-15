# BorrowBox curriculum specification

## Product

BorrowBox helps a fictional college catalog equipment, register borrowers, check items out and in, manage reservations, inspect damage, enforce simple limits, and report inventory status.

## Simple business rules

- Equipment has one current status and cannot be checked out twice.
- A borrower must be active and below their loan limit.
- A loan records checkout and due dates; a return records a UTC event.
- Reserved equipment can be collected only by the reservation owner before expiry.
- Damaged equipment requires inspection before becoming available.
- Public API models do not expose database entities directly.

## Fifteen sprints

| Sprint | Outcome | New skill |
| --- | --- | --- |
| 01 | Create and retrieve equipment | classes, records, DI, HTTP |
| 02 | Update and retire equipment | CRUD, validation, status codes |
| 03 | Search and paginate inventory | LINQ, query parameters |
| 04 | Register borrowers | relationships and DTOs |
| 05 | Check equipment out; `v0.5.0` | services and business rules |
| 06 | Return equipment | dates and transitions |
| 07 | Store data with SQLite | EF Core and migrations |
| 08 | Reserve equipment | competing states |
| 09 | Expire reservations | clocks and background services |
| 10 | Damage and inspections | workflow and history |
| 11 | Borrower limits | configuration and policy |
| 12 | Authentication/roles concept | authorization boundaries |
| 13 | Reports and CSV | projections and export |
| 14 | Three-bug harvest | regressions and concurrency |
| 15 | Operations and `v1.0.0` | health, logging, deployment judgment |

## Out of scope

Real student data, fines/payments, production identity, notifications, microservices, Kubernetes, and copying a proprietary campus system.
