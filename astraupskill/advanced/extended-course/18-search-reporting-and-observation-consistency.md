# 18. Search, reporting, and observation consistency

Read-only operations still have contracts worth analyzing. BorrowBox search filters and paginates equipment, while reporting groups states and exports CSV. These operations do not intentionally mutate equipment, but they observe shared mutable objects and expose data to users. This chapter explores ordering, grouping, boundary arithmetic, escaping, and the meaning of a report taken while lending activity continues.

Read [EquipmentService.SearchAsync](../../../src/BorrowBox.Core/Equipment/Equipment.cs), [InventoryReportService](../../../src/BorrowBox.Core/Reporting/InventoryReportService.cs), [search tests](../../../tests/BorrowBox.Infrastructure.Tests/Equipment/EquipmentSearchTests.cs), [report tests](../../../tests/BorrowBox.Core.Tests/Reporting/InventoryReportTests.cs), and [report endpoints](../../../src/BorrowBox.Api/Reporting/ReportingEndpoints.cs). The deliverable is a report contract and a discriminating test plan, not an unrequested reporting feature.

## Search order is part of the result

SearchAsync rejects page values below one and page sizes outside one through fifty. It loads all equipment through ListAsync, filters by optional query and category, orders by AssetTag, materializes the filtered array, and then applies Skip and Take. Total is the filtered count before pagination. The existing test demonstrates filtering before pagination and asset-tag order with a small fixture.

Query matches Name or AssetTag using ordinal case-insensitive containment. Category uses ordinal case-insensitive equality. A blank or whitespace-only query or category disables that filter. A nonblank value is not trimmed before comparison in this method, so a query containing surrounding spaces can behave differently from the trimmed input a user expects. Do not assume constructor normalization also normalizes every search parameter.

The ordering key is AssetTag without an explicit tie-breaker. The in-memory adapter can contain two different IDs with the same normalized tag, so equal-key ordering has no business identity tie-breaker in this query. The SQLite model's unique index narrows that situation for that adapter, but the service contract itself does not express uniqueness. Stable paging across changing data needs more than a single sort key in a method body.

## Pagination arithmetic deserves boundaries

The skip expression is (page - 1) multiplied by pageSize using integer values. The method validates positivity and size but does not impose an upper page bound or use an explicit checked arithmetic expression. An exercise should examine extreme positive page values rather than assuming every accepted input produces the intended large offset. Exact runtime behavior should be observed in a bounded test before being described as a specific response.

A proposed fix can compute the offset in a wider checked representation, reject out-of-range offsets, or choose a different pagination contract. The product should decide whether a far-beyond-end page returns an empty page or a validation error. That decision is separate from accidental arithmetic overflow. Tests should distinguish the intended policy from implementation artifacts.

Offset paging also shifts when items are inserted or removed between requests. A user can see duplicates or miss items while moving from page one to page two under concurrent changes. A proposed cursor-based query can improve traversal semantics, but it needs a stable ordered key and a defined consistency model. This chapter asks you to describe the limitation before choosing a more complex feature.

## Summary groups do not freeze objects

SummarizeAsync obtains a collection, records its Count, groups by Status.ToString, and groups by Category. With the default in-memory store, the collection is an array of references to mutable equipment. Another operation can change a member's status or category between observations. The array's membership may be fixed for that lookup while the members' values continue changing.

The status and category groups are computed in separate passes. Their totals can each match the item count while reflecting observations taken at different moments. A numerically consistent summary is not necessarily a transactionally consistent snapshot of all fields. If the product only needs a best-effort dashboard, this may be acceptable; label the report accordingly.

Category grouping uses the category strings as keys without the search method's explicit case-insensitive comparer. Because equipment trims but does not uppercase Category, Media and media can form separate summary groups while a category search matches both case-insensitively. This is a concrete behavior difference to test and discuss. A proposed canonical category catalog would change the data model rather than merely the report formatting.

## Worked mixed-observation scenario

Imagine two items, camera C in Media and microphone M in Audio. The summary begins while both are Available. During the status grouping, another operation checks out C. Before the category grouping, a staff update changes C's category to Production. Depending on the precise schedule, the returned groups can reflect different stages of those changes.

Do not claim one exact schedule from the plain in-memory loop without instrumentation. Instead, build a controlled reporting model or a snapshot adapter experiment that lets you pause between observations. The source establishes that mutable references are used and passes are separate; the experiment establishes a particular mixed result. Keep those evidence levels distinct.

A proposed snapshot contract could copy the selected scalar fields into immutable report rows under a defined read boundary. Copying without coordination still observes each field at some time, so the strongest snapshot promise requires a coordinated database read or another explicit mechanism. An immutable result prevents later mutation of the result; it does not retroactively make its construction atomic.

## CSV escaping has a specific purpose

ExportCsvAsync writes a fixed header and rows ordered by AssetTag. Escape surrounds values containing comma, quote, carriage return, or newline with quotes, and doubles embedded quotes. The existing test checks a comma in the name and a quote in the category. This is evidence for those delimiter and quoting cases, not a complete export-consumer compatibility test.

Add cases for embedded newlines, carriage returns, empty-looking whitespace after constructor trimming, and multiple quotes. Verify the resulting rows with a parser in a separate exercise if the intended consumer requires round trips. Counting newline characters alone is not a valid row-count test when quoted fields can contain newlines. The representation permits content that spans physical lines.

CSV syntax escaping is distinct from spreadsheet formula interpretation. A value beginning with a formula marker is not transformed by this Escape method unless it also contains one of the delimiter characters. If spreadsheet-safe export is a product requirement, define and test a separate policy appropriate to the consumer. Do not claim that doubling quotes neutralizes all downstream interpretation of cell content.

## Authorization and data minimization still matter

The reporting route group requires StaffOnly. That is a visible access boundary for summary and export endpoints under the configured demo authentication scheme. The report itself includes equipment fields rather than borrower details, which limits one category of exposure. The demo role trust limitation from chapter twelve still applies; a staff policy name is not production identity verification.

A future lending report that includes borrower names or loan histories requires its own data-access and retention design. Do not add personal fields simply because the service can reach those stores. Start from the reporting question and include the minimum fields needed to answer it. A reconciliation report may need IDs and states while a front-desk receipt may need a different projection.

The export endpoint returns text/csv. The endpoint source does not add an explicit download filename or streaming implementation. ExportCsvAsync builds the complete output string in memory after loading all items. A proposed large-inventory export should consider bounded memory, cancellation during generation, and a stable snapshot, but those are additional requirements rather than current guarantees.

## Guided investigation: one dataset, three views

Create four invented items with distinct tags, two category spellings differing only by case, and one name containing a comma and quote. Compare a category search, the summary groups, and the CSV output. Predict the differing category semantics before execution. This exercise connects three methods through the same data without assuming their comparison policies are identical.

Then design a boundary test for a very large page number without creating a huge dataset. The arithmetic issue depends on parameters, not millions of stored items. Record the actual result in a disposable harness and propose a documented response policy. A small fixture can reveal a large-input boundary when the experiment targets the correct mechanism.

## Independent exercises

**E18.1: Search contract.** Specify cases for blank filters, surrounding-space filters, case-insensitive matching, duplicate tags, invalid page size, and extreme positive page. Separate current behavior from a proposed normalized-search policy and avoid assuming database uniqueness in an in-memory test.

**E18.2: Report snapshot.** Propose either a best-effort summary contract or a consistent snapshot contract. Define the observation boundary and what a user may infer from Total, ByStatus, and ByCategory. Give a test that would distinguish your stronger promise from the current live-reference behavior.

**E18.3: Export consumers.** Design delimiter-escaping tests and a separate spreadsheet-interpretation policy. Explain why those are different concerns. Include a multiline field and a leading formula-like value, and state which checks are executed versus proposed.

## Review checkpoint

Your report contract should explain comparison rules, ordering, pagination boundaries, observation consistency, and consumer interpretation. Read-only code can be correct for a modest dashboard while insufficient for an audit snapshot or large export. Naming the intended use lets you improve the right boundary without burdening every query with unnecessary guarantees.
