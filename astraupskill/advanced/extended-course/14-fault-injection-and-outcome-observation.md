# 14. Fault injection and outcome observation

Fault injection is the practice of making a chosen operation fail at a controlled point so that you can observe the resulting state. In BorrowBox, it is more informative than a generic "throws an exception" test because the order of mutation, save, and insertion determines what remains afterward. This chapter teaches a small, repeatable experimental method that works in disposable copies without corrupting application data.

Use [the observation harness](lab/Program.cs.txt), [its generator](lab/generate_observations.py), and the actual service files linked in previous chapters. The harness already contains before-add and before-save failures. Your task is to expand the reasoning to after-effect faults, cancellation, and misleading test doubles while keeping every claimed observation tied to a specific experiment.

## Start with an experimental question

"Does checkout handle errors?" is too broad. "After equipment checkout succeeds, what state remains if LoanStore.AddAsync throws before inserting?" is precise. It names the successful prefix, the injected boundary, the failure timing, and the state you need to inspect. A good experiment can distinguish at least two plausible implementations with different consequences.

Write the initial ledger first: equipment Available, borrower active, no loans, fixed clock, valid due date. Then write the expected successful prefix: equipment lookup, borrower lookup, capacity read, candidate construction, conditional checkout. Finally, inject the designated failure and inspect equipment status and loan-store contents. The result answers the question even if the exception's human-readable message changes.

Use a dedicated exception type such as InjectedFailure so that the test catches only the intended fault. Catching Exception and declaring success can hide a null reference, a missing file, or a broken fixture. A fault experiment should fail loudly when it reaches the wrong boundary. Counters or a small event recorder can confirm that the intended collaborator method was invoked exactly as designed.

## Before and after are different experiments

A before-add double throws without recording the entity. An after-add wrapper delegates to a real in-memory store and then throws. Both make the service call fail, but one leaves no record and the other leaves a stored record. Their retry implications differ. Naming both simply FailingStore makes the test harder to read and encourages overbroad conclusions.

For reservation creation, before-add failure leaves Reserved equipment and no reservation in the controlled default case. After-add failure can leave Reserved equipment and an Active reservation even though the caller receives an exception. The latter resembles a lost confirmation after an effect. It does not require inventing a database crash to teach the ambiguity, although the in-memory experiment does not prove durable crash behavior.

For an equipment save, failure timing interacts with ownership. In the live-reference adapter, the object mutation may already be visible before the save call. An after-save fault therefore may not produce a different in-memory state from a before-save fault. That result is itself informative: the chosen adapter cannot distinguish a persistence boundary solely through object aliases. A detached or database-backed experiment is needed for the stronger persistence question.

## Record the whole relevant state

A test that asserts only equipment status can miss an extra loan. A test that asserts only loan count can miss a changed reservation. Define the relevant state from the business invariant and inspect every part. For checkout, record item status, active loans for the borrower, and the candidate or stored loan identity where available. For return, record ReturnedAt, IsActive, and equipment status.

Check state through the appropriate observer. The original variable reveals local mutation. A store lookup reveals what that adapter exposes. A fresh context reveals a persistence round trip. These observations can agree for different reasons, especially with live aliases. Report them separately instead of treating repeated reads of the same object as multiple independent confirmations of durability.

Also record the caller outcome: returned value, expected domain code, designated injected exception, or cancellation. Two experiments can leave the same final state while giving callers different information. Recovery design depends on both. A completed operation whose response is lost is different from a cleanly returned success even if the records are identical.

## Preserve collaborator contracts in doubles

A useful double changes one behavior at the chosen seam and preserves the rest needed by the experiment. If a store wrapper forgets to delegate the cancellation token, clones objects unexpectedly, or returns an empty collection regardless of setup, it can create a different system than the one you meant to study. Review the double with the same care as the service under test.

The before-save wrapper in the observation lab delegates Add, Get, List, and TryCheckOut to an actual InMemoryEquipmentStore, then throws in SaveAsync. This preserves the live-reference ownership model deliberately. The before-add reservation double exposes an empty store because it never records any insertion. Its simple behavior is sufficient for that bounded case, not a replacement implementation for every reservation workflow.

A race double has a different purpose. It may coordinate reads with a barrier to produce a chosen schedule. That can prove the service responds correctly to a collaborator's conditional failure or reveal a capacity gap. It does not automatically prove the production adapter uses the same synchronization. Label mechanism tests and real-adapter tests separately.

## Make asynchronous faults deterministic

An arbitrary delay can occasionally produce a desired overlap, but it is sensitive to machine load and scheduler timing. Prefer explicit gates: signal when a read has completed, await a release task, and then continue. Use bounded waits so a broken experiment fails instead of hanging indefinitely. The failure should explain which expected arrival never occurred.

For example, two capacity reads can both capture the same initial list before either is released to add. A TaskCompletionSource with asynchronous continuations can coordinate that schedule in a disposable double. The important evidence is the forced ordering, not the number of milliseconds waited. A test that succeeds only on a slow machine is poor evidence for a concurrency invariant.

Cancellation is also injectable without sleeping. A wrapper can cancel a token source after one known operation and let the next real store call observe the token. This lets you inspect partial state at an exact boundary. Do not claim cancellation is rollback: the experiment should show which effects happened before the token was observed and whether later operations ran.

## Avoid testing the test's own fiction

A common trap is a fake that implements the desired transaction perfectly while the real adapter lacks it. Service tests pass because the fake rolls back on failure, and the team concludes the service is atomic. The fake has assumed the property under test. A useful unit test can verify orchestration against a declared transactional contract, but separate integration evidence must establish that the real implementation satisfies that contract.

Another trap is a test that mirrors every source statement. Such a test may fail on harmless refactoring while missing a business inconsistency. Use event sequences to investigate, then keep durable assertions about semantic boundaries: no effect on invalid input, one winner for a declared claim, consistent committed records, or a documented partial outcome. This makes the test explain the requirement rather than memorize the implementation.

Do not remove all internal observations, either. A designated fault counter is useful to prove that the intended seam was reached. The distinction is purpose: instrumentation confirms the experiment, while state and outcome assertions establish the behavior. Both can coexist when each has a clear role.

## Guided experiment: a paired fault report

Choose reservation creation and build two disposable wrappers around a real reservation store. The first throws before AddAsync delegates; the second delegates and then throws. Use the same valid fixture and fixed clock for both. Record the exception, equipment status, reservation count, and returned-or-unreturned result. Reset the entire fixture between experiments so one case cannot contaminate the next.

Now write a client-facing explanation for each result without exposing internal implementation jargon. The before-add case means the reservation was not recorded but equipment state may need reconciliation. The after-add case means the caller lacks confirmation of an effect that occurred. A proposed receipt mechanism can make the latter queryable. The exercise connects low-level fault placement to meaningful user behavior.

Keep the experiment local and bounded. There is no need to kill a production process, corrupt a database, or disable a network interface to establish these in-memory distinctions. More realistic failure testing belongs in a separately designed disposable environment once the corresponding durable feature exists.

## Independent exercises

**E14.1: Fault catalogue.** Define six named faults across checkout, return, reservation, and inspection. For each, state before or after effect, the designated exception, the observer, and the expected state. Include at least one case where a domain error occurs after a prior mutation.

**E14.2: Double review.** Review a proposed wrapper that returns cloned equipment on GetAsync but delegates SaveAsync to the live store. Explain how that ownership change affects a before-save fault experiment. Decide whether to preserve it as an explicit snapshot model or replace it with a faithful wrapper for the default adapter.

**E14.3: Deterministic cancellation.** Design a token-cancellation seam between a reservation's equipment mutation and its next store operation. Predict the state and caller outcome with the actual in-memory adapter. Explain what a separate durable-adapter experiment would need to establish about committed state.

## Review checkpoint

A useful fault report contains initial state, exact seam, expected schedule, observed outcome, final state, and limitation. If another learner cannot recreate the failure without guessing a delay or changing original source, improve the experiment before broadening the claim. This discipline will support the later concurrency and cancellation chapters, where reproducible schedules matter more than large numbers of uncontrolled tasks.
