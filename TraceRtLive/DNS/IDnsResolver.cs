using System.Net;

namespace TraceRtLive.DNS
{
    public interface IDnsResolver
    {
        /// <summary>
        /// Resolve the <paramref name="hostname"/> to one or more <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="hostname">The hostname to resolve</param>
        /// <returns>
        /// The list of <see cref="IPAddress"/> this resolved to,
        /// or <see langword="null"/> on errors.
        /// </returns>


        Task<IPAddress[]?> ResolveAsync(string hostname);

        /// <summary>
        /// Reverse lookup the <paramref name="ip"/> to find the associated hostname.
        /// </summary>
        /// <param name="ip">The IP to lookup</param>
        /// <returns>The <see cref="IPHostEntry"/> for the <paramref name="ip"/>,
        /// or <see langword="null"/> on any errors.</returns>
        Task<IPHostEntry?> ResolveAsync(IPAddress ip);

    }
}