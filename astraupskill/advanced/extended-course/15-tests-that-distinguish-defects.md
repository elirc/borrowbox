# 15. Tests that distinguish defects

A test is useful when a plausible incorrect implementation produces a different observation from the intended implementation. This sounds obvious, yet many tests exercise a method without checking the property that makes it important. BorrowBox provides concrete opportunities to improve discrimination: invalid input can produce the expected exception after a mutation, a persistence call can be missing while aliases still show changes, and a concurrency test can accidentally serialize its own requests.

Read [checkout tests](../../../tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs), [reservation tests](../../../tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs), [equipment tests](../../../tests/BorrowBox.Core.Tests/Equipment/EquipmentTests.cs), and [the lab recipe](../lab/recipe.json). This chapter develops a test review method and a small manual mutation exercise. All deliberate defects belong in newly generated disposable source, never in the original application.

## Begin with the competing implementation

For every important assertion, describe a defect it would catch. The assertion that an invalid expiry throws catches an implementation that accepts the expiry. The assertion that equipment remains Available catches an implementation that mutates before throwing. The assertion that the reservation store remains empty catches an implementation that records an invalid candidate. Together these observations establish more than any one alone.

Now consider an assertion that a returned object's Name equals the requested name. A defect removing SaveAsync from EquipmentService.UpdateAsync may still pass with the in-memory adapter. The assertion is not useless; it verifies mapping and mutation. It is simply not sufficient evidence for persistence. Add a complementary real-adapter round trip when the requirement concerns stored state, rather than pretending one assertion proves both concerns.

This technique is sometimes called thinking in counterexamples. You do not need a mutation-testing tool or a package installation to use it. Write the wrong implementation in one sentence, predict which assertion rejects it, and identify any requirement for which no assertion would notice the defect. Those gaps provide a focused test backlog.

## Fixtures should explain the rule

Use small fixtures with distinct roles. For a borrower-capacity test, create one borrower at the configured limit and one available target item. Existing loans need valid identities and dates, but they do not all need full API creation if the test's scope is the policy boundary. State that shortcut and avoid claiming an end-to-end workflow from the resulting unit or service test.

For a return test, make the initial loan and equipment state explicit. An active loan, CheckedOut equipment, and a fixed checkout instant are enough to test early-return rejection. A fixture helper that silently creates additional reservations or modifies status makes the failure harder to interpret. Prefer a few transparent setup statements over a broad factory whose defaults you cannot remember.

Distinct values help catch mapping errors. If every ID is the same GUID or every string is Camera, swapping fields may go unnoticed. Use different invented equipment and borrower IDs, different tags, and meaningful fixed dates. The values should illuminate the invariant without introducing real personal data or relying on the current day.

## Assert the relevant unchanged state

Negative tests deserve positive postconditions. When an operation rejects, specify what remains true. Invalid due date leaves equipment Available and no new active loan. Invalid return time leaves ReturnedAt null and equipment CheckedOut. Mismatched reservation cancellation leaves reservation Active. These assertions make the rejection's effect boundary observable.

Do not assert that the entire world is unchanged unless the test can actually observe that world and the requirement warrants it. A service may emit a diagnostic log or allocate a candidate object while correctly leaving business state untouched. Define the business state relevant to the invariant. This avoids brittle tests that prohibit harmless implementation details while still catching meaningful partial effects.

Error-code assertions complement state assertions. A wrong rule code can indicate changed precedence or an unintended failure path. However, when multiple inputs are invalid, decide whether precedence is contractual. A test that fixes every incidental guard order can obstruct reasonable refactoring. Keep strong assertions where the public contract requires them and narrower assertions where only rejection-before-effects matters.

## Test names should reveal evidence scope

Compare "Checkout_IsSafe" with "InvalidDueDate_DoesNotCheckOutEquipment." The second name tells a reviewer the condition and protected state. Compare "DatabaseWorks" with "Save_PersistsChangesAcrossContexts." The latter identifies the actual boundary and avoids promising process-restart durability from an in-memory SQLite connection.

Characterization checks need equally explicit names. The observation lab's failed-add test intentionally passes when it reproduces a current limitation. A future engineer should not mistake that green check for a requirement that equipment must remain stranded forever. Prefixing or documenting it as an observation is useful, but the accompanying explanation must say how the assertion should change when the feature is repaired.

For proposed features, a failing acceptance test can be legitimate before implementation. Label it as an unimplemented contract rather than a regression in current behavior. This distinction keeps a course honest: learners know whether they are preserving an existing guarantee, documenting a limitation, or building new behavior.

## Manual mutation in a disposable copy

Generate the existing compatible starter. Its recipe moves candidate construction after equipment mutation in reservation and checkout. Before running, predict which unchanged-state assertions fail and which exception assertions might still pass. Run the starter and compare your prediction with its expected two behavioral failures. The exercise demonstrates that a test can reach the intended exception while still exposing a broken postcondition.

Repair one ordering defect at a time and rerun. The intermediate result should improve by one case, which helps establish cause rather than correlation. If both failures disappear after an unrelated broad edit, inspect whether you accidentally removed assertions or changed fixture behavior. A passing suite is useful only when the checks still discriminate against the intended defects.

For a second mutation, remove an explicit save only in a generated copy and predict which in-memory checks remain green. Treat the surviving mutant as a test-scope lesson. Do not respond by adding a mock assertion for every method call automatically. Decide whether the correct complementary evidence is an interaction contract, an adapter round trip, or a higher-level persistence test.

## Determinism is part of test quality

Fixed clocks remove date drift. Explicit barriers remove accidental scheduler dependence. New store instances remove cross-test shared state. Bounded waits prevent a broken concurrency experiment from hanging indefinitely. These choices make failures easier to reproduce and explain; they are not merely ways to make the suite run faster.

Avoid hidden order dependence between tests. A test that retires an item should not rely on another test to recreate it. A shared WebApplicationFactory can have shared application stores depending on its lifecycle, so request tests need deliberate data isolation. Generated unique IDs help, but they do not replace understanding which singleton stores persist across client instances in the test host.

Deterministic does not mean unrealistic. A controlled interleaving can expose a real race more reliably than thousands of random requests. The key is to justify that the forced schedule is allowed by the production contract. If the fake creates an impossible condition, the test teaches the fake rather than the application.

## Balance layers of evidence

Domain tests should be fast and precise about object rules. Service tests should cover orchestration and state relationships with collaborators whose behavior is explicit. Adapter tests should verify real storage semantics. API tests should cover binding, authorization, routes, status, and response mapping. Operational tests should cross the deployment boundary relevant to restart or readiness claims.

You do not need to repeat every constructor edge case through a full HTTP host. Choose a few representative request cases to prove wiring and mapping, while retaining detailed domain coverage at the cheaper layer. Conversely, do not claim authorization from service tests that never create a principal. A layered plan reduces duplication by assigning each property to the boundary that can actually prove it.

The existing project already offers examples at these layers. Your task is to extend the evidence thoughtfully, not to replace all tests with one preferred style. A good review explains why a new test adds a missing observation and which existing test remains responsible for adjacent behavior.

## Independent exercises

**E15.1: Mutation map.** Choose five existing assertions and name a plausible defect each catches. Then identify two defects that would survive the selected tests. Propose the smallest additional observation that distinguishes each survivor without reproducing the implementation line by line.

**E15.2: Fixture review.** Design a fixed-time return fixture with distinct identities and no unrelated state. List which workflow steps you bypassed in setup and what claims that limits. Add one test for early return and one for duplicate return, with complete relevant postconditions.

**E15.3: Evidence allocation.** Assign twelve proposed checks across domain, service, adapter, API, and operational layers. Explain why each belongs there. Include one tempting but misplaced check and show how moving it improves both precision and execution cost.

## Review checkpoint

Your final test plan should identify a defect behind each major assertion, a clear observation boundary, and a reproducible fixture. It should also tell a maintainer which green checks document limitations rather than desired outcomes. Those qualities matter more than a large raw count of tests that all exercise the same happy path.
