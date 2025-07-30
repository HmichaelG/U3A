using DevExpress.AIIntegration;
using DevExpress.AIIntegration.Extensions;
using Microsoft.Extensions.AI;
using Serilog;

public class AIExceptionHandler : IAIExceptionHandler
{
    public void ProcessException(AIExceptionArgs args)
    {
        var exception = args.Exception;
        var msg = $"an error has occurred in Chat, please try again later.{Environment.NewLine}{exception.Message}";
        Log.Error(exception, "An error occurred while processing an AI Chat");
        args.Response = new ChatResponse(new ChatMessage(ChatRole.System,msg));
    }
}