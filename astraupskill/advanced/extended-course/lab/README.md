# Executable observations for chapters 01 through 06

This optional lab copies current domain and selected in-memory adapter source using the existing advanced-course generator. It adds six bounded checks for the new workbook. Four checks characterize current limitations or ownership behavior; two check timestamp boundaries. A passing observation of a limitation is evidence that the limitation is reproduced, not a proposed production acceptance criterion.

Run from the BorrowBox repository root using Python 3 and the installed .NET 10 SDK. The generated project has no external NuGet packages and clears package feeds. Use an explicitly new directory outside BorrowBox. The commands below choose a fresh temporary path and print it so that the output remains inspectable.

~~~powershell
$borrowBoxObservationLab = Join-Path $env:TEMP ("borrowbox-observations-" + [guid]::NewGuid().ToString("N"))
python astraupskill/advanced/extended-course/lab/generate_observations.py --output $borrowBoxObservationLab
if ($LASTEXITCODE -ne 0) { throw "Lab generation failed; do not continue" }
Write-Output $borrowBoxObservationLab
dotnet run --project (Join-Path $borrowBoxObservationLab "Lab.csproj")
~~~

Expected unchanged-source outcome: six passing checks and exit zero. A source change that strengthens a currently weak boundary can legitimately break a characterization check. Review that change rather than undoing an improvement to make an observation pass. The earlier advanced starter is separate: it intentionally produces two failures, and this observation lab does not modify its recipe.

The checks cover shared equipment references, direct Return from Retired, reservation insertion failure after equipment mutation, equipment-save failure after both return mutations, reservation expiry equality, and return-time equality followed by duplicate rejection. All faults occur at named in-process seams. The lab does not load SQLite, start an API listener, verify authorization, test crash recovery, or establish cross-process synchronization.

SOURCE-MAP.json records the copied application-file hashes. Program.cs is the observation harness from this directory. Edit only the generated copy when extending an exercise. Existing-output and in-project-output refusals come from the reused generator, so a mistyped destination should fail before any application writes. No cleanup command is required; preserve the output while reviewing your findings.

## Recorded verification

On 2026-09-29, this generated observation harness compiled and ran with six passes, zero failures, and exit zero. Separate generation attempts targeting an existing output and a path inside BorrowBox were refused. The original application and test files matched all 53 hashes in the previous teaching-work baseline. These checks validate the bounded harness and isolation behavior; the additional independent exercise designs remain learner work rather than reported executed tests.

## Inspection suite for chapter 09

Choose a second new output and pass the inspection suite explicitly. This suite copies the actual InMemoryInspectionStore as well as the original recipe sources and adds its hash to SOURCE-MAP.json. It does not modify the foundation suite or the application.

~~~powershell
$borrowBoxInspectionLab = Join-Path $env:TEMP ("borrowbox-inspection-" + [guid]::NewGuid().ToString("N"))
python astraupskill/advanced/extended-course/lab/generate_observations.py --suite inspection --output $borrowBoxInspectionLab
if ($LASTEXITCODE -ne 0) { throw "Lab generation failed; do not continue" }
Write-Output $borrowBoxInspectionLab
dotnet run --project (Join-Path $borrowBoxInspectionLab "Lab.csproj")
~~~

Expected current-source outcome is five passes. The suite observes blank-damage mutation, completion mutation before an equipment mismatch, the Repair outcome, accepted completion before opening, and accepted unnamed enum values. The last two are direct domain-method observations; they do not establish HTTP binding behavior. Four tests characterize current limitations, while Repair records the existing named-outcome behavior. A future improvement should deliberately replace the relevant limitation assertion with a regression contract in a disposable implementation.

On 2026-09-29 the inspection suite compiled and passed all five checks with exit zero. The foundation suite was then regenerated through the extended generator and passed all six checks, also with exit zero. Neither run started the API or used SQLite.
