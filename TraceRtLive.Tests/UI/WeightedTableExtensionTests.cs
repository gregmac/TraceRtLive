using NUnit.Framework;
using TraceRtLive.UI;

namespace TraceRtLive.Tests.UI
{
    [TestFixture]
    public class WeightedTableExtensionTests
    {
        [Test, TestCaseSource(nameof(FillBlankColumns_TestData))]
        public IEnumerable<string?> FillBlankColumns(IEnumerable<(int columnIndex, string value)> columnValues, int numColumns)
            => WeightedTableExtensions.FillBlankColumns(columnValues, numColumns)
                .Select(x => x.RenderToString())
                .ToArray();

        public static IEnumerable<TestCaseData> FillBlankColumns_TestData()
        {
            yield return new TestCaseData(
                new[]
                {
                    (1, "one"),
                    (3, "three"),
                },
                5)
                .Returns(new[]
                {
                    "",
                    "one",
                    "",
                    "three",
                    "",
                })
                .SetName("{m}(mixed: 2 of 5)");

            yield return new TestCaseData(
                new[]
                {
                    (0, "zero"),
                    (1, "one"),
                },
                3)
                .Returns(new[]
                {
                    "zero",
                    "one",
                    "",
                })
                .SetName("{m}(at end: 2 of 3)");
            yield return new TestCaseData(
                new[]
                {
                    (1, "one"),
                    (2, "two"),
                },
                3)
                .Returns(new[]
                {
                    "",
                    "one",
                    "two",
                })
                .SetName("{m}(at start: 2 of 3)");

            yield return new TestCaseData(
                new[]
                {
                    (0, "zero"),
                },
                1)
                .Returns(new[]
                {
                    "zero",
                })
                .SetName("{m}(all filled: 1 of 1)");
            yield return new TestCaseData(
               new[]
               {
                    (0, "zero"),
                    (1, "one"),
                    (2, "two"),
               },
               3)
               .Returns(new[]
               {
                    "zero",
                    "one",
                    "two",
               })
               .SetName("{m}(all filled: 3 of 3)");

            yield return new TestCaseData(
                Enumerable.Empty<(int, string)>(),
                1)
                .Returns(new[]
                {
                    "",
                })
                .SetName("{m}(all blank: 0 of 1)");
            yield return new TestCaseData(
                Enumerable.Empty<(int, string)>(),
                3)
                .Returns(new[]
                {
                    "",
                    "",
                    "",
                })
                .SetName("{m}(all blank: 0 of 3)");

            
            yield return new TestCaseData(
                Enumerable.Empty<(int, string)>(),
                0)
                .Returns(Array.Empty<string>())
                .SetName("{m}(extras ignored: 0 of 0)");

            yield return new TestCaseData(
                new[]
                {
                    (1, "one"),
                },
                0)
                .Returns(Array.Empty<string>())
                .SetName("{m}(extras ignored: 1 of 0)");
            yield return new TestCaseData(
                new[]
                {
                    (0, "zero"),
                    (1, "one"),
                },
                1)
                .Returns(new[]
                {
                    "zero",
                })
                .SetName("{m}(extras ignored: 2 of 1)");
        }
    }
}
