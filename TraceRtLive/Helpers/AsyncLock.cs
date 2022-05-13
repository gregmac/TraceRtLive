namespace TraceRtLive.Helpers
{
    public class AsyncLock
	{
		private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		public async Task<IDisposable> ObtainLock()
		{
			await _semaphore.WaitAsync();
			return new OnDispose(() => _semaphore.Release());
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		private class OnDispose : IDisposable
		{
			public OnDispose(Action dispose)
			{
				_dispose = dispose;
			}

			Action _dispose;

			public void Dispose() => _dispose();
		}
	}
}
