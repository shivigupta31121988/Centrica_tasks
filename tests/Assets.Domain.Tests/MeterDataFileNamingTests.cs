using Assets.Domain.MeterData;
using FluentAssertions;
using Xunit;

namespace Assets.Domain.Tests;

public class MeterDataFileNamingTests
{
    [Theory]
    [InlineData("570715000000088747.csv", true, "570715000000088747", "csv")]
    [InlineData("123.xlsx", true, "123", "xlsx")]
    [InlineData("not-a-number.csv", false, "", "")]
    [InlineData("123.txt", false, "", "")]
    [InlineData("../../etc/passwd.csv", false, "", "")]
    [InlineData("123.csv.exe", false, "", "")]
    [InlineData("", false, "", "")]
    public void TryParse_ValidatesStrictly(string fileName, bool expectedSuccess, string expectedMeterPointId, string expectedExtension)
    {
        var success = MeterDataFileNaming.TryParse(fileName, out var meterPointId, out var extension);

        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            meterPointId.Should().Be(expectedMeterPointId);
            extension.Should().Be(expectedExtension);
        }
    }

    [Fact]
    public void BuildFileName_CombinesMeterPointIdAndExtension()
    {
        MeterDataFileNaming.BuildFileName("12345", "csv").Should().Be("12345.csv");
    }
}
