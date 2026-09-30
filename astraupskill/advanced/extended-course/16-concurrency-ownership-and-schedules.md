# 16. Concurrency, ownership, and schedules

Concurrency problems occur when several operations rely on observations that can change before their effects complete. BorrowBox has multiple coordination keys: equipment identity, borrower capacity, reservation identity, and the relationship between a lifecycle record and equipment. This chapter asks you to design explicit schedules and synchronization scopes instead of describing the whole application as either thread safe or unsafe.

Use [InMemoryEquipmentStore](../../../src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs), [checkout and return](../../../src/BorrowBox.Core/Loans/Loan.cs), [reservation services](../../../src/BorrowBox.Core/Reservations/Reservation.cs), and [the racing checkout test](../../../tests/BorrowBox.Infrastructure.Tests/Loans/CheckoutTests.cs). The current code has one item-level lock in conditional checkout; the stronger coordination schemes below are proposals for disposable experiments and capstone design.

## Identify the contested fact

Two checkouts for the same equipment contest one availability transition. Two checkouts for different items and the same borrower contest remaining capacity. Cancellation and expiry for one reservation contest its terminal reason and equipment release. A return and an inspection decision can contest the equipment lifecycle. Each race needs its own invariant and participating operations.

This is why adding a lock around one method rarely supports a universal concurrency claim. A lock protects code paths that acquire the same synchronization object. It does not protect unrelated mutations, another process, or a replacement object with the same business ID. The useful question is which fact the lock owns and which writers obey that ownership.

Begin every race analysis with a set of participants and initial state. If the participants can never overlap in the intended deployment, say so and justify the deployment constraint. If overlap is allowed, identify where each participant reads the contested fact and where it commits a decision based on that read.

## Linearization points are specific claims

A proposed linearization point is the moment at which an operation can be treated as taking effect atomically for a particular abstract contract. In the default TryCheckOutAsync path, the availability check and status mutation occur inside the same item lock. For competing calls on the same stable instance through that method, one can identify a decisive transition within the protected region.

That does not make CheckoutService as a whole linearizable for the relationship between equipment and loan. Loan addition occurs later, outside that item lock and through another store. The narrower operation has a coordinated point while the broader workflow still has an intermediate state. Keep the scope in the sentence whenever you use concurrency terminology.

The SQLite conditional update similarly provides an atomic database statement for a selected row condition under the provider's execution behavior, but the following read and loan insertion remain separate. A returned Task does not unify those boundaries. Design the business operation around the invariant rather than assuming atomic primitives compose into an atomic workflow automatically.

## Worked schedule: capacity and item ownership

Start with borrower B at two active loans under a maximum of three, with equipment E1 and E2 Available. A reads count two for E1; B's other request reads count two for E2. Both pass capacity, each wins its separate equipment claim, and both add a loan. Final count four violates capacity while equipment uniqueness is preserved. The same schedule can occur even when the item operations are perfectly synchronized.

A borrower-keyed coordinator can serialize the capacity decision and loan addition within one process. Different borrowers can proceed independently. However, a request may need both borrower and equipment authority. If one workflow acquires borrower then equipment while another acquires equipment then borrower, a waiting cycle becomes possible. The design must choose and consistently apply an acquisition order or use a coordinator that owns the combined operation.

Do not add nested locks casually to asynchronous methods. A synchronization primitive's usage rules and release behavior matter, especially across awaits. For this course, first write an abstract coordinator contract with acquisition, cancellation, and release semantics. Implement it only in the disposable capstone after the ordering and ownership rules are reviewable.

## Optimistic concurrency needs a checked version

A proposed version field lets an operation read state at version V and commit only if the stored version remains V. A successful update advances the version. A failed comparison signals a conflict requiring a fresh decision or a defined rejection. Reading a version without checking it atomically at the write provides no protection.

Apply this to reservation cancellation. The operation reads Active at version seven and plans Cancelled. Expiry may concurrently plan Expired from the same version. Only one conditional update should succeed under the proposal. The loser must not blindly retry the old decision against the new terminal state; it should reload and apply the product's duplicate or conflict semantics.

The equipment release still needs to agree with the winning reservation transition. A version on each row is useful evidence, but separate successful row updates can still leave a partial pair if the second fails. Combine optimistic checks with a transaction or explicit recovery protocol when the invariant spans records. Versions detect stale decisions; they do not substitute for every consistency mechanism.

## Define conflict handling before retries

Some conflicts are safe to retry from a fresh snapshot, such as a benign competing detail update under a policy that recomputes the intended patch. Other conflicts indicate a business loss, such as another borrower claiming the item. Retrying until success can change the meaning of the request or starve other participants. A retry policy must preserve the original intent and respect bounded effort.

For a checkout receipt, an identical operation retry after an unknown result should first resolve the original outcome. It should not immediately enter a new competition for equipment. For a capacity conflict, a fresh read may show the limit has been reached and produce a domain rejection. These are different paths even if both began with a concurrency exception.

Record the conflict's scope in diagnostics: equipment claim lost, borrower capacity changed, reservation terminal state changed, or stale detail version. A generic retry counter tells little about whether the system is healthy or whether a product rule is repeatedly contested. Useful observability follows the same ownership model as synchronization.

## Deterministic schedules beat uncontrolled load

To test the capacity gap, hold both callers after their count observations and release them together. To test optimistic conflict handling, hold both after reading version V, let one commit, then release the other. To test lock ordering, use explicit acquisition signals and bounded waits in a disposable coordinator test rather than hoping a deadlock appears under random load.

The schedule should be written as a sequence of named events that another learner can reproduce. Each gate needs a clear release condition and a timeout that fails the experiment with context. Avoid a barrier that requires two arrivals when one request can legitimately reject before reaching it; that would make a valid implementation look hung. Place the gate at a seam both chosen fixtures are guaranteed to reach.

Load testing has a different role. It can reveal throughput, contention, and operational behavior under realistic pressure after the correctness model exists. It is not a substitute for a small schedule that demonstrates one invariant violation. A thousand successful requests do not rule out a rare allowed interleaving that no request happened to exercise.

## Cross-process scope must be explicit

A static lock registry coordinates only participants sharing that registry in one process. A singleton store is singleton within its host container, not across every deployed instance. If two application instances use separate in-memory stores, they do not even share the same records. If they share a database, their in-process locks still do not coordinate database writers in the other process.

A proposed production design therefore needs coordination at the shared durable authority or another distributed mechanism with clearly defined failure behavior. This workbook does not ask you to build a distributed lock service. It asks you to avoid claiming cross-process correctness from same-process tests and to choose the simplest mechanism that matches the deployment you actually need.

For a teaching capstone, a same-process coordinated implementation can be a legitimate milestone. Label its boundary prominently and include a test demonstrating its invariant within that scope. Treat a later durable implementation as additional work with additional evidence, rather than renaming the milestone "production ready" because its unit tests are green.

## Independent exercises

**E16.1: Ownership matrix.** Map checkout, return, reserve, cancel, expire, and inspection completion to the resources they read and mutate. Identify two pairs of operations whose coordination cannot be solved by an equipment-only lock. State the invariant for each pair.

**E16.2: Controlled conflict.** Design a versioned cancellation-versus-expiry test with explicit gates. Specify the winner, loser result, equipment state, and receipt behavior. Explain how the test changes under a first-winner policy versus expiry-at-deadline precedence.

**E16.3: Scope review.** Review a proposal using a static borrower lock dictionary. List what it can establish, what all writers must do, how lock identity remains stable, and what it cannot establish across processes. Include a lifecycle strategy for entries without implementing an unbounded cache by accident.

## Review checkpoint

Your concurrency argument should name the invariant, participants, shared authority, decisive point, and failure response. Your test schedule should force the relevant overlap without relying on sleep. With those pieces, a reviewer can evaluate the design's actual scope instead of debating a vague claim of thread safety.
