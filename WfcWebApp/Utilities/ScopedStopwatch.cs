using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;

namespace WfcWebApp.Utils
{

public class ScopedStopwatch : IDisposable
{
	private readonly string _name;

	public ScopedStopwatch([CallerMemberName] string name = "")
	{
		_name = name;
		StopwatchManager.Start(_name);
	}

	public void Dispose()
	{
		StopwatchManager.Stop(_name);
	}
}




public static class StopwatchManager
{
	private class StopwatchData
	{
		public Stopwatch Stopwatch = new();
		public TimeSpan Accumulated = TimeSpan.Zero;

		public int Laps = 0;
		public bool Running => Stopwatch.IsRunning;

	}

	private static Dictionary<string, StopwatchData> _watches = new();

	public static void Start(string name)
	{
		if (!_watches.TryGetValue(name, out var data))
		{
			data = new StopwatchData();
			_watches[name] = data;
		}
		if (!data.Running) {
			data.Stopwatch.Start();
			data.Laps++;
		}
	}

	public static void Stop(string name)
	{
		if (_watches.TryGetValue(name, out var data) && data.Running)
		{
			data.Stopwatch.Stop();
			data.Accumulated += data.Stopwatch.Elapsed;
			data.Stopwatch.Reset();
		}
	}

	public static TimeSpan GetElapsed(string name)
	{
		if (_watches.TryGetValue(name, out var data))
			return data.Accumulated + (data.Running ? data.Stopwatch.Elapsed : TimeSpan.Zero);
		return TimeSpan.Zero;
	}

	public static TimeSpan GetAvgElapsed(string name)
	{
		return GetElapsed(name) / _watches[name].Laps;
	}

	public static void Reset(string name)
	{
		if (_watches.TryGetValue(name, out var data))
		{
			data.Stopwatch.Reset();
			data.Accumulated = TimeSpan.Zero;
		}
	}

	private static string StopwatchToString(StopwatchData data) {
		double elapsed_ms = data.Accumulated.TotalMilliseconds;
		return $"{elapsed_ms:F2} ms over {data.Laps} laps, avg: {elapsed_ms/data.Laps:F2} ms/lap";
	}


    public static void Dump(bool sorted = true)
    {	
		if (sorted) {
			foreach (var pair in _watches.OrderByDescending(p => GetAvgElapsed(p.Key)))
			{
				Console.WriteLine($"{pair.Key}: {StopwatchToString(pair.Value)}");
			}
		} else {
			foreach (var pair in _watches)
			{
				Console.WriteLine($"{pair.Key}: {StopwatchToString(pair.Value)}");
			}
		}
        
    }

}


}
