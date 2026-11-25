using System.Linq;
using Core.Reports;
using Xunit;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory]
    [InlineData(10, 2)]
    [InlineData(1, 0)]
    [InlineData(100, 4)]
    public void ScarceBooks_CountsOnlyBelowThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.ScarceBooks(TestData.Books(), threshold).Count);

    [Fact] // детермінований топ-N з тижня 7
    public void TopBooks_IsDeterministic() =>
        Assert.Equal(new[] { "ISBN-BIG", "ISBN-MID" },
            ReportService.TopBooks(TestData.Books(), 2).Select(l => l.Isbn));
}
