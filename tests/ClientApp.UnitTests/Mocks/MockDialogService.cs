namespace ClientApp.UnitTests.Mocks;

public class MockDialogService : IDialogService
{
    public string? LastMessage { get; private set; }

    public string? LastTitle { get; private set; }

    public string? LastButtonLabel { get; private set; }

    public Task ShowAlertAsync(string message, string title, string buttonLabel)
    {
        LastMessage = message;
        LastTitle = title;
        LastButtonLabel = buttonLabel;
        return Task.CompletedTask;
    }
}
