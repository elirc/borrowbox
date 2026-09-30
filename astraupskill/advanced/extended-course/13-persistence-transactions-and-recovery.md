# 13. Persistence, transactions, and recovery

Several BorrowBox workflows change more than one object. Checkout changes equipment and creates a loan; reservation creation changes equipment and creates a reservation; return changes a loan and equipment; inspection completion changes inspection and equipment. The earlier chapters identified exact partial outcomes. This chapter turns those traces into a proposed consistency design with explicit failure and recovery responsibilities.

Use [loan workflows](../../../src/BorrowBox.Core/Loans/Loan.cs), [reservation workflows](../../../src/BorrowBox.Core/Reservations/Reservation.cs), [inspection workflows](../../../src/BorrowBox.Core/Inspections/Inspection.cs), and [the current database model](../../../src/BorrowBox.Infrastructure/Data/BorrowBoxDbContext.cs) as the starting evidence. The database model currently exposes equipment rows only. The transactional lending design below is a capstone proposal, not a description of existing infrastructure.

## Define the invariant before the transaction

For checkout, a useful proposed commit invariant is that the equipment claim, the active loan, and the completed operation receipt agree. For return, the loan's return state and equipment availability agree under the chosen lifecycle policy. For reservation, the reservation's Active state owns a specific equipment reservation claim. These statements identify related facts; they do not yet prescribe a schema.

Be careful with universal phrasing. Equipment under inspection should not become Available merely because a loan is returned if the product requires damage handling. A reservation marked Expired does not prove an item should be released when another newer reservation owns it. The invariant must include ownership and lifecycle context, not just compare two enum values. A transaction preserves the rules you encode, including incorrect ones.

Write the smallest record set that participates in each invariant. A checkout may need the equipment row, borrower eligibility or capacity authority, loan row, and receipt. A global transaction over every table is unnecessary unless the operation truly depends on all of them. Conversely, a transaction around only the loan insert misses the equipment effect that motivated the design.

## Validation and commit are different phases

Pure validation can reject malformed or internally inconsistent input before a transaction begins. Examples include nonempty IDs, a well-defined due-date comparison, and required notes. State-dependent validation must be evaluated against a coordinated view if another writer can invalidate it. Equipment availability and borrower capacity are examples; their truth can change while the request waits.

A proposed workflow can first construct a validated command, then enter a transaction, load coordinated state, decide eligibility, apply changes, and commit. Do not assume the early decision remains valid simply because it was once true. Also avoid duplicating expensive or side-effectful work inside a retry loop without deciding whether it is safe to repeat.

The current candidate-first ordering remains valuable in such a design. Constructing a valid Loan before equipment mutation prevents known date errors from creating effects. A transaction adds a stronger boundary for later persistence failure, but it does not make clear validation obsolete. Good designs use each mechanism for the problem it actually addresses.

## Worked transaction proposal for checkout

Suppose a future adapter stores equipment, borrowers, loans, and operation receipts in one database. The command carries a stable operation ID and immutable request payload. The service first checks whether a completed receipt exists for that ID and matching payload. If so, it returns the recorded result. Conflicting payload reuse is rejected. Otherwise, it enters the defined transactional workflow.

Inside that workflow, it coordinates the borrower capacity authority and equipment claim, verifies eligibility, records the loan, and records a completed receipt referring to that loan. Commit makes the chosen set of changes durable together under the database's actual guarantees. The response is then constructed from the committed result. If the response is lost, a retry can discover the receipt rather than create another loan.

This design still needs implementation-specific answers. What prevents two transactions from both accepting the last capacity slot? What order do they acquire contested resources? How are transient transaction conflicts retried? Which errors are domain conflicts versus infrastructure failures? A diagram with one transaction rectangle is a useful outline but not proof that the database isolation and queries enforce the invariant.

## Failure windows need named outcomes

Before the transaction begins, rejection should have no business effects. During an uncommitted transaction, failure should leave no committed partial result according to the selected database mechanism. After commit but before response, the business result exists even if the client sees a timeout. These three windows have different meanings and should appear separately in the API and operational design.

Cancellation adds another distinction. A canceled request before commit may be safely abandoned if the transaction is rolled back. Cancellation observed after a successful commit cannot make the committed loan disappear. The implementation must avoid reporting a fictional rollback just because the caller disconnected. A receipt and query path let later requests discover the actual result.

External side effects require additional care. Sending an email inside the transaction does not make the email rollback when the database does. Sending after commit can fail while the loan remains valid. A proposed notification outbox can record a pending notification in the same commit and deliver it later with idempotency. BorrowBox does not currently implement that pattern; it is a design option for a capstone that adds external notifications.

## Compensation is a protocol, not an inverse call

When records cannot share a transaction, a workflow may use explicit steps and compensating actions. The compensation must identify the effect it owns and verify that reversing it is still valid. Calling equipment.Return after any failure does not meet that standard because another workflow may have changed the item or acquired a newer claim.

A proposed reservation saga could record an operation state, reserve equipment with an owner ID and version, create the reservation record, and mark completion. If creation fails, a compensator releases only the equipment claim still owned by that operation. If ownership changed or the outcome is ambiguous, it records a reconciliation task rather than overwriting current state. The operation log must survive process failure if recovery is expected after restart.

This protocol is more complex than the existing teaching services. Do not introduce it merely to demonstrate fashionable terminology. It becomes justified when the deployment cannot use a simpler shared commit and the product needs recoverable multi-step behavior. Your design review should compare that complexity with placing related records in one transactional boundary.

## Recovery must survive the failure it addresses

A catch block can handle an exception while the process remains alive. It cannot execute after a process crash or power loss. An in-memory queue of unfinished work disappears with the process. A recovery design promising restart behavior therefore needs durable evidence of intent, progress, ownership, and completion, plus a component that can resume or reconcile that evidence.

Define what a recovery worker is allowed to decide automatically. It may safely retry an idempotent step with a stable operation ID. It may safely release a claim only when an atomic ownership/version check confirms the claim belongs to the failed operation. It should not invent a loan recipient or erase an inspection decision based solely on an unexplained status mismatch.

Human review can be part of the design for ambiguous cases. A useful reconciliation record provides enough IDs, timestamps, and observed states to investigate without dumping sensitive data. It also explains what automatic actions were attempted and why they stopped. Recovery is successful when the system can reach a justified consistent outcome, not merely when an exception disappears from logs.

## Test the design at its real boundary

A pure unit test can verify that a transaction coordinator is called, but it cannot prove the database atomically commits the intended rows. A same-process fake can demonstrate the workflow's decision order, but it cannot prove crash recovery. Match each claim to a test that crosses the relevant boundary, using the evidence method from chapter one.

For a proposed database implementation, include failures before commit, conflicts on contested capacity, and fresh-context verification of all related records. For restart recovery, use a file-backed disposable database and a separate process under a controlled harness. No such environment is required for this reading exercise; mark those tests as planned until implemented and executed.

The existing observation lab remains useful as a baseline. Its partial-state checks show what the current implementation permits. A capstone implementation should replace those limitation assertions with the proposed invariant while retaining separate tests that prove the old failure is no longer possible under the declared scope. Do not delete inconvenient cases merely because the design changed.

## Independent exercises

**E13.1: Commit set.** Define the exact records and fields that must agree for checkout, return, and reservation creation. Include ownership and receipt data only where your contract needs them. Explain which current interfaces cannot express your proposed commit boundary.

**E13.2: Lost response.** Draw a sequence where commit succeeds but the response fails. Specify the retry request, the receipt lookup, payload comparison, and final response meaning. Include a conflicting reuse case and explain why generating a new operation ID on every retry defeats the design.

**E13.3: Recovery ownership.** Design a compensating release for a failed reservation operation. State the evidence required to release, the condition that causes refusal, and the durable record left for reconciliation. Show how the design behaves if a new reservation already owns the equipment.

## Review checkpoint

Your proposal should make a falsifiable claim about one explicit deployment boundary. Avoid promising database atomicity from a fake, cross-process coordination from a lock, or crash recovery from a catch block. A modest design with a complete failure story is more useful than a large architecture whose successful path is the only path explained.
