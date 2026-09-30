# 07. Time, instants, and calendar rules

Time enters BorrowBox through several different shapes. CheckedOutAt and reservation expiry are instants represented by DateTimeOffset. DueOn is a DateOnly calendar value. A background worker uses a periodic timer, while services may use an injected clock or read the system clock directly. These choices are related, but they are not interchangeable. This chapter teaches you to describe each temporal rule without depending on the day your test happens to run.

Read [IClock and SystemClock](../../../src/BorrowBox.Core/Common/Time.cs), [Loan](../../../src/BorrowBox.Core/Loans/Loan.cs), [Reservation](../../../src/BorrowBox.Core/Reservations/Reservation.cs), and [the expiry worker](../../../src/BorrowBox.Api/Reservations/ReservationExpiryWorker.cs). Your deliverable is a temporal specification with examples at equality, across a UTC date boundary, and during a delayed operation. No machine clock changes are required.

## An instant is not a local calendar date

A DateTimeOffset identifies a point on the timeline using a displayed local time and offset. Two values with different offsets can represent the same instant. BorrowBox normalizes accepted checkout, return, reservation, and inspection timestamps to UTC when storing them in domain objects. That normalization gives a consistent representation, but it does not define a campus's business calendar or opening hours.

Loan validates DueOn against DateOnly.FromDateTime(checkedOutAt.UtcDateTime). The governing date is therefore the UTC date of checkout. Consider a checkout displayed as January 2 at 00:30 with an offset of plus two hours. Its UTC instant is January 1 at 22:30. A DueOn of January 1 is accepted by the constructor because it equals the UTC date, even though the displayed local calendar says January 2. This is an exact consequence of the current comparison.

Reverse the offset to see the other boundary. A checkout displayed as January 1 at 23:30 with an offset of minus two hours occurs on January 2 in UTC. DueOn January 1 is rejected. Neither outcome should be called a timezone bug without first specifying the intended business calendar. The current rule is UTC-based; a campus-local rule would be a deliberate design change needing a timezone authority and tests around its own boundaries.

## Equality is a product decision

Loan rejects due dates strictly before the checkout UTC date. Same-day loans are therefore permitted. Loan.Return rejects returnedAt strictly before CheckedOutAt, so a return at exactly the checkout instant is permitted. Reservation construction rejects expiresAt less than or equal to reservedAt, so a zero-length reservation is prohibited. These three comparisons intentionally produce different equality outcomes in current code.

Write a table before writing tests. For each rule, include a value immediately before, exactly at, and immediately after the boundary. For DueOn, the smallest relevant unit is a day. For timestamp rules, choose a representable small interval appropriate to the assertion, such as a tick, without implying that a physical equipment desk operates with that precision. The unit belongs to the representation and test, not necessarily to the product experience.

Boundary tests should discriminate between plausible mistakes. A test that rejects a reservation expiring yesterday would pass whether equality is accepted or rejected. A test at equality distinguishes less-than from less-than-or-equal. A return test with an offset-equivalent instant distinguishes chronological comparison from a mistaken comparison of displayed clock fields. Choose cases because they expose alternatives, not because a table needs more rows.

## Captured time and elapsed work

CheckoutService captures checkedOutAt at the beginning of the operation, before asynchronous lookups. ReservationService reads its selected clock when constructing the reservation after equipment and borrower checks. ReservationExpiryService captures now once before listing reservations. These observation points differ. A clock abstraction makes the points controllable in a test, but it does not make them semantically identical.

Imagine checkout starts just before midnight UTC and an adapter is delayed until after midnight. The candidate loan still uses the earlier captured instant. Its due-date rule is evaluated against that earlier date. This behavior may be reasonable if checkout time means request acceptance time. If the product instead means the commit time, the design must move or redefine the observation and consider what happens when validation changes while the operation waits.

For expiry, a single captured now provides a stable threshold for one sweep. A reservation becoming due halfway through a long sweep may not be selected until the next invocation if its expiry is later than the captured threshold. This is not necessarily incorrect; it is a batch-time policy. Re-reading the clock for every item would create a different policy and could make test outcomes depend on iteration speed. State which behavior the product wants before optimizing the loop.

## A clock fake is an observation tool

The existing tests use a FixedClock or FakeClock record implementing IClock. A fixed value removes dependence on wall time and makes the boundary obvious. It also reveals whether a service actually uses the injected clock. The expiry test with a future 2098 instant demonstrates that ExpireDueAsync follows the supplied clock rather than the current machine date.

A more advanced fake can return a sequence of instants and count reads. Use it in a disposable experiment to determine how often a method reads time. For ExpireDueAsync, one read is the expected current behavior. However, do not automatically make an exact read-count assertion a permanent requirement. Decide whether the requirement is one stable threshold, a specific observation point, or merely correct selection. The strongest test observes the semantic consequence of repeated reads when that consequence matters.

For example, supply two reservations with expiries on opposite sides of the first clock value and make a second possible clock read much later. A service using one threshold should process only the first eligible reservation. A service that reevaluates per item might process both. This test distinguishes temporal policy through outcomes rather than coupling solely to an implementation counter.

## Timer scheduling is another boundary

ReservationExpiryWorker creates a PeriodicTimer with a one-minute interval and waits for ticks before invoking the service. It creates a dependency-injection scope for each invocation and logs a count only when the returned count is positive. The worker does not perform an immediate sweep before the first wait. The service's time rules and the worker's scheduling are separate responsibilities.

A one-minute interval is not a promise that every reservation becomes Available exactly sixty seconds after expiry. Startup timing, sweep duration, scheduling delays, failures, and equipment-state mismatches can affect observation. The worker awaits each sweep in its loop rather than launching a new unawaited sweep every tick. This avoids one obvious source of overlapping invocations within that worker, but it does not coordinate another application instance or a manually triggered service call.

The worker source has no local catch-and-retry block around ExpireDueAsync. Therefore, do not claim it silently retries every service failure on the next minute. The enclosing host's behavior is outside this file's local logic and would need separate configuration and runtime evidence. A future resilience proposal should define which failures are retried, which are surfaced, and how partial progress is observed.

## Guided laboratory: offset and threshold cases

In a disposable reference workspace, construct loans directly with the two offset examples above. Keep all IDs nonempty so that identity validation does not mask the time rule. Assert the accepted UTC representation and the expected rule code for the rejected date. Then construct a loan and return it using a timestamp with a different offset but the same instant. Predict the result before running.

For expiry, use two active reservations and two Reserved items. Set one expiry equal to the fake clock and one a minute later. Invoke the service once and assert both the returned count and each pair of states. Equality is due in the service because it selects ExpiresAt less than or equal to now. This complements the constructor rule, which requires expiry strictly after creation. The rules concern different boundaries and are not contradictory.

Do not test the one-minute worker interval by sleeping for a minute in a unit test. That conflates scheduling, service behavior, and machine load while making the test slow. A proposed worker abstraction can accept a controllable tick source, but that is a separate design experiment. The current source-reading evidence is enough to describe the scheduling loop honestly.

## Independent exercises

**E07.1: UTC calendar cases.** Produce four checkout/due-date pairs using positive and negative offsets around midnight. Include two accepted and two rejected cases, and show the UTC conversion for each. Explain which cases would change under a proposed campus-local rule without implementing that rule implicitly.

**E07.2: One sweep, one threshold.** Specify a deterministic experiment in which reading the clock once and reading it per item produce different outcomes. State the initial reservation and equipment states, the sequence of clock values, and the expected count under current behavior. Explain why an iteration-order assumption must be controlled if your fake depends on item order.

**E07.3: Scheduling promise.** Rewrite "reservations expire within one minute" into a defensible statement about the current worker, then propose a measurable service-level objective for a future deployment. Identify the metrics and failure reporting needed to assess that objective. Keep timer configuration distinct from measured operational performance.

## Review checkpoint

Your temporal specification should name the clock source, the read point, the comparison operator, and the representation for each rule. It should distinguish a due date from an expiry instant and a timer interval from a completion guarantee. If any test uses the current date only to manufacture a convenient future value, ask whether a fixed clock would make its intent clearer.

Carry a list of unresolved product policies into your capstone notes: campus-local due dates, grace periods, daylight-saving interpretation, delayed request timestamps, and expiry completion targets. Those topics are deliberately not implemented by this chapter. Recognizing their absence prevents accidental calendar assumptions from becoming undocumented lending rules.
