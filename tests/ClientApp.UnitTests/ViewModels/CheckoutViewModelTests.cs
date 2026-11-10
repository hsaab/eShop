using ClientApp.UnitTests.Mocks;
using eShop.ClientApp.Models.Basket;
using eShop.ClientApp.Services.Identity;

namespace ClientApp.UnitTests.ViewModels;

[TestClass]
public class CheckoutViewModelTests
{
    private readonly IAppEnvironmentService _appEnvironmentService;
    private readonly MockDialogService _dialogService;
    private readonly IdentityMockService _identityService;
    private readonly MockNavigationService _navigationService;
    private readonly ISettingsService _settingsService;

    public CheckoutViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _settingsService = new MockSettingsService();
        _dialogService = new MockDialogService();
        _identityService = new IdentityMockService();

        var mockBasketService = new BasketMockService();
        var mockCatalogService = new CatalogMockService();
        var mockOrderService = new OrderMockService();

        _appEnvironmentService =
            new AppEnvironmentService(
                mockBasketService, mockBasketService,
                mockCatalogService, mockCatalogService,
                mockOrderService, mockOrderService,
                _identityService, _identityService);

        _appEnvironmentService.UpdateDependencies(true);
    }

    private BasketViewModel CreateBasketViewModel() =>
        new(_appEnvironmentService, _navigationService, _settingsService);

    private CheckoutViewModel CreateCheckoutViewModel(BasketViewModel basketViewModel) =>
        new(basketViewModel, _appEnvironmentService, _dialogService, _settingsService, _navigationService);

    private static BasketItem CreateBasketItem() =>
        new()
        {
            Id = "1",
            ProductId = 1,
            ProductName = "Adventurer GPS Watch",
            Quantity = 1,
            UnitPrice = 199.99M,
            PictureUrl = "fake_product_01.png"
        };

    [TestMethod]
    public async Task InitializeAsyncBuildsOrderAndShippingFromBasketAndIdentityTest()
    {
        await _identityService.SignInAsync();

        var basketViewModel = CreateBasketViewModel();
        var basketItem = CreateBasketItem();
        await basketViewModel.AddCommand.ExecuteUntilComplete(basketItem);
        await basketViewModel.CheckoutCommand.ExecuteUntilComplete();

        var ordersBefore = (await _appEnvironmentService.OrderService.GetOrdersAsync()).Count();
        var checkoutViewModel = CreateCheckoutViewModel(basketViewModel);
        await checkoutViewModel.InitializeAsync();

        Assert.IsNotNull(checkoutViewModel.ShippingAddress);
        Assert.AreEqual("123 Sample Street", checkoutViewModel.ShippingAddress.Street);
        Assert.AreEqual("Washington", checkoutViewModel.ShippingAddress.State);
        Assert.AreEqual("USA", checkoutViewModel.ShippingAddress.Country);
        Assert.AreEqual("12345", checkoutViewModel.ShippingAddress.ZipCode);
        Assert.AreEqual("123 Sample Street", checkoutViewModel.ShippingAddress.City);

        Assert.IsNotNull(checkoutViewModel.Order);
        Assert.AreEqual("sampleUser", checkoutViewModel.Order.UserName);
        Assert.AreEqual("XXXXXXXXXXXX3456", checkoutViewModel.Order.CardNumber);
        Assert.AreEqual("Submitted", checkoutViewModel.Order.OrderStatus);
        Assert.HasCount(1, checkoutViewModel.Order.OrderItems);
        Assert.AreEqual(basketItem.ProductName, checkoutViewModel.Order.OrderItems[0].ProductName);
        Assert.AreEqual(199.99M, checkoutViewModel.Order.Total);
        Assert.AreEqual(ordersBefore + 1, checkoutViewModel.Order.OrderNumber);
    }

    [TestMethod]
    public async Task CheckoutCommandPlacesOrderAndClearsBasketTest()
    {
        await _identityService.SignInAsync();

        var basketViewModel = CreateBasketViewModel();
        var basketItem = CreateBasketItem();
        await basketViewModel.AddCommand.ExecuteUntilComplete(basketItem);
        await basketViewModel.CheckoutCommand.ExecuteUntilComplete();

        var checkoutViewModel = CreateCheckoutViewModel(basketViewModel);
        await checkoutViewModel.InitializeAsync();

        Assert.IsTrue(checkoutViewModel.CheckoutCommand.CanExecute(null));

        await checkoutViewModel.CheckoutCommand.ExecuteUntilComplete();

        Assert.IsEmpty(basketViewModel.BasketItems);
        Assert.AreEqual(0, basketViewModel.BadgeCount);
        Assert.AreEqual(0m, basketViewModel.Total);

        var basket = await _appEnvironmentService.BasketService.GetBasketAsync();
        Assert.IsEmpty(basket.Items);

        Assert.AreEqual("//Main/Catalog", _navigationService.LastRoute);
        Assert.AreEqual("Order sent successfully!", _dialogService.LastMessage);
        Assert.AreEqual("Checkout", _dialogService.LastTitle);
    }
}
