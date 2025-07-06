using DevExpress.AIIntegration;
using DevExpress.AIIntegration.Extensions;
using Serilog;

public class AIExceptionHandler : IAIExceptionHandler
{
    public Exception ProcessException(Exception exception)
    {
        Log.Error(exception, "An error occurred while processing an AI Chat");
        return new Exception(exception.Message, exception);
    }
}