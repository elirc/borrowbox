# 5. Exercises and a lending-command capstone

Do these in a disposable lab or a separately chosen feature workspace. The shipped application is a reference, not a scratchpad. Each exercise needs a prediction, a bounded check, and a note stating the remaining uncertainty. Do not open the solutions until you have written the prediction.

## B1 - Date and time boundaries
Add two cases to the harness: expiry exactly T and expiry T plus one tick. Then exercise a due date equal to checkout's UTC date. Acceptance is one expiry rejection without mutation, one successful reservation, and an accepted same-day loan. Explain why DateOnly and DateTimeOffset need different boundary statements.

## B2 - A disappearing borrower
Use a deactivated borrower and valid dates. Predict which check rejects before a candidate or equipment mutation. Assert borrower.inactive, Available equipment and no loan. Compare this with a missing borrower; do not let a broad exception assertion hide the difference.

## B3 - Return lifecycle matrix
Add a returned-loan retry and an equipment-status mismatch. Capture ReturnedAt, IsActive and equipment status before calling the service. Acceptance is stable state after each rejection and distinct business codes. Explain why calling EquipmentItem.Return directly is not equivalent to invoking ReturnService.

## B4 - Fault after validation
Replace FailingLoanStore with an adapter that records the loan and then throws. Compare it with the existing before-add failure. Acceptance is a table distinguishing "zero loan recorded" from "loan recorded but acknowledgement lost." Do not call either failure outcome proof that a retry is safe.

## B5 - Reconciliation without invention
Write a read-only reconciliation sketch joining active loans to equipment. List candidate inconsistencies and ambiguous cases. Acceptance includes CheckedOut equipment with no known loan, active loan with Available equipment, and multiple active loans for one item. The sketch must report evidence rather than automatically setting statuses; a reservation or inspection can make a naive repair destructive.

## B6 - Two concurrency dimensions
Keep the deterministic borrower-limit race and add a same-item competition case. Acceptance distinguishes one item winner from a cross-item borrower limit violation. Use gates or the store's existing atomic method, not delays. State why success on the first test cannot imply success on the second.

## B7 - Expiry as a resumable batch
Seed one due reservation with missing equipment, one due and consistent reservation, and one future reservation. Predict the expiry count and every status. Then inject a save failure partway through a second scenario. Acceptance explains completed-prefix progress and why retry needs state-aware handling. No claim of whole-batch atomicity is allowed.

## B8 - Cancellation placement
Cancel before the first equipment read, then design an adapter that cancels between checkout and loan add. Acceptance records token state, response outcome and surviving state independently. Explain why merely passing CancellationToken through all methods does not establish an all-or-nothing command.

## Capstone - A reliable checkout receipt

Design a proposed CheckoutCommand with equipment ID, borrower ID, due date, caller request ID and an expected equipment version. Its result is a durable receipt identifying the committed loan. Preserve input validation ordering; add a transaction boundary covering equipment, loan, and request receipt. The same request ID with the same normalised payload should replay the receipt. Reuse with different payload should reject. Scope the key to the authenticated caller or another explicitly justified boundary.

Deliver a schema sketch, state trace, HTTP failure contract and focused test plan before writing adapters. Tests must cover competing same-item requests, competing distinct-item requests at the borrower limit, insertion failure, lost response followed by retry, cancellation before commit, and restart before receipt lookup. Make normalised date semantics explicit rather than hashing raw JSON.

For this course, a convincing design plus a small disposable prototype is a complete milestone. Deploying authentication, a database migration and a reconciler is separate work. Your handoff should say which behavior exists in the original application, which you implemented in the prototype, and which remains a proposal. That separation is part of feature ownership.
