using LazyCache;
using System.Net;

namespace TraceRtLive.DNS
{
    /// <summary>
    /// DNS resolver that caches entries
    /// </summary>
    public class CachingResolver
    {
        private CachingService Cache { get; } = new CachingService();

        /// <summary>
        /// Resolve the <paramref name="hostname"/>.
        /// Optionally run <paramref name="afterAdd"/> when added. Ignores errors.
        /// </summary>
        /// <param name="hostname">The hostname to resolve</param>
        /// <param name="afterAddAsync">Optional method to invoke if this item was newly-added</param>
        public async Task<IPAddress[]> ResolveAsync(string hostname, Func<IPAddress[]?, Task>? afterAddAsync = null)
        {
            var invokeAdd = false;
            var result = await Cache.GetOrAddAsync(hostname,
                async _ =>
                {
                    invokeAdd = true;
                    try
                    {
                        return await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);
                    }
                    catch
                    {
                        return null;
                    }
                }).ConfigureAwait(false);

            if (invokeAdd && afterAddAsync != null)
            {
                await afterAddAsync(result).ConfigureAwait(false);
            }
            
            return result!;
        }

        /// <summary>
        /// Reverse lookup the <paramref name="hostname"/>.
        /// Optionally run <paramref name="afterAdd"/> when added.
        /// Ignores errors
        /// </summary>
        /// <param name="ip">The hostname to lookup</param>
        /// <param name="afterAddAsync">Optional method to invoke if this item was newly-added</param>
        public async Task<IPHostEntry> ResolveAsync(IPAddress ip, Func<IPHostEntry?,Task>? afterAddAsync = null)
        {
            var invokeAdd = false;
            var result = await Cache.GetOrAddAsync(ip.ToString(), async _ =>
            {
                await Task.Delay(4000);
                invokeAdd = true;
                try
                {
                    return await Dns.GetHostEntryAsync(ip).ConfigureAwait(false);
                }
                catch
                {
                    return null;
                }
            }).ConfigureAwait(false);

            if (invokeAdd && afterAddAsync != null)
            {
                await afterAddAsync(result).ConfigureAwait(false);
            }

            return result!;
        }
    }
}
