using ClientApp.UnitTests.Mocks;
using eShop.ClientApp.Services.Identity;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
public class ProfileViewModelTests
{
    private readonly IAppEnvironmentService _appEnvironmentService;
    private readonly MockNavigationService _navigationService;
    private readonly ISettingsService _settingsService;

    public ProfileViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _settingsService = new MockSettingsService();

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

    private ProfileViewModel CreateViewModel() =>
        new(_appEnvironmentService, _settingsService, _navigationService);

    [TestMethod]
    public async Task RefreshLoadsOrdersFromOrderServiceTest()
    {
        var profileViewModel = CreateViewModel();
        Assert.IsEmpty(profileViewModel.Orders);

        var expectedOrders = (await _appEnvironmentService.OrderService.GetOrdersAsync()).ToList();

        await profileViewModel.RefreshCommand.ExecuteUntilComplete();

        Assert.HasCount(expectedOrders.Count, profileViewModel.Orders);
        Assert.AreEqual(expectedOrders[0].OrderNumber, profileViewModel.Orders[0].OrderNumber);
    }

    [TestMethod]
    public async Task LogoutCommandCompletesWithoutBusyStateTest()
    {
        var profileViewModel = CreateViewModel();

        Assert.IsTrue(profileViewModel.LogoutCommand.CanExecute(null));

        await profileViewModel.LogoutCommand.ExecuteUntilComplete();

        Assert.AreEqual("//Login", _navigationService.LastRoute);
        Assert.IsNotNull(_navigationService.LastRouteParameters);
        Assert.IsTrue(_navigationService.LastRouteParameters.ContainsKey("Logout"));
        Assert.IsTrue((bool)_navigationService.LastRouteParameters["Logout"]);
        Assert.IsFalse(profileViewModel.IsBusy);
    }
}
