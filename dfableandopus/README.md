# BorrowBox interview pack

Job-readiness material built on the BorrowBox codebase: a .NET 10 campus equipment lending API (`src/BorrowBox.Api`, `src/BorrowBox.Core`, `src/BorrowBox.Infrastructure`) with 29 xUnit tests across three projects. The `astraupskill/` course next door teaches how the app works. This folder turns the same code into answers you can give out loud to a hiring panel.

## The pitch

BorrowBox lends laptops, cameras and lab kits. Four workflows, reservation, checkout, return and background expiry, each touch two objects: an `EquipmentItem` aggregate and a `Reservation` or `Loan` record. The defect I fixed was an ordering defect: `ReserveAsync` called `equipment.Reserve()` before constructing the `Reservation`, so a request with an expiry in the past threw `reservation.expiry_invalid` from the constructor and left the equipment stuck in `Reserved` with no reservation row to release it. Checkout had the mirror bug: `TryCheckOutAsync` ran before `new Loan(...)`, so a bad due date left an item `CheckedOut` with no loan. The repair is validate-before-mutate: read every reference, capture `IClock.UtcNow` once, construct the fallible candidate, check every state precondition, then mutate. Restoring the pre-fix sources reproduces 5 failures out of 19 in the infrastructure suite, and in all five the exception still fires; what the assertions catch is the surviving equipment status.

## Target stacks

C# / .NET 10 minimal APIs, ASP.NET Core dependency injection, EF Core (the SQLite adapter in `src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs`), xUnit and `WebApplicationFactory`. The concurrency and transaction answers transfer to any backend role.

## Two-week route

- Days 1 to 3: read `01-INTERVIEW-QUESTIONS.md` alongside the source, answer out loud, do not read the model answer first.
- Days 4 to 5: memorise `02-STORIES-AND-RESUME.md`. Say the 60-second summary until it fits in 60 seconds.
- Days 6 to 8: `03-CODE-READING-DRILL.md` on a timer, screen shared with nobody, talking the whole time.
- Days 9 to 11: `04-TAKE-HOME.md` under a real 4-hour clock, then grade yourself with its rubric.
- Days 12 to 13: `05-SYSTEM-DESIGN-FOLLOWUPS.md`, drawing the diagram from memory.
- Every day: ten minutes of `06-FLASHCARDS.md`.

## Files

- [01-INTERVIEW-QUESTIONS.md](01-INTERVIEW-QUESTIONS.md)
- [02-STORIES-AND-RESUME.md](02-STORIES-AND-RESUME.md)
- [03-CODE-READING-DRILL.md](03-CODE-READING-DRILL.md)
- [04-TAKE-HOME.md](04-TAKE-HOME.md)
- [05-SYSTEM-DESIGN-FOLLOWUPS.md](05-SYSTEM-DESIGN-FOLLOWUPS.md)
- [06-FLASHCARDS.md](06-FLASHCARDS.md)
