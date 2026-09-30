# Worked solutions and review: chapters 01 through 04

These answers are reference arguments, not strings to copy into a submission. An alternative design can be correct when it states a different product policy and enforces that policy consistently. Current-behavior questions are less flexible: their answers must agree with the present source. Keep your attempted answer beside this file and mark the exact sentence where your assumptions diverged.

## E01.1: A defensible claim ledger

A constructor row can say that EquipmentItem rejects a blank name with equipment.name_required before an instance is returned. Its owner is the constructor in Equipment.cs. This does not establish uniqueness of names, and it does not prove that an import process outside this constructor cannot store invalid data. A service row can say that CheckoutService constructs Loan before calling TryCheckOutAsync, so the constructor's invalid-due-date rejection occurs before that equipment transition. The counterexample to a broader safety claim is a subsequent loan-store failure.

A store row can say that the default in-memory equipment GetAsync returns the stored reference. The evidence is the dictionary lookup followed by Task.FromResult(item), with no reconstruction. The limitation is adapter-specific: SQLite GetAsync reconstructs an object from a row. A registration row can say that Program binds IEquipmentStore to singleton InMemoryEquipmentStore. That supports a claim about the current teaching composition, not about every future deployment configuration.

A time row can say that Loan compares DueOn to the UTC date of CheckedOutAt and stores CheckedOutAt in UTC. A due date equal to that UTC date is accepted. It does not promise a campus-local calendar interpretation. An error row can say that inactive borrowers are rejected before active-loan counting in CheckoutService. It does not promise which HTTP status every unrelated infrastructure exception will receive.

A concurrency row can state that InMemoryEquipmentStore.TryCheckOutAsync locks the selected instance around its availability test and transition. State the same-instance and participating-path assumptions. A missing-guarantee row can state that the visible default checkout does not atomically commit equipment and loan across the two stores. The evidence is the ordered separate effects and the absence of a shared commit operation in this path. A reviewer should reward this scoped statement more highly than an unsupported slogan about all repositories or all possible adapters.

## E01.2: Observers with different power

The first test creates equipment, calls UpdateAsync with a new name and category, and checks the returned object's fields. It establishes the service's successful domain mutation and returned result. In the default adapter, an assertion against the original setup variable would also see that change because the variables alias one object. Removing SaveAsync could leave this test green, making it a poor detector of a missing persistence call.

The second test uses the actual SQLite adapter and a database connection kept alive for the experiment. It adds the item, performs the service update through one context, then loads through a separate context. It checks the stored values, not an object still held by the updating service. If SaveAsync is missing, the changed detached domain object alone does not update the database row. This test therefore distinguishes a correct update path from that particular defect.

The existing SQLite adapter test shows how to keep an in-memory SQLite connection open across contexts. That pattern is sufficient for a round-trip test and avoids installing a server. It is not a restart test: closing the owner connection destroys that in-memory database. If your answer claimed disk durability from this setup, revise the claim rather than adding unrelated test machinery to conceal the mismatch.

## E01.3: A stronger transaction argument

CheckoutService reads equipment and borrower, checks activity and capacity, constructs Loan, performs TryCheckOutAsync, and then performs LoanStore.AddAsync. There is no visible shared transaction object passed to both collaborators and no service-level commit call spanning the two effects. In the default adapter, TryCheckOutAsync mutates the shared item immediately. If the following loan add throws before storage, the method exits with checked-out equipment and no recorded loan. That concrete trace demonstrates the relevant partial outcome.

An arbitrary alternative implementation might coordinate its operations through hidden ambient state, so interface inspection alone cannot prove that every possible implementation lacks atomicity. The correct conclusion is about this visible service path and the selected default stores. Good review language states the demonstrated failure case and its assumptions. A search for the word transaction is supporting navigation, not the central proof.

## E02.1: Transition assertions

For Retire, an Available item becomes Retired, while a CheckedOut item throws equipment.retire_unavailable and remains CheckedOut. For Reserve, Available becomes Reserved, while Inspection throws equipment.not_available and remains Inspection. For PassInspection, Inspection becomes Available, while Retired throws equipment.not_in_inspection and remains Retired. These cases cover three different guards and verify that rejected transitions do not silently alter status.

The fourth case should expose a boundary rather than merely repeat the pattern. Direct Return on Retired makes the item Available in current source. Name it as a characterization observation. A proposed stricter Return contract would instead require CheckedOut and reject Retired, but that is a change to implement and review separately. If your test asserted the proposed behavior against current code and failed, it is a valid feature test for an unimplemented change, not evidence that you found a regression in an existing promise.

Field assertions also matter for UpdateDetails. On a retired item, the method throws before checking or assigning the supplied name and category. Verify both fields remain unchanged when a test's purpose is mutation safety. Merely checking status can miss an implementation that changes the name and then throws while leaving status Retired.

## E02.2: Owning a relationship

A reasonable system rule is that every checked-out item has exactly one matching active loan, and every active loan refers to checked-out equipment. Current constructors cannot establish this bidirectional relationship. You can construct CheckedOut equipment with no loan, or construct a Loan referring to an equipment ID that is absent from the equipment store. Both constructions can satisfy their local rules.

A proposed lending transaction service can own the relationship if all lending transitions go through it and both records commit consistently. It must cover checkout and return, define imports or reconstruction, and reject or reconcile inconsistent stored state. Adding LoanId to equipment does not by itself solve the problem: two stores can still disagree, and the new field introduces another value to keep synchronized. A stronger aggregate design may help, but the answer must describe how storage preserves its boundary.

There is also a product nuance. Reservations and inspections can alter equipment availability without creating loans. Your invariant should apply to CheckedOut rather than to every unavailable state. Overbroad wording such as "unavailable items always have active loans" would incorrectly classify a normal Reserved item as corrupt. Precise state predicates prevent an integrity check from generating false alarms.

## E02.3: The identity of the lock

One valid schedule begins with dictionary key E pointing to object O1. Operation A obtains O1. Another operation saves a distinct object O2 under E, replacing the dictionary value. Operation B obtains O2. A locks O1 while B locks O2; these locks do not exclude each other because the lock identities differ. Both objects can have the same GUID without sharing a monitor.

This schedule is an abstraction stress test, not a claim that an ordinary endpoint necessarily performs those exact replacements concurrently. It exposes the assumption behind the simpler same-item proof: the participants must coordinate on the same stable object and use the same protected path. A proposed lock keyed by E can establish a stable authority if all relevant writers participate and the lock registry does not create duplicate live locks for that key. Cross-process operation still needs another coordination mechanism.

## E03.1: Unique enrollment and retries

One coherent proposal normalizes CollegeId once using the established trimming and uppercase rule, stores the normalized value, and enforces uniqueness across all borrower records. Re-enrollment updates or reactivates a retained identity through an explicit workflow rather than silently creating another person record. This is a product choice; a policy allowing historical duplicates would need a different key or filtered uniqueness rule.

The sequential duplicate request should return a defined conflict or the existing result if it is recognized as an idempotent retry. The simultaneous duplicate case requires atomic enforcement at the storage boundary. A prior lookup followed by an unconstrained insert is insufficient because both requests can observe absence. In a durable implementation, a unique constraint can arbitrate concurrent inserts, after which the application maps the resulting conflict to its chosen response contract.

For an unknown result, the client should retry using a stable enrollment operation identifier or another explicitly defined idempotency contract. Matching CollegeId alone may be insufficient when requests differ in other data or when the identity has a history. The solution should avoid leaking another person's details merely to help a caller resolve a duplicate. That is an authorization concern to review alongside the uniqueness mechanism, not an automatic consequence of normalization.

## E03.2: Deactivation and decision time

Under a decision-time proposal, checkout eligibility is evaluated when the service checks IsActive. A deactivation that occurs later does not revoke that already accepted decision. This can simplify the workflow, but the policy must explain the user-visible timing and may need an operation receipt showing when eligibility was decided. The current code alone does not define such a durable receipt.

Under a commit-time proposal, deactivation must coordinate with checkout so that an inactive borrower cannot acquire a new loan after the governing boundary. A borrower-scoped lock can demonstrate the rule in one process if both operations use it. A database transaction with an appropriate concurrency check can support a durable version. Merely re-reading activity before the write still leaves a gap between the read and the write unless the read participates in the coordinating mechanism.

The exercise does not require choosing the stricter policy. It requires recognizing that "check again" is not a complete synchronization design. A high-quality answer describes which operation wins when checkout and deactivation overlap, and gives a deterministic schedule that the implementation must handle.

## E03.3: Capacity proof and counterexample

For serialized successful checkouts, assume the initial active count is at most the maximum and each checkout adds exactly one active loan only after the policy accepts. Acceptance means count is strictly less than maximum, so adding one leaves count at most maximum. A following request observes the updated set. This simple inductive argument depends on serialization and on the count accurately representing the relevant active loans.

For the concurrent counterexample, start at two with maximum three. Requests A and B each observe two, pass the predicate, claim different items, and add a loan. The final count is four. Protecting the two individual items does not coordinate the shared capacity. The contested resource is the borrower's available lending capacity across all those items. A corrected design must also consider returns or other operations that change the counted set, particularly if it uses a maintained counter rather than a fresh query.

## E04.1: Boundary values and postconditions

With a fixed reservedAt of 10:00 UTC, expiry at 09:59 is rejected, expiry at 10:00 is rejected, and expiry at 10:01 is accepted when the other preconditions hold. The two rejected cases produce reservation.expiry_invalid before equipment mutation in current source. Equipment remains Available and the reservation store remains empty. The accepted case produces an Active reservation, Reserved equipment, and one stored reservation after normal completion.

Equality matters because the constructor uses less-than-or-equal rejection. A test only for a day-old expiry would also pass if an incorrect implementation allowed equality. Boundary tests discriminate among plausible implementations. The three-case specification should keep equipment, borrower, and clock identical so that the changing expiry is the cause of the different result.

## E04.2: Guard precedence

With an inactive borrower and invalid expiry, current ReserveAsync throws borrower.inactive because it checks activity before constructing Reservation. With an unavailable equipment item, active borrower, and invalid expiry, it throws reservation.expiry_invalid because constructing Reservation precedes calling equipment.Reserve. Loading equipment does not itself validate availability.

This ordering can be observable, but the exercise does not establish it as an eternal public compatibility promise. If a product requires stable error precedence, document and test that requirement explicitly. Otherwise, service refactoring may legitimately change which of several simultaneous invalid conditions is reported first. In either case, a failure should preserve the state appropriate to the chosen boundary.

## E04.3: The failed addition

The throwing reservation store accepts the interface's methods but makes AddAsync throw before storing R. All setup and input are valid. ReserveAsync changes E to Reserved and successfully saves E before calling that failing method. The caught designated exception is therefore accompanied by Reserved equipment and no new reservation. Both the original reference and an in-memory GetAsync observe the same mutated object.

That agreement establishes a same-process state observation. It is not evidence of disk persistence or a restart guarantee. To distinguish a snapshot adapter, retrieve a fresh object before mutation, mutate it without saving, and then perform an independent lookup. The live adapter exposes the mutation; a genuine detached snapshot adapter should continue to expose its stored prior state until a save succeeds. State which implementation your experiment uses and avoid giving a wrapper model the authority of a real database test.

## E04.4: Compensation needs ownership

Consider a proposed concurrent workflow in which operation A reserves equipment and then its reservation addition fails. Before A's catch block runs, operation B observes the Reserved item and legitimately moves it to Inspection under the current local inspection-entry rule. If A blindly calls Return, the item becomes Available and B's inspection state is lost. The example shows why "restore the old status" is not generally a safe inverse once other writers can intervene.

A safer compensation proposal needs evidence that the state still belongs to A, such as an expected version and operation identifier checked atomically with the reversal. If that evidence no longer matches, compensation should report a conflict or schedule reconciliation rather than overwrite newer state. This still leaves the question of durable recovery after a crash; an in-memory catch block cannot run after its process disappears.

## Review rubric and completion evidence

Score each chapter out of ten. Award two points for accurate source grounding, two for an explicit state trace, two for assertions that distinguish plausible defects, two for separating current behavior from proposals, and two for a specific limitation. An answer that falsely claims multi-store atomicity or restart durability cannot receive the source-grounding or limitation points, even if its code formatting is excellent.

For installment completion, retain your eight-row claim ledger, equipment transition matrix, borrower-capacity schedule, and reservation failure frontier. Add the disposable lab's before and after output if you executed it, including whether you used the unchanged baseline harness or extended it. Mark unexecuted tests as designs. A peer reviewer should be able to reproduce your reasoning from these artifacts without relying on your confidence or memory of the session.

The most valuable revision is often a narrower claim supported by better evidence. Replacing "thread safe" with "two participating same-instance checkout calls cannot both win this transition" is progress because it identifies both the achieved property and the next missing coordination boundary. The later course chapters will expand those boundaries deliberately, using these artifacts as the starting point.
