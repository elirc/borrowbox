# 2. Trace one item across two kinds of failure

Use equipment E, borrower B, and fixed time T = 2026-03-01 12:00 UTC. Give B an active account and no active loans. Begin with E Available. Treat the domain record and the store entry as separate concepts even when the implementation currently gives them the same reference.

| Step | Reservation attempt with expiry T | Equipment | Reservation count |
|---|---|---|---:|
| Read inputs | Equipment and borrower exist | Available | 0 |
| Build candidate | Constructor rejects expiry equal to creation | Available | 0 |
| Save equipment | Not reached | Available | 0 |
| Add reservation | Not reached | Available | 0 |

Now repeat with expiry T plus one hour. The constructor succeeds, Reserve changes E to Reserved, Save completes, and Add makes the active reservation visible. Capture the constructor's creation time once. Reading a moving clock separately in setup and assertion can turn a boundary test into a scheduling lottery.

The state diagram shows legal ideas rather than claiming every arrow is exposed by an HTTP endpoint:

~~~mermaid
stateDiagram-v2
    Available --> Reserved: reserve after expiry validation
    Reserved --> Available: cancel or expire
    Available --> CheckedOut: checkout after loan validation
    CheckedOut --> Available: validated return workflow
    Available --> Retired: retire
    CheckedOut --> Inspection: mark for inspection
    Inspection --> Available: pass inspection
    Inspection --> Retired: retire after inspection
~~~

Read EquipmentItem.Return carefully: it unconditionally assigns Available. The coordinating ReturnService supplies the checked-out precondition. A direct entity call and the service workflow therefore have different protection. Similarly, Reservation.Collect and EquipmentItem.CollectReservation exist as domain methods, but this course does not invent a durable reservation-to-loan workflow that coordinates them.

For a checkout with yesterday's due date, the candidate Loan rejects before TryCheckOutAsync. The domain compares the due date against the UTC date of checkedOutAt, not the client's wall-clock date. For a valid due date followed by a loan-store failure, the story changes: TryCheckOutAsync already changed E, and AddAsync can throw. The caller sees failure while E remains CheckedOut and there is no stored loan. The new lab deliberately characterises this limitation; it does not call that result a desirable invariant.

Trace returns with three rows: active loan plus CheckedOut equipment; active loan plus Available equipment; returned loan plus Available equipment. In the first case a valid time returns both. In the second, equipment.not_checked_out occurs before the loan changes. In the third, loan.already_returned occurs before equipment lookup. A second return is rejected rather than replayed as an idempotent success. Say which behavior your client expects instead of treating retries as universally harmless.

For expiry, [ReservationExpiryService](../../src/BorrowBox.Core/Reservations/Reservation.cs) captures now once and selects active reservations whose expiry is less than or equal to now. Missing equipment and non-Reserved equipment are skipped. They are not silently repaired. A successful iteration expires the reservation, releases equipment, saves, and increments a count. An exception on a later item does not undo earlier iterations.

Your trace worksheet should contain state before each await, state after it, and who could observe that state. Mark object mutations separately from dictionary inserts. Finish by explaining why an expiry count of one says "one iteration completed," not "the entire batch committed atomically." That explanation is the bridge from state machines to operational reasoning.
