using NUnit.Framework;
using Shouldly;
using TraceRtLive.Helpers;

namespace TraceRtLive.Tests.Helpers
{
    public class CircularBufferTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(5)]
        public void Partial(int numPopulated)
        {
            var buffer = new CircularBuffer<int>(5);

            for (var i = 0; i < numPopulated; i++) buffer.Add(i);

            buffer.Count.ShouldBe(numPopulated);
            buffer.ToArray().ShouldBe(Enumerable.Range(0, numPopulated));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(19)]
        [TestCase(21)]
        public void Full(int numInitial)
        {
            var buffer = new CircularBuffer<int>(5);

            // initialize with some number of entries
            for (var i = 0; i < numInitial; i++) buffer.Add(999);

            // populate 0-4
            for (var i = 0; i < 5; i++) buffer.Add(i);

            buffer.ToArray().ShouldBe(new[] { 0, 1, 2, 3, 4 });
        }
    }
}
