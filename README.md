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

Start with the [learner guide](LEARNER-GUIDE.md) and [course specification](docs/curriculum/SPEC.md).
