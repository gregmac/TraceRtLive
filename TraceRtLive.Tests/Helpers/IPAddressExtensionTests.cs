using NUnit.Framework;
using System.Net;
using TraceRtLive.Helpers;

namespace TraceRtLive.Tests.Helpers
{
    [TestFixture]
    public class IPAddressExtensionTests
    {
        [Test]
        [TestCase(null, ExpectedResult = false)]
        [TestCase("0.0.0.0", ExpectedResult = false)]
        [TestCase("255.255.255.255", ExpectedResult = false)]
        [TestCase("192.168.0.1", ExpectedResult = true)]
        public bool IsValid(string ip)
            => (ip != null ? IPAddress.Parse(ip) : null).IsValid();
    }
}
