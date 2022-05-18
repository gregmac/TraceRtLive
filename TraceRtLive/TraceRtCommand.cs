﻿using Spectre.Console;
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
            public int MaxHops { get; init; } = 50;


            [Description("Number of pings to send for each")]
            [CommandOption("-n|--num-pings")]
            public int NumPings { get; init; } = 5;


            [Description("Timeout in milliseconds for each reply")]
            [CommandOption("-w|--timeout")]
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
                var ips = await dnsResolver.ResolveAsync(settings.Target).ConfigureAwait(false);
                if (ips.Length > 1)
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
            table.AddColumn(new TableColumn("Fail").RightAligned().Width(10));      // 5
            table.AddColumn(new TableColumn("Hostname"));                           // 6

            var returnCode = 1;

            await AnsiConsole.Live(table)
                .StartAsync(async live =>
                {
                    var tracer = new AsyncTracer(settings.TimeoutMilliseconds);

                    async Task addResult(int weight, int hop, IPAddress? ip, string ipColor)
                    {
                        await table.AddOrUpdateWeightedCells(weight, new[]
                        {
                            (0, hop.ToString()),
                            (1, ip switch
                            {
                                null => Placeholder,
                                IPAddress when ip.Equals(IPAddress.Any) => Failed,
                                _ => $"[{ipColor}]{ip}[/]"
                            }),
                        });

                        live.Refresh();
                        await Task.Yield();
                    }

                    await tracer.TraceAsync(target,
                        maxHops: settings.MaxHops,
                        cancellation: CancellationToken.None,
                        numPings: settings.NumPings,
                        resultAction: async (hop, traceResultStatus, ip) =>
                        {
                            switch (traceResultStatus)
                            {
                                case TraceResultStatus.InProgress:
                                    await addResult(hop, hop, ip, "gray");
                                    break;
                                case TraceResultStatus.HopResult:
                                    await addResult(hop, hop, ip, "darkcyan");
                                    break;
                                case TraceResultStatus.FinalResult:
                                    await addResult(hop, hop, ip, "cyan");

                                    // success, return 0
                                    returnCode = 0;

                                    break;
                                case TraceResultStatus.Obsolete:
                                    await table.RemoveWeightedRow(hop);
                                    live.Refresh();
                                    await Task.Yield();
                                    break;
                            }
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
                                (5, $"[{failColor}]{pingReply.NumFail}[/] [gray]{percentFail,3:n0}%[/]"),
                            });

                            live.Refresh();
                            await Task.Yield();
                        },
                        dnsResolvedAction: async (hop, reverseDns) =>
                        {
                            await table.UpdateWeightedCells(hop, new[]
                                {
                                    (6, reverseDns?.HostName ?? Failed),
                                });
                            live.Refresh();
                            await Task.Yield();
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
