using NUnit.Framework;
using TraceRtLive.Helpers;

namespace TraceRtLive.Tests.Helpers
{
    public class TimeStatsTests
    {
        [Test, TestCaseSource(nameof(Mean_TestData)), MaxTime(500)]
        [TestCase(new[] { 9, 1, 5, }, ExpectedResult = 5)]
        [TestCase(new[] { 1, 2 }, ExpectedResult = 1.5)]
        [TestCase(new[] { 2, int.MaxValue }, ExpectedResult = 1073741824.5d)]
        public double Mean(IEnumerable<int> milliseconds)
        {
            var stats = new TimeStats();
            foreach (var item in milliseconds) stats.Add(TimeSpan.FromMilliseconds(item));
            return stats.Mean.TotalMilliseconds;
        }
        public static IEnumerable<TestCaseData> Mean_TestData()
        {
            yield return new TestCaseData(Enumerable.Range(0, 10000000)).Returns(4999999.5d).SetName("{m}(10,000,000 entries)");
        }

        [Test]
        [TestCase(new[] { 4, 100, 2 }, ExpectedResult = 2)]
        [TestCase(new[] { 4 }, ExpectedResult = 4)]
        [TestCase(new[] { -10, -5 }, ExpectedResult = -10)]
        [TestCase(new[] { int.MinValue, int.MaxValue, 0 }, ExpectedResult = int.MinValue)]
        public double Min(IEnumerable<int> milliseconds)
        {
            var stats = new TimeStats();
            foreach (var item in milliseconds) stats.Add(TimeSpan.FromMilliseconds(item));
            return stats.Min.TotalMilliseconds;
        }

        [Test]
        [TestCase(new[] { 4, 100, 2 }, ExpectedResult = 100)]
        [TestCase(new[] { 4 }, ExpectedResult = 4)]
        [TestCase(new[] { -10, -5 }, ExpectedResult = -5)]
        [TestCase(new[] { int.MinValue, int.MaxValue, 0 }, ExpectedResult = int.MaxValue)]
        public double Max(IEnumerable<int> milliseconds)
        {
            var stats = new TimeStats();
            foreach (var item in milliseconds) stats.Add(TimeSpan.FromMilliseconds(item));
            return stats.Max.TotalMilliseconds;
        }
    }
}
