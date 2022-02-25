using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Open.Diagnostics;

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

	public IEnumerable<(int Index, TimedResult Result)> TestOnce()
	{
		var index = 0;
		var total = TimeSpan.Zero;
		foreach (var r in TestOnceInternal())
		{
			total += r.Duration;
			yield return (index++, r);
		}

		yield return (index, new TimedResult("TOTAL", total));
	}

	protected abstract IEnumerable<TimedResult> TestOnceInternal();

	public IEnumerable<IEnumerable<(int Index, TimedResult Result)>> TestRepeated()
	{
		for (var i = 0; i < RepeatCount; i++)
		{
			yield return TestOnce();
		}
	}

	TimedResult[]? _result;
	public TimedResult[] Result
		=> LazyInitializer.EnsureInitialized(
			ref _result, () =>
				TestRepeated()
				.SelectMany(s => s) // Get all results.
				.GroupBy(k => k.Index) // Group by their 'id' (ordinal).
				.Select(Sum) // Sum/merge those groups.
				.OrderBy(r => r.Index) // Order by their ordinal.
				.Select(r => r.Result) // Select the actual result.
				.ToArray() // And done.
			)!;

	static (int Index, TimedResult Result) Sum(IEnumerable<(int Index, TimedResult Result)> results)
	{
		var a = results.ToArray();
		if (a.Length == 0) return (-1, default);

		var i = a[0].Index;
		return a.Skip(1).Any(r => r.Index != i)
			? throw new InvalidOperationException("Summing unmatched TimeResults.")
			: ((int Index, TimedResult Result))(i, a.Select(s => s.Result).Sum());
	}
}
