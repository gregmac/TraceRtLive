using System.Net;

namespace TraceRtLive.DNS
{
    /// <summary>
    /// Simple DNS resolver based on <see cref="Dns.GetHostAddresses(string)"/>
    /// </summary>
    public class DnsResolver : IDnsResolver
    {
        /// <inheritdoc/>
        public async Task<IPAddress[]?> ResolveAsync(string hostname)
        {
            try
            {
                return await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IPHostEntry?> ResolveAsync(IPAddress ip)
        {
            try
            {
                return await Dns.GetHostEntryAsync(ip).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }
    }
}
