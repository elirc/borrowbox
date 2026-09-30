# 6. Solutions, counterexamples and review rubric

Use this guide after attempting the exercises. A solution is a claim plus evidence, not just a rearranged statement. Keep your failed prediction beside the corrected one; it shows which model of the code changed.

B1: Reservation requires expiry strictly after reservation time, so equality rejects. A tick later is valid if all other prerequisites hold. Loan accepts a due date equal to the UTC date of checkout. Capture one fixed clock value and derive inputs from it; a real clock in either fixture construction or assertion reintroduces an uncontrolled boundary.

B2: Checkout reads equipment, reads borrower, checks IsActive, then reads active loans. Deactivation rejects before the candidate loan and atomic equipment transition. Missing borrower has a different code and path. In either case, no equipment effect is expected. A count of zero alone would miss the ordering defect if equipment still changed.

B3: Returned-loan retry produces loan.already_returned. An active loan whose equipment is Available produces equipment.not_checked_out. A too-early return produces loan.return_time_invalid. These guards protect different relationships. EquipmentItem.Return by itself simply assigns Available, so a direct call bypasses workflow coordination. Do not add a service assertion and then accidentally exercise only the entity.

B4: Before-add failure leaves zero loans; after-add failure can leave one. Both can leave CheckedOut equipment. The caller cannot infer persistence outcome from the thrown exception alone. A durable idempotency receipt helps a retry discover prior success; blindly invoking checkout again is not equivalent. Tests should inject the failure at a named point rather than use an unreliable network outage.

B5: Reconciliation should classify and report inconsistencies, including the identities and observed versions used to derive them. It should not assume that every non-Available item has an active loan. Inspection and reservation are legitimate states. A safe repair proposal needs a separate authorised action, refreshed reads and a conflict policy. Discovery and mutation belong in different steps.

B6: Same-item protection is the lock around the current status check and transition. Borrower-level capacity spans items and the loan store, so that lock does not cover it. A deterministic two-reader gate makes the missing boundary visible. The fake must capture the count before releasing either caller; capturing after the gate would allow a scheduler-dependent false green.

B7: The expiry service skips missing or mismatched equipment, expires eligible consistent rows, and leaves future rows alone. A later failure does not restore completed earlier rows. It also may leave a current iteration's live state mutated before saving. An operational retry strategy should preserve diagnostics and check current states; the returned count is not a transaction receipt.

B8: Initial cancellation can prevent the first read. Later cancellation can interrupt a store call after an earlier effect. For a future atomic adapter, decide a last precommit cancellation checkpoint and avoid reporting cancellation after a completed commit as though no effect occurred. A client timeout remains distinct from server cancellation or transaction rollback.

For the capstone, prefer a request receipt written in the same transaction as the loan and equipment transition. A separate request-key cache can acknowledge an operation whose write failed, or miss one that committed. Normalise and compare the meaningful payload. Add a uniqueness constraint on the chosen request scope and reject reuse with different input. Borrower-limit correctness requires a serialisation mechanism covering all that borrower's competing loans, not just a loan-count query.

| Dimension | 0 | 1 | 2 |
|---|---|---|---|
| Source grounding | Invented methods | Correct classes, vague flow | Exact contracts and boundaries |
| Failure evidence | Exception only | Some after-state | Code, equipment and record assertions |
| Reproduction | Manual source overwrite | Compiling but nondeterministic | Disposable deterministic red/green lab |
| Concurrency | "Thread safe" claim | One contention dimension | Item and borrower boundaries separated |
| Durability | Memory called persistence | Database proposed | Transaction, receipt and restart evidence scoped |
| Handoff | No limitations | General warning | Implemented, observed and proposed separated |

A strong review scores at least ten of twelve with no zero for evidence or durability. Ask a partner to explain the fault table without your narration. If they cannot identify the commit boundary, revise the design before adding more code. Historical snapshots remain useful for reading changes, but the current-source generator is the repeatable starter for this course.
