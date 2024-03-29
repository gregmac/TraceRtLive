﻿﻿using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net;
using System.Text;
using TraceRtLive.DNS;
using TraceRtLive.Trace;
using TraceRtLive.UI;

namespace TraceRtLive
{
    public sealed class TraceRtCommand : AsyncCommand<TraceRtCommand.Settings>
    {
        public class Settings : CommandSettings
        {
            [Description("Target to route to")]
            [CommandArgument(0, "[target]")]
            public string? Target { get; init; }

            [Description("Maximum number of hops to search for target")]
            [CommandOption("--max-hops")]
            public int MaxHops { get; init; } = 50;

            [Description("How many seconds to execute for. When this is non-zero, pings are sent to each hop once a second to gather statistics.")]
            [CommandOption("-t|--time")]
            public int ExecuteSeconds { get; init; } = 0;

            [Description("Timeout in milliseconds for each reply. Defaults to 2000ms.")]
            [CommandOption("-w|--hop-timeout")]
            public int TimeoutMilliseconds { get; init; } = 2000;
        }

        public const string Placeholder = "[gray]\u2026[/]";

        public const string Failed = "[grey]\u02E3[/]";

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var dnsResolver = new DnsResolver();

            if (IPAddress.TryParse(settings.Target, out var target))
            {
                AnsiConsole.MarkupLine($"Tracing to [cyan]{target}[/]...");
            }
            else
            {
                AnsiConsole.MarkupLine($"Looking up IP for [darkcyan]{settings.Target}[/]...");
                //
                var ips = await dnsResolver.ResolveAsync(settings.Target!).ConfigureAwait(false);
                if (ips == null)
                {
                    AnsiConsole.MarkupLine($"[red]Failed resolving[/] [darkcyan]{settings.Target}[/]");
                    return -1;
                }
                else if (ips.Length == 0)
                {
                    AnsiConsole.MarkupLine($"[red]Faile to resolve any IPs for[/] [darkcyan]{settings.Target}[/]");
                    return -1;
                }
                else
                {
                    AnsiConsole.MarkupLine("Resolved to " + string.Join(", ", ips.Select(x => $"[cyan]{x}[/]")));
                }
                target = ips[0];
                AnsiConsole.MarkupLine($"Tracing to [cyan]{target}[/] ([darkcyan]{settings.Target}[/])...");
            }

            var table = new Table();
            table.AddColumn(new TableColumn("Hop").RightAligned().Width(3));        // 0
            table.AddColumn(new TableColumn("IP").Centered().Width(15));            // 1
            table.AddColumn(new TableColumn("Min").RightAligned().Width(7));        // 2
            table.AddColumn(new TableColumn("Avg").RightAligned().Width(7));        // 3
            table.AddColumn(new TableColumn("Max").RightAligned().Width(7));        // 4
            table.AddColumn(new TableColumn("Last").RightAligned().Width(7));       // 5
            table.AddColumn(new TableColumn("Pings").RightAligned().Width(5));      // 6
            table.AddColumn(new TableColumn("Fail").RightAligned().Width(10));      // 7
            table.AddColumn(new TableColumn("Hostname"));                           // 8

            var returnCode = 1;

            // control ping mode based on ExecuteSeconds
            CancellationToken cancellation;
            int numPings;
            if (settings.ExecuteSeconds > 0)
            {
                var runtime = TimeSpan.FromSeconds(settings.ExecuteSeconds);
                cancellation = new CancellationTokenSource(runtime).Token;
                numPings = int.MaxValue; // indefinitely
                AnsiConsole.MarkupLine($"Running for [darkcyan]{runtime}[/]...");
            }
            else
            {
                cancellation = CancellationToken.None;
                numPings = 1;
            }
            try
            {
                await AnsiConsole.Live(table)
                    .StartAsync(async live =>
                    {
                        var tracer = new AsyncTracer(settings.TimeoutMilliseconds);
                        await tracer.TraceAsync(target,
                            maxHops: settings.MaxHops,
                            cancellation: cancellation,
                            numPings: numPings,
                            numHistoryPings: 10,
                            resultAction: async (hop, traceResultStatus, ip) =>
                            {
                                if (traceResultStatus == TraceResultStatus.Obsolete)
                                {
                                    await table.RemoveWeightedRow(hop);
                                }
                                else
                                {
                                    var ipColor = traceResultStatus switch
                                    {
                                        TraceResultStatus.HopResult => "darkcyan",
                                        TraceResultStatus.FinalResult => "cyan",
                                        _ => "gray"
                                    };
                                    await table.AddOrUpdateWeightedCells(hop, new[]
                                    {
                                        (0, hop.ToString()),
                                        (1, ip switch
                                        {
                                            null => Placeholder,
                                            IPAddress when ip.Equals(IPAddress.Any) => Failed,
                                            _ => $"[{ipColor}]{ip}[/]"
                                        }),
                                    });
                                }

                                live.Refresh();
                                await Task.Yield();
                            },
                            pingAction: async (hop, pingReply) =>
                            {
                                var percentFail = pingReply.NumFail / pingReply.NumSent * 100.0;
                                var failColor = pingReply.NumFail == 0 ? "green"
                                    : pingReply.NumFail < 3 ? "darkred" // arbitrary cut-off
                                    : "red";

                                await table.UpdateWeightedCells(hop, new[]
                                {
                                    (2, $"[{RttColor(pingReply.RoundTripTimeMin)}]{pingReply.RoundTripTimeMin.TotalMilliseconds:n0}ms[/]"),
                                    (3, $"[{RttColor(pingReply.RoundTripTimeMean)}]{pingReply.RoundTripTimeMean.TotalMilliseconds:n0}ms[/]"),
                                    (4, $"[{RttColor(pingReply.RoundTripTimeMax)}]{pingReply.RoundTripTimeMax.TotalMilliseconds:n0}ms[/]"),
                                    (5, $"[{RttColor(pingReply.RoundTripTime)}]{pingReply.RoundTripTime.TotalMilliseconds:n0}ms[/]"),
                                    (6, BrailleBarGraph.CreateGraph(pingReply.History ?? Array.Empty<int>(), highestValue: 100, alignRight: true, renderCallback: RenderColorGlyph)),
                                    (7, $"[{failColor}]{pingReply.NumFail}[/] [gray]{percentFail,3:n0}%[/]"),
                                });

                                live.Refresh();
                                await Task.Yield();
                            },
                            dnsResolvedAction: async (hop, reverseDns) =>
                            {
                                await table.UpdateWeightedCells(hop, new[]
                                    {
                                    (8, reverseDns?.HostName ?? Failed),
                                    });
                                live.Refresh();
                                await Task.Yield();
                            });
                    });
            }
            catch (TaskCanceledException) { /* end of time: ignored */ }

            return returnCode;
        }

        private static readonly Dictionary<TimeSpan, string> ColorThresholds = new Dictionary<TimeSpan, string>
        {
            { TimeSpan.Zero, "Green" },
            { TimeSpan.FromMilliseconds(200), "DarkGreen" },
            { TimeSpan.FromSeconds(1), "DarkRed" },
            { TimeSpan.FromSeconds(2), "Red" },
        };

        public static string RttColor(TimeSpan time)
            => ColorThresholds.LastOrDefault(x => time >= x.Key).Value ?? "Red";

        public static void RenderColorGlyph(int a, int b, char glyph, StringBuilder result)
        {
            var index = Math.Min(new[] { a, b, 1 }.Max(), ColorThresholds.Count) - 1;
            var color = ColorThresholds.Values.ElementAt(index);
            result.Append('[').Append(color).Append(']').Append(glyph).Append("[/]");
        }
    }
}
