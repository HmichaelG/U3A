using DevExpress.Blazor.RichEdit.SpellCheck;
using DevExpress.Drawing;
using Microsoft.Extensions.FileProviders;
using System.Reflection;

namespace U3A.Extensions.HostBuilder;

public static class DevExpressServiceExtensions
{
    public static WebApplicationBuilder AddDevExpressService(this WebApplicationBuilder builder)
    {
        DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.UI.Reports.ProFormaReportFactory).Assembly);
        DevExpress.Utils.DeserializationSettings.RegisterTrustedAssembly(typeof(U3A.Model.Class).Assembly);
        DevExpress.Drawing.Settings.DrawingEngine = DrawingEngine.Default;
        foreach (var file in Directory.GetFiles(@"wwwroot/fonts"))
        {
            DXFontRepository.Instance.AddFont(file);
        }


        builder.Services.AddDevExpressBlazor().AddSpellCheck(opts =>
        {
            opts.FileProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly(), "U3A");
            opts.MaxSuggestionCount = 6;
            opts.AddToDictionaryAction = (word, culture) =>
            {
                //Write the selected word to a dictionary file
            };
            opts.Dictionaries.Add(new HunspellDictionary
            {
                DictionaryPath = Path.Combine("resources", "en_AU.dic"),
                GrammarPath = Path.Combine("resources", "en_AU.aff"),
                Culture = "en-AU"
            });
        });

        builder.Services.AddDevExpressServerSideBlazorReportViewer();
        builder.Services.AddDevExpressBlazor(options =>
        {
            options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
        });

        builder.Services.AddDevExpressServerSideBlazorPdfViewer();

        return builder;
    }
}