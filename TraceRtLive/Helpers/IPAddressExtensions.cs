using System.Net;

namespace TraceRtLive.Helpers
{
    public static class IPAddressExtensions
    {
        /// <summary>
        /// Checks if the given <paramref name="ip"/> is not one of: <see langword="null"/>,
        /// <see cref="IPAddress.Any">0.0.0.0</see> or <see cref="IPAddress.Broadcast">255.255.255.255</see>.
        /// </summary>
        /// <param name="ip">The IP address to check</param>
        public static bool IsValid(this IPAddress? ip)
            => ip != null && !ip.Equals(IPAddress.Any) && !ip.Equals(IPAddress.Broadcast);
    }
}
