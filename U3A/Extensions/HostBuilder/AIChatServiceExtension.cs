using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration;
using DevExpress.AIIntegration.Blazor.Reporting.Viewer.Models;
using DevExpress.Blazor.Reporting;
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
        string azureAIuri = builder.Configuration.GetValue<String>("AzureAIEndpoint")!;
        string azureAIkey = builder.Configuration.GetValue<String>("AzureAIKey")!;
        string openAIkey = builder.Configuration.GetValue<String>("OpenAIKey")!;
        string model = "gpt-4o-mini";

        OpenAIClient openAiClient;

        switch (chatService)
        {
            case ChatServiceType.Azure:
                openAiClient = new AzureOpenAIClient(
                    new Uri(azureAIuri),
                    new AzureKeyCredential(azureAIkey));
                break;
            case ChatServiceType.OpenAI:
                openAiClient = new OpenAI.OpenAIClient(openAIkey);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(chatService), chatService, null);
        }

        IChatClient aiChatClient = openAiClient.AsChatClient(model);

        builder.Services.AddDevExpressBlazor();
        builder.Services.AddChatClient(aiChatClient);
        builder.Services.AddDevExpressAI(config =>
        {
            config.RegisterOpenAIAssistants(openAiClient, model);
            config.AddBlazorReportingAIIntegration(options =>
            {
                options.Languages = new List<LanguageItem>() {
            new LanguageItem() { Key = "en", Text = "English" },
            new LanguageItem() { Key = "ch", Text = "Chinese" },
            new LanguageItem() { Key = "fr", Text = "French" },
            new LanguageItem() { Key = "de", Text = "German" },
            new LanguageItem() { Key = "gr", Text = "Greek" },
            new LanguageItem() { Key = "it", Text = "Italian" },
            new LanguageItem() { Key = "jp", Text = "Japanese" },
            new LanguageItem() { Key = "es", Text = "Spanish" },
            new LanguageItem() { Key = "vn", Text = "Vietnamese" }
            };
                options.SummarizationMode = SummarizationMode.Abstractive;
            });
        });

        builder.Services.AddSingleton<IAIExceptionHandler>(new AIExceptionHandler());


        return builder;
    }
}