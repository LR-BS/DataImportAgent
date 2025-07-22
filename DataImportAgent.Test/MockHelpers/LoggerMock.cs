using DataImportAgent.Logger;

namespace DataImportAgent.Test.MockHelpers;

public class LoggerMock : ILogger
{
    private string logHistory = "";

    public void LogError(string errorMessage, Exception? exception = null)
    {
        logHistory += errorMessage;
    }

    public void LogInformation(string message, object? parameters = null)
    {
        logHistory += message;
    }
}