# 22. Integrated review and engineering handoff

The workbook began with a small claim ledger and ends with a complete engineering review. You have examined local invariants, live references, validation frontiers, persistence gaps, API shapes, authorization boundaries, time, concurrency, cancellation, reporting, and two proposed capstones. The final task is to assemble those findings into evidence another engineer can use without relying on your memory or confidence.

Use [the course route](README.md), [the executed lab instructions](lab/README.md), [Program](../../../src/BorrowBox.Api/Program.cs), and [the health test](../../../tests/BorrowBox.Api.Tests/Operations/HealthTests.cs). This chapter defines completion criteria for the learning work and a disciplined handoff. It does not require implementing every optional proposal or running every application suite to call the workbook complete.

## Separate four kinds of deliverable

First, source observations describe what the current implementation does. Examples include the default in-memory registrations, candidate-first checkout validation, missing loan save contract, and inspection mutation ordering. Second, executed experiments record what a bounded harness actually produced. Third, test designs describe checks that have not yet run. Fourth, proposals describe desired future behavior and its enforcement plan.

A final report should label these categories directly. A proposed durable transaction does not become implemented because its diagram is detailed. A passing same-process observation does not become a restart guarantee because the word persistence appears in the chapter. Clear labels protect both the learner and the reviewer from accidentally promoting a useful artifact beyond its evidence.

This distinction also prevents unnecessary scope expansion. Completing the course means providing coherent instruction, usable exercises, separate solutions, reproducible laboratories, and honest verification. It does not mean silently shipping the capstones into the user's application. Learners can choose their implementation milestone and report it precisely.

## Build the final evidence index

Create an index mapping each important claim to a source symbol, test or experiment, expected observation, actual result if executed, and limitation. Reuse the claim ledger from chapter one, but update it with the later distinctions. For example, split "reservation expiry works" into deadline selection, equipment mismatch behavior, per-item mutation, batch progress, and worker scheduling.

Keep the index concise enough to review. Link to detailed traces rather than repeating entire chapters. A row should answer what is known and where to verify it. If the evidence is a code reading, say so. If a test was inspected but not run, do not list its expected result under executed checks. The reviewer should not have to infer evidence status from vague wording.

Include negative findings constructively. Inspection's blank-damage ordering and the loan-add persistence gap are valuable learning discoveries. Describe their trigger and state consequence, then link to the disposable observation and proposed repair. Avoid presenting them as a broad claim that the whole application is unusable or insecure. Specific defects support specific decisions.

## Review the default deployment honestly

Program registers in-memory stores and exposes readiness information that says persistence is in-memory and durable is false. The health test checks that message and echoes a supplied correlation ID in the response header. This is useful operational transparency, but readiness does not validate every lending invariant or prove that all future requests will succeed.

The SQLite equipment adapter exists without being selected as the teaching default. A deployment proposal that changes that registration must account for the other stores and their relationships. Your handoff should make that explicit so nobody mistakes the presence of a database adapter for a complete durable deployment. The earlier adapter substitution checklist belongs in the proposed-change section.

The demo authentication handler trusts accepted role headers and does not bind a verified subject to a borrower. Explain this as the current teaching trust model. Any production identity and ownership design remains proposed unless actually implemented and tested in a separate authorized change. This is an evidence boundary, not a reason to obscure or remove the useful local authorization examples.

## Explain changes by outcome and scope

If you implemented a disposable repair, describe the concrete trigger, previous outcome, new outcome, and test. For example: blank damage previously changed equipment to Inspection before rejection; candidate-first construction now rejects while equipment remains Available in the generated copy. Then state that persistence and concurrency guarantees are unchanged by that local repair.

Avoid a chronological diary of every edit unless an abandoned approach explains a tradeoff. A reviewer needs to understand the final design and evidence. Group changes by the invariant they address, and distinguish behavior changes from testability changes such as clock injection. This makes review faster without hiding important limitations.

For a capstone, lead with the user-visible capability: interrupted checkout can be resolved by stable operation identity, or delayed reservation cancellation cannot release a newer claim. Then explain the commit and ownership mechanisms that support it. Architectural terms should clarify the outcome rather than replace it.

## A final oral review

Explain checkout to a peer using five landmarks: captured time, eligibility and capacity observation, candidate validation, conditional equipment claim, and loan insertion. Ask the peer to interrupt at any landmark with a failure or concurrent change. Your answer should predict state and caller outcome without assuming a transaction that does not exist.

Repeat for return, reservation expiry, and inspection completion. These workflows are similar enough to invite superficial answers but different enough to expose whether you really understand their order. Return mutates an existing loan before equipment save; expiry skips some mismatches; inspection can reject after a prior mutation. A correct explanation names those differences instead of repeating one generic safety slogan.

Finally, explain one proposed improvement and its proof boundary. If you choose a borrower lock, name the same-process scope. If you choose a database transaction, name the records and contention mechanism. If you choose a receipt, explain payload identity and retention. The peer should be able to state a counterexample outside your declared scope, showing that the limitation is understood.

## Evidence freshness and source drift

The generators copy current selected source and record hashes. That helps detect when an old lab result no longer describes the current implementation. A future source change can legitimately break a characterization observation by fixing a limitation. Review the source map and test intent before restoring old behavior just to make the observation green.

Keep the distinction between file equality and behavior equality. Matching source hashes show that the selected files did not change, but runtime configuration or dependency behavior can still affect execution. Conversely, a documentation-only edit changes a file hash without changing the application. Use hashes as provenance evidence, not as a substitute for understanding the relevant execution boundary.

For this workbook's verification, the original application and test baseline contains fifty-three files. The reported comparisons cover those files. Course additions and disposable outputs are intentionally new artifacts. Do not write that the entire machine or every repository was unchanged when the check only covered this scoped set.

## A disciplined next-step backlog

Rank future work by the concrete failure it addresses. A candidate-first inspection repair is small and has a focused regression test. A durable lending transaction is larger and requires schema, identity, concurrency, and recovery decisions. A response DTO improvement controls API evolution but does not fix storage. A reporting snapshot feature answers a consistency need that a best-effort dashboard may not have.

For each item, record the trigger, affected invariant, proposed owner, evidence needed, and deployment scope. Avoid a backlog of vague nouns such as security, reliability, and performance. The specificity developed in the workbook should carry into planning. An engineer should know what observation would demonstrate that an item is done.

Optional further reading or stretch features should not block the completed learning path. Collection, repair work orders, notification outboxes, and production identity integration can remain clearly scoped proposals. The course is complete when its promised chapters, exercises, solutions, and bounded checks are present and accurately reported, not when every imaginable lending feature has been built.

## Independent exercises

**E22.1: Evidence handoff.** Produce a one-page index with ten claims spanning domain, service, adapter, API, authorization, time, concurrency, cancellation, reporting, and operations. Label each as source-observed, executed, designed, or proposed, and include one specific limitation per claim.

**E22.2: Review conversation.** Present one current workflow and one capstone to a peer. Record three counterexamples the peer raises and revise your explanation or design accordingly. If working alone, use the chapter's interruption questions and write both the challenge and response explicitly.

**E22.3: Prioritized follow-up.** Select three future changes with different sizes. Give each a trigger, invariant, implementation boundary, and acceptance evidence. Explain why an optional feature is deferred without calling the completed workbook unfinished.

## Completion standard

You have completed the route when your artifacts predict the demonstrated behaviors, your laboratories are reproducible in disposable outputs, your solutions distinguish current code from proposals, and your final report makes no unsupported durability or authorization claim. Preserve the evidence and move to a chosen implementation project with a clear boundary. The value of the course is the reasoning you can transfer, not the number of patterns you can name.

Before handing over a prototype, include the exact command and observation boundary needed to reproduce its strongest claim. This small practical detail prevents a sound analysis from becoming unusable evidence.
