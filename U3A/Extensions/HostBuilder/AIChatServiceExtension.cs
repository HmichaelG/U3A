using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration;
using Microsoft.Extensions.AI;
using OpenAI;

namespace U3A.Extensions.HostBuilder;

public static class AIChatServiceExtension
{
    public enum ChatServiceType
    {
        Azure,
        OpenAI
    }
    public static WebApplicationBuilder AddAIChatService(this WebApplicationBuilder builder, ChatServiceType chatService)
    {
        string azureAIuri = builder.Configuration.GetValue<string>("AzureAIEndpoint")!;
        string azureAIkey = builder.Configuration.GetValue<string>("AzureAIKey")!;
        string openAIkey = builder.Configuration.GetValue<string>("OpenAIKey")!;
        string model = "gpt-4o-mini";
        OpenAIClient openAiClient = chatService switch
        {
            ChatServiceType.Azure => new AzureOpenAIClient(
                                new Uri(azureAIuri),
                                new AzureKeyCredential(azureAIkey)),
            ChatServiceType.OpenAI => new OpenAI.OpenAIClient(openAIkey),
            _ => throw new ArgumentOutOfRangeException(nameof(chatService), chatService, null),
        };
        IChatClient aiChatClient = openAiClient.AsChatClient(model);

        _ = builder.Services.AddDevExpressBlazor();
        _ = builder.Services.AddChatClient(aiChatClient);
        _ = builder.Services.AddDevExpressAI(config =>
        {
            config.RegisterOpenAIAssistants(openAiClient, model);
        });

        _ = builder.Services.AddSingleton<IAIExceptionHandler>(new AIExceptionHandler());


        return builder;
    }
}