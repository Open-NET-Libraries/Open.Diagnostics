using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Open.Diagnostics
{
	public class BenchmarkConsoleReport<TBenchParam>
	{

		protected readonly uint Iterations;
		TextWriter Output;
		Func<uint, uint, TBenchParam, TimedResult[]> BenchmarkFunction;

		public BenchmarkConsoleReport(uint iterations, TextWriter output, Func<uint, uint, TBenchParam, TimedResult[]> benchmark) : this(iterations, benchmark)
		{
			Output = output;
		}


		public BenchmarkConsoleReport(uint iterations, StringBuilder output, Func<uint, uint, TBenchParam, TimedResult[]> benchmark) : this(iterations, new StringWriter(output), benchmark)
		{
		}

		public BenchmarkConsoleReport(uint iterations, Func<uint, uint, TBenchParam, TimedResult[]> benchmark)
		{
			if (iterations < 10)
				throw new ArgumentOutOfRangeException(nameof(iterations), iterations, "Need to have a significant number of iterations to be certain of validity.");
			Iterations = iterations;
			BenchmarkFunction = benchmark;
		}

		string[] _resultLabels;
		List<string[]> _results = new List<string[]>();
		public string[][] Results => _results.ToArray();
		public string[] ResultLabels => _resultLabels.ToArray();

		const string SEPARATOR = "------------------------------------";
		public void Separator(bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.WriteLine(SEPARATOR);
			Console.WriteLine(SEPARATOR);
		}

		public void NewLine(bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.WriteLine();
			Console.WriteLine();
		}

		public void Write(char value, bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.Write(value);
			Console.Write(value);
		}

		public void Write(string value, bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.Write(value);
			Console.Write(value);
		}

		public void WriteLine(string value, bool consoleOnly = false)
		{
			if (!consoleOnly) Output?.WriteLine(value);
			Console.WriteLine(value);
		}

		static readonly Regex TimeSpanRegex = new Regex(@"((?:00:)+ (?:0\B)?) ([0.]*) (\S*)", RegexOptions.IgnorePatternWhitespace);

		protected void OutputResult(TimeSpan result, bool consoleOnly = false)
		{
			var match = TimeSpanRegex.Match(result.ToString());
			Console.ForegroundColor = ConsoleColor.Black;
			Write(match.Groups[1].Value, consoleOnly);
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Write(match.Groups[2].Value, consoleOnly);
			Console.ResetColor();
			Write(match.Groups[3].Value, consoleOnly);
		}

		protected void OutputResult(TimedResult result, ConsoleColor? labelColor = null, bool consoleOnly = false)
		{
			OutputResult(result.Duration, consoleOnly);
			Write(' ', consoleOnly);
			if (labelColor.HasValue) Console.ForegroundColor = labelColor.Value;
			WriteLine(result.Label, consoleOnly);
			Console.ResetColor();
		}

		protected void OutputResult(TimedResult result, TimedResult[] all, bool consoleOnly = false)
		{
			var duration = result.Duration;
			OutputResult(duration, consoleOnly);
			Write(' ', consoleOnly);
			var these = all.Where(r => r.Label == result.Label).Select(r => r.Duration).OrderBy(d => d).ToArray();
			var min = these.First();
			var max = these.Last();
			if (min != max)
			{
				if (duration == min)
					Console.ForegroundColor = ConsoleColor.Green;
				else if (duration == max)
					Console.ForegroundColor = ConsoleColor.Red;
			}
			WriteLine(result.Label, consoleOnly);
			Console.ResetColor();
		}

		protected void OutputResults(IEnumerable<TimedResult> results, bool consoleOnly = false)
		{
			foreach (var e in results)
				OutputResult(e, consoleOnly: consoleOnly);

			NewLine(consoleOnly);
		}

		protected void OutputResults(IEnumerable<TimedResult> results, TimedResult[] all, bool consoleOnly = false)
		{
			foreach (var e in results)
				OutputResult(e, all, consoleOnly);

			NewLine(consoleOnly);
		}

		protected virtual TimedResult[] BenchmarkResults(uint count, uint repeat, TBenchParam param)
		{
			return BenchmarkFunction(count, repeat, param);
		}

		protected TimedResult[] OutputResults(uint count, uint repeat, TBenchParam param, bool consoleOnly = false)
		{
			var results = BenchmarkResults(count, repeat, param);
			OutputResults(results, consoleOnly);
			return results;
		}

		public Tuple<string, TimedResult[]> TestResult(string batch, string poolName, uint count, uint repeat, TBenchParam param)
		{
			var header = poolName + "........................................................".Substring(poolName.Length);
			WriteLine(header);
			var results = OutputResults(count, repeat, param);

			if (_resultLabels == null)
			{
				var labels = new List<string>
				{
					"Batch",
					"Pool Type",
				};
				foreach (var r in results)
					labels.Add(r.Label.Substring(4));
				_resultLabels = labels.ToArray();
			}

			var list = new List<string>
			{
				batch,
				poolName
			};
			foreach (var r in results)
				list.Add(r.Duration.ToString());
			_results.Add(list.ToArray());

			return Tuple.Create(header, results);
		}

		List<Tuple<string, Func<uint, TBenchParam>>> _benchmarks = new List<Tuple<string, Func<uint, TBenchParam>>>();
		public void AddBenchmark(string name, Func<uint, TBenchParam> param)
		{
			_benchmarks.Add(Tuple.Create(name, param));
		}

		public void Pretest(uint count, uint repeat)
		{
			foreach (var bench in _benchmarks)
				BenchmarkResults(count, repeat, bench.Item2(count));
		}

		public void Test(uint count, uint multiple = 1)
		{
			var data = new List<string>();
			var repeat = multiple * Iterations / count;
			Console.ForegroundColor = ConsoleColor.Cyan;
			var batch = String.Format("Repeat {1:g} for size {0:g}", count, repeat);
			WriteLine(batch);
			Separator();
			Console.ResetColor();
			NewLine();

			var cursor = Console.CursorTop;
			var results = _benchmarks.Select(b => TestResult(batch, b.Item1, count, repeat, b.Item2(count))).ToList();

			var all = results.SelectMany(r => r.Item2).ToArray();

			Console.SetCursorPosition(0, cursor);

			foreach (var r in results)
			{
				Console.WriteLine(r.Item1);
				OutputResults(r.Item2, all, true);
			}

			NewLine();
		}
	}
}
