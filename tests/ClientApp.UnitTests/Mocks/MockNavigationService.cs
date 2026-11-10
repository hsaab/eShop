namespace ClientApp.UnitTests.Mocks;

public class MockNavigationService : INavigationService
{
    public string? LastRoute { get; private set; }

    public IDictionary<string, object>? LastRouteParameters { get; private set; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null)
    {
        LastRoute = route;
        LastRouteParameters = routeParameters;
        return Task.CompletedTask;
    }

    public Task PopAsync()
    {
        return Task.CompletedTask;
    }
}
