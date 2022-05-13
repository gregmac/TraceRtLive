using System.Net;

namespace TraceRtLive.Trace
{
    public class TraceResult
    {
        public int Hops { get; init; }
        public bool InProgress { get; init; }
        public IPAddress? IP { get; init; }
        public TimeSpan? RoundTripTime { get; init; }
    }
}
