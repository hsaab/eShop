using ClientApp.UnitTests.Mocks;
using eShop.ClientApp.Services.Identity;
using eShop.ClientApp.Services.OpenUrl;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
public class LoginViewModelTests
{
    private readonly IAppEnvironmentService _appEnvironmentService;
    private readonly MockNavigationService _navigationService;
    private readonly IOpenUrlService _openUrlService;
    private readonly ISettingsService _settingsService;

    public LoginViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _settingsService = new MockSettingsService();
        _openUrlService = new FakeOpenUrlService();

        var mockBasketService = new BasketMockService();
        var mockCatalogService = new CatalogMockService();
        var mockOrderService = new OrderMockService();
        var mockIdentityService = new IdentityMockService();

        _appEnvironmentService =
            new AppEnvironmentService(
                mockBasketService, mockBasketService,
                mockCatalogService, mockCatalogService,
                mockOrderService, mockOrderService,
                mockIdentityService, mockIdentityService);

        _appEnvironmentService.UpdateDependencies(true);
    }

    private LoginViewModel CreateViewModel() =>
        new(_openUrlService, _appEnvironmentService, _navigationService, _settingsService);

    [TestMethod]
    public async Task MockSignInCompletesWhenValidTest()
    {
        var loginViewModel = CreateViewModel();
        loginViewModel.IsValid = true;

        Assert.IsTrue(loginViewModel.MockSignInCommand.CanExecute(null));

        await loginViewModel.MockSignInCommand.ExecuteUntilComplete();

        Assert.AreEqual("//Main/Catalog", _navigationService.LastRoute);
        Assert.IsFalse(loginViewModel.IsBusy);
    }

    [TestMethod]
    public async Task PerformLogoutClearsCredentialsAndFakeLocationTest()
    {
        var loginViewModel = CreateViewModel();
        loginViewModel.UserName.Value = "sampleUser";
        loginViewModel.Password.Value = "password";
        _settingsService.UseFakeLocation = true;

        await loginViewModel.PerformLogoutCommand.ExecuteUntilComplete();

        Assert.AreEqual(string.Empty, loginViewModel.UserName.Value);
        Assert.AreEqual(string.Empty, loginViewModel.Password.Value);
        Assert.IsFalse(_settingsService.UseFakeLocation);
    }

    private sealed class FakeOpenUrlService : IOpenUrlService
    {
        public Task OpenUrl(string url) => Task.CompletedTask;
    }
}
