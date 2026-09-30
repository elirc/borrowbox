# 01. Evidence and executable contracts

BorrowBox is small enough to understand, but it contains several different kinds of truth. A constructor can establish that an object was valid when created. A service can order operations so that known invalid input causes no mutation. A store can protect a particular update. An API can reject an unauthorized request. None of those facts automatically establishes the others. The first skill in this workbook is learning to make a claim at exactly the level your evidence supports.

The practical setting is a campus equipment desk. A member of staff sees an available microphone, selects a borrower, and submits checkout. The screen may show success, the service may return a loan, and the equipment object may be marked checked out. If you use those observations interchangeably, a defect can hide between them. Perhaps the response failed after state changed. Perhaps the equipment changed but the loan was never recorded. Perhaps the test observed the same object reference that the service mutated. Your task is to say what happened, where it happened, and who could observe it.

## Prerequisites that matter

You need comfortable reading knowledge of C# classes, interfaces, nullable references, tasks, exceptions, and collections. You do not need to memorize framework APIs. More important is understanding that a reference variable is not an independent copy of its object. Two variables may point at the same equipment item, so a mutation through one is visible through the other. An array of references copies the array structure; it does not clone every referenced item. This distinction will explain several apparently surprising test results.

You also need to distinguish the invocation of an asynchronous method from the transaction implied by a business request. Awaiting a store call means waiting for that call's completion. It does not make all earlier and later calls atomic. Similarly, a cancellation token communicates a request to stop; it does not rewind mutations. These statements are basic language and API reasoning, but BorrowBox turns them into concrete engineering consequences.

Begin with [the composition root](../../../src/BorrowBox.Api/Program.cs), [equipment contracts](../../../src/BorrowBox.Core/Equipment/Equipment.cs), and [the checkout service](../../../src/BorrowBox.Core/Loans/Loan.cs). Read only enough to trace dependencies, then return to the chapter. Reading every file first is usually less effective than following one narrow operation with a precise question.

## Draw an evidence map

The composition root selects singleton in-memory stores for equipment, borrowers, loans, inspections, and reservations. Services are scoped. A scoped service therefore does not imply scoped data ownership: multiple service instances can act on the same singleton store and the same contained objects. The default active-loan policy allows three loans. The registered clock is a system clock. A background reservation-expiry worker is also registered.

The project contains a SQLite equipment adapter, but the presence of that file does not mean the default API uses SQLite. Program explicitly registers the in-memory equipment store. Its readiness response also describes in-memory persistence and reports that it is not durable. Treat that as deployment evidence about the teaching default, rather than inferring the deployment from a package reference or a migrations directory.

Create a four-column notebook page: claim, owner, evidence, limitation. One row might state "invalid due date leaves equipment available." The owner is the checkout ordering together with loan construction. Evidence includes the service sequence and the existing invalid-date test. The limitation is that this particular rejection precedes the equipment effect; it does not establish recovery from a loan-store failure after checkout. Writing that limitation forces you to identify the next useful experiment.

Another row might state "equipment changes can be read from a new SQLite context." Evidence is the [SQLite adapter test](../../../tests/BorrowBox.Infrastructure.Tests/Equipment/SqliteEquipmentStoreTests.cs). That test keeps one in-memory SQLite connection open while using separate contexts. It establishes a useful context-boundary property. It does not demonstrate surviving a process restart, a machine crash, or closing the connection that owns the in-memory database. The word "persists" needs an explicit boundary.

## Worked example: a misleading green assertion

Suppose a test creates equipment, adds it to the in-memory store, invokes a service, and then asserts against the original equipment variable. The assertion can be completely legitimate: it observes the object's postcondition. But it does not prove an explicit save was necessary. The in-memory adapter stores the original object reference. If the service mutates that object and never calls SaveAsync, the original variable and a subsequent store lookup can still show the changed state.

Now imagine a proposed implementation that removes SaveAsync from an update service. A test that asserts only the original reference's Name can remain green. A SQLite-backed test that loads through a fresh context may fail because the adapter returns newly reconstructed domain objects and depends on SaveAsync to update rows. The two tests are answering different questions. Neither should be presented as a universal persistence test.

Write the trace using names rather than vague boxes. Let variable original refer to object O1. The dictionary entry for equipment ID E1 also refers to O1. GetAsync returns another reference to O1. UpdateDetails changes O1.Name. Every alias now sees that field value. SaveAsync assigns O1 to the same dictionary key. In this adapter, the mutation was already observable before that assignment. This is not a claim that saving is pointless; it is a claim that the abstraction's persistence intent is stronger than what the simplest adapter can reveal.

An effective test suite therefore needs complementary observations. A domain test checks validation and mutation. A service interaction check may verify that an explicit persistence boundary is used when its contract requires one. An adapter test verifies actual round trips. A request test verifies transport behavior and authorization. Later, a deployment test can examine restart behavior. Each has a role; requiring every property from one test usually makes that test slow and ambiguous.

## A vocabulary for exact outcomes

Use "rejected before effects" when known invalid input produces an error without changing business state. Use "partially applied" when one effect occurred and a later required effect failed. Use "committed but response unknown" when the durable effect may have completed but the caller did not receive confirmation. These are different outcomes with different retry implications. Calling all three "failure" loses the information needed to decide the next step.

In current checkout, constructing the Loan before TryCheckOutAsync protects the invalid-due-date case. The service still adds the loan after changing equipment. A loan-store exception at that later point can leave equipment checked out without a recorded loan. The correct current claim is narrow and useful: date validation precedes the checkout effect, while multi-store atomicity remains unimplemented. Avoid rewriting it into either "checkout is safe" or "validation does nothing." Both erase meaningful detail.

Exception codes are another part of this vocabulary. A BorrowBoxException code such as loan.due_date_invalid describes a specific rule failure. A generic I/O exception describes a different class of event. Tests that accept any exception may pass when the intended domain rejection is replaced by a null-reference defect or a configuration failure. Assert the intended kind and code where the exercise's contract is specific, and then assert the affected state separately.

## Guided investigation: classify five claims

Make five cards: equipment starts Available by default; asset tags are unique; staff authorization exists; all data survives restart; a rejected reservation never changes anything. For each card, decide whether it is supported as written, supported only with a narrower scope, or unsupported. Find the exact method, registration, or test that would settle the claim. You may read source, but do not run the entire application merely to obtain information already visible in a constructor.

The asset-tag card is particularly instructive. The constructor normalizes a tag, while the in-memory dictionary is keyed by equipment ID. Normalization is not uniqueness. An uppercase representation makes comparison easier, but it does not search the store or reject another item with the same normalized tag. Keep any proposed uniqueness rule separate from existing behavior, and inspect a database constraint before making claims about another adapter.

The reservation card needs a chosen failure point. Invalid expiry is rejected before Reserve mutates equipment in current source. A later reservation-store failure occurs after equipment mutation and SaveAsync. The unqualified word "never" therefore demands more evidence than that one validation test provides. Revise the card into two claims with distinct expected outcomes. This is the first step toward a useful fault matrix.

## Independent exercises

**E01.1: Claim ledger.** Produce eight rows covering a constructor rule, a service ordering rule, a store behavior, a registration, a time rule, an error code, a concurrency boundary, and a missing guarantee. Each row must link to an exact file and name the relevant symbol. Include one counterexample that would falsify an overbroad version of your claim. Do not count a test name alone as an explanation of what the test observes.

**E01.2: Two observers.** Design two tests for EquipmentService.UpdateAsync. One observes the returned item; the other observes data reloaded through a distinct persistence context. Explain why the second cannot be simulated simply by assigning the returned item to another variable. You may describe the SQLite setup from the existing adapter test rather than installing anything. Identify which test would expose a missing SaveAsync and why.

**E01.3: Negative evidence.** A reviewer says there is no transaction because they searched for the word transaction and found none. Write a stronger argument using the actual service interfaces and call order. Then state what your argument cannot rule out about an unseen alternative store implementation. The goal is to reason from observable contracts, rather than treating a text search as proof of a universal negative.

## Review checkpoint

Close the source and explain the default system to another learner in ninety seconds. Include where data lives, which object validates a due date, when checkout changes equipment, and what happens if the following loan write fails. If you cannot name the observation boundary for each statement, reopen only the relevant file. The deliverable is a precise model that can predict experiments, not a polished architecture slogan.

Before moving on, underline every absolute word in your notes: always, never, guaranteed, atomic, durable, unique. Keep the word only if you can state the conditions under which it is true. This practice is not timid writing. It produces stronger specifications because it exposes exactly where an implementation must enforce a property and exactly which experiment would catch its violation.

[Hints](hints-01-04.md) and [worked review](solutions-01-04.md) are separate so that you can attempt the reasoning first.
