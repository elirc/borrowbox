# BorrowBox advanced: owning a lending workflow

This second route builds on the existing lifecycle course. It follows current C# source and adds a disposable, signature-compatible failure lab. Nothing here replaces the application or asks you to restore a historical snapshot over your work.

| Session | Read | Deliverable |
|---|---|---|
| 1, 45 minutes | [Contract and route](01-CONTRACT-AND-ROUTE.md) | Your operation matrix and evidence plan |
| 2, 60 minutes | [State trace](02-STATE-TRACE.md) | A before/after ledger and lifecycle diagram |
| 3, 60-90 minutes | [Failure lab](03-FAILURE-LAB.md) | Two reproducible failures, then six passing checks |
| 4, 60 minutes | [Failure and concurrency boundaries](04-FAILURE-BOUNDARIES.md) | A fault table and deterministic race explanation |
| 5, two sessions | [Exercises and feature capstone](05-EXERCISES-AND-CAPSTONE.md) | Eight independent exercises and a scoped design |
| 6, 45 minutes | [Solutions and rubric](06-SOLUTIONS-AND-RUBRIC.md) | Reviewed evidence and remaining risks |

Keep solutions closed until you have attempted each exercise. Python 3 and the repository's .NET 10 SDK are enough for the new lab; it has no external NuGet package references, database, API listener or browser dependency. The application suites remain separate evidence.

[Return to the original course](../README.md). Existing original-file snapshots remain historical evidence, not a runnable starter.

Continue with the completed [extended engineering workbook](extended-course/README.md): twenty-two source-based chapters, sixty-seven exercises, separate hints and solutions, two capstones, and isolated observation labs.

## Verification of this expansion

Executed on 2026-09-29 with installed .NET SDK 10.0.302, using newly generated directories outside the application. No application suites, servers or dependency installation were run.

| Mode | Result | Exit |
|---|---|---:|
| starter | 4 passed; 2 failed | 1 |
| reference | 6 passed; 0 failed | 0 |

The starter exit is intentional: these are behavioral assertion failures, not compile errors. Both reference harnesses compile from current copied source. Existing-output and in-project-output refusal checks also passed. Application source and test files were hash-checked before and after this work and are unchanged. Database, HTTP and cross-process guarantees are not established by this lab.
