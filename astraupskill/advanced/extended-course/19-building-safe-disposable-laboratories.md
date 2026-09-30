# 19. Building safe disposable laboratories

A learning lab should make experimentation easy without making the original project fragile. BorrowBox already contains user changes and evolving method signatures, so restoring old source snapshots over current files is a poor exercise mechanism. This chapter explains the course's explicit-output generators and teaches you to extend them while preserving source provenance, bounded execution, and honest evidence.

Read [the original generator](../lab/generate_lab.py), [its recipe](../lab/recipe.json), [the extended generator](lab/generate_observations.py), and [the lab instructions](lab/README.md). The deliverable is a lab design review and a small proposed extension. You do not need to install packages, clone another repository, or modify the application to complete the analysis.

## A lab boundary begins with its output path

The original generator requires an absolute output path, resolves it, rejects a path inside the application project, and rejects any existing output directory. These checks prevent routine mistakes such as targeting the repository itself or overwriting a previous experiment. The generator creates its output only after reading sources and validating deliberate-edit anchors.

The documented commands choose a new temporary directory with a generated identifier and print its path. This is convenient, but the important property is explicit ownership: the learner knows exactly which directory may be edited. A script should not infer a destructive destination from the current working directory or silently clean an existing folder to make room.

The refusal checks are guardrails for ordinary usage, not a security sandbox against a hostile local actor racing filesystem changes. State that boundary accurately. In this course, the expected operator is a learner following the explicit-output workflow on their own files. The application remains untouched, and no cleanup command is necessary to run or review the lab.

## Copy current source, then apply bounded defects

The recipe names selected domain and in-memory adapter files. The starter mode reads their current bytes, normalizes text for its controlled replacements, and checks that each expected anchor occurs exactly once. If source drift makes an anchor absent or ambiguous, generation fails rather than guessing where to edit. This preserves signature compatibility better than distributing a whole historical class with outdated dependencies.

The deliberate changes are small and purposeful: move reservation construction after equipment reservation, and move loan construction after conditional checkout. Those mutations preserve successful compilation while violating a specific unchanged-state contract. A learner can reason from one moved boundary rather than first repairing unrelated compile errors caused by an obsolete snapshot.

A future lab mutation should have the same discipline. Name the intended defect, identify the exact source anchor, specify the affected assertion, and reject ambiguous application. Do not use a broad text replacement that changes every similar line in unrelated methods. A lab generator is a small source transformation tool and deserves clear preconditions.

## Provenance makes results reviewable

SOURCE-MAP.json records original and copied hashes for each application file, together with deliberate defect descriptions. That allows a reviewer to distinguish source drift from learner edits and from planned starter mutations. The map does not automatically prove the harness is correct; it establishes which source bytes the experiment used.

The extended inspection suite copies one additional real adapter and adds its hash to the map. It uses a separate harness template while leaving the foundation harness available. This is an example of a narrow extension: add exactly the source needed by the experiment, rather than copying every build artifact, dependency directory, or backup snapshot into the lab.

Keep a run record beside your course notes containing output path, generator mode or suite, expected result, actual result, and source-map identity. If a later application change alters behavior, you can tell whether an old result applies. A statement that "the lab passed last week" is much less useful without knowing which copied source and assertions produced the result.

## Build isolation should be deliberate

The generated project targets the repository's .NET generation and copies global.json. It creates local Directory.Build.props and Directory.Build.targets files so unrelated ancestor build customizations do not silently alter the lab. Its NuGet configuration clears package sources, and the console project has no external package references. These choices keep the exercise small and reproducible with the installed SDK.

This does not mean every possible future exercise needs no dependencies. A SQLite integration lab would need an explicit dependency and restoration plan, or an existing tested project boundary. It should be a separate documented experiment rather than quietly adding downloads to a previously package-free command. The learner should know when the evidence crosses from domain/in-memory checks into provider-specific execution.

Avoid starting the API merely to test a constructor rule. The console harness can call actual copied domain and service code directly. Conversely, do not claim HTTP authorization from that harness because it never starts the request pipeline. Isolation makes some claims cheaper to test while intentionally excluding others. The documentation must state both sides.

## Expected failures must be unmistakable

The original starter is expected to compile and then fail two behavioral assertions. A compilation failure, missing SDK, or generator refusal is a setup problem, not the intended learning result. The reference mode is expected to pass its six checks. The extended foundation and inspection suites have their own documented passing counts.

When learners add checks, those counts change. A lab report should identify whether it used the unchanged baseline or a modified harness. Do not keep quoting six passes after adding three new cases. Likewise, an observation test that intentionally detects a current limitation should be named differently from a desired regression contract.

A good harness returns a nonzero exit code when an unexpected assertion fails and prints the failed case name. It should catch exceptions only around individual cases so the report identifies which experiment failed, while still treating unexpected exception types as failures. Avoid swallowing errors and printing a reassuring final message regardless of the actual result.

## Extensions need their own scope statement

Suppose you add a before-add/after-add reservation comparison. The extension should say that it studies in-memory outcome ambiguity and does not establish durable recovery. Suppose you add a detached-store model. It should say that the model changes ownership to illustrate a contract issue and is not the real SQLite adapter. Those labels prevent a useful teaching simplification from becoming false application evidence.

For a concurrency extension, include a bounded timeout and a deterministic gate. Document what event releases the gate and why the selected fixtures reach it. For a cancellation extension, name the seam at which the token is canceled. These details make the lab reproducible without requiring the learner to guess scheduling delays.

Do not turn every chapter into another generator mode. A small number of coherent suites is easier to understand than dozens of flags with overlapping mutations. Independent exercises can remain implementation assignments in a generated copy, with hints and solutions explaining the intended reasoning. The workbook's completeness does not require shipping every proposed feature as finished application code.

## Verify isolation as an invariant

Before and after running a new generator or lab, compare hashes of the original application and test files relevant to the task. The course work used a baseline of fifty-three such files and found no changes after its bounded observations. This is evidence about those files, not a claim that every unrelated file on the machine was monitored.

Test both existing-output refusal and inside-project refusal. Also inspect the generated source map and ensure copied paths remain under the chosen output. Do not test destructive behavior by pointing at valuable directories and hoping the generator is safe; the refusal path should be exercised with known, controlled destinations and no deletion step.

Keep output artifacts until review is complete. Removing them immediately can erase the exact experiment behind a reported result. If you later clean them up manually, use the printed absolute path and confirm it is the disposable directory you intended. Cleanup is separate from learning and should not be embedded in a command sequence that could target the original project accidentally.

## Guided design review

Review the inspection extension as if another learner submitted it. Confirm it selects the intended template, reads the additional source before generation, delegates output validation to the original generator, writes only within the newly created output, and records the added source hash. Then inspect the harness cases and identify which are current limitations versus ordinary contracts.

Propose one improvement to evidence clarity without broadening application scope. Examples include a suite name in the generated README, a run timestamp in a separate result artifact, or a source-drift explanation. Avoid adding a package, web server, or cleanup framework merely to make the lab look more sophisticated. The best improvement makes the existing experiment easier to reproduce or interpret.

## Independent exercises

**E19.1: Generator review.** Write a checklist of six preconditions or invariants for a compatible starter generator. Link each to a current implementation point or mark it as a proposed improvement. Include output ownership, anchor uniqueness, provenance, and expected failure classification.

**E19.2: New fault suite.** Design a reservation after-add observation suite using current copied source. Specify additional files, harness cases, expected results, and limitations. Explain why the suite should not silently modify the foundation template or original application.

**E19.3: Run evidence.** Produce a sample run record distinguishing generator refusal, compile failure, intended starter assertion failure, passing limitation observation, and passing proposed regression. State which cases establish application behavior and which establish only setup or harness behavior.

## Review checkpoint

Your laboratory should have a clear owner, explicit output, traceable source, bounded execution, and a narrow claim. Those properties let learners experiment confidently while preserving the original project. They also make course evidence durable enough for another reviewer to inspect without rerunning a broad application suite.
