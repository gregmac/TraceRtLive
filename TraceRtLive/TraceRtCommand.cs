using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net;
using TraceRtLive.DNS;
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
            public int? TimeoutMilliseconds { get; init; } = 2000;
        }

        public const string Placeholder = "\u2026";

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var dnsResolver = new CachingResolver();

            if (IPAddress.TryParse(settings.Target, out var target))
            {
                AnsiConsole.MarkupLine($"Tracing to [cyan]{target}[/]...");
            }
            else
            {
                AnsiConsole.MarkupLine($"Looking up IP for [darkcyan]{settings.Target}[/]...");
                var ips = await dnsResolver.ResolveAsync(settings.Target).ConfigureAwait(false);
                if (ips.Length > 1)
                {
                    AnsiConsole.MarkupLine("Resolved to " + string.Join(", ", ips.Select(x => $"[cyan]{x}[/]")));
                }
                target = ips[0];
                AnsiConsole.MarkupLine($"Tracing to [cyan]{target}[/] ([darkcyan]{settings.Target}[/])...");
            }

            var table = new Table();
            table.AddColumn(new TableColumn("Hop").RightAligned().Width(3));
            table.AddColumn(new TableColumn("IP").Centered().Width(15));
            table.AddColumn(new TableColumn("RTT").RightAligned().Width(10));
            table.AddColumn(new TableColumn("Hostname"));

            var waitTasks = new List<Task>();
            var returnCode = 1;

            await AnsiConsole.Live(table)
                .StartAsync(async live =>
                {
                    var tracer = new AsyncTracer(settings.TimeoutMilliseconds.Value);

                    async Task addResult(int weight, TraceResult result, string ipColor)
                    {
                        await table.UpdateWeightedCells(weight, new[]
                        {
                            (0, result.Hops.ToString()),
                            (1, $"[{ipColor}]{result.IP?.ToString() ?? Placeholder}[/]"),
                            (2, result.RoundTripTime.HasValue ? $"[{RttColor(result.RoundTripTime.Value)}]{result.RoundTripTime.Value.TotalMilliseconds:n0}ms[/]" : $"[gray]{Placeholder}[/]"),
                        });

                        live.Refresh();
                        await Task.Yield();

                        // perform DNS lookup
                        if (result.IP != null)
                        {
                            waitTasks.Add(dnsResolver.ResolveAsync(result.IP, async resolved =>
                            {
                                await table.UpdateWeightedCells(weight, new[]
                                {
                                    (3, resolved.HostName),
                                });
                                live.Refresh();
                                await Task.Yield();
                            }));
                        }

                    }

                    await tracer.TraceAsync(target, settings.MaxHops.Value,
                        async result =>
                        {
                            switch (result.Status)
                            {
                                case TraceResultStatus.InProgress:
                                    await addResult(result.Hops, result, "gray");
                                    break;
                                case TraceResultStatus.HopResult:
                                    await addResult(result.Hops, result, "darkcyan");
                                    break;
                                case TraceResultStatus.FinalResult:
                                    await addResult(int.MaxValue, result, "cyan");

                                    // success, return 0
                                    returnCode = 0;

                                    break;
                                case TraceResultStatus.Obsolete:
                                    await table.RemoveWeightedRow(result.Hops);
                                    live.Refresh();
                                    await Task.Yield();
                                    break;
                            }
                        });

                    // wait for dns tasks
                    if (waitTasks.Any(x => !x.IsCompleted))
                    {
                        //AnsiConsole.MarkupLine("Waiting for all results...");
                        await Task.WhenAll(waitTasks);
                    }
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
