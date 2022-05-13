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
            table.AddColumn(new TableColumn("Hop").RightAligned().Width(3));
            table.AddColumn(new TableColumn("IP").Centered().Width(15));
            table.AddColumn(new TableColumn("RTT").RightAligned().Width(10));

            var returnCode = 1;

            await AnsiConsole.Live(table)
                .StartAsync(async live =>
                {
                    async Task updateTable(int weight, TraceResult result, string ipColor)
                    {
                        table.AddOrUpdateWeightedRow(weight,
                                result.Hops.ToString(),
                                $"[{ipColor}]{result.IP}[/]",
                                result.RoundTripTime.HasValue ? $"[{RttColor(result.RoundTripTime.Value)}]{result.RoundTripTime.Value.TotalMilliseconds:n0}ms[/]" : "...");
                        live.Refresh();
                        await Task.Yield();
                    }

                    var tracer = new AsyncTracer(settings.TimeoutMilliseconds.Value);
                    await tracer.TraceAsync(target, settings.MaxHops.Value,
                        async hopResult =>
                        {
                            await updateTable(hopResult.Hops, hopResult, "darkcyan");
                        },
                        async targetResult =>
                        {
                            await updateTable(int.MaxValue, targetResult, "cyan");

                            // success, return 0
                            returnCode = 0;
                        });
                });

            return returnCode;
        }

        private static readonly Dictionary<TimeSpan, string> ColorThresholds = new Dictionary<TimeSpan, string>
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
