# 05. Checkout as a coordinated decision

Checkout joins several decisions that have different lifetimes. The equipment exists at one lookup, the borrower is active at another observation, the active-loan count permits borrowing, and the requested due date is valid against a captured clock instant. The service then attempts an equipment transition and records a loan. A sound analysis follows those facts through time rather than compressing them into one statement that checkout was validated.

Read [CheckoutService](../../../src/BorrowBox.Core/Loans/Loan.cs), [both equipment adapters](../../../src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs), [the SQLite conditional update](../../../src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs), and [the checkout tests](../../../tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs). The chapter uses current source for its trace. Transaction receipts and durable coordination discussed later in the chapter are proposals for your design work.

## The operation begins before the first lookup

CheckoutService captures checkedOutAt before loading equipment. It uses the injected IClock when supplied, otherwise a SystemClock. That timestamp therefore represents the service's early observation, not necessarily the instant at which equipment changes or a loan becomes stored. On a slow adapter, considerable time could pass between capture and the final write. Whether that is acceptable is a product decision about what CheckedOutAt means.

The service loads equipment by the requested ID and throws equipment.not_found when absent. It then loads the borrower and checks IsActive. After reading active loans and applying BorrowerLoanPolicy, it constructs Loan with the equipment and borrower IDs, the captured instant, and DueOn. This construction can reject a due date before any checkout effect. A new loan ID has been generated, but the candidate is not yet part of stored business state.

The next step is decisive: the service does not simply call CheckOut on the earlier loaded object. It calls IEquipmentStore.TryCheckOutAsync with the requested equipment ID and requires a non-null result. This gives the adapter an opportunity to make availability testing and the transition one coordinated action. The initial lookup alone would be insufficient because another caller could change availability after that read.

## Worked trace: stale reads with one winner

Two desk requests, A and B, target the same camera. Both initial lookups can observe Available. Both borrowers are eligible, both capacity checks pass, and both candidate loans construct successfully. If the service relied only on those earlier observations, both callers could believe they owned the camera. The conditional transition is the later authority for that particular decision.

With the default same-instance adapter, A enters the item's lock, sees Available, and changes it to CheckedOut. B later enters the same lock, sees CheckedOut, and receives null. The service turns B's null into equipment.not_available. A continues to add its candidate loan. Under the stated assumptions, B's candidate remains an unrecorded local object. The existence of a candidate GUID is not a committed checkout.

The existing competing-request test uses a specialized RacingEquipmentStore. Its initial reads are deliberately coordinated, and its TryCheckOutAsync uses an atomic winner flag. The test verifies that CheckoutService respects a store-level conditional winner instead of trusting stale initial reads. It is not itself a comprehensive contention test of the actual in-memory dictionary implementation or of SQLite. Reading the test double is necessary to understand what the test proves.

## Two adapters, one narrow intent

InMemoryEquipmentStore protects its availability test and CheckOut call with a lock on the item. SqliteEquipmentStore issues an update filtered by ID and Available status, setting status to CheckedOut. It treats exactly one changed row as success and then performs GetAsync to return the updated item. Both express a conditional claim on availability, but their mechanisms and failure boundaries differ.

For SQLite, there is a boundary between the successful ExecuteUpdateAsync and the subsequent read. If that read fails or is canceled, the earlier update is not automatically described as undone by this method. There is no explicit surrounding transaction in this adapter method. Do not turn a convenient returned object into proof that the write and response are inseparable. A fault analysis should consider an update that completed before the caller lost confirmation.

The service's candidate loan uses the IDs from the earlier loaded entities. The returned equipment value is checked for non-null, but it is not used to reconstruct a second candidate loan. Under normal contracts the ID still refers to the requested item. A malicious or badly written fake could violate this assumption. Tests should model realistic collaborator contracts while also identifying critical assumptions that deserve explicit validation in a production boundary.

## The write after the claim

After a successful TryCheckOutAsync, the service calls loanStore.AddAsync. This is where a narrow availability win becomes a broader lending consistency problem. If AddAsync throws before recording, the equipment claim has already happened. The service does not catch the exception and does not perform a compensating transition. The result is a failed method call with changed equipment and no corresponding stored loan in the controlled failure case.

A user may reasonably interpret a failed request as "nothing happened." That interpretation is unsafe here. A client retry for the same item may now receive equipment.not_available. The second error is not proof that another person borrowed the item; it may be the residue of the first attempt. Without a stable operation identity and outcome record, the service cannot distinguish these explanations for the client.

This is why error handling belongs in the domain design discussion as well as in transport code. Mapping an IOException to a friendly message helps the user understand that something went wrong, but it does not repair the inconsistent relationship. A proposed durable workflow needs a commit boundary or a recovery protocol that records enough information to determine which operation owns the partial effect.

## A proposed receipt contract

For the exercise, design a CheckoutReceipt identified by a client-supplied operation ID scoped to the caller or institution. The receipt could record requested equipment, borrower, due date, accepted decision time, and final loan ID. A repeat with the same operation ID and identical request should return the same completed result under the proposal. A repeat with different request data should produce a defined conflict rather than silently replay an unrelated result.

This is a proposed interface contract, not an implemented BorrowBox type. A receipt stored only after all other writes still leaves a crash gap. A robust implementation must commit the receipt consistently with the loan and equipment, or use explicit states and recovery for a multi-step protocol. A receipt that says Pending must not be mistaken for a successful loan; the client needs a way to query or safely retry the operation.

Think about retention as well. If receipts are deleted too soon, an old retry can look like a new request. If the operation identifier is globally visible without authorization, it can reveal another person's lending data. The exercise does not require a production storage schema yet, but it does require stating the scope and lifespan of idempotency. Otherwise, the word idempotent hides several unresolved product decisions.

## Guided lab: split the outcomes

Use your disposable reference workspace and retain the existing before-add failing loan-store observation. Add a recorder that logs logical events to an in-memory list: clock read, equipment get, borrower get, active-loan read, conditional checkout, and loan addition. Place recording wrappers around collaborators rather than adding logging statements to original application source. Make sure wrappers delegate the same arguments and cancellation token.

Run three cases: invalid due date, unavailable equipment at the conditional transition, and loan-store failure before insertion. Compare the event sequence and final state. The invalid-date case should never reach conditional checkout. The unavailable case should reach that boundary but not loan addition. The insertion-failure case reaches both effectful stages and leaves the equipment transition observable. The final state, not merely the event count, distinguishes the cases.

Do not overfit a regression test to every harmless read order unless order is part of the requirement. The recorder is an investigation tool. The durable test should emphasize semantic boundaries: no equipment claim for invalid date; no loan addition when the claim loses; an explicit known limitation or corrected atomic outcome when addition fails. This keeps the tests useful after reasonable refactoring.

## Independent exercises

**E05.1: The losing candidate.** Explain why constructing two candidate loans for competing requests does not necessarily create two stored loans. Identify the point at which each candidate becomes externally meaningful. Then state a failure mode in which a candidate should be persisted as a recovery record under a proposed design, and explain why that is a deliberate change in semantics.

**E05.2: Ambiguous SQLite outcome.** Design a fault at the read following a successful conditional update. State what is known, what remains unknown to the caller, and what a retry can observe. Do not claim the application default runs this adapter. Your answer should distinguish the adapter experiment from the in-memory checkout path selected by Program.

**E05.3: Request identity.** Specify five receipt cases: first success, identical retry, conflicting reuse, pending outcome, and expired receipt. Give a response meaning for each without choosing HTTP status codes yet. Explain which records must agree for the completed response to be trustworthy.

## Review checkpoint

Draw a vertical line at the conditional equipment transition and another at loan insertion. Annotate which errors happen before both lines, between them, and after a successful write whose response is lost. A transaction proposal should say which lines collapse into one commit boundary. A recovery proposal should say how the space between them is recorded and reconciled.

The chapter's central deliverable is a vocabulary of checkout outcomes with enough detail to guide retries. Preserve the useful conditional claim already present in the service, while making the larger consistency gap explicit. A mature review can recognize an improvement and its remaining limits at the same time.
