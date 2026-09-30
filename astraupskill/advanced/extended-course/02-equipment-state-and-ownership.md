# 02. Equipment state and ownership

A lifecycle is more than a list of enum values. It is a set of permitted transitions, the conditions under which they may occur, and the authority responsible for enforcing those conditions. BorrowBox's EquipmentItem is useful because some rules live directly on the object while other rules live in services. Understanding the distinction prevents both accidental bypasses and unnecessary duplication.

Read [EquipmentItem and EquipmentService](../../../src/BorrowBox.Core/Equipment/Equipment.cs), [the in-memory equipment adapter](../../../src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs), and [the existing equipment tests](../../../tests/BorrowBox.Core.Tests/Equipment/EquipmentTests.cs). Keep a blank transition table beside the source. Do not initially draw what a lending desk ought to permit. Draw what the methods actually permit. Product rules can become a proposal after the current behavior is explicit.

## Constructor state is an entry point

The constructor accepts an equipment status parameter whose default is Available. It validates identity and required strings, normalizes AssetTag using trimming and invariant uppercase, and trims Name and Category. It assigns the supplied status without proving how the equipment reached that state. This makes reconstruction from stored state possible, but it also means construction is not evidence that the usual workflow ran.

For example, a test can construct CheckedOut equipment without constructing a loan. That setup may be intentional when testing return preconditions. It is also a reminder that the equipment object cannot independently guarantee the existence of a matching active loan. The relationship spans objects and storage. Distinguish an invariant inside one EquipmentItem from an invariant about the whole lending system.

This distinction matters when reading tests. A test fixture that directly supplies Inspection status is not necessarily an invalid test. It may be isolating the inspection rule. However, a claim that a full checkout-to-inspection workflow works requires evidence for the connecting service steps. A shortcut in test setup should appear in the evidence limitation, especially when the omitted steps are exactly where failures can occur.

## Build the transition matrix from methods

There are five named states: Available, CheckedOut, Reserved, Inspection, and Retired. CheckOut and Reserve each require Available. CollectReservation requires Reserved and moves to CheckedOut. ReleaseReservation requires Reserved and moves to Available. Retire requires Available. PassInspection and RetireAfterInspection each require Inspection. These are local guard-and-mutate operations: a failed guard throws before assigning the new state.

Two methods deserve special attention. MarkForInspection rejects Retired, but accepts the other named states. Return has no local guard and simply sets Available. Do not silently replace these facts with a prettier state machine. In particular, direct Return on a Retired item is permitted by this method's implementation. The application ReturnService adds a CheckedOut precondition before calling it, so the public workflow has a stronger boundary than the primitive operation.

| Method | Locally accepted starting state | Result |
|---|---|---|
| CheckOut | Available | CheckedOut |
| Reserve | Available | Reserved |
| CollectReservation | Reserved | CheckedOut |
| ReleaseReservation | Reserved | Available |
| Retire | Available | Retired |
| MarkForInspection | Any named state except Retired | Inspection |
| PassInspection | Inspection | Available |
| RetireAfterInspection | Inspection | Retired |
| Return | No local status restriction | Available |

Read the table as a description, not a recommendation. A proposed stricter Return method could protect more callers, but it might change adapter reconstruction strategies, tests, or workflow assumptions. Before making that change, enumerate every caller and choose where the rule belongs. The objective is a coherent contract, not the maximum possible number of checks scattered across the codebase.

## Worked trace: retirement and a bypass

Consider a camera C with status Available, name Field Camera, and category Media. Calling Retire succeeds and changes status to Retired. Calling UpdateDetails afterward throws equipment.retired before assigning either new field. The existing equipment test verifies that retirement prevents later edits and preserves the retired status. This is strong evidence for that particular sequence.

Now take a separate disposable instance with the same initial values. Retire it, then call Return directly. Its status becomes Available, and a later UpdateDetails can succeed. This does not prove that the API exposes a retirement bypass: that would require a reachable route and service sequence. It proves that the object-level terminal-state claim "Retired can never leave Retired" is false for direct method access. The distinction guides the next review instead of creating an exaggerated security claim.

A useful design discussion follows. Should Return enforce CheckedOut? Should reconstruction use a separate factory? Should some methods become internal to an aggregate boundary? Each option has costs. Tightening Return improves local defense but cannot prove that the loan belongs to the equipment. Restricting visibility changes who can compose workflows but does not provide persistence atomicity. A larger aggregate can own relationships but may become expensive to load or coordinate. State the invariant first, then evaluate enforcement choices against it.

## Ownership is about aliases and authority

The in-memory store uses a ConcurrentDictionary keyed by equipment ID. AddAsync and SaveAsync assign the supplied object to that key. GetAsync returns the stored object, and ListAsync creates an array containing stored references. Those methods do not create immutable snapshots. A caller holding an item can invoke its public methods and affect what another lookup observes.

Imagine a reporting operation obtains an array when item C is Available. Before the report reads C.Status, another operation calls Reserve on the same object. The report's array still contains the same reference, but the status is now Reserved. Copying the collection protected the collection's shape at that point; it did not freeze each item's fields. Whether such a report is acceptable is a product question. The implementation detail must be understood before making consistency promises.

ConcurrentDictionary supports concurrent operations on the dictionary. It does not automatically make every sequence of methods on the objects inside it atomic. In TryCheckOutAsync, the adapter locks the item while checking Available and calling CheckOut. That establishes a specific coordinated path for competing calls using that lock. Reserve and other direct mutations do not acquire the same lock in their ordinary service paths. A synchronization argument must identify all participants, not merely point to the existence of one lock statement.

## A careful same-item race argument

Suppose two callers invoke TryCheckOutAsync for the same stored instance while no other operation replaces that entry or mutates the item outside the lock. The first enters the lock, sees Available, changes it, and returns. The second enters later, sees CheckedOut, and returns null. Under these stated assumptions, the method's check and transition prevent two successful checkouts through that path.

Change the scenario: a reservation service already holds the same item and calls Reserve without participating in that lock. The lock alone no longer supplies a complete ordering argument across both workflows. You should not manufacture a probabilistic test that occasionally fails and call it a proof. Instead, identify the uncoordinated access and design a controlled test seam in a disposable implementation. Later concurrency chapters will develop that seam and discuss database conditional updates.

There is another subtle assumption: the dictionary entry remains the same object. AddAsync and SaveAsync can replace the value for a key. Locking object O1 does not lock a newly assigned object O2 with the same business ID. This is a reason to define a stable coordination authority, such as a keyed lock registry or database row version, when proposing stronger synchronization. It is not an instruction to add that complexity to the teaching application before a requirement exists.

## Guided lab: observe before changing

Use a new reference output from the [existing lab generator](../lab/generate_lab.py), following its documented command in [the failure lab](../03-FAILURE-LAB.md). Work only in that output. Add a small characterization check that creates an item, stores it, retrieves it twice, and uses ReferenceEquals to determine whether both results alias the original. Then reserve through one reference and assert what the other observes. Name the check as an observation of the in-memory adapter rather than a desired universal store contract.

Add a second check that calls ListAsync, then changes the item after obtaining the list. Confirm whether the list's item reflects the mutation. Record that a stable collection length does not imply stable member state. Do not alter the adapter to clone objects merely to make the test say what you expected. A characterization lab first establishes current behavior, then evaluates proposed alternatives separately.

For a proposed clone-on-read experiment, implement a wrapper only in your disposable workspace that reconstructs EquipmentItem on GetAsync. Compare a service update with and without SaveAsync. State explicitly that this wrapper is a teaching model, not the repository's SQLite implementation. The model illustrates ownership but cannot establish SQL behavior, transaction isolation, or database error handling.

## Independent exercises

**E02.1: Transition coverage.** Select four methods with different guards and write a minimal table of success, failure, and unchanged-state assertions. Include at least one rejected transition from Retired and one direct Return observation. Explain which entries describe current behavior and which would change under your proposed stricter lifecycle. A table that silently treats the proposal as implemented does not pass review.

**E02.2: Two-object invariant.** State a rule connecting equipment and loan state, then give a construction sequence that violates it while both individual constructors succeed. Propose an owner for that rule. Your proposal must explain how creation, return, and data reconstruction reach the owner. Simply adding a boolean property to EquipmentItem is insufficient unless you explain how it stays synchronized with the loan.

**E02.3: Lock scope.** Write a five-step schedule in which two operations use different equipment instances with the same ID. Explain why locking the instances does not coordinate them. You need not claim that this is a normal API request path; it is an abstraction stress test. Identify the extra assumption that makes the current same-instance checkout argument valid.

## Review checkpoint

Your diagram should now have two kinds of edges. Solid edges describe direct object methods and their actual guards. Annotated workflow edges describe extra service preconditions. A third legend can mark proposed transitions, but those must remain visually distinct. This is especially important for Return and inspection entry, where intuitive business rules can differ from the local code.

Finish by writing an ownership statement for each artifact: dictionary, array, equipment instance, service, and proposed snapshot. Say who can mutate it, whether it is shared, and what synchronization it uses. Ownership statements turn an attractive lifecycle picture into an engineering model. They also prepare you to understand why validation can succeed while concurrent state invalidates the decision before the next effect occurs.
