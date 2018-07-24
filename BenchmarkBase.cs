using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Open.Diagnostics
{
	[SuppressMessage("ReSharper", "NotAccessedField.Global")]
	public abstract class BenchmarkBase<TBenchParam>
	{
		public readonly uint TestSize;
		public readonly uint RepeatCount; // Number of times to repeat the test from scratch.
		protected readonly TBenchParam Param;

		protected BenchmarkBase(uint size, uint repeat, TBenchParam param)
		{
			TestSize = size;
			RepeatCount = repeat;
			Param = param;
		}

		public IEnumerable<Tuple<int, TimedResult>> TestOnce()
		{
			var index = 0;
			var total = TimeSpan.Zero;
			foreach (var r in TestOnceInternal())
			{
				total += r.Duration;
				yield return Tuple.Create(index++, r);
			}

			yield return Tuple.Create(index, new TimedResult("TOTAL", total));
		}

		protected abstract IEnumerable<TimedResult> TestOnceInternal();

		public IEnumerable<IEnumerable<Tuple<int, TimedResult>>> TestRepeated()
		{
			for (var i = 0; i < RepeatCount; i++)
			{
				yield return TestOnce();
			}
		}

		TimedResult[] _result;
		public TimedResult[] Result
		{
			get
			{
				return LazyInitializer.EnsureInitialized(ref _result, () =>
					TestRepeated()
					.SelectMany(s => s) // Get all results.
					.GroupBy(k => k.Item1) // Group by their 'id' (ordinal).
					.Select(Sum) // Sum/merge those groups.
					.OrderBy(r => r.Item1) // Order by their ordinal.
					.Select(r => r.Item2) // Select the actual result.
					.ToArray() // And done.
				);
			}
		}

		static Tuple<int, TimedResult> Sum(IEnumerable<Tuple<int, TimedResult>> results)
		{
			var a = results.ToArray();
			if (a.Length == 0) return null;
			var i = a[0].Item1;
			if (a.Skip(1).Any(r => r.Item1 != i))
				throw new InvalidOperationException("Summing unmatched TimeResults.");

			return Tuple.Create(i, a.Select(s => s.Item2).Sum());
		}

	}
}
