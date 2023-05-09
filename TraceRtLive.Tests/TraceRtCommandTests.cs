using NUnit.Framework;
using System.Text;

namespace TraceRtLive.Tests
{
    [TestFixture]
    public class TraceRtCommandTests
    {
        [TestCase(0, ExpectedResult = "Green")]
        [TestCase(1, ExpectedResult = "Green")]
        [TestCase(199, ExpectedResult = "Green")]
        [TestCase(200, ExpectedResult = "DarkGreen")]
        [TestCase(999, ExpectedResult = "DarkGreen")]
        [TestCase(1000, ExpectedResult = "DarkRed")]
        [TestCase(1999, ExpectedResult = "DarkRed")]
        [TestCase(2000, ExpectedResult = "Red")]
        [TestCase(20000, ExpectedResult = "Red")]
        public string RttColor(int milliseconds)
        {
            return TraceRtCommand.RttColor(TimeSpan.FromMilliseconds(milliseconds));
        }

        [TestCase(0, 0, ExpectedResult = "[Green]X[/]")] // technically, color irrelevant here

        [TestCase(1, 0, ExpectedResult = "[Green]X[/]")]
        [TestCase(0, 1, ExpectedResult = "[Green]X[/]")]
        [TestCase(1, 1, ExpectedResult = "[Green]X[/]")]

        [TestCase(2, 0, ExpectedResult = "[DarkGreen]X[/]")]
        [TestCase(0, 2, ExpectedResult = "[DarkGreen]X[/]")]

        [TestCase(0, 3, ExpectedResult = "[DarkRed]X[/]")]
        [TestCase(3, 0, ExpectedResult = "[DarkRed]X[/]")]

        [TestCase(0, 4, ExpectedResult = "[Red]X[/]")]
        [TestCase(3, 4, ExpectedResult = "[Red]X[/]")]
        [TestCase(4, 0, ExpectedResult = "[Red]X[/]")]
        [TestCase(4, 4, ExpectedResult = "[Red]X[/]")]

        // following values shouldn't happen from bar graph, since it can only render up to 4 anyway
        [TestCase(5, 0, ExpectedResult = "[Red]X[/]")]
        [TestCase(6, 0, ExpectedResult = "[Red]X[/]")]
        public string RenderColorGlyph(int a, int b)
        {
            var result = new StringBuilder();
            TraceRtCommand.RenderColorGlyph(a, b, 'X', result);
            return result.ToString();
        }
    }
}
