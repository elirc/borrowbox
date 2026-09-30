# Worked solutions and review: chapters 07 through 14

These solutions focus on the reasoning that distinguishes a defensible answer from a plausible shortcut. Each exercise can admit additional valid examples. Keep source observations separate from proposed policies, and mark a test design unexecuted until you have actually run it in a suitable disposable environment.

## Chapter 07: temporal contracts

For E07.1, January 2 at 00:30 plus two hours is January 1 at 22:30 UTC. DueOn January 1 is accepted and December 31 is rejected. January 1 at 23:30 minus two hours is January 2 at 01:30 UTC. DueOn January 2 is accepted and January 1 is rejected. These four cases exercise two offset directions and both outcomes. A campus-local rule might classify the same dates differently, so the proposed timezone must be named rather than inferred from the machine running the test.

For E07.2, use an ordered list with R1 expiring at 10:00 and R2 at 10:01. The fake clock first returns 10:00 and would return 10:02 on a later read. Current ExpireDueAsync captures the first instant and expires only R1, assuming both equipment items are Reserved. A per-item clock policy could expire both. The test should assert each reservation and equipment state, not merely the clock-read counter. If the fake's meaning depends on which item is visited first, the ordered store is an explicit experimental condition.

For E07.3, a defensible current statement is that the worker waits on a one-minute periodic timer and then awaits a sweep using the stopping token. It does not promise completion within one minute of every deadline. A proposed operational objective could measure the delay from eligible expiry to completed release for healthy, matching records, with a separately reported mismatch backlog. The objective needs observed timestamps, sweep failures, skipped categories, and workload context. A configured interval alone is not an operational measurement.

## Chapter 08: terminal decisions and skipped work

For E08.1, an Active due reservation with Reserved equipment expires and contributes one. A future reservation remains Active and contributes zero. A Cancelled reservation is excluded even if its expiry is old. An Active due reservation with missing equipment is skipped. An Active due reservation with Available equipment is skipped. An Active reservation expiring exactly at now with Reserved equipment expires and contributes one. The normal total is two, with only the two matching items released. A successful return value does not indicate that the skipped due cases were repaired.

For E08.2, one policy can let whichever coordinated terminal command wins first determine the reason. Another can define expiry as authoritative at or after ExpiresAt, converting a late cancellation request into an expiry outcome or rejecting it with an explicit result. Each policy needs a shared decision boundary over reservation status and the governing time observation. Checking only in the HTTP endpoint leaves the background worker and direct service callers outside the rule. A versioned or transactional decision can enforce the chosen policy when all writers participate.

For E08.3, a report row can contain reservation ID, equipment ID, reservation status and deadline, observed equipment status, observation instant, and mismatch category. Active-plus-Available might follow a partial release, an inconsistent import, or a newer workflow that changed equipment without updating this record. The report alone does not tell which history is true. An automatic repair needs operation ownership, a trustworthy event history, or another specific authority. Without it, preserve the evidence and request a reviewed reconciliation decision rather than inventing a terminal reason.

## Chapter 09: inspection ordering

For E09.1, a valid report moves equipment to Inspection, constructs a local Inspection, saves equipment, and inserts the inspection. Blank damage moves equipment first and then fails construction, leaving no inspection in the default store. A before-save failure with valid damage leaves the live equipment mutated and a local candidate that was never inserted. A before-add failure occurs after equipment save and likewise leaves no stored inspection. The difference between local candidate existence and stored record existence must appear in the ledger.

For E09.2, one coherent proposal keeps equipment unavailable after Repair and creates a repair work order tied to the closed inspection. The work order records its own status, responsible actor, and completion evidence. A later repair-completion command can move equipment according to a defined policy, perhaps requiring another inspection. Current code creates none of that work-order state. An answer that says "Repair reopens the inspection" contradicts the actual Complete method, which sets CompletedAt for every accepted outcome.

For E09.3, current completion checks open status and nonblank notes, then mutates inspection; equipment checks occur afterward for Passed and Retired. Chronology and named-outcome validation are proposed additions. A pure combined decision can validate the selected transition and all desired preconditions before mutating either object. Applying the decision later still needs coordination if other writers can change those preconditions. This distinction prevents a local validation repair from being falsely presented as a concurrency solution.

## Chapter 10: adapter substitution

For E10.1, missing GetAsync returns null in both inspected adapters. In-memory reads expose stored references, while SQLite reconstructs new domain objects. In-memory SaveAsync assigns a dictionary key and can create a missing entry; SQLite SaveAsync uses SingleAsync and expects a row. Duplicate IDs replace values through the in-memory assignment, while database insertion is constrained by the primary key. Duplicate normalized tags are not rejected by the in-memory dictionary but conflict with the SQLite model's unique index. Conditional checkout is implemented through an item lock in one adapter and a status-filtered update in the other.

For E10.2, use two contexts to read Available equipment with original details. Caller A changes the name and saves. Caller B changes status on its stale domain object and saves afterward. Because SaveAsync assigns Name, Category, and Status from that object, B's save can restore stale details while applying its intended status. A proposed version field would make B's save conditional on the version it read; a mismatch should produce a defined concurrency conflict rather than silently overwrite A. Execute the actual adapter experiment before reporting a database result.

For E10.3, a status rename needs a translation plan for stored strings and public responses. A rolling deployment may temporarily have old and new readers, so writing only the new name can break an old reader. One option preserves a stable storage value while renaming internal symbols; another stages readers that accept both values before migrating rows and writers. Rollback must consider whether the old binary can read the new data. Compilation verifies symbol references, not data compatibility.

## Chapter 11: public contracts

For E11.1, equipment item GET returns either NotFound or an EquipmentResponse. Checkout returns Created with LoanResponse; return returns Ok with the same response type. Reservation creation returns Created with ReservationResponse. Inspection completion returns Ok with the domain Inspection. Service-thrown BorrowBoxException is handled as 422 with a code extension, while direct NotFound results follow their endpoint branches. Request-binding failures need request-level evidence; the custom handler alone does not establish their exact response format.

For E11.2, a proposed InspectionResponse can explicitly choose Id, EquipmentId, damage, opened/completed instants, outcome, and notes according to product needs. Consider whether every caller should see all notes before including them. A shape test can compare the response's property set against the intended contract, while a mapping-value test checks representative values and null states. The migration should account for existing clients that consume the directly serialized object. Explicit projection improves control but does not erase compatibility obligations.

For E11.3, audit the emitted location against the full mapped route set. If the resource lacks a GET, propose one with a response model and ownership policy or document the more limited identifier semantics. A create-follow-location test should first create a valid resource, capture the actual Location header, then read with the intended principal. A passing creation test alone verifies neither the follow-up route nor permission to use it.

## Chapter 12: identity and authority

For E12.1, the existing retirement tests establish absent identity as Unauthorized and Student as Forbidden. An invalid demo role should be tested through the configured host rather than assumed equivalent to a domain validation error. A Staff success test needs a real Available item and should verify retirement state. A Student checkout targeting another borrower passes the current route's general authentication gate; the service has no principal-to-borrower ownership comparison. Under the proposed self-service policy, that mismatch should reject before lending effects.

For E12.2, a trusted actor context can contain a verified subject ID, effective permissions, and a resolved borrower association when applicable. It is created from validated identity evidence, not from a request-body Staff flag. The application boundary then decides whether the actor may target the supplied borrower. Missing mapping is an authorization or enrollment-resolution condition, while an inactive borrower is a separate business eligibility rejection. Staff delegation can be explicit without pretending the staff subject is the borrower.

For E12.3, an audit event can identify the verified actor, target borrower ID, equipment ID, loan ID, operation ID, completion instant, and result. Server trace and caller correlation support diagnosis but have different authority. The caller correlation value is not proof of identity or uniqueness. A personal address or full borrower profile is unnecessary for many checkout audit questions and can be omitted. An audit design should explain access and retention rather than collecting every available field by default.

## Chapter 13: commit and recovery

For E13.1, a proposed checkout commit may include equipment claim ownership, borrower capacity authority, loan, and completed receipt. Return includes loan return state, the appropriate equipment transition, and receipt if retry replay is promised. Reservation creation includes the reservation and its owned equipment claim. Current separate store methods do not expose one shared commit over these sets. Adding persistence methods without defining coordination makes writes explicit but does not alone enforce the cross-record invariant.

For E13.2, the server commits records and receipt, then loses the connection before returning. The client retries with the same operation ID and payload. The server finds the completed receipt, verifies payload equivalence, and returns the recorded result. Different payload under the same ID produces a conflict. A newly generated ID on every retry prevents the server from recognizing the same logical operation, so it cannot provide the proposed replay semantics from that identifier alone.

For E13.3, compensation should release only when equipment still records the failed operation as owner and its expected version matches. The check and release must be atomic under the chosen mechanism. A newer owner or version mismatch causes refusal and a reconciliation record. Durable operation state lets another process continue recovery after restart. A catch block with an unconditional Return call lacks both ownership protection and restart evidence.

## Chapter 14: experimental discipline

For E14.1, useful named faults include checkout loan-add-before-write, checkout loan-add-after-write, return equipment-save-before-delegate, reservation add-before-write, inspection blank-damage-after-marking, and inspection Passed-after-record-completion-with-mismatched-equipment. Each has a different prefix and observation. The last two demonstrate that even a domain error can occur after an earlier mutation; classifying every domain exception as a clean rejection would be incorrect.

For E14.2, cloning on GetAsync changes ownership. A before-save failure no longer necessarily leaves the stored equipment mutated, even though the local object changed. Keep that wrapper only if the experiment explicitly models detached ownership. To study the default adapter, delegate GetAsync faithfully and preserve live aliases. The test's title and limitation should make the distinction visible to a reader who never inspects the wrapper implementation.

For E14.3, a wrapper can cancel the token immediately before forwarding the next store call after equipment.Reserve. The real in-memory store checks cancellation at method entry, so the caller sees cancellation while the prior live equipment mutation remains. A durable adapter experiment must separately establish whether any database operation committed before cancellation was observed. Cancellation communicates an interrupted request; it is not evidence that all previous effects were undone.

## Review rubric for this installment

Score the eight chapters independently out of ten: three points for exact source behavior, two for discriminating examples, two for a coherent proposed boundary, two for evidence limitations, and one for clear artifacts another learner can reproduce. A polished answer loses source points when it invents borrower ownership enforcement, a shipped collection workflow, transaction support, or a repair work order.

Keep a temporal table, expiry ledger, inspection failure trace, adapter contract matrix, endpoint inventory, authorization matrix, commit-set diagram, and fault catalogue. These are different artifacts because they answer different engineering questions. The next installment will use them to design stronger tests and capstones, rather than asking you to repeat the same general warning about side effects.
