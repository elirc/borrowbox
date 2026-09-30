# Worked solutions and review: the final route

The final solutions connect the earlier source observations to test design and bounded feature proposals. They are reference answers for the workbook, not an implemented durable lending system. A learner who builds a capstone must report their own executed evidence and retain the distinction between model behavior, real-adapter behavior, and deployment guarantees.

## Chapter 15: discriminating tests

For E15.1, the invalid-due-date equipment assertion catches candidate construction moved after checkout. The empty-loan assertion catches insertion despite rejection. The return timestamp assertion catches a mutation before validating chronological order. The reservation mismatch assertion catches Cancel being called before checking equipment. The SQLite fresh-context name assertion catches a missing persistence write. A test that checks only a returned in-memory object can miss the last defect, and a sequential capacity test can miss two requests observing the same count.

The additional evidence should match those survivors. Use a real adapter round trip for the missing save and a controlled two-read schedule for capacity. Do not add dozens of identical domain cases to address a persistence or concurrency gap. The lesson is evidence allocation, not maximizing assertion count indiscriminately.

For E15.2, create a loan with a fixed checkout instant, a distinct equipment ID and borrower ID, and CheckedOut equipment. Early return asserts the specific rule, null ReturnedAt, active loan, and unchanged equipment. Duplicate return first completes successfully and then checks loan.already_returned while preserving the first accepted timestamp and Available equipment. The fixture bypasses checkout and borrower registration, so it proves return behavior from the stated initial state rather than the complete lending journey.

For E15.3, put constructor normalization and timestamp comparisons at the domain layer; orchestration order and partial-state faults at the service layer; uniqueness and saved round trips at the adapter layer; binding, authorization, and response projection at the API layer; and restart outcome discovery at the operational boundary. A constructor blank-name test through a full host may be unnecessarily expensive, while a service-only authorization test cannot prove a route policy it never executes.

## Chapter 16: concurrency scope

For E16.1, checkout touches equipment, borrower eligibility, and the borrower's active-loan set. Return touches loan and equipment. Reserve touches borrower, equipment, and reservation. Cancel and expire touch reservation and equipment. Inspection completion touches inspection and equipment. Two different-item checkouts for one borrower require capacity coordination beyond item locks. Cancel and expire require a coordinated terminal decision over the reservation relationship, not merely independent equipment transitions.

For E16.2, both operations read Active at version V and pause. Release the chosen winner to commit its terminal state, owned equipment release, and event. Release the loser and assert its expected-version conflict or documented terminal no-op. Under first-winner policy the winner's reason remains. Under deadline-authoritative policy, the coordinated decision at equality must use expiry semantics even if initiated by cancellation. A newer reservation's claim must remain untouched by any delayed loser retry.

For E16.3, a static borrower lock registry can coordinate participating callers in one process when they acquire the same stable handle before capacity observation and hold it through the relevant mutation. All capacity-changing writers must obey the protocol. Entry cleanup must avoid removing a handle while another caller still uses it and creating a second live handle for the same key. It cannot coordinate another process and does not provide persistence atomicity. A bounded teaching implementation may retain entries for its small fixture rather than claim a production cache lifecycle it has not solved.

## Chapter 17: cancellation outcomes

For E17.1, a pre-canceled checkout stops at the first real store check before new business effects. Cancellation observed before loan addition can follow a successful equipment claim, leaving CheckedOut equipment and no inserted loan in the designated before-add case. Cancellation before return save occurs after both live objects mutate, so the loan may already be inactive and equipment Available. In the second expiry item, cancellation before lookup leaves that item untouched, while cancellation before save can follow its domain mutations. The seam is part of the expected result.

For E17.2, a canceled waiter should leave the current holder unaffected and should not consume an acquisition that blocks later callers. A canceled holder must release its local handle through the coordinator's cleanup path. A later request with a bounded wait demonstrates that the handle was not leaked. Separate tests inspect whether any business effects occurred before cancellation; successful resource cleanup does not imply business rollback.

For E17.3, the proposed client retains its operation ID after interruption and queries or retries that operation. Completed returns the original receipt, pending explains that resolution is still in progress, and unresolved provides a reviewed escalation path. The client does not silently generate a new identity for the same intent. Current BorrowBox has no such receipt query, so the workbook can explain the limitation but cannot claim that the existing API already resolves every canceled request.

## Chapter 18: read contracts

For E18.1, blank filters are disabled, while nonblank surrounding spaces remain part of the comparison input. Query and category comparisons are case-insensitive as implemented. Duplicate tags can exist in the in-memory adapter, making a tie-breaker relevant to a proposed stable paging contract. Invalid page size rejects before listing. An extreme page value requires a bounded arithmetic observation and an explicit proposed policy; do not manufacture a huge dataset when the issue is the offset expression.

For E18.2, a best-effort summary may document that counts reflect one collection membership observation with mutable member values read during aggregation. A stronger snapshot proposal captures the relevant scalar fields under a coordinated read boundary and groups that snapshot. Its test should control a concurrent update and verify that the result corresponds entirely to one permitted observation state. Merely returning immutable dictionaries after separate mutable reads does not establish the stronger property.

For E18.3, delimiter tests cover comma, quote, carriage return, and newline using a parser or exact expected escaping. A multiline quoted field can span physical lines without becoming multiple logical rows. Spreadsheet interpretation requires a separate consumer policy for formula-like values. Decide whether the export is intended for raw data interchange or interactive spreadsheet opening, then test the chosen treatment. The current Escape method only performs structural CSV quoting.

## Chapter 19: laboratory review

For E19.1, the generator should require an explicit new external output, validate source anchors before mutation, copy current compatible source, record provenance, isolate build configuration, and classify expected outcomes. The existing generator provides concrete mechanisms for these properties. A proposed improvement might record a suite identifier in a separate run artifact; it should not quietly add destructive cleanup or network restoration to the workflow.

For E19.2, an after-add suite can use the copied reservation service and a wrapper around an in-memory reservation store. The wrapper delegates insertion and then throws a designated exception. The expected observation is a failed call with a stored reservation and Reserved equipment, distinct from a before-add failure with no reservation. The suite should use a separate template or clearly named extension and record any additional copied source in the map.

For E19.3, generator refusal establishes a destination guard. Compile failure establishes a setup or compatibility problem. Intended starter failures establish that the injected defect reaches a discriminating assertion. Passing limitation observations reproduce current behavior. Passing proposed regression checks establish only the implemented experiment's declared scope. A good run record never collapses these five outcomes into a single word such as successful.

## Chapter 20: complete lending reference design

For E20.1, one coherent design uses a stable operation key scoped to an authenticated actor or institution, a canonical payload for equivalence checking, a borrower capacity authority, an equipment claim, a loan, and a receipt. The completed receipt is written in the same proposed transaction as the business result. A conflicting operation key rejects without altering the previous outcome. Actor authorization is evaluated separately from borrower activity and capacity.

The checkout decision sequence is: resolve existing operation outcome; reject conflicting reuse; establish the coordinated transaction; verify actor permission, active borrower, capacity, due date, and equipment availability; claim equipment; insert the loan; insert the completed receipt; commit; return the receipt. An implementation may arrange safe reads differently, but all state-dependent decisions must be protected by the chosen mechanism. If receipt lookup occurs before the transaction, concurrent first requests still require an atomic uniqueness or claim mechanism inside it.

A suitable rejected alternative is a receipt inserted only after an independently committed loan write. A crash between those writes leaves the business effect without the lookup record needed for retry. Another rejected alternative is item-only locking, which preserves same-item exclusivity but allows the last-capacity-slot race across different items. These are concrete failures of the proposed invariant, not abstract objections to a pattern name.

For return, load and coordinate the active loan and its equipment, validate the governing timestamp and lifecycle policy, update both, record the return receipt, and commit. An identical retry returns the receipt. Different payload under the same operation identity conflicts. A mismatched equipment state must follow a defined rejection or reconciliation policy; unconditional release is not acceptable because it can erase inspection or newer ownership.

For E20.2, invalid date and inactive borrower leave all business records and completed receipts unchanged. Same-item competition yields one committed loan. Last-slot competition yields at most the permitted active count while the losing item remains unclaimed. Pre-commit failure leaves the durable prior state. Lost-response retry yields the original loan ID. Conflicting reuse leaves the original receipt intact. Return tests verify both loan and equipment through the claimed persistence boundary. A model can simulate these outcomes, but the real-adapter suite must verify the transaction implementation rather than assume it.

For E20.3, the operator first uses the retained operation ID to resolve the interrupted request. A completed receipt confirms the recorded loan and equipment. A pending result calls for the documented bounded wait or retry path. A conflicting payload indicates accidental operation-ID reuse and requires correction of the request, not a blind retry. An unresolved outcome goes to reconciliation with the operation identity and trace evidence. The runbook never advises creating repeated new checkout commands until one succeeds.

## Chapter 21: complete reservation reference design

For E21.1, equipment stores a claim owner R and version V, while reservation R stores Active status and its own version. Cancellation or expiry conditionally transitions R and releases only the equipment claim still owned by R at the expected version, in one proposed transaction. The audit event records the terminal reason and operation identity in that commit. A delayed cancellation for old R cannot release a claim owned by R2 because the ownership predicate fails.

For E21.2, choose deadline-authoritative semantics as one reference answer. At exact expiry, a user cancellation produces an Expired outcome; before expiry it can produce Cancelled. If the worker runs later, the event records the effective deadline and the later processing instant separately. A first-winner design is also acceptable when documented consistently, but it must not claim that a late cancellation can never win. The policy must live at the shared coordinated boundary, not only in a browser or endpoint.

For E21.3, a terminal reservation still owning an unchanged equipment claim can permit an automatic version-checked release if the model proves that ownership. An Active reservation without its expected claim may require reviewed reconciliation because several histories are possible. A repair based on a stale version must be refused and refreshed, even when the operator has authority. Authorization permits the action category; it does not make outdated evidence current.

The audit event should preserve reservation ID, equipment ID, verified actor or system authority, operation ID, reason, effective time, processing time where needed, and resulting version. It need not contain a full borrower profile. An operator repair is another audited command with expected-state protection. Collection remains a separately declared milestone unless implemented with capacity, loan creation, and shared ownership transition; the presence of two Collect methods is not evidence of that full feature.

## Chapter 22: handoff and completion

For E22.1, a strong index includes constructor normalization, checkout validation order, live-reference ownership, loan response projection, demo role trust, UTC due-date semantics, borrower-capacity race, cancellation after mutation, report grouping comparisons, and non-durable readiness. Each row names its evidence category and limit. The executed lab rows link to their exact suite and result, while source-inspected API tests remain labeled as inspected unless run.

For E22.2, useful peer challenges include a failed loan insertion after checkout, a return save failure, expiry racing with cancellation, and a response lost after commit in the proposed capstone. The revised explanation should predict both caller information and stored state. If a challenge reveals an unstated product policy, record and resolve it explicitly rather than hiding it behind a technical term.

For E22.3, a sensible small change is candidate-first inspection validation with focused regression checks. A medium change is explicit inspection response projection with compatibility tests. A larger change is durable lending receipts with schema, coordination, identity, and recovery evidence. Optional notification delivery or collection can be deferred because they are separate features, not missing pages in the completed instructional route.

## Final scoring and evidence standard

Score the complete workbook submission across five dimensions: accurate current behavior, discriminating experiments, coherent proposed invariants, reproducible artifacts, and honest evidence scope. Award equal weight to each. A submission that invents shipped durability or borrower ownership cannot receive full accuracy or scope credit regardless of its code volume.

For capstone review, require a command contract, state model, successful trace, failure trace, concurrency case, retry case, and limitation statement. Executed implementation evidence is scored at its actual boundary. A complete design-only submission remains a design submission, while a durable prototype must show real persistence tests. The distinction is the central professional habit this course is intended to develop.
