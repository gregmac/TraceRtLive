using System.Net;
using TraceRtLive.Ping;
using IPStatus = System.Net.NetworkInformation.IPStatus;

namespace TraceRtLive.Tests
{
    public class AsyncPingMock : IAsyncPing
	{
		public AsyncPingMock(params int[] rttValues)
		{
			_rttValues = rttValues;
		}

		private int[] _rttValues;

		public async Task<PingReply> PingAsync(IPAddress target, int ttl, CancellationToken cancellation)
		{
			if (ttl == 0) throw new ArgumentOutOfRangeException(nameof(ttl));

			// if past end, use last entry
			var rttIndex = ttl > _rttValues.Length ? _rttValues.Length - 1 : ttl - 1;

			var rtt = TimeSpan.FromMilliseconds(Math.Abs(_rttValues[rttIndex]));
			await Task.Delay(rtt);

			if (ttl < _rttValues.Length)
			{
				return new PingReply
				{
					Address = new IPAddress(new byte[] { 10, 10, 10, (byte)ttl }),
					RoundtripTime = rtt,
					Status = _rttValues[rttIndex] > 0 ? IPStatus.TtlExpired : IPStatus.TimedOut,
				};
			}
			else
			{
				return new PingReply
				{
					Address = target,
					RoundtripTime = rtt,
					Status = _rttValues[rttIndex] > 0 ? IPStatus.Success : IPStatus.TimedOut,
				};
			}
		}
	}

}
