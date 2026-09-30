# 04. Reservation validation and effects

Reservation creation is a compact example of a larger engineering problem: an operation can validate all its input correctly and still fail after changing shared state. This chapter separates preventable validation damage from persistence failure. You will repair a deliberate ordering defect in a disposable starter, then explain why that repair is necessary but does not create a transaction.

Read [Reservation and its services](../../../src/BorrowBox.Core/Reservations/Reservation.cs), [reservation tests](../../../tests/BorrowBox.Infrastructure.Tests/Reservations/ReservationTests.cs), and [the laboratory recipe](../lab/recipe.json). The recipe is especially important because it describes exactly how the broken starter differs from current source. You are not being asked to restore an old method signature or replace application files with a historical snapshot.

## Follow one reservation through its boundaries

Reservation construction requires nonempty reservation, equipment, and borrower IDs. Expiry must be strictly later than reservedAt; equality is rejected. The stored timestamps are normalized to UTC. Status starts Active. Those are rules about a newly constructed reservation object. The constructor cannot establish whether the equipment exists, whether it is available, whether the borrower is active, or whether the new reservation will be saved.

ReservationService supplies those surrounding steps. ReserveAsync first loads equipment, then borrower, and rejects missing records. It checks borrower activity. It constructs the reservation using the selected clock. Only after that construction succeeds does it call equipment.Reserve, save equipment, and add the reservation. This order is visible in the current method; it is also supported by a test showing that invalid expiry does not reserve equipment.

Use a fixed instant for the example: 2026-01-02 at 10:00 UTC. A requested expiry at 12:00 is valid. An expiry at 10:00 is invalid, as is 09:00. Avoid relying on the current wall clock when your purpose is to test this boundary. If time advances during setup, a test can cross a threshold for reasons unrelated to the code under examination.

## Worked trace: valid input

Let equipment E be Available and borrower B be active. Initially the reservation store is empty. The equipment lookup returns E, and the borrower lookup returns B. Construction produces a new Active reservation R with E's and B's IDs. At this point, R exists only as a local object: no store addition has occurred, and E is still Available.

Calling E.Reserve changes its status to Reserved. With the in-memory adapter, that mutation is observable through existing aliases immediately. SaveAsync then assigns the item to its dictionary key. Reservation AddAsync stores R. On normal completion, the service returns R, E is Reserved, and the reservation list contains R. This is the intended successful path, but it contains multiple observability points rather than one shared commit.

If you draw only the final pair "Reserved plus Active," you miss those intermediate states. Draw a ledger with columns for E.Status, local R, stored R, and last completed operation. That ledger reveals the first effect and every point at which failure can leave a distinguishable result. It will later become your fault-injection plan.

## Worked trace: invalid expiry

Now use an expiry equal to the fixed current instant. Equipment and borrower lookups succeed. The constructor rejects with reservation.expiry_invalid before returning a reservation. Equipment.Reserve has not run. Equipment remains Available, and no reservation has been added. This is a clean rejection before effects for the specific invalid-expiry path.

The disposable starter intentionally swaps the reservation construction and equipment.Reserve statements. With that defect, the same invalid expiry first makes E Reserved and then throws from construction. The caller receives the correct error code, but shared equipment state is wrong. A test that asserts only the exception code cannot distinguish the defective sequence from the corrected one. The unchanged-state assertion is essential.

The repair is to construct the fallible candidate before the first equipment mutation, matching current application behavior. It is not to catch every exception and unconditionally set equipment Available. Such compensation could erase a concurrent change or release an item that is no longer owned by this operation. A local ordering improvement avoids that particular side effect instead of attempting an unsafe universal undo.

## The failure frontier

Name the boundary immediately before equipment.Reserve the validation frontier for this operation. The service has checked existence, activity, and reservation construction rules before crossing it. Equipment availability is still checked by Reserve itself at the transition. After Reserve succeeds, the operation has entered the effectful region. Failures there need recovery semantics beyond rearranging constructor calls.

| Failure point | Current in-memory observation | What has not been established |
|---|---|---|
| Equipment missing | No new reservation or mutation | Nothing about other concurrent requests |
| Borrower inactive | Equipment unchanged | Future borrower activity |
| Invalid expiry | Equipment unchanged | Persistence recovery |
| Reserve rejects non-Available item | Candidate exists only locally | Any successful reservation |
| SaveAsync throws after Reserve | Shared item may already be Reserved | Recorded reservation |
| Reservation AddAsync throws before storing | Equipment remains Reserved | A corresponding stored reservation |

The wording "may already" in the save row reflects adapter behavior and failure location. The default in-memory instance was mutated before SaveAsync. A proposed snapshot adapter might hold an unpersisted object instead. A failing save could also fail before or after writing in a different implementation. Fault injection must define which case it simulates, rather than treating all thrown exceptions as identical.

## Run the compatible failure lab

The [advanced failure-lab chapter](../03-FAILURE-LAB.md) contains the exact generator and run commands. Generate a starter in a fresh absolute directory outside the repository, and generate a separate reference output if you need a comparison. The generator refuses an existing output and a path inside the application. It copies current selected sources and applies bounded, checked mutations only to the starter copy.

The existing starter has two intentional assertion failures: invalid reservation expiry leaves equipment reserved, and invalid loan due date leaves equipment checked out. Its expected result is four passing checks and two failures. The reference has six passing checks. Those totals belong to the unchanged existing harness. If you add exercises to your copy, update your own expected totals rather than continuing to quote the baseline as if nothing changed.

Before editing the copied source, predict the two failing state assertions from the recipe. Run the starter and confirm that the failures are behavioral assertions, not compiler errors or missing dependency failures. Repair the reservation ordering first and rerun. You should now have removed one deliberate defect while the checkout defect remains. This intermediate result is valuable because it demonstrates that your change addressed a specific cause.

Repair the checkout ordering separately, then rerun. Keep a short record of the before, intermediate, and after outcomes. Do not copy the entire original Loan.cs over the starter as a shortcut; moving the relevant candidate construction teaches the boundary and preserves the laboratory's intent. Do not move changes back into the original application, where the correct ordering is already present.

## Extend the lab without changing its claim

Add a reservation store double in the disposable workspace that throws a designated exception before inserting a reservation. It should implement the existing interface and make the failure position obvious. Invoke current ReserveAsync with valid input and verify that equipment becomes Reserved while no reservation is recorded. Name this an observation of the current persistence gap. The expected outcome is not a recommendation for production behavior.

Then design, on paper, a proposed transactional boundary that would commit equipment status and reservation creation together. Decide whether both records live in the same database and whether the operation owns a transaction. If they do not share a durable coordinator, explain what recovery record and reconciliation process would be needed. Do not label a try/catch wrapper transactional merely because it attempts an inverse mutation.

For a second extension, distinguish failure before insertion from insertion followed by an exception. In the latter case, retrying without an operation identifier can create another reservation object or encounter an unavailable item, depending on the state seen. The client cannot infer the commit outcome solely from the exception. This sets up later work on idempotency and receipts, but the immediate deliverable is a clear pair of traces.

## Independent exercises

**E04.1: Error and state.** Write a three-case reservation specification for expiry before, equal to, and after the captured clock instant. For each case, state the expected error or result and the expected equipment and reservation-store state. Explain why testing only a timestamp one day in the past misses an important boundary.

**E04.2: Guard precedence.** Construct a scenario with an inactive borrower and invalid expiry. Which error does current ReserveAsync produce first? Then construct an unavailable equipment item with valid borrower and invalid expiry. Explain the different ordering. Your answer should follow the source exactly and avoid inventing a universal priority among business rules.

**E04.3: Failed add.** Specify the before-add throwing store described above. Record all state after the exception using both the original equipment reference and a store lookup. Explain why agreeing observations do not prove durable persistence. Propose a test that would distinguish an adapter returning snapshots from the live-reference adapter.

**E04.4: Unsafe compensation.** Give a concrete sequence in which catching an arbitrary failure and calling equipment.Return would overwrite state belonging to another operation. State what ownership or version evidence a compensating action would require before it could be safe. You may use a proposed concurrent workflow, but label it as such and make every state transition explicit.

## Review checkpoint

Your repaired starter should preserve current signatures and pass the baseline reference contract. Your written explanation should still identify the loan and reservation persistence gaps. If the explanation ends with "everything is atomic now," the exercise is incomplete even if all six checks pass. Passing a bounded harness establishes its bounded assertions, not every desirable property of a lending system.

Carry forward two artifacts: the failure-frontier ledger and a list of unresolved commit outcomes. The next installment will use them to analyze checkout, return, and time-based expiry without repeating the same validation lesson. You now have a concrete way to locate effects and a disciplined way to distinguish a small correct repair from a much larger architectural guarantee.
