# 20. Capstone: durable lending receipts

This capstone asks you to design and prototype a lending boundary that commits checkout and return consistently and lets a caller resolve an interrupted response. It builds on the actual CheckoutService and ReturnService failure traces, but the durable transaction and receipt features are proposed work. Complete the capstone in an explicitly named disposable workspace; do not alter the original application as part of the course.

Your starting evidence is [Loan and its services](../../../src/BorrowBox.Core/Loans/Loan.cs), [the equipment adapters](../../../src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs), [the database model](../../../src/BorrowBox.Infrastructure/Data/BorrowBoxDbContext.cs), and the chapter thirteen commit-set design. The current database model is equipment-only, so a durable lending prototype requires new proposed records rather than pretending that the existing SQLite adapter already persists loans.

## The user story and acceptance boundary

A desk operator checks out a camera for a borrower. The operation must not leave a claimed camera without its corresponding active loan after a failed commit. If the response is interrupted after a successful commit, the operator can retry with the same operation ID and discover the original result. Returning the camera must update the loan and equipment consistently under the chosen lifecycle policy.

Keep the feature bounded. Do not add billing, email delivery, a new frontend, or a distributed deployment unless a separate requirement demands it. The capstone concerns lending consistency, capacity coordination, and outcome discovery. A small prototype that demonstrates those properties clearly is more valuable than a large application rewrite with an unexplained successful path.

Define the deployment scope before implementation. Milestone one can use a same-process transactional model to clarify the command contract. Milestone two, if undertaken, uses a real disposable database and integration tests. Only the latter can support database commit claims, and restart claims require an additional process boundary. Report the milestone reached rather than treating every prototype as equally durable.

## Proposed command and receipt contract

A checkout command carries operation ID, equipment ID, borrower ID, and due date. Actor context comes from trusted application identity rather than a client-selected role flag. A receipt records the normalized command identity, final loan ID, accepted timestamps, and a completed outcome. The operation ID's scope and retention period must be defined so an old retry cannot silently become a new checkout.

For identical retries, return the original completed result. For conflicting payload under an existing operation ID, return a defined conflict without changing the original records. For a pending operation, provide an explicit pending response or resolution path. Do not return a newly created loan merely because the client repeated the request after a timeout.

The return command needs the same care. Decide whether it uses a new operation ID referencing a loan and whether its timestamp is server-selected. A duplicate logical return can replay its receipt under the proposed contract, while an authorized correction of return time remains a different operation. This preserves history rather than allowing a retry to rewrite accepted facts.

## Model the durable relationship

Sketch records for equipment, borrower capacity authority, loans, and receipts. Equipment needs enough state to identify the current claim or version. Loans need checkout and return fields. Receipts need a stable link to the committed operation result and enough request identity to reject conflicting reuse. Avoid storing only a response string if later reconciliation needs structured ownership evidence.

Specify constraints that the database can enforce directly, such as unique operation identity and appropriate keys. Then specify invariants requiring coordinated application logic, such as capacity under overlapping checkouts. A unique loan ID does not enforce a maximum number of active loans for one borrower. A unique receipt does not automatically make its business effects atomic.

Choose a transaction boundary that includes the records needed by the operation. For checkout, coordinate borrower capacity and equipment availability before recording the loan and receipt. For return, coordinate the active loan and equipment transition before recording completion. Document acquisition or update order to avoid inconsistent locking strategies across the two directions.

## Worked successful and interrupted traces

In the successful checkout trace, the server validates the command shape, resolves actor authority, begins the coordinated operation, verifies borrower eligibility and capacity, claims equipment, inserts the loan, records the receipt, commits, and returns. Mark which validations are pure and which require protected current state. The returned receipt represents the committed event, not merely a mutable entity read at an arbitrary later moment.

In the interrupted-response trace, the same commit succeeds but the network response is lost. The client retries with the same operation ID. The server reads the receipt, verifies payload equivalence, and returns the original result. No second equipment claim or loan insertion occurs. This trace is the core reason for the receipt feature.

In the failed-commit trace, inject a failure after planned changes but before commit. A fresh durable observer should see the prior consistent state and no completed receipt under the proposed transaction contract. If your prototype uses only a fake transaction, label the result as a model observation. Do not claim a database property from a fake that simply discards a temporary dictionary.

## Capacity remains part of the capstone

Run two different-item requests for a borrower with one remaining slot. Both should not commit. The loser should receive a defined capacity conflict or rule rejection, and its equipment should remain available. This test distinguishes the capstone from a design that only solves same-item contention while preserving the earlier borrower-capacity gap.

Run two borrowers competing for one item. Exactly one should obtain a committed loan, and the losing operation should not leave a completed success receipt. These two concurrency cases use different coordination keys. Include both even if the implementation's transaction mechanism is the same, because the invariants and failure explanations differ.

Do not use arbitrary sleeps to manufacture the overlap. In a model, use explicit gates at observations. In a real adapter test, use controlled transactions or instrumentation appropriate to the implementation. State the allowed schedule and final invariant, then choose the smallest reliable mechanism that creates it.

## Return must preserve the same relationship

The return feature should not rely on the current ILoanStore's live-reference mutation as its persistence mechanism. Define an explicit durable update within the chosen unit of work. Reject an invalid early timestamp before changing either record, and define duplicate behavior through the operation receipt rather than accidentally falling into an already-returned error after a lost response.

Include an equipment-state mismatch case. The capstone must decide whether to reject, reconcile, or apply a special damaged-return policy when the item is not CheckedOut. Do not silently make it Available because a loan return was requested. That could overwrite an inspection or a newer claim. The chosen policy needs an ownership argument and an observable result.

A correction workflow is optional and outside the core acceptance boundary. If you omit it, document that returned timestamps cannot be edited through retry. If you include it, retain previous values and verified actor information in an audit record. Keep the core lending receipt implementation understandable before adding this extension.

## Deliverables and staged review

First deliver a command contract, schema sketch, and three traces: success, pre-commit failure, and lost response after commit. Review those before writing infrastructure code. Second deliver a disposable prototype with deterministic acceptance checks. Third, if implementing durability, add real-adapter tests and document the database observation boundary. This staging makes assumptions reviewable before they become expensive code.

The acceptance checks must include valid checkout, invalid due date, inactive borrower, same-item competition, last-capacity-slot competition, failed commit, identical retry, conflicting operation reuse, valid return, and interrupted return response. Add cancellation at the commit boundary if your contract promises a particular behavior there. Record unimplemented checks honestly rather than substituting happy-path output.

Keep a limitations section naming the deployment scope, identity source, storage provider, and untested failure classes. A model-only submission can still be strong learning work if it precisely identifies what remains before durable deployment. The course solution provides a reference design, not a finished production implementation to copy without review.

## Independent capstone challenges

**E20.1: Design submission.** Write the exact command/receipt contract, record set, and conflict semantics. Include one rejected alternative and explain the concrete invariant it failed to preserve. Avoid generic pattern comparisons unrelated to BorrowBox's observed failure traces.

**E20.2: Acceptance harness.** Implement or specify the ten acceptance cases above in a disposable environment. For each, name the observer and expected state. Mark every unexecuted case and explain what dependency or implementation milestone is required before execution.

**E20.3: Operational handoff.** Write a short runbook for an operator who receives an interrupted checkout response. Include receipt lookup, safe retry, conflicting reuse, and unresolved outcome escalation. The runbook must not advise repeatedly creating new operation IDs until equipment becomes available.

## Review checkpoint

The capstone passes design review when its state transitions, transaction boundary, capacity coordination, and retry semantics agree. It passes implementation review only to the extent demonstrated by executed tests at the claimed boundary. Keep those verdicts separate so a clear design does not masquerade as deployed durability and a passing prototype does not conceal missing product decisions.
