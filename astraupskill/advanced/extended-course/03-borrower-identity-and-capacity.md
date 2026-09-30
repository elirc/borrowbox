# 03. Borrower identity and capacity

Borrower rules look simpler than equipment rules because Borrower exposes few operations. That simplicity can be misleading. Identity normalization, account activity, lending capacity, and authorization answer different questions. A correct solution keeps them distinct and then coordinates the ones that participate in a business decision.

Read [Borrower and BorrowerService](../../../src/BorrowBox.Core/Borrowers/Borrower.cs), [loan policy and checkout](../../../src/BorrowBox.Core/Loans/Loan.cs), and [checkout tests](../../../tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs). This chapter follows two students borrowing different items at the same desk. The cases use invented identities, so you can reproduce them without any real account data or external identity service.

## Three kinds of identity

Borrower has a Guid Id, a CollegeId string, and a DisplayName string. Id is the object identity used by store lookups and loan references. CollegeId is normalized by trimming and invariant uppercase. DisplayName is trimmed but remains a presentation value. The constructor rejects empty IDs and blank required strings. None of those operations authenticates the person standing at the desk.

Normalization makes equivalent representations easier to compare. It does not establish uniqueness, ownership, or external validity. Two calls to RegisterAsync create two new GUIDs. The borrower interface offers AddAsync and GetAsync by GUID; it does not expose a lookup by CollegeId or a uniqueness decision. Therefore, a proposed "one borrower per college ID" rule requires an explicit enforcement boundary. Do not assume that uppercase strings make duplicates impossible.

DisplayName should not become an accidental identifier. Two people can share a name, and a person's display name can change in a larger system. The current object makes its display name immutable after construction, but that implementation choice does not turn the name into a stable external key. If you design an update feature later, preserve the distinction between changing a label and changing who owns existing loans.

## Activity is a business gate

IsActive starts true. Deactivate sets it false. Checkout and reservation creation load the borrower and reject an inactive borrower before their equipment effects. This is a business eligibility rule. It is separate from API authentication and staff authorization, which must be evaluated at the request boundary. A staff user may submit a request concerning an inactive borrower; being staff does not make that borrower eligible.

The current Borrower object does not reactivate itself, publish an event, cancel reservations, or return outstanding loans when deactivated. Those behaviors might be reasonable product features, but they are not consequences of assigning IsActive. State mutation is not a hidden workflow engine. If an exercise proposes "deactivate and cancel all future reservations," it must define orchestration, failure behavior, and how partial progress is reported.

Consider a borrower with two active loans who is then deactivated. The loans still exist, and their return workflow should not be inferred from the checkout eligibility gate. Trace ReturnService separately: it loads a loan and equipment, checks return conditions, and does not load a borrower. This is a sensible distinction to notice in the current implementation. Preventing new borrowing and accepting returned property are different operations with different preconditions.

## Capacity is a predicate over a changing set

BorrowerLoanPolicy requires a positive maximum at construction. EnsureCanBorrow rejects when activeLoanCount is greater than or equal to that maximum. In Program, the selected maximum is three. The policy object receives a count; it does not fetch loans, reserve capacity, or update a counter. Its job is a local decision on a supplied observation.

The service obtains active loans from ILoanStore, passes Count to the policy, constructs a candidate loan, attempts equipment checkout, and adds the loan. This separation makes the rule easy to test but leaves a coordination question: can the observed count change before the addition? The answer matters even if every individual method is thread safe. A business invariant often spans a read, a decision, and a write.

Start with a sequential case. Mira has two active loans and the maximum is three. A checkout reads two, the predicate permits borrowing, and the new loan raises the count to three. A following request reads three and is rejected with borrower.loan_limit before equipment checkout. This is the case a simple limit test can establish. It is useful, but it does not cover simultaneous requests.

## Worked interleaving: two items, one borrower

For the concurrent case, reduce the maximum to one to make the violation obvious. Mira initially has zero active loans. Request A wants camera C; request B wants microphone M. Both equipment items are Available. A reads zero active loans and pauses. B reads zero active loans and pauses. Both policy checks accept their observed counts.

A checks out C, and B checks out M. The equipment operations do not conflict because they concern different items. A adds loan LA, and B adds loan LB. Mira now has two active loans despite a maximum of one. Per-equipment exclusivity did its job: neither item was checked out twice. The borrower-capacity invariant still failed because its coordination key is the borrower, not an individual item.

| Step | Request A | Request B | Stored active loans |
|---|---|---|---:|
| 1 | Observe zero | Waiting | 0 |
| 2 | Waiting | Observe zero | 0 |
| 3 | Pass policy; claim C | Pass policy; claim M | 0 |
| 4 | Add LA | Waiting | 1 |
| 5 | Complete | Add LB | 2 |

The advanced lab already includes a deterministic observation of this boundary using an interleaved loan store. Treat its passing observation as confirmation that the limitation is reproducible, not as a desired production rule. A test named "known borrower-capacity boundary" can pass when it detects the current gap. A future regression test for a corrected implementation would assert a different result and require a different contract.

## Why common fixes are incomplete

Checking the count twice narrows a timing window but does not close it. Both requests can perform the second read before either writes. Locking each equipment item also misses the shared borrower resource. A static global lock can serialize a single process if all participants obey it, but it says nothing about another process or another application instance. Each proposal must name the participants it coordinates.

A borrower-keyed lock is a useful same-process teaching proposal. Acquire the lock before reading the active count, hold it through the addition, and release it reliably. You must also decide what happens if equipment checkout succeeds and loan addition fails. Capacity coordination does not repair the multi-store failure on its own. Locks and transactions solve related but distinct problems: ordering access versus committing a consistent result.

A durable design could use a database transaction with an enforceable borrower capacity mechanism. Depending on schema and database semantics, that might involve a locked borrower row, a conditional counter update, or another explicit serialization strategy. Saying "use a transaction" without identifying the contested row and isolation behavior is incomplete. This chapter asks for the invariant and schedule; later capstones will require an implementation-specific durable argument.

## Guided investigation: policy and mechanism

In a disposable reference workspace, create one borrower and two available items. First execute checkouts sequentially with a maximum of one and record the expected rejection. Then inspect the existing lab's deterministic interleaving observation. Identify the point at which both callers have received the same count. Write down why a sleep is unnecessary and less reliable than a controlled barrier at that point.

Do not change the policy predicate to reject zero active loans in order to make the race disappear. That would prevent legitimate borrowing rather than coordinate it. Similarly, do not decrement the configured maximum as an improvised concurrency buffer. Product policy should express the desired limit, while a mechanism should enforce it under the declared execution model.

Next, propose a same-process coordinator interface in your notes. Specify who acquires it, how cancellation while waiting behaves, whether different borrowers can proceed concurrently, and how the lock object remains stable for a borrower key. Avoid writing a large lock cache before deciding its lifecycle. The goal of this investigation is to expose design obligations, not to produce infrastructure code whose cleanup behavior you have not analyzed.

## Independent exercises

**E03.1: Identity proposal.** Design an enrollment rule that rejects duplicate normalized CollegeId values. State whether uniqueness applies forever or only to active records. Describe the sequential duplicate case, the simultaneous duplicate case, and the response to a retry after an unknown result. Your answer must identify where normalization occurs and which component makes the uniqueness decision atomically.

**E03.2: Deactivation schedule.** Sketch a checkout that reads an active borrower, pauses, and then continues after another caller deactivates that borrower. Decide whether the proposed product contract permits completion based on the earlier decision or requires revalidation at commit. Both choices need an explicit rule. Explain why re-reading without coordination may still race with another deactivation.

**E03.3: Capacity proof.** Give a sequential proof that the current predicate respects the maximum under serialized checkouts, then give the concurrent counterexample above using a maximum of three and an initial count of two. State the exact shared resource that must be coordinated. A solution that only protects equipment does not satisfy the capacity invariant.

## Review checkpoint

You should now be able to explain four separate statements: this record identifies a borrower; this request is authenticated; this borrower is currently eligible; this operation preserves lending capacity. Each statement has a different owner and evidence requirement. A good design joins them in a workflow without collapsing them into one boolean called valid.

Finish with a product question that the code alone cannot answer. Examples include whether deactivation cancels reservations, whether duplicate college IDs can represent a corrected enrollment, or whether a checkout already in progress survives deactivation. Mark the chosen answer as a proposal. Clear uncertainty is productive here because it prevents an accidental implementation detail from becoming an undocumented business policy.
