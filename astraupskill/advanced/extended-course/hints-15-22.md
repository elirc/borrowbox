# Hints for the final route and capstones

For chapter fifteen, invent the wrong implementation before adding another assertion. Moving validation after mutation preserves the exception but changes state. Removing SaveAsync can preserve alias observations while breaking a real round trip. A useful fixture makes those differences visible without requiring unrelated setup.

For chapter sixteen, name the shared fact rather than starting with a lock type. Different equipment items can still compete for one borrower's capacity. A version must be checked at the write, and two versioned rows still need a consistent relationship. Draw the schedule before choosing a synchronization mechanism.

For chapter seventeen, put a mark at every token check and a different mark at every domain mutation. A cancellation observed after a mutation does not erase it. Cleanup of a local synchronization handle and compensation of business state have different conditions. Keep them as separate assertions in your test plan.

For chapter eighteen, compare the exact string comparers and the order of filtering, materialization, and pagination. An array of objects does not freeze their fields. CSV quoting protects field structure, while a spreadsheet can interpret a syntactically valid field in additional ways. Separate those contracts before choosing tests.

For chapter nineteen, a generator's output refusal is setup evidence, while a harness assertion is behavior evidence. Current source plus a checked mutation anchor avoids signature drift. A source map explains provenance but does not prove the harness itself is correct. Review both the transformation and the observer.

For the lending capstone, write the completed receipt and business records inside the same proposed commit set. Then force two different-item requests to compete for the last borrower slot. If your design passes only same-item contention, it has not solved the capacity problem. A retry should preserve operation identity instead of starting a new logical request.

For the reservation capstone, put the owning reservation ID on the claim or provide equivalent ownership evidence. Releasing any Reserved item is too broad. Choose the deadline policy before implementing cancellation and expiry, and record effective time separately from processing time when they answer different questions.

For the final review, use four labels: source-observed, executed, designed, and proposed. A clear design can be complete educational work without being deployed functionality. Your handoff should let another engineer find the exact evidence and the next missing boundary without guessing which claims were actually tested.
