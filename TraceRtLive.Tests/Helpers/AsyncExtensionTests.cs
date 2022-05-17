using NUnit.Framework;
using Shouldly;
using TraceRtLive.Helpers;

namespace TraceRtLive.Tests.Helpers
{
    [TestFixture]
    public class AsyncExtensionTests
    {
        [Test]
        public async Task FirstAsync_Single()
        {
            (await SingleAsyncEnumerable().FirstAsync()).ShouldBe(1);
        }
        [Test]
        public async Task FirstAsync_Empty()
        {
            await Should.ThrowAsync<InvalidOperationException>(() => EmptyAsyncEnumerable().FirstAsync());
        }

        [Test]
        public async Task FirstOrDefaultAsync_Single()
        {
            (await SingleAsyncEnumerable().FirstOrDefaultAsync()).ShouldBe(1);
        }
        [Test]
        public async Task FirstOrDefaultAsync_Empty()
        {
            (await EmptyAsyncEnumerable().FirstOrDefaultAsync()).ShouldBeNull();
        }

        [Test]
        public async Task NextOrDefaultAsync()
        {
            var enumerator = CreateAsyncEnumerable(Enumerable.Range(0,3)).GetAsyncEnumerator();

            (await enumerator.NextOrDefaultAsync().ConfigureAwait(false)).ShouldBe(0);
            (await enumerator.NextOrDefaultAsync().ConfigureAwait(false)).ShouldBe(1);
            (await enumerator.NextOrDefaultAsync().ConfigureAwait(false)).ShouldBe(2);
            (await enumerator.NextOrDefaultAsync().ConfigureAwait(false)).ShouldBe(default(int));
        }

        private static async IAsyncEnumerable<int?> SingleAsyncEnumerable()
        {
            yield return 1;
            await Task.Delay(1).ConfigureAwait(false);
            throw new InvalidOperationException("Iterated after second item");
        }

        private static async IAsyncEnumerable<int?> EmptyAsyncEnumerable()
        {
            await Task.Delay(1).ConfigureAwait(false);
            yield break;
        }
        private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                await Task.Delay(1);
                yield return item;
            }
        }
    }
}
