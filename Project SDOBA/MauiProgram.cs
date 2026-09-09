using Microsoft.Extensions.Logging;

namespace Project_SDOBA;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("ShareTechMono-Regular.ttf", "ShareTechMono");
                fonts.AddFont("Inter-Variable.ttf", "Inter");
                fonts.AddFont("Inter-Italic.ttf", "InterMedium");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}