# Why BorrowBox has tests

Tests were added to make business promises executable and to help a junior engineer change code safely. A test is not proof that software has no bugs; it is fast evidence that named behavior still works.

## The test layers

| Project | Purpose | Example |
| --- | --- | --- |
| `BorrowBox.Core.Tests` | Fast business-object and pure projection checks | invalid state, CSV escaping |
| `BorrowBox.Infrastructure.Tests` | Workflows across real store implementations | checkout, reservation, in-memory SQLite |
| `BorrowBox.Api.Tests` | HTTP wiring and security boundaries | status codes, 401 vs 403, health headers |

Use the cheapest layer that can prove the behavior. Do not write an HTTP test for a rule a plain object test can cover.

## A good test tells a story

1. **Arrange** the smallest meaningful starting state.
2. **Act** once through the public behavior.
3. **Assert** the important outcome and, when useful, an unchanged side effect.

Name the test `Behavior_Condition_ExpectedResult`. Prefer observable outcomes over private-method calls. One test may contain several assertions when they describe one outcome.

## Practices to build

- Test boundaries: empty values, exactly-at-limit counts, and state transitions.
- Keep time, randomness, and external systems controllable.
- Use a real SQLite engine for persistence behavior; a mock cannot prove SQL works.
- Make concurrency tests deterministic with synchronization, not repeated sleeps.
- When fixing a bug, commit the failing counterexample before the fix when practical.
- Treat flaky tests as defects. Find the uncontrolled dependency instead of adding retries.

## What not to optimize for

Coverage percentage is a clue, not the goal. Avoid tests that only repeat implementation lines, mock every collaborator, or break whenever code is renamed. Optimize for confidence in decisions that matter to a borrower, staff member, integrator, or operator.

Run everything with `dotnet test BorrowBox.slnx`. During development, run the nearest test project first, then the whole solution before a pull request.
