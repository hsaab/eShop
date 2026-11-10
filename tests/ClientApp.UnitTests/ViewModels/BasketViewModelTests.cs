using ClientApp.UnitTests.Mocks;
using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Services.Identity;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
public class BasketViewModelTests
{
    private readonly IAppEnvironmentService _appEnvironmentService;
    private readonly INavigationService _navigationService;
    private readonly ISettingsService _settingsService;

    public BasketViewModelTests()
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

    private BasketViewModel CreateViewModel() =>
        new(_appEnvironmentService, _navigationService, _settingsService);

    [TestMethod]
    public async Task AddItemUpdatesBasketItemsBadgeCountAndTotalTest()
    {
        var basketViewModel = CreateViewModel();
        var item = new BasketItem
        {
            Id = "99",
            ProductId = 99,
            ProductName = "Test Catalog Item",
            Quantity = 2,
            UnitPrice = 10.00M
        };

        var beforeCount = basketViewModel.BasketItems.Count;
        var beforeBadge = basketViewModel.BadgeCount;
        var beforeTotal = basketViewModel.Total;

        await basketViewModel.AddCommand.ExecuteUntilComplete(item);

        Assert.HasCount(beforeCount + 1, basketViewModel.BasketItems);
        Assert.AreEqual(beforeBadge + item.Quantity, basketViewModel.BadgeCount);
        Assert.AreEqual(beforeTotal + (item.Quantity * item.UnitPrice), basketViewModel.Total);
        Assert.IsTrue(basketViewModel.BasketItems.Contains(item));
    }

    [TestMethod]
    public async Task DeleteItemRemovesItemAndRefreshesCountsTest()
    {
        var basketViewModel = CreateViewModel();
        var item = new BasketItem
        {
            Id = "88",
            ProductId = 88,
            ProductName = "Delete Me",
            Quantity = 1,
            UnitPrice = 5.50M
        };

        await basketViewModel.AddCommand.ExecuteUntilComplete(item);
        Assert.HasCount(1, basketViewModel.BasketItems);
        Assert.AreEqual(1, basketViewModel.BadgeCount);
        Assert.AreEqual(5.50M, basketViewModel.Total);

        await basketViewModel.DeleteCommand.ExecuteUntilComplete(item);

        Assert.IsEmpty(basketViewModel.BasketItems);
        Assert.AreEqual(0, basketViewModel.BadgeCount);
        Assert.AreEqual(0m, basketViewModel.Total);
        Assert.IsFalse(basketViewModel.BasketItems.Contains(item));
    }

    [TestMethod]
    public async Task CheckoutCommandPreparesLocalBasketItemsTest()
    {
        // MockNavigationService does not record routes; assert checkout readiness instead.
        var basketViewModel = CreateViewModel();
        var item = new BasketItem
        {
            Id = "77",
            ProductId = 77,
            ProductName = "Checkout Ready Item",
            Quantity = 1,
            UnitPrice = 12.00M
        };

        await basketViewModel.AddCommand.ExecuteUntilComplete(item);

        Assert.IsTrue(basketViewModel.CheckoutCommand.CanExecute(null));

        await basketViewModel.CheckoutCommand.ExecuteUntilComplete();

        var localItems = _appEnvironmentService.BasketService.LocalBasketItems?.ToList();
        Assert.IsNotNull(localItems);
        Assert.HasCount(1, localItems);
        Assert.AreEqual(item.ProductId, localItems[0].ProductId);
    }
}
