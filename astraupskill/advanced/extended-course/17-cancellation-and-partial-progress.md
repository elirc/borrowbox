# 17. Cancellation and partial progress

Cancellation is a request to stop work, not a command to reverse history. BorrowBox passes CancellationToken through its services and stores, and the in-memory adapters check it at method entry. Those checks are useful, but their placement matters because domain mutations can occur between awaited store calls. This chapter develops a precise cancellation ledger for single operations and batches.

Read [the in-memory equipment store](../../../src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs), [loan store](../../../src/BorrowBox.Infrastructure/Loans/InMemoryLoanStore.cs), [reservation services](../../../src/BorrowBox.Core/Reservations/Reservation.cs), and [the expiry worker](../../../src/BorrowBox.Api/Reservations/ReservationExpiryWorker.cs). Use a disposable wrapper to trigger cancellation at named seams; do not use the application UI or machine process controls to create uncontrolled failures.

## A token is observed at particular points

The in-memory adapters call ThrowIfCancellationRequested before accessing or changing their dictionaries. Their operations then complete synchronously through completed tasks. If cancellation is already requested on entry, the store throws before that method's dictionary action. If a domain object was mutated earlier by the service, that earlier mutation is outside the store check and remains observable through aliases.

ReservationService constructs a candidate, calls equipment.Reserve, and then invokes SaveAsync with the token. A wrapper that cancels the token just before delegating that save can cause the real store to throw while equipment is already Reserved. The reservation has not yet been added. This is a cancellation-shaped partial outcome, not a persistence exception, but the state reasoning follows the same effect boundary.

Checkout changes equipment through TryCheckOutAsync before adding the loan. Cancellation observed by LoanStore.AddAsync can leave the equipment claim applied. Return mutates loan and equipment before SaveAsync observes the token. A canceled task therefore does not establish a universal no-effects postcondition. Clients and tests must avoid that assumption.

## Cancellation before work is a simpler case

Use an already-canceled token and valid fixture data. Checkout's first equipment lookup checks the token and rejects before the later business mutations. Reservation creation similarly stops at its first store read. This is useful evidence for honoring cancellation early, but it does not cover cancellation after the effectful frontier.

Do not overgeneralize a passing pre-canceled-token test into a claim that the entire operation is cancellation safe. Name the exact boundary: cancellation before the first store read produces no new business mutation in that path. Then add an effect-adjacent experiment if the product needs to understand partial outcomes. The pair of tests teaches more than several variations of the same initial canceled token.

Also distinguish cancellation from domain rejection in result handling. A user canceling an operation has not necessarily violated a lending rule. Mapping cancellation to equipment.not_available would obscure the cause and could produce misleading retry behavior. The current custom BorrowBoxException handler only handles its domain exception type; broader transport behavior requires its own evidence.

## Worked trace: cancel after reservation mutation

Begin with Available equipment, an active borrower, and an empty reservation store. Use a token source that is initially not canceled. The service loads both records and constructs a valid reservation. It calls Reserve, changing equipment to Reserved. The save wrapper cancels the source and forwards the token to InMemoryEquipmentStore.SaveAsync, which throws at its entry check.

After catching the expected cancellation in the experiment, inspect the original item and a store lookup. Both show Reserved because they refer to the mutated object. The reservation store remains empty because AddAsync was never reached. The caller has no returned reservation. Write all four observations: cancellation, equipment state, store membership, and absence of a returned result.

This experiment does not prove that a database save would behave identically. A detached object may remain changed only locally, while a database operation may have committed before cancellation was observed in another schedule. Use the real adapter and a specific seam for stronger persistence claims. The conceptual lesson is that cancellation timing must be part of the contract.

## A batch can have successful prefixes

ReservationExpiryService processes candidates sequentially and saves each equipment item. If cancellation is observed while processing a later candidate, earlier iterations are not automatically undone. The method can exit without returning its accumulated count even though some reservations have expired and equipment has been released.

For a deterministic test, return an ordered pair of due reservations from a fake store. Allow the first iteration to complete. Cancel just before the second equipment lookup or save, depending on the question. These two seams produce different second-item observations. The first case leaves the second pair untouched; the second may occur after both second-item domain mutations with live references.

A proposed batch result can report completed, skipped, and interrupted work, but it needs a way to return or durably record that information when cancellation propagates. Catching cancellation solely to return a partial count changes the method contract and may hide shutdown intent from the worker. Design the reporting channel deliberately, perhaps using durable progress records or structured logs in addition to the final result.

## Shutdown is not an ordinary user retry

The worker passes its stopping token to the timer and expiry service. A shutdown request can therefore interrupt waiting or a store boundary during a sweep. The current code does not persist a sweep checkpoint. With in-memory stores, a process restart also loses the underlying lending data, so a durable resume promise would be inappropriate for the default teaching configuration.

In a proposed durable deployment, restarting a sweep from current stored state may be simpler than persisting an iterator position. Successfully terminal reservations are excluded by the Active filter, while unresolved mismatches remain visible for reconciliation. However, this only works reliably when each item transition is itself consistent and repeatable under the chosen ownership contract. A batch restart cannot repair an unsafe per-item operation automatically.

Decide which work may continue briefly during shutdown and which should stop immediately. A graceful completion window can be useful, but it must be bounded and distinguish committing already accepted work from accepting new work. This chapter does not prescribe host settings; it requires the product and operational policy to name the intended boundary before implementation.

## Cleanup and compensation are different

Releasing a semaphore or disposing a scope in a finally block is cleanup of local resources. Reversing an equipment claim is a business compensation. The first is generally necessary regardless of success or failure; the second requires ownership and policy evidence. Do not put an unconditional equipment.Return into a finally block merely because cleanup belongs there.

A proposed coordinator should release its synchronization handle when cancellation occurs while holding it, while preserving any committed business result. If a transaction is still uncommitted, its disposal or rollback behavior belongs to the transaction contract. If commit already succeeded, cleanup must not pretend to undo it. Separate these concerns in code and in the failure ledger.

This separation also improves tests. One test can verify that a canceled waiter does not leak a lock and block later work. Another verifies the business outcome at a particular effect boundary. Combining them into one large scenario makes it harder to tell whether a failure is resource management or state consistency.

## Design a caller-facing cancellation story

A client may cancel because the user navigated away, a timeout elapsed, or the network disconnected. The server may still have performed effects. A proposed operation receipt lets the client later query the result using the same operation ID. Without that mechanism, the client should avoid asserting that cancellation means the request never happened.

For a durable checkout capstone, specify whether an accepted operation continues to commit after client disconnection or attempts to abort before commit. Either policy must handle the boundary where cancellation races with commit. The response cannot be delivered to a disconnected client, so later outcome discovery matters more than the exact exception text produced internally.

For the current in-memory teaching application, document the limitation rather than inventing a receipt. The laboratory can demonstrate partial state and explain what a stronger contract would require. That is a complete educational outcome even before the proposed feature exists.

## Independent exercises

**E17.1: Cancellation ledger.** Compare pre-canceled checkout, cancellation before loan addition, cancellation before return save, and cancellation during the second expiry item. Record token observation, caller outcome, and all relevant state. Specify the exact default adapter or wrapper used for each row.

**E17.2: Resource cleanup.** Design a test for a borrower coordinator that is canceled while waiting and another canceled while holding the coordinator. State how later requests prove no handle leaked. Keep the business state assertion separate from the synchronization cleanup assertion.

**E17.3: Client policy.** Write a short user-facing flow for an interrupted checkout under a proposed receipt design. Include pending, completed, and unresolved outcomes, and explain why creating a new request immediately can be unsafe. Contrast it with what the current application can actually report.

## Review checkpoint

Your cancellation explanation should name when the token is observed and which effects precede that point. Your design should distinguish local cleanup, rollback of uncommitted work, compensation of owned effects, and discovery of a committed outcome. These distinctions turn cancellation from a vague error-handling concern into a predictable part of the workflow contract.
