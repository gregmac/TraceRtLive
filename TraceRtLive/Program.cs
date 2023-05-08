using Spectre.Console;
using Spectre.Console.Cli;
using TraceRtLive;

try
{ 
    var app = new CommandApp<TraceRtCommand>();
    app.Configure(c =>
    {
        c.PropagateExceptions();
        var assembly = typeof(TraceRtCommand).Assembly.GetName();
        c.SetApplicationName("TraceRtLive");
        c.SetApplicationVersion(assembly.Version!.ToString());
        c.AddExample(new[] { "8.8.8.8" });
    });
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex,
        ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes | ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
    return 255;
}
