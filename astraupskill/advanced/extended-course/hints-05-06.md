# Hints for checkout and return investigations

For E05.1, distinguish allocation from publication. A generated GUID is a local value until an external observer can use it or a store records it. Then ask what changes if a proposed recovery protocol deliberately records a pending operation before the business result is complete. Its status and ownership become part of the contract, so the pending record cannot be presented as an ordinary successful loan.

For E05.2, read the two awaited expressions in SqliteEquipmentStore.TryCheckOutAsync separately. The update result is evaluated before the read begins. Next, ask whether an exception in the read proves that the update affected zero rows. Finally, separate what an instrumented server can know from what a client receiving a failed request can know.

For E05.3, write the request identity beside its payload. Compare both when deciding whether a retry is equivalent. Next, think about a receipt that has not reached a final state and a receipt that has been removed. Finally, name the shared commit boundary rather than assuming that storing a receipt anywhere makes the entire operation idempotent.

For E06.1, number the guards and mutations in ReturnService, including the nested guard in Loan.Return. Next, remember that equipment.Return has no local guard. Finally, use the real in-memory stores for the specified save-failure row; a detached model would produce a different observation and should have its own row.

For E06.2, an added loan SaveAsync makes one intention explicit but does not merge two writes. Next, describe the state visible between those writes. Finally, if the response is lost after commit, identify the stable data that a later process can query. A local boolean variable disappears with the process and cannot serve as durable outcome evidence.

For E06.3, a retry asks for the same logical result, while a correction asks to change a previously accepted result. Next, consider who is allowed to request a correction and what earlier value must remain auditable. Finally, do not make a correction occur accidentally because a repeated call supplied a new timestamp.
