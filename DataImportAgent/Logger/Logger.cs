using Serilog;
using Serilog.Exceptions;

namespace DataImportAgent.Logger;

public class Logger : ILogger
{
    private readonly Serilog.Core.Logger loggerInstance;

    public Logger()
    {
        loggerInstance = new LoggerConfiguration().Enrich.WithExceptionDetails()
              .Enrich.WithProperty("Application", "DataImportAgent")
              .WriteTo.Seq("http://localhost:5341")
              .CreateLogger();
    }

    public void LogError(string errorMessage, Exception? exception = null)
    {
        loggerInstance.Error(exception, $"An error occurred: {errorMessage}", exception?.Message);
        Console.WriteLine(errorMessage);
    }

    public void LogInformation(string message, object? parameters = null)
    {
        Console.WriteLine(message);
        loggerInstance.Information(message, parameters);
    }
}