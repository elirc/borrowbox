# Astra upskill: BorrowBox lifecycle safety

This course follows one bounded repair in BorrowBox, a .NET 10 equipment lending demo. The selected improvement is validate-before-mutate in reservation, checkout, return, and expiry workflows. The application uses singleton in-memory stores by default; an SQLite equipment adapter exists but is not selected. That makes object references visible and tests fast, while leaving durable transaction questions explicit.

Read the chapters in order. Chapter 01 maps the Core, Infrastructure, and API layers. Chapter 02 explains domain state machines, captured time, and live reference stores. Chapter 03 works the invalid reservation, checkout, and return defects against the real source. Chapter 04 explains deterministic tests and failure diagnosis. Chapter 05 supplies nine exercises, and chapter 06 supplies matched solutions. Chapter 07 traces API and worker flows. `VERIFICATION.md` gives repeatable local commands and scope limits.

The actual repair constructs a `Reservation` using one injected `IClock.UtcNow` before `EquipmentItem.Reserve()`. Checkout constructs and validates `Loan` before `TryCheckOutAsync`. `Loan.Return` rejects a return instant before checkout, and `ReturnService` checks that the equipment is still checked out before changing either entity. Cancel and expiry inspect equipment state before changing reservation state. The old constructors remain source-compatible through optional clocks, while production DI supplies the existing `SystemClock` singleton.

The tests use a small fixed clock and the existing in-memory stores. Rejection tests compare equipment status and store contents before and after. They do not claim rollback after a store write fails, because stores persist live references and expose no transaction or Save contract for reservations and loans. The existing per-equipment checkout lock still protects competing checkouts; borrower-limit checks across different equipment remain a documented race.

Original changed files are preserved as exact byte snapshots under `snapshots/`. The guides refer to actual methods and test names, with proposed future concurrency work labeled as unimplemented.

## Follow the code with the course

Open the [map](01-CODEBASE-MAP.md), [concepts](02-CONCEPTS.md), [worked change](03-WORKED-CHANGE.md), [debugging guide](04-TESTING-AND-DEBUGGING.md), [practice](05-PRACTICE.md), [solutions](06-SOLUTIONS-AND-REVIEW.md), [trace lab](07-TRACE-LAB.md) and [verification](VERIFICATION.md). Compare the [loan workflow](../src/BorrowBox.Core/Loans/Loan.cs) with its [original snapshot](snapshots/Loan-before.cs.txt), then read the [actual checkout tests](../tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs). The final test run includes 29 tests across three projects.

## Before you start

**Prerequisites.** You should be able to read C# classes and `async`/`await`, run `dotnet test` from a terminal, and recognise an xUnit `[Fact]`. You do **not** need EF Core, SQL or ASP.NET experience: the workflows this course repairs are plain C# objects behind in-memory stores. If you have never written an xUnit test, read chapter 04 before chapter 03.

**Toolchain.** .NET SDK 10.0.302 (pinned by the checked-in `global.json`). Any OS the .NET 10 SDK supports works; the commands below are written for PowerShell, and the same commands run unchanged in `bash`/`zsh` on macOS and Linux (only the quoting of `--filter` differs — in bash use single quotes: `--filter 'FullyQualifiedName~ReservationTests'`).

## Reading route and time estimates

| Chapter | What you get | Estimate |
|---|---|---|
| [01 Codebase map](01-CODEBASE-MAP.md) | Where Core, Infrastructure and API live, and which four workflows the repair touched | 25–35 min |
| [02 Concepts](02-CONCEPTS.md) | Validate-before-mutate, captured time, live references, and the database equivalents | 40–60 min |
| [03 Worked change](03-WORKED-CHANGE.md) | The actual defect and the actual diff, workflow by workflow | 45–60 min |
| [04 Testing and debugging](04-TESTING-AND-DEBUGGING.md) | Deterministic clocks, state assertions, and how to read a failure | 40–50 min |
| [05 Practice](05-PRACTICE.md) | Exercise 0 (red-first) plus ten exercises | 3–5 hours |
| [06 Solutions and review](06-SOLUTIONS-AND-REVIEW.md) | Matched solutions — open only after you have written your own | 45–60 min |
| [07 Trace lab](07-TRACE-LAB.md) | Request and worker paths end to end | 30–40 min |
| [VERIFICATION](VERIFICATION.md) | The commands that produced the 29-test evidence, and the limits | 15 min |

Total: roughly one long day, or three evenings if you do the exercises properly.

## Glossary

- **Aggregate** — an object that owns its own rules and is changed only through its own methods. `EquipmentItem` is the aggregate here: nothing outside it sets `Status` directly; callers go through `Reserve()`, `CheckOut()`, `Return()`.
- **Candidate object** — a newly constructed domain object (`new Reservation(...)`, `new Loan(...)`) that may throw in its constructor because its inputs are invalid. It is a *candidate* until it has been accepted into a store.
- **Live reference** — an object handed back by an in-memory store that is the very same instance the store holds. Mutating it changes "storage" with no `SaveAsync` call, which is why these tests capture scalar state before an operation instead of comparing object references.
- **Domain precondition vs. workflow precondition** — a domain precondition is enforced inside the aggregate (`Loan.Return` refuses a return instant before checkout). A workflow precondition is enforced by the service that coordinates two objects (`ReturnService` refuses to act unless the equipment is still `CheckedOut`). Both are needed: the aggregate cannot see the equipment, and the service must not duplicate the aggregate's own rules.
- **Deterministic clock** — an `IClock` implementation that returns a fixed instant (`FixedClock`), so a boundary such as "expiry exactly equal to now" is testable instead of flaky.
- **Split-brain state** — two related records that disagree because one was changed and the other was not: equipment `Reserved` with no reservation row. Every defect in this course produces one.
- **Validate-before-mutate** — read everything, capture time once, construct the fallible candidate, check every precondition, *then* change live state.

## Continue with the advanced feature-ownership pack

Read the [six-chapter advanced route](advanced/README.md) after the original walkthrough, or begin with its [safe disposable failure lab](advanced/03-FAILURE-LAB.md) when you want a compiling red-first exercise. It adds source-grounded traces, failure injection, eight exercises, a feature capstone and a separate [solution rubric](advanced/06-SOLUTIONS-AND-RUBRIC.md). The lab never overwrites application source; historical snapshots are for comparison.
