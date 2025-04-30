namespace WfcWebApp.Wfc
{

using System.Collections.Generic;

// Least Entropy Position Tracker
public class LepTracker {
	private readonly SortedSet<Entry> entries = new(EntryComparer.Instance);
	private readonly Dictionary<(int, int), Entry> entryMap = new();


	public void Add((int, int) pos, float entropy) {
		var entry = new Entry(pos, entropy);
		if (entryMap.ContainsKey(pos)) {
			UpdateEntropy(pos, entropy); // Replace if already exists
			return;
		}

		entries.Add(entry);
		entryMap[pos] = entry;
	}

	public void UpdateEntropy((int, int) pos, float newEntropy) {
		if (!entryMap.TryGetValue(pos, out var oldEntry)) return;

		entries.Remove(oldEntry);
		entryMap.Remove(pos);

		var newEntry = new Entry(pos, newEntropy);
		entries.Add(newEntry);
		entryMap[pos] = newEntry;
	}

	public bool TryGetLEP(out (int X, int Y) pos) {
		while (entries.Count > 0) {
			var entry = entries.Min;
            
			if (entry.entropy <= 1) {
				entries.Remove(entry);
				entryMap.Remove(entry.pos);
				continue;
			}
			pos = entry.pos;
			return true;
		}
		pos = default;
		return false;
	}

	private record struct Entry((int X, int Y) pos, float entropy);

	private class EntryComparer : IComparer<Entry> {
		public static readonly EntryComparer Instance = new();

		public int Compare(Entry a, Entry b) {
			int cmp = a.entropy.CompareTo(b.entropy);
			if (cmp != 0) return cmp;
			// Tie-break using position to avoid equality issues
			cmp = a.pos.X.CompareTo(b.pos.X);
			if (cmp != 0) return cmp;
			return a.pos.Y.CompareTo(b.pos.Y);
		}
	}
}


}