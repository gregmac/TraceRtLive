using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net;
using TraceRtLive.Trace;
using TraceRtLive.UI;

namespace TraceRtLive
{
    internal sealed class TraceRtCommand : AsyncCommand<TraceRtCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [Description("Target to route to")]
            [CommandArgument(0, "[target]")]
            public string? Target { get; init; }

            [Description("Maximum number of hops to search for target")]
            [CommandOption("-h|--max-hops")]
            public int? MaxHops { get; init; } = 50;


            [Description("Timeout in milliseconds for each reply")]
            [CommandOption("-w|--timeout")] 
            public int? TimeoutMilliseconds { get; init; } = 4000;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var target = IPAddress.Parse(settings.Target);

            var table = new Table();
            table.AddColumn(new TableColumn("Hop").RightAligned());
            table.AddColumn(new TableColumn("IP").Centered());
            table.AddColumn(new TableColumn("RTT").RightAligned());

            var returnCode = 1;

            await AnsiConsole.Live(table)
                .StartAsync(async live =>
                {
                    var tracer = new AsyncTracer(settings.TimeoutMilliseconds.Value);
                    await tracer.TraceAsync(target, settings.MaxHops.Value,
                        hopResult =>
                        {
                            table.AddWeightedRow(hopResult.Hops,
                                hopResult.Hops.ToString(),
                                $"[darkcyan]{hopResult.IP}[/]",
                                $"[{RttColor(hopResult.RoundTripTime)}]{hopResult.RoundTripTime.TotalMilliseconds:n0}ms[/]");
                            live.Refresh();
                        },
                        targetResult =>
                        {
                            table.AddWeightedRow(targetResult.Hops,
                                targetResult.Hops.ToString(),
                                $"[cyan]{targetResult.IP}[/]",
                                $"[{RttColor(targetResult.RoundTripTime)}]{targetResult.RoundTripTime.TotalMilliseconds:n0}ms[/]");
                            live.Refresh();

                            // sucess, return 0
                            returnCode = 0;
                        });
                });

            return returnCode;
        }

        private static readonly Dictionary<TimeSpan?, string> ColorThresholds = new Dictionary<TimeSpan?, string>
        {
            { TimeSpan.FromSeconds(2), "Red" },
            { TimeSpan.FromSeconds(1), "DarkRed" },
            { TimeSpan.FromMilliseconds(200), "DarkGreen" },
            { TimeSpan.Zero, "Green" },
        };

        private static string RttColor(TimeSpan time)
            => ColorThresholds.FirstOrDefault(x => time >= x.Key).Value ?? "Green";
    }
}
