# 06. Return and the missing save contract

Returning equipment appears to reverse checkout, but the implementation has its own rules and failure order. It changes an existing loan and an existing equipment item. The loan-store interface has no SaveAsync method, while the equipment-store interface does. This asymmetry is easy to overlook when every test uses shared in-memory objects. It is a productive place to study how an abstraction can depend on hidden ownership behavior.

Read [Loan.Return and ReturnService](../../../src/BorrowBox.Core/Loans/Loan.cs), [the in-memory loan store](../../../src/BorrowBox.Infrastructure/Loans/InMemoryLoanStore.cs), and [return tests within CheckoutTests](../../../tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs). Use the equipment ownership model from chapter two. This chapter does not claim that a durable loan adapter already exists; designing one is an exercise that exposes the current contract's limits.

## Two entry points for time

ReturnService has an overload accepting a loan ID and cancellation token. It reads the selected clock and delegates to an overload accepting an explicit returnedAt timestamp. The explicit overload is useful for controlled tests and for a caller with a defined return instant. Both paths then follow the same state validation and mutation sequence.

Loan.Return checks that the loan remains active and that returnedAt is not before CheckedOutAt. It stores the accepted return instant in UTC. Equality with checkout time is allowed by this comparison. IsActive is derived from ReturnedAt being null rather than maintained as a separate flag, so those two properties cannot drift independently inside this object through the public API.

That representation is a useful local simplification. It still does not prove that equipment is available, that the borrower has been notified, or that any external ledger has recorded the return. Derived properties reduce one kind of local inconsistency; they do not coordinate other objects automatically. Keep the scope of the benefit precise.

## The service guard sequence

The explicit return method loads the loan or throws loan.not_found. It rejects an already returned loan before looking up equipment. It loads equipment by the loan's EquipmentId, rejects missing equipment, and requires CheckedOut status. Only then does it call loan.Return(returnedAt), equipment.Return(), and equipmentStore.SaveAsync. The return value is the mutated loan.

The loan's time validation occurs before equipment.Return. Therefore, a return timestamp earlier than checkout leaves both entities unchanged in current source. The existing ReturnBeforeCheckout test checks active status, null ReturnedAt, and CheckedOut equipment. Those three assertions make the validation-order property stronger than an assertion that merely expects an exception.

The equipment guard belongs to ReturnService because EquipmentItem.Return itself unconditionally makes the item Available. A direct call to that primitive has weaker preconditions. This division may be acceptable in the teaching design, but a proposed stronger domain boundary should address all direct callers. Adding a guard in one service does not make an unrestricted object method universally safe.

## Worked trace: successful return

Let loan L refer to equipment E and borrower B. L.CheckedOutAt is 2026-03-10 at 10:00 UTC, DueOn is March 20, ReturnedAt is null, and E is CheckedOut. The requested return instant is March 12 at 15:00 UTC. The service's existence and status checks pass. Loan.Return sets ReturnedAt to the accepted instant, making IsActive false. Equipment.Return sets E to Available, then SaveAsync completes.

In the default in-memory stores, L and E are the same objects held by the dictionaries. A subsequent loan lookup observes the changed ReturnedAt even though the service never invokes a loan save. A subsequent active-loan query excludes L because it reads IsActive from the stored object. This explains why the return can appear fully persisted in same-process tests without an explicit loan persistence operation.

Write this trace with two separate mutation points. Immediately after Loan.Return but before Equipment.Return, the loan is inactive while equipment is still CheckedOut. That interval may be brief, but a concurrency or fault argument cannot erase it merely because the statements are adjacent. Whether another operation can observe or act on that interval depends on synchronization, not on visual proximity in the source file.

## Worked trace: equipment save fails

Use a failing equipment store wrapper that delegates GetAsync to the actual in-memory store but throws before delegating SaveAsync. The service obtains the live E reference, mutates L, mutates E, and reaches the failure. After the exception, L is already inactive and E is already Available through shared aliases. The absence of a completed SaveAsync does not mean the default in-memory business state stayed unchanged.

Now contrast a proposed equipment adapter that returns detached objects. In that model, E's local mutation may not be visible in storage when SaveAsync throws before writing. If the loan adapter still returns live references, the stored loan can be inactive while the stored equipment remains CheckedOut. This mixed ownership model makes the inconsistency even more visible. Label it as a proposed adapter combination, not as a configuration currently registered in Program.

There is no general rollback in the service for either case. Retrying the same return through the in-memory loan store may encounter loan.already_returned before it reaches equipment. That rejection is consistent with the current loan object, but it does not tell the caller whether every effect of the previous attempt completed. A retry contract needs an outcome record or another explicit reconciliation rule if the product requires a trustworthy receipt.

## The missing loan persistence operation

ILoanStore exposes AddAsync, GetAsync, and ListActiveByBorrowerAsync. It does not expose SaveAsync or an atomic Return operation. A new durable loan adapter that reconstructs a fresh Loan for GetAsync would need a way to record the changed return timestamp. Returning a detached object and hoping its mutation updates storage would violate the normal meaning of reconstruction.

There are several possible proposals. Add SaveAsync to the interface and make ReturnService explicitly save both entities. This makes persistence intent visible but still leaves two effects without shared atomicity. Add a transaction-oriented repository operation that commits the relationship. This can improve consistency but must define how domain validation and database concurrency interact. Use a tracked unit of work and commit it explicitly. This can work when lifecycle and context ownership are carefully controlled, but it should not remain an invisible assumption.

Do not choose among those designs by counting methods. The right question is whether the contract supports the promised invariant under failure and concurrency. A smaller interface that relies on unexplained live references can be harder to implement correctly than a larger explicit contract. Conversely, adding SaveAsync everywhere without a transaction story can merely make partial writes more visible.

## Duplicate return is not yet idempotent success

The current duplicate-return behavior is rejection with loan.already_returned. The existing test calls ReturnAsync twice and expects that code on the second call. This is different from a proposed idempotent API that returns the original successful receipt for a repeated operation. Both can be sensible contracts, but they lead to different client behavior after a timeout.

For a proposed idempotent return, distinguish a repeat of the same logical request from a new conflicting return request. If the first request recorded March 12 at 15:00 and the next supplies March 11, should the service replay the first receipt, reject conflicting data, or permit a correction workflow? A silent overwrite would erase history. A correction should be a separately authorized and auditable operation if the product needs it.

The current code also checks activity before checking the new return time. An already returned loan with a newly supplied invalid early timestamp produces loan.already_returned first. This is an exact current observation, not a recommendation that all APIs should prioritize duplicate detection the same way. Record error precedence only to the extent your contract needs to preserve it.

## Guided lab: make hidden ownership visible

In a disposable reference output, create an equipment-save wrapper with a designated exception and a counter. Use valid loan and equipment setup, call ReturnAsync, and capture the exception. Assert the counter to confirm the intended boundary was reached. Then inspect ReturnedAt, IsActive, and equipment status through both original references and store lookups. Name the check as a current-behavior observation.

For the second experiment, implement a tiny detached loan-store model in the disposable workspace. Reconstruct a Loan with the same immutable fields when GetAsync is called. For an active source loan, this is straightforward; a returned source loan would also need its return state reconstructed through the available method and a valid timestamp. Demonstrate that mutating the returned copy does not update the model's stored source. You have now exposed why an explicit persistence contract is needed.

Keep this experiment honest: it is a model of detached ownership, not a tested production database adapter. Its purpose is to falsify the assumption that any implementation of GetAsync automatically tracks domain mutations. Do not add it to the original infrastructure project or claim that it verifies Entity Framework change tracking for domain objects the adapter never attaches.

## Independent exercises

**E06.1: Return failure matrix.** Cover missing loan, already returned loan, missing equipment, wrong equipment status, early timestamp, and equipment-save failure. For each row, list the last successful guard, the expected error category, and the loan/equipment state. Keep the save-failure row specific to the live-reference default plus a before-save throwing wrapper.

**E06.2: Explicit persistence proposal.** Choose one of the three persistence designs above. Draw the sequence for success, failure before commit, and lost response after commit. State how a fresh process learns whether a return completed. If your design cannot answer that last question, call it a same-process experiment and list durable completion as remaining work.

**E06.3: Correction versus retry.** Specify a repeated return with identical data, repeated return with different data, and authorized correction of an existing return instant. Describe what history must remain visible in each case. Keep the current rejecting behavior separate from your proposed feature.

## Review checkpoint

You should be able to explain why the current in-memory return works without a loan save and why that fact is a warning when designing another adapter. The warning is concrete: the service depends on mutation becoming visible through stored aliases. It is not a blanket claim that in-memory stores are bad or that every repository must expose identical CRUD methods.

Carry the successful and failed return traces into the next chapters. Checkout and return now provide complementary examples: checkout creates a new record after claiming equipment, while return mutates an existing record before releasing equipment and saving. A coherent durable design must handle both directions of the relationship, including retries, cancellation, and ambiguous outcomes.
