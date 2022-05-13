using Spectre.Console.Cli;
using TraceRtLive;

return await new CommandApp<TraceRtCommand>().RunAsync(args);
