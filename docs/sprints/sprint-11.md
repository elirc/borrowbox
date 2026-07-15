# Sprint 11: borrower limits

## Goal

Make a configurable lending rule explicit: a borrower can have at most three active loans.

`BorrowerLoanPolicy` contains the decision. `CheckoutService` gathers the active-loan count and asks the policy to validate it. This separates a reusable rule from workflow and storage details without creating a general-purpose rules framework.

## Testing lesson

The boundary-value test sets a limit of two, creates exactly two active loans, and proves the third is rejected before equipment changes state. Boundary tests usually provide more value than testing arbitrary middle values.
