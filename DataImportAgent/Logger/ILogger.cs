namespace DataImportAgent.Logger;

public interface ILogger
{
    public void LogError(string errorMessage, Exception? exception = null);

    public void LogInformation(string message, object? parameters = null);
}