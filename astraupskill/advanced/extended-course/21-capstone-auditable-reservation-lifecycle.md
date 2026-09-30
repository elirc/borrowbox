# 21. Capstone: an auditable reservation lifecycle

The second capstone focuses on reservation ownership, terminal reasons, and operational reconciliation. Current BorrowBox can create, cancel, and expire reservations, but equipment status alone does not identify which reservation owns a claim. The sweep skips some mismatches, and collection primitives exist without a coordinated collection service in the inspected implementation. Your proposed feature will make those boundaries explicit and auditable.

Start from [Reservation and its services](../../../src/BorrowBox.Core/Reservations/Reservation.cs), [EquipmentItem](../../../src/BorrowBox.Core/Equipment/Equipment.cs), and [the expiry worker](../../../src/BorrowBox.Api/Reservations/ReservationExpiryWorker.cs). Use the earlier cancellation, expiry, and reconciliation ledgers as requirements evidence. Implement only in a disposable copy or prototype; the workbook does not ship this feature into the original application.

## A bounded product story

A student reserves an item until a stated instant. The student or an authorized staff member can cancel according to a chosen deadline policy. A worker expires due reservations. Each equipment reservation claim identifies its owning reservation, and every terminal transition records why it occurred. An operator can inspect unresolved mismatches without guessing which reservation owns the item.

Keep collection as a deliberate stretch milestone unless you choose to include it in the core. A complete collection feature must create a loan, enforce borrower capacity, and coordinate the transition from Reserved to CheckedOut with reservation completion. Merely calling Collect and CollectReservation does not fulfill that story. The base capstone can be substantial and coherent with reserve, cancel, expire, and reconcile alone.

Define actor permissions separately from borrower eligibility. A valid authenticated student should not automatically cancel any reservation ID. A staff member may have delegated authority. An expiry worker acts under a system authority with a specific purpose. These distinctions build on the identity-binding proposal rather than assuming the demo role header is a production trust mechanism.

## Ownership belongs in the model

Propose an equipment reservation claim containing reservation ID or operation ID, version, and relevant status. The exact schema can vary, but a release must be able to prove it is releasing the claim owned by the reservation being canceled or expired. Status Reserved alone is insufficient when an old terminal record and a newer active claim can refer to the same item.

The reservation record includes its immutable identities, reserved and expiry instants, current status, version, and terminal decision metadata. A terminal event can record the reason, effective instant, verified actor or worker identity, and operation identifier. Avoid overwriting the only history of how a reservation ended if later corrections or reconciliation matter to the product.

Decide whether the audit log is authoritative state or a record derived from committed state changes. Either choice needs consistency. If the reservation changes but the audit insert fails independently, the system can lose the very explanation the feature promises. Include the event in the shared commit where possible, or define a durable delivery protocol with explicit recovery when records are separated.

## Deadline policy must be chosen

Choose one of two policies and defend it. Under a first-winner policy, cancellation and expiry compete for the Active reservation; the coordinated winner determines the terminal reason. Under a deadline-authoritative policy, any terminal decision at or after ExpiresAt must use the expiry meaning, even if triggered by a user cancellation request. Both can be coherent, but the code and user messages must agree.

Specify the clock observation point and equality rule. The current expiry service treats equality as due, while cancellation does not inspect time. A proposed deadline-authoritative policy changes that cancellation behavior. Document the compatibility effect and ensure background and interactive callers use the same governing rule rather than duplicating inconsistent checks at different entry points.

Avoid describing a worker's delayed processing time as the deadline itself. The reservation can be logically due at ten o'clock even if the worker records completion at ten oh one. Decide whether audit data stores both effective expiry and processing time. Those values answer different questions: what the policy required and when the system performed the transition.

## Worked cancellation and expiry race

Reservation R is Active, expires at 10:00, and owns equipment claim E at version four. A cancellation request and expiry worker both read that state. Under the first-winner proposal, the cancellation transaction commits first, marks R Cancelled, releases only its owned claim, and records the event. The expiry attempt then sees a stale version or terminal state and reports a defined no-op or conflict without releasing a newer claim.

Under the deadline-authoritative proposal, a decision at 10:00 must record Expired regardless of which caller initiated it. The cancellation response can explain that the reservation had reached its deadline. This requires a shared policy at the coordinated decision boundary. A UI disabling a cancel button after the displayed deadline cannot enforce the rule for other callers or clock differences.

Now add a newer reservation R2 after R ends. Equipment becomes Reserved under R2's ownership. A delayed retry of R's terminal operation must not release R2's claim. The ownership/version check is what makes this test discriminating. A design checking only Reserved would pass simple cancellation tests and fail this delayed-retry case.

## Reconciliation should expose uncertainty safely

Create a read-only report categorizing mismatches: Active reservation missing its equipment, Active reservation without its owned claim, terminal reservation still owning a claim, claim without a matching reservation, and multiple active claims for one item if the schema permits that corruption. Include observation time and consistency scope. A report generated from changing records may need confirmation before repair.

For each category, define whether automatic recovery is justified. A terminal reservation still owning an unchanged claim may permit a version-checked release. A claim owned by another reservation does not. Missing historical records may require human investigation or a documented import correction process. The feature should prefer an explicit unresolved state over a confident but unsupported repair.

Operator actions should themselves be auditable commands with authorization, expected version, reason, and result. A dashboard button that directly sets status Available would bypass the ownership discipline you just designed. The runbook should explain what evidence the operator reviews and what happens when the state changes before the repair commits.

## Collection as an optional feature milestone

If you implement collection, define a single coordinated operation that verifies Active reservation, deadline policy, borrower eligibility, capacity, equipment ownership, and the transition to a new loan. It then marks the reservation Collected, changes equipment to CheckedOut, records the loan and receipt, and commits the related event. Reuse the lending capstone's commit reasoning rather than stitching together two public methods without a shared boundary.

Decide whether a student may collect just after expiry when the worker has not yet processed the reservation. Current primitives do not answer that product question. Also decide whether collection consumes the reservation's borrower identity or permits an authorized reassignment. Any reassignment must be explicit; silently using a request-supplied borrower ID can undermine the reservation's purpose.

Collection is not required to finish the base capstone's reserve/cancel/expire/reconcile design. If omitted, label it unimplemented and retain the primitives as source observations. This keeps scope disciplined while ensuring the course does not claim an end-to-end feature merely because a method name suggests it.

## Acceptance cases and evidence boundaries

The core acceptance matrix includes valid reservation, invalid expiry without effects, duplicate operation replay, cancellation of an owned claim, expiry at equality, skipped or rejected mismatched ownership, cancel-versus-expire race, delayed retry after a newer reservation, failed commit, and reconciliation refusal on stale evidence. Each case should inspect reservation state, equipment ownership, audit event, and caller outcome.

A model prototype can exercise these rules deterministically with an in-memory transaction model and controlled gates. A durable implementation requires real storage tests for atomic event and state commits. A restart recovery claim requires restarting a separate process against retained disposable storage. Do not blur those milestones in the final report. The acceptance matrix should identify which observer establishes each claim.

Include an audit-content review. Verify that events contain enough identifiers and timestamps to explain the transition while avoiding unnecessary personal data. Check that a diagnostic correlation value is not treated as the authoritative actor identity. A technically consistent lifecycle can still have misleading audit evidence if those fields are confused.

## Independent capstone challenges

**E21.1: Ownership model.** Define the proposed claim and reservation records, version checks, and terminal event. Show how a delayed cancellation for an old reservation fails to release a newer reservation's equipment. State the exact atomic condition at release.

**E21.2: Deadline decision.** Choose first-winner or deadline-authoritative semantics. Write two user-visible examples at equality and one worker-delay example. Specify the clock observation point and the effective-versus-processing timestamps recorded in the audit event.

**E21.3: Reconciliation runbook.** Design three mismatch categories with automatic, reviewed, and refused actions. Include expected-version protection and a result record. Explain why an operator's authorization does not make stale evidence safe to apply blindly.

## Review checkpoint

The capstone is coherent when ownership, terminal reasons, audit evidence, and repair authority tell the same story. Its implementation claim is only as strong as the executed storage and concurrency evidence. Preserve that distinction while presenting a complete feature design that another engineer could implement and review without inventing missing lifecycle policy.
