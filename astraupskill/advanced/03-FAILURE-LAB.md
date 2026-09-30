# 3. Reproduce a behavioral failure without replacing live source

The lab generator reads current Core files and selected in-memory adapters into a new directory. Starter mode makes exactly two ordering mistakes in those copies. It preserves constructor signatures and service methods, including optional clocks. Reference mode copies the repaired source unchanged. Both compile the same small console harness; neither invokes the application server or its full test suite.

From the BorrowBox root, choose two unused absolute paths outside this project:

    python astraupskill/advanced/lab/generate_lab.py --mode starter --output C:/Temp/borrowbox-starter-01
    dotnet run --project C:/Temp/borrowbox-starter-01/Lab.csproj
    python astraupskill/advanced/lab/generate_lab.py --mode reference --output C:/Temp/borrowbox-reference-01
    dotnet run --project C:/Temp/borrowbox-reference-01/Lab.csproj

On another operating system use new absolute directories such as /tmp/borrowbox-starter-01. The output must not exist. The generator refuses an existing destination or a destination inside BorrowBox. It validates every mutation anchor before creating output. If source has evolved and an anchor is absent or repeated, it stops; do not relax that check merely to obtain a red test.

The generated NuGet.Config clears package feeds. There are no external package references: the installed .NET 10 SDK and framework packs are sufficient. The first run still creates normal restore/build metadata in the disposable directory. A missing SDK or compile error is a setup failure, not the intended lesson. The expected starter result is four passing checks and two failures, with exit code one. The reference result is six passing checks and exit code zero.

The two red cases are invalid expiry and invalid due date. Each expects the correct domain rejection and then inspects surviving equipment state. The starter has moved the observable equipment mutation before candidate validation. Reordering statements changes behavior while preserving the API surface; that is why these failures are more useful than a constructor mismatch from an old snapshot.

Edit only the generated starter source. Repair one ordering defect, run again, and record five passes with one remaining failure. Repair the second and expect six passes. Do not solve this by removing assertions or changing the expected state to the leaked state. Keep SOURCE-MAP.json with your notes; it records source hashes and the named mutations, so another learner can reproduce the same starting point.

Two checks have OBSERVATION in their names. They intentionally document current limitations: an injected loan AddAsync failure leaves the earlier checkout effect, and two requests for different items can both pass a borrower limit of one. Those checks pass when the limitation is observed. They are not acceptance tests for an improved transactional design. If you implement that design in a later capstone copy, change the contract and its tests deliberately.

The concurrency demonstration uses a TaskCompletionSource gate in a test store. Both requests capture an empty active-loan list before either proceeds. A five-second timeout bounds the experiment. It does not sleep and hope to win a race. The fake controls a legal interleaving through the real ILoanStore interface, so the lesson is about the service's coordination boundary.

Keep a short failure log: command, mode, failure names, state observation and changed lines. If the reference fails, stop and inspect compiler output, SDK selection or source drift before trying exercises. Your evidence should distinguish a runnable red scenario from a toolchain failure and a known limitation from a regression.
