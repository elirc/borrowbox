using BorrowBox.Core.Borrowers;

namespace BorrowBox.Core.Tests.Borrowers;

public sealed class BorrowerTests
{
    [Fact]
    public void Register_NormalizesCollegeIdAndStartsActive()
    {
        var borrower = new Borrower(Guid.NewGuid(), " s123 ", "Ada Student");
        Assert.Equal("S123", borrower.CollegeId);
        Assert.True(borrower.IsActive);
    }
}
