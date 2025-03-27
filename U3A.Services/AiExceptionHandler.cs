using DevExpress.AIIntegration;
using DevExpress.AIIntegration.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;

public class AIExceptionHandler : IAIExceptionHandler
{
    public Exception ProcessException(Exception exception)
    {
        Log.Error(exception,"An error occurred while processing an AI Chat");
        return new Exception("Oops! Something went wrong. Please try again later.", exception);
    }
}