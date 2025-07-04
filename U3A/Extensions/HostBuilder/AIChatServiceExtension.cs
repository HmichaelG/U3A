using Azure;
using Azure.AI.OpenAI;
using DevExpress.AIIntegration;
using DevExpress.AIIntegration.Blazor.Reporting.Viewer.Models;
using DevExpress.AIIntegration.Reporting.Common.Models;
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

        var languages = new List<LanguageInfo>()
        {
            new LanguageInfo() {Id="en",Text="English" },
            new LanguageInfo() { Id = "ch", Text = "Chinese" },
            new LanguageInfo() { Id = "fr", Text = "French" },
            new LanguageInfo() { Id = "de", Text = "German" },
            new LanguageInfo() { Id = "gr", Text = "Greek" },
            new LanguageInfo() { Id = "it", Text = "Italian" },
            new LanguageInfo() { Id = "jp", Text = "Japanese" },
            new LanguageInfo() { Id = "es", Text = "Spanish" },
            new LanguageInfo() { Id = "vn", Text = "Vietnamese" }
        };
        builder.Services.AddDevExpressAI(config =>
        {
            config.RegisterOpenAIAssistants(openAiClient, model);
            config.AddBlazorReportingAIIntegration(options =>
            {
                options.AddTranslation(options =>
                {
                    options.SetLanguages(languages);
                });
                options.AddSummarization(options => options.SetSummarizationMode(SummarizationMode.Abstractive));
            });
        });

        builder.Services.AddSingleton<IAIExceptionHandler>(new AIExceptionHandler());


        return builder;
    }
}