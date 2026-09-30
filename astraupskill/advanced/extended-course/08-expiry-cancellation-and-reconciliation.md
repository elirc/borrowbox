# 08. Expiry, cancellation, and reconciliation

Reservation cancellation and automatic expiry both release equipment, but they reach that result through different selection rules. Cancellation is a targeted command that rejects invalid state. Expiry is a sweep that skips certain mismatches and returns a count. The distinction affects user feedback, monitoring, and recovery when the reservation and equipment no longer agree.

Read [ReservationService and ReservationExpiryService](../../../src/BorrowBox.Core/Reservations/Reservation.cs), [reservation tests](../../../tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs), and [expiry tests](../../../tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationExpiryTests.cs). This chapter builds a batch-progress ledger and a reconciliation proposal. Reconciliation is not currently implemented; it is a design response to states the existing code can encounter.

## Two state machines form one relationship

Reservation has Active, Collected, Cancelled, and Expired statuses. Collect, Cancel, and Expire each require Active before assigning the corresponding terminal status. These methods do not look at equipment or time. In particular, Expire does not itself verify that the deadline has passed; ReservationExpiryService owns that selection rule.

Equipment separately has Reserved and Available states, among others. Releasing a reservation requires Reserved at the equipment-method level. A complete cancellation or expiry normally moves the reservation out of Active and equipment from Reserved to Available. The relationship is maintained by orchestration rather than by one object containing both state machines.

There is no reservation collection workflow exposed by ReservationService in the current file. Reservation.Collect and EquipmentItem.CollectReservation exist as primitives, but their presence does not establish a coordinated service that creates a loan, checks capacity, and commits all related changes. A future collection feature must compose those obligations explicitly. Do not draw a shipped end-to-end collection path merely because both method names exist.

## Targeted cancellation rejects mismatches

CancelAsync retrieves an active reservation, loads its equipment, and verifies Reserved status. It then calls reservation.Cancel, equipment.ReleaseReservation, and equipmentStore.SaveAsync. No borrower lookup or clock check appears in this path. A reservation can be canceled through this service even when its expiry instant has passed, provided it is still Active and the equipment remains Reserved.

If the equipment is Available instead, the service throws equipment.not_reserved before changing reservation status. The existing mismatch test demonstrates this preservation. The service does not repair the mismatch automatically because it cannot infer why the equipment became Available. Perhaps another operation completed without updating the reservation; perhaps the data was seeded inconsistently. Rejecting avoids inventing a history.

A duplicate cancellation rejects because the reservation is no longer Active. That is current behavior, distinct from a proposed idempotent cancellation receipt. An API client retrying after a lost response may need to query an outcome in a richer design. Returning a generic success for every nonactive reservation would be dangerous if Collected, Cancelled, and Expired have different business meanings.

## Expiry skips rather than rejects

ExpireDueAsync captures now, obtains reservations, and filters for Active entries whose ExpiresAt is at or before that threshold. For each candidate it loads equipment. Missing equipment or a status other than Reserved causes a continue, leaving the reservation unchanged and not incrementing the count. For a matching item, it expires the reservation, releases equipment, saves equipment, and then increments the count.

The result is the number of successful iterations that reached the increment during a normally completed invocation. It is not the total number of due reservations, the number of mismatches, or a durable audit log. If a later iteration throws, the method does not return the count accumulated so far, even though earlier mutations may remain visible. This distinction is central to designing useful operational reporting.

Consider three reservations: R1 due with Reserved equipment, R2 due with Available equipment, and R3 not yet due with Reserved equipment. A normal sweep expires R1 and releases its item, skips R2, and leaves R3 untouched. The returned count is one. A dashboard that interprets that count as "all due work is healthy" would miss R2 indefinitely unless another observation detects the mismatch.

## Worked batch failure with controlled order

For a deterministic experiment, use a reservation-store double returning an explicit ordered array containing R1 then R2. Both are due and both equipment items are Reserved. Configure the equipment store to complete the first save and throw at the second save. R1 becomes Expired and its item Available; R2 is also mutated before its save boundary throws. The method returns no integer because it exits exceptionally.

Under live-reference equipment and reservation storage, both pairs may already show their new states despite the second save failure. The internal count reached one before the second iteration, but that variable is not an externally returned completion report. A detached equipment model would require a different state ledger. Always name the adapter and failure position when explaining partial batch outcomes.

Do not rely on ConcurrentDictionary enumeration order to choose which reservation fails first. The production in-memory ListAsync returns an array of values without a business ordering contract. Your test double should supply the order needed by the experiment, and the test should say why that ordering is controlled. Otherwise, the assertion can accidentally depend on runtime implementation details.

## Cancellation meets expiry

Now imagine a targeted cancellation and an expiry sweep select the same Active reservation and Reserved equipment. The domain methods guard current status, but neither service wraps the whole pair of transitions in one shared lock or transaction. The outcome depends on interleaving and shared references. A guard on one object is not automatically an atomic claim on the two-object workflow.

A proposed reservation-version mechanism could let one operation claim the Active reservation only if its expected version still matches. The winner would then need to commit the equipment transition consistently. A version on the reservation alone prevents two terminal decisions from overwriting each other, but it does not by itself make equipment agree. This is the same cross-record coordination lesson as checkout, now with two competing terminal reasons.

The product should decide whether cancellation or expiry takes precedence at the exact deadline. Current targeted cancellation does not check time, so a late cancellation can still win if it acts before expiry changes status. A stricter policy might prohibit cancellation after ExpiresAt or record a user cancellation request separately from the effective terminal reason. That is a product decision, not a behavior to infer from enum ordering.

## Reconciliation needs categories

Design a read-only reconciliation report before designing an automatic repair. Useful categories include Active reservation with missing equipment, Active due reservation with non-Reserved equipment, terminal reservation with equipment still Reserved, and multiple Active reservations referring to one item. Each category suggests a broken relationship but does not necessarily reveal the correct repair.

For example, Reserved equipment beside an Expired reservation might belong to a newer reservation for the same item. Blindly releasing it would damage valid current work. A repair needs ownership information linking the equipment claim to the reservation or operation that owns it. The current equipment model has no ReservationId field, so status alone cannot identify that owner. This is a concrete limitation to address in a proposed schema.

A useful report records IDs, observed states, observation time, and the category, while avoiding unnecessary borrower personal data. It should distinguish a consistent snapshot from a best-effort read that might observe concurrent transitions. An operator needs to know whether a mismatch was stable across observations before treating it as persistent corruption. Reporting is part of the recovery design, not merely an afterthought.

## Guided investigation: count versus coverage

Build the three-reservation normal case described above in a disposable workspace using a fixed clock. Assert returned count one, R1 Expired, R2 Active, and R3 Active. Also assert the equipment states. This test teaches that a successful sweep can leave due work unresolved by design. Name the skipped mismatch explicitly rather than hiding it behind a total count assertion.

Next, specify the two-item save-failure experiment with an ordered fake. Record what you expect the caller to receive and what subsequent reads reveal. You may implement the fake in the disposable lab, but do not introduce a loop that retries every failure automatically. First decide whether a retry would safely identify completed work, whether the underlying state is consistent, and what observation the operator needs.

## Independent exercises

**E08.1: Sweep ledger.** Create six reservations covering due matching equipment, future expiry, already Cancelled, missing equipment, wrong equipment status, and equality at now. Predict the count and every final status. Use invented IDs and an explicit list order, even where order should not affect the normal result.

**E08.2: Terminal reason policy.** Specify the outcome of cancel versus expire at the exact deadline under two possible product policies. For each, identify the coordination boundary needed to preserve the chosen terminal reason. Explain why checking the clock only in the HTTP endpoint would leave other callers outside the rule.

**E08.3: Reconciliation proposal.** Design a report row and a safe operator workflow for an Active reservation whose equipment is Available. List at least two possible histories that produce the same observation. State what extra evidence would justify a repair and what the system should do when that evidence is unavailable.

## Review checkpoint

Your batch ledger should separate selected, skipped, changed, saved, and counted work. Your reconciliation proposal should preserve uncertainty instead of guessing ownership from status. These artifacts prepare you to discuss partial progress and cancellation later without pretending that a batch is atomic simply because it is one method call.
