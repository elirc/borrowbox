# BorrowBox extended engineering workbook

This completed workbook continues the [advanced route](../README.md) with deeper, cumulative engineering work on the current BorrowBox implementation. It contains twenty-two substantial chapters, sixty-seven independent exercises, separate hint ladders and full solutions, two capstone briefs, and executable observation labs. It exceeds the requested 37,000 newly authored prose words excluding fenced code. The capstones teach proposed features; they do not change the application.

Work against a disposable copy when an exercise changes code. Keep the application's source, existing tests, and your own edits intact. The existing [lab generator](../lab/generate_lab.py) copies selected current files into an explicitly named new directory and creates compatible defects there. Historical snapshots are reading material; they are not an instruction to overwrite today's application.

An additional [six-check observation lab](lab/README.md) makes the opening chapters' ownership and failure traces executable without changing the application. Its limitation observations are deliberately labeled separately from desired contracts.

## Complete course route

| Chapter | Question you will learn to answer | Main artifact |
|---|---|---|
| [01: Evidence and executable contracts](01-evidence-and-contracts.md) | What does a passing check actually establish? | A claim ledger and observation plan |
| [02: Equipment state and ownership](02-equipment-state-and-ownership.md) | Who may change an item, and who sees the change? | A transition matrix and alias trace |
| [03: Borrower identity and capacity](03-borrower-identity-and-capacity.md) | Which policy is local, and which needs coordination? | A capacity interleaving and identity proposal |
| [04: Reservation validation and effects](04-reservation-validation-and-effects.md) | Where can a rejected operation still leave state behind? | A failure frontier and repaired disposable starter |
| [05: Checkout as a coordinated decision](05-checkout-as-a-coordinated-decision.md) | What does winning an equipment claim establish? | Outcome traces and a proposed receipt contract |
| [06: Return and the missing save contract](06-return-and-the-missing-save-contract.md) | Why does mutation appear persisted without a loan save? | An ownership experiment and return failure matrix |
| [Hints for installment one](hints-01-04.md) | What is the smallest clue that unblocks the exercise? | A second attempt before opening solutions |
| [Solutions and review for installment one](solutions-01-04.md) | What makes an answer defensible? | Worked answers, counterexamples, and scoring |
| [Hints for checkout and return](hints-05-06.md) | Where does the outcome become ambiguous? | Focused clues for the six new exercises |
| [Checkout and return solutions](solutions-05-06.md) | What does a coherent retry design require? | Separate worked answers and review criteria |
| [07: Time and calendar rules](07-time-instants-and-calendar-rules.md) | Which instant and calendar govern the decision? | Boundary examples and scheduling promises |
| [08: Expiry and reconciliation](08-expiry-cancellation-and-reconciliation.md) | What does a successful sweep leave unresolved? | Batch ledger and ownership report |
| [09: Inspection workflows](09-inspection-and-workflow-composition.md) | Where does inspection mutate before rejection? | Two reproducible failure observations |
| [10: Adapter contracts](10-adapter-contracts-and-sqlite-boundaries.md) | What changes when the adapter changes? | Contract suite and substitution plan |
| [11: API contracts](11-dto-endpoint-and-error-contracts.md) | What can a client observe and follow? | Endpoint and response inventory |
| [12: Authorization evidence](12-authorization-evidence-and-identity-binding.md) | Who may act on which borrower? | Policy matrix and actor-binding proposal |
| [13: Transactions and recovery](13-persistence-transactions-and-recovery.md) | Which records must commit together? | Commit set and recovery protocol |
| [14: Fault injection](14-fault-injection-and-outcome-observation.md) | What remains after a precisely placed failure? | Paired before/after fault report |
| [Hints for chapters 07-14](hints-07-14.md) | Which observation distinguishes the alternatives? | Focused clues |
| [Solutions for chapters 07-14](solutions-07-14.md) | Which claims withstand source and failure review? | Worked arguments and scoring |
| [15: Discriminating tests](15-tests-that-distinguish-defects.md) | Which plausible defects do assertions catch? | Mutation map and evidence allocation |
| [16: Concurrency schedules](16-concurrency-ownership-and-schedules.md) | Which shared fact needs coordination? | Ownership matrix and gated conflict test |
| [17: Cancellation](17-cancellation-and-partial-progress.md) | Which effects precede token observation? | Cancellation ledger and client policy |
| [18: Search and reporting](18-search-reporting-and-observation-consistency.md) | What does a read actually promise? | Query, snapshot, and export contracts |
| [19: Safe laboratories](19-building-safe-disposable-laboratories.md) | How can an experiment preserve original work? | Generator review and run evidence |
| [20: Lending capstone](20-capstone-durable-lending-receipts.md) | How can interrupted lending be resolved? | Transaction and receipt feature design |
| [21: Reservation capstone](21-capstone-auditable-reservation-lifecycle.md) | How can terminal decisions preserve ownership? | Auditable lifecycle and reconciliation design |
| [22: Integrated review](22-integrated-review-and-engineering-handoff.md) | What can another engineer verify? | Evidence index and handoff |
| [Hints for chapters 15-22](hints-15-22.md) | Which next observation will unblock the work? | Focused clues before solutions |
| [Final-route solutions](solutions-15-22.md) | What constitutes a complete bounded answer? | Worked solutions and capstone reference designs |

Do the chapters in order for the first pass. On a second pass, pair a chapter with the relevant current source and inspect every assumption in your previous answer. A reasonable session is sixty to ninety minutes of reading followed by forty-five minutes of implementation or review. There is no reward for finishing a chapter quickly if your observations cannot distinguish a correct implementation from a plausible defect.

## Completion and optional implementation

The full planned route is published above. Each chapter has its own worked reasoning and independent exercises, with separate hints and solutions. The two capstones include acceptance criteria, failure traces, review standards, and reference designs. Learners may implement them in disposable workspaces at a chosen evidence boundary; optional production integrations do not block completion of this instructional route.

The capstones cover durable lending receipts and an auditable reservation lifecycle. They require a clear boundary between code already present, a proposed design, a working experimental implementation, and evidence still missing. Neither capstone is described as shipped application behavior.

## Working agreement

Use three labels in your notes: **current**, **observation**, and **proposal**. Current means a behavior directly supported by the present implementation. Observation means a result produced by a specified executable experiment with a stated environment. Proposal means a change you would like to make. A proposal becomes an observation only after an experiment exists; an observation becomes a broad guarantee only when its assumptions match the deployment you are discussing.

Use invented equipment tags and borrower names. Do not put real student data into exercises. Keep each experiment's initial state small enough to write by hand. Record the exact expected error code, state changes, and state that must remain unchanged. If you cannot predict these before running the experiment, slow down and draw the call sequence.

A completed exercise should include the question, the source evidence, the attempted change or experiment, the result, and one explicit limitation. The limitation must be specific. "More tests are needed" is less useful than "this proves a same-process item transition but does not coordinate two database sessions." Later chapters will build directly on this habit.

## Verification boundary

The foundation observation suite passed six checks and the inspection suite passed five. Generation refused existing-output and in-project destinations. Application and test files matched the fifty-three-file baseline. Relative source and course links were checked. These are bounded in-memory observations and documentation checks; database transactions, HTTP authorization, restart recovery, and capstone implementations remain separately labeled designs unless the learner executes them.
