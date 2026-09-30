# Hints for chapters 07 through 14

For the time exercises, convert the instant to UTC before extracting the date. An offset changes the displayed local representation, while DueOn has no offset of its own. For the sweep experiment, make the store return a known ordered list when order affects your fake's clock sequence. For the worker promise, separate the configured wait interval from the time at which all eligible work actually completes.

For the expiry ledger, apply the Active-and-due filter before considering equipment. Then distinguish a skip from a thrown error. Cancellation and expiry both need ownership of the same relationship if their terminal reasons must be coordinated. In reconciliation, one observed pair of statuses can have several histories; enumerate those histories before selecting a repair.

For inspection, number the current operations rather than borrowing the ordering from reservation creation. Candidate construction is after MarkForInspection. Completion changes inspection before invoking the selected equipment method. The Repair branch does not invoke PassInspection or RetireAfterInspection, so ask what concrete work record, if any, exists after that result. Chronology and named-enum membership are proposed additional validations, not current ones.

For adapter comparison, inspect the dictionary key, the database index, and the method used to find a row during SaveAsync. These three details explain several behavioral differences. In the stale-save experiment, write down every mutable field assigned by SaveAsync, including fields the caller did not intend to change. For a status rename, consider a row written by yesterday's binary and read by tomorrow's binary.

For API contracts, distinguish the request model from the response model. ReturnedAt exists in the domain but is absent from LoanResponse. A Created location must be compared with actual route registrations. Inspection's direct domain response deserves a different compatibility analysis from a hand-selected response record. The custom exception handler handles one exception type; do not infer every framework failure response from it.

For authorization, first decide whether the request has a principal, then whether that principal satisfies the route policy, then whether it may act on the target borrower. The current code does not make those three questions equivalent. The demo header supplies a role without verifying a real identity. Correlation metadata helps diagnostics but cannot appoint an actor or prove ownership of a resource.

For transaction proposals, draw the committed record set before naming a pattern. A receipt written outside the business commit can leave another gap. For compensation, identify the operation that owns the state being reversed and the atomic condition that proves ownership still holds. A process-local catch block cannot recover work after that process has disappeared.

For fault injection, give before-effect and after-effect wrappers different names. Catch only the designated injected exception, then inspect all relevant state. A double that changes reference ownership may be useful, but it is a different experiment. For cancellation, choose a seam that deterministically cancels the token before the next real store check instead of waiting an arbitrary number of milliseconds.
