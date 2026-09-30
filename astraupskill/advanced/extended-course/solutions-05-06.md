# Worked solutions and review: checkout and return

## E05.1: Candidates, claims, and records

Both competing requests may construct valid Loan objects with distinct IDs. Those objects are candidates: the service has not yet added either to ILoanStore. If only one conditional equipment checkout succeeds, only that caller proceeds to AddAsync. The losing candidate becomes unreachable when its call fails unless another observer has deliberately recorded it. Constructing it early is useful because its validation occurs before the equipment effect.

A proposed recovery design might store a separate pending operation before attempting the claim. That record must distinguish intent from completion and carry enough identity to resume or reconcile safely. Reusing the ordinary active Loan representation for pending work without an explicit status would make reports and capacity queries count an uncompleted checkout as a completed loan. The design should decide whether pending work reserves capacity and encode that policy deliberately.

## E05.2: An update followed by a failed read

An instrumented SQLite adapter experiment can arrange for ExecuteUpdateAsync to affect the available row and then fail at the following GetAsync. At the server-side sequence level, the conditional update has completed. The method still fails to return a successful equipment result because its read did not complete. The caller's exception alone does not prove the earlier update was rolled back.

A fresh read may reveal CheckedOut status, and a repeated conditional update may affect zero rows. Without operation ownership data, those observations do not identify whether this request or another request caused the status. The experiment therefore demonstrates an ambiguous response boundary, not a complete recovery mechanism. Program currently selects InMemoryEquipmentStore, so the result must be described as an adapter-specific investigation.

The exercise can remain a test design if the disposable harness does not include SQLite dependencies. Mark it unexecuted rather than modifying the lab to download packages merely to claim a passing result. A correct written fault specification is useful evidence of analysis, but it is not execution evidence.

## E05.3: Receipt semantics

A first successful request should produce a completed receipt naming the committed loan. An identical retry should return that receipt under the proposed contract. Conflicting reuse of the same operation ID should reject without changing the original result. A pending receipt should communicate that completion is unresolved and provide a defined query or retry path. An expired receipt requires an explicit retention policy; treating every old request as new can duplicate a previously completed operation.

The receipt's completed state must agree with the loan and equipment relationship. If all three are in one durable database transaction, the implementation can argue that the successful commit makes them visible together under its chosen isolation model. If they are separate, the design needs a protocol and recovery owner. A receipt written last without coordination leaves a window in which business effects exist but the receipt does not.

## E06.1: The failure matrix

A missing loan produces loan.not_found before any equipment lookup or mutation. An inactive loan produces loan.already_returned before equipment lookup. A missing equipment item produces equipment.not_found after the active-loan check. An equipment item not in CheckedOut produces equipment.not_checked_out before either domain mutation. An early returnedAt reaches Loan.Return and produces loan.return_time_invalid before ReturnedAt changes and before equipment.Return.

For those rejection cases, no new mutation is performed by ReturnService. That statement does not claim the initial data was consistent: the missing-equipment and wrong-status cases may begin with an already inconsistent relationship. Your matrix should preserve the initial state rather than describing every row as healthy.

For the specified live-reference save-failure experiment, both loan and equipment have changed before the wrapper throws. ReturnedAt contains the valid instant, IsActive is false, and equipment is Available. A later return attempt observes the inactive loan and can reject immediately. The counter confirms that the intended save boundary was reached, but a passing counter assertion alone would not expose the state consequences.

## E06.2: One explicit durable proposal

One coherent proposal uses a lending unit of work with equipment and loan rows in the same database. The operation loads the relevant state, validates the return, changes both records, and commits one transaction. A concurrency mechanism prevents an overlapping return or incompatible equipment transition from silently overwriting the decision. The operation also stores a stable completion receipt in that commit if the retry contract requires replaying the successful result.

Failure before commit should leave the durable prior state under the transaction's guarantees. A lost response after commit should be resolved by querying the receipt using the same operation identity. This requires actual database and adapter tests before it becomes an implementation claim. Drawing a transaction box is a design artifact; it does not establish that the selected database isolation level or entity mappings enforce the intended behavior.

An answer proposing separate SaveAsync calls can still be useful if it accurately labels the remaining partial-write gap. It should not receive full invariant-preservation credit until it adds a shared commit or a justified recovery protocol. The objective is coherent reasoning, not mandatory adoption of one architectural pattern.

## E06.3: A correction is a new event

Under a proposed idempotent contract, a repeated operation with identical data replays the original outcome. Reuse with different data is rejected as a conflict. An authorized correction is a distinct operation referencing the original return and recording the previous and corrected values, the actor, and the reason under an appropriate audit policy. Its authorization and validation deserve their own tests.

Current BorrowBox does not implement that correction history or idempotent replay. It rejects a second return because the loan is inactive. Your feature design should explain how existing clients experience any change and how historical records remain interpretable. Quietly changing ReturnedAt when a duplicate request arrives would conflate retries with edits and could corrupt lending reports.

## Review rubric

Award four points for an exact current call-order and state explanation, three for a proposed design whose failure and retry behavior is explicit, and three for evidence discipline. Evidence discipline means distinguishing a code reading, an executed in-memory check, a database test design, and a deployment guarantee. Deduct those points when a solution claims the default API uses SQLite or treats a test double as proof of the real adapter's complete behavior.

A strong submission includes one losing-checkout trace, one failed-loan-add trace, one failed-return-save trace, and a receipt state table. It also states which experiments were actually executed. Preserve those artifacts because later chapters on cancellation and fault injection will reuse their boundaries without assuming that a canceled call or an exception reversed earlier effects.
