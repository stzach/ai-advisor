namespace ai_advisor.Application.FunctionalTests.Infrastructure;

public abstract class TestBase
{
    [SetUp]
    public async Task SetUp()
    {
        await TestApp.ResetState();
    }
}
