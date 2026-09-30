# Hint ladders for chapters 01 through 04

Use one hint at a time. Return to the exercise after the first hint, and open the next only if you remain stuck. These hints point at evidence rather than supplying polished answers. The [full solutions](solutions-01-04.md) contain the worked arguments and review criteria.

## Evidence and contracts

For E01.1, start with the difference between a field assignment and a call to a collaborator. The constructor can normalize a tag without consulting a store. For the next hint, locate the dependency registrations before deciding which adapter is active. For the final hint, write one statement that is true only for invalid input and contrast it with a failure occurring after the first state change. Your ledger should make the difference visible without requiring a reader to guess your assumptions.

For E01.2, draw two arrows to the same circle to represent two aliases. A second variable does not create a second persistence context. Next, inspect how the SQLite adapter reconstructs EquipmentItem from a row. Finally, ask what would happen if the service changed a detached object and returned it without saving. One observer can see the changed object while another sees the old stored row.

For E01.3, list the methods that the checkout service can call on each store. Look for a shared operation that commits both equipment and loan. Next, trace what occurs between the successful conditional equipment transition and the loan addition. Finally, remember that an interface does not expose every internal implementation detail. Your strongest claim concerns the visible implementation and default adapters, not all conceivable implementations of the interfaces.

## Equipment and ownership

For E02.1, do not assume Return has the same guard as ReturnService. Next, test both the resulting state and the rule code for a rejection. Finally, choose one method whose accepted states surprise you; explain the surprise using the exact source rather than correcting the table to match intuition.

For E02.2, construct the equipment and loan independently. Ask whether either constructor queries the other. Next, consider whether one object's status can prove that a relationship exists in a different store. Finally, an owner must be reached by all relevant workflows, including reconstruction or import. A rule enforced only on the happy-path API route is narrower than a repository-wide invariant.

For E02.3, label instances O1 and O2 while giving them the same business ID E. The lock target is the object, not the GUID. Next, use SaveAsync's dictionary assignment to think about replacement. Finally, state a stable-key coordination proposal without claiming that the current code already implements it.

## Borrowers and capacity

For E03.1, compare normalized CollegeId with dictionary identity. Next, imagine two concurrent requests that both perform a "does it exist?" query before either inserts. Finally, decide whether a duplicate submission is a conflict or a retry of the same logical enrollment. The answer changes the response contract but does not remove the need for atomic uniqueness enforcement.

For E03.2, distinguish a business decision time from a commit time. Next, decide which one the proposed product contract uses. Finally, a second read does not prevent a change immediately after the read. Explain the coordination needed only if your chosen contract requires the later state to govern the operation.

For E03.3, use the predicate count greater than or equal to maximum for the sequential proof. Next, pause two requests after both have observed the same initial count. Finally, draw two different equipment items so that an item lock cannot accidentally solve the borrower problem in your example.

## Reservation effects

For E04.1, inspect the constructor's comparison operator carefully. Next, keep the clock fixed for all three cases. Finally, include both collection membership and equipment status; an exception alone leaves the ordering defect invisible.

For E04.2, number the statements in ReserveAsync. Next, distinguish loading an equipment object from calling its Reserve method. Finally, the order of two invalid conditions may be an observable current behavior without being a permanently promised API policy.

For E04.3, make the double fail before recording anything. Next, retrieve equipment after the failure through the same adapter. Finally, explain why shared aliases can agree without any database round trip. Use the SQLite adapter's reconstruction behavior as a contrast, not as evidence that your new double tests SQLite.

For E04.4, ask what state the compensation assumes it still owns. Next, introduce another legitimate transition between the original effect and the attempted compensation. Finally, identify an operation identifier or version that could let compensation reject stale ownership. A universal inverse method is not enough when the surrounding state can change.
