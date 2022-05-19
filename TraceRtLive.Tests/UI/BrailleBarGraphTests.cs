using NUnit.Framework;
using TraceRtLive.UI;

namespace TraceRtLive.Tests.UI
{
    [TestFixture]
    public class BrailleBarGraphTests
    {
        [Test]
        // null/empty
        [TestCase(null, false, 4, ExpectedResult = "")]
        [TestCase(new int[0], false, 4, ExpectedResult = "")]
        [TestCase(new[] { 0 }, false, 4, ExpectedResult = "⠀")]
        [TestCase(new[] { 0, 0 }, false, 4, ExpectedResult = "⠀")]
        [TestCase(new[] { 0, 0, 0 }, false, 4, ExpectedResult = "⠀⠀")]
        [TestCase(new[] { 0, 0, 0, 0 }, false, 4, ExpectedResult = "⠀⠀")]

        // single-character, padding
        [TestCase(new[] { 1 }, false, 4, ExpectedResult = "⡀")]
        [TestCase(new[] { 1, 0 }, false, 4, ExpectedResult = "⡀")]
        [TestCase(new[] { 1 }, true, 4, ExpectedResult = "⢀")]

        // incrementing
        [TestCase(new[] { 1, 2, 3 }, false, 4, ExpectedResult = "⣠⡆")]
        [TestCase(new[] { 1, 2, 3 }, true, 4, ExpectedResult = "⢀⣴")]
        [TestCase(new[] { 1, 2, 3, 4 }, false, 4, ExpectedResult = "⣠⣾")]
        [TestCase(new[] { 1, 2, 3, 4 }, true, 4, ExpectedResult = "⣠⣾")]
        [TestCase(new[] { 10, 20, 30, 40 }, true, 4, ExpectedResult = "⣠⣾")]

        // every character
        [TestCase(new[] { 0, 0, 1, 0, 2, 0, 3, 0, 4, 0 }, false, 4, ExpectedResult = "⠀⡀⡄⡆⡇")]
        [TestCase(new[] { 0, 1, 1, 1, 2, 1, 3, 1, 4, 1 }, false, 4, ExpectedResult = "⢀⣀⣄⣆⣇")]
        [TestCase(new[] { 0, 2, 1, 2, 2, 2, 3, 2, 4, 2 }, false, 4, ExpectedResult = "⢠⣠⣤⣦⣧")]
        [TestCase(new[] { 0, 3, 1, 3, 2, 3, 3, 3, 4, 3 }, false, 4, ExpectedResult = "⢰⣰⣴⣶⣷")]
        [TestCase(new[] { 0, 4, 1, 4, 2, 4, 3, 4, 4, 4 }, false, 4, ExpectedResult = "⢸⣸⣼⣾⣿")]

        public string CreateGraph(int[] values, bool alignRight, int maxValue)
            => BrailleBarGraph.CreateGraph(values, maxValue, alignRight);
    }
}
