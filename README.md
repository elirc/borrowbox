# BorrowBox

BorrowBox is a beginner-friendly fifteen-sprint C#/.NET teaching repository for lending campus equipment such as laptops, cameras, calculators, and lab kits.

The repository is designed for a new graduate with limited .NET experience. It starts with plain C# and one HTTP slice, then gradually introduces validation, LINQ, dependency injection, SQLite, background work, authorization concepts, testing, and operations.

## Deliberately small architecture

```text
BorrowBox.Api             HTTP routes and request/response models
BorrowBox.Core            equipment, borrowers, loans, and simple services
BorrowBox.Infrastructure  in-memory and SQLite storage
```

BorrowBox does not use microservices, MediatR, CQRS, generic repositories, AutoMapper, or a message broker.

```powershell
dotnet restore BorrowBox.slnx --locked-mode
dotnet run --project src/BorrowBox.Api
dotnet test BorrowBox.slnx
```

## Run it

Requires the .NET 10 SDK.

```powershell
dotnet restore BorrowBox.slnx --locked-mode
dotnet run --project src/BorrowBox.Api
```

OpenAPI is available at `/openapi/v1.json` in Development. Inventory reads and health endpoints are public. Mutating inventory uses the teaching header `X-Demo-Role: Staff`; borrowing accepts `Student` or `Staff`. This header is not production authentication.

## Course map

| Sprints | Theme | Result |
| --- | --- | --- |
| 1–3 | C# and HTTP foundations | equipment CRUD, validation, search, pagination |
| 4–6 | Business workflows | borrowers, checkout, return, v0.5 release |
| 7–10 | Data and state | SQLite adapter, reservations, expiry, inspections |
| 11–13 | Policies and API design | lending limits, authorization, reports |
| 14–15 | Engineering judgment | bug harvest, threat model, operations, v1.0 |

Each sprint has a focused lesson in [`docs/sprints`](docs/sprints). Start with the [learner guide](LEARNER-GUIDE.md) and [course specification](docs/curriculum/SPEC.md). Then use [TESTREADME.md](TESTREADME.md) to practice testing and [OEReadMe.md](OEReadMe.md) to discuss which complexity is justified.

## Useful commands

```powershell
dotnet test BorrowBox.slnx
dotnet format BorrowBox.slnx --verify-no-changes
dotnet list BorrowBox.slnx package --vulnerable --include-transitive
```

The default API store is intentionally non-durable. The SQLite adapter and integration test demonstrate persistence without making local setup harder.


## Lifecycle learning course

Read the [nine-chapter CRUD engineering course](astraupskill/README.md) for validation ordering, time boundaries, complete tests, exercises and verified limits.
