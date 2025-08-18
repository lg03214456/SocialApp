using Xunit;

public class WeatherTests
{
    [Theory]
    [InlineData(0, 32)]
    [InlineData(100, 212)]
    public void TemperatureF_IsCalculatedCorrectly(int c, int expectedF)
    {
        var w = new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), c, null);
        Assert.Equal(expectedF, w.TemperatureF);
    }
}
