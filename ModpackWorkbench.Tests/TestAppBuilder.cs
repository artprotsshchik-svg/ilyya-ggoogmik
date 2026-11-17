using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(ModpackWorkbench.Tests.TestAppBuilder))]

namespace ModpackWorkbench.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
