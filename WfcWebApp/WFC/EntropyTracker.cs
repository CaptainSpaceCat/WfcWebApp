using WfcWebApp.Utils;

namespace WfcWebApp.Wfc
{

class EntropyTracker {
	private readonly SortedSet<Entry> globalHeap = new(EntryComparer.Instance);
	private readonly SortedSet<Entry> fringeHeap = new(EntryComparer.Instance);
	private readonly Dictionary<(int, int), Entry> entryMap = new();
	private readonly HashSet<(int, int)> fringeSet = new();

	public int FringeCount => fringeHeap.Count;

	public void AddOrUpdate((int, int) pos, int entropy, int count) {
		if (entryMap.TryGetValue(pos, out var old)) {
			globalHeap.Remove(old);
			if (fringeSet.Contains(pos)) fringeHeap.Remove(old);
		}

		var entry = new Entry(pos, entropy, count);
		entryMap[pos] = entry;

		// Only add to global if uncollapsed
		if (count > 1) {
			globalHeap.Add(entry);
		}

		// Add to fringe if already flagged
		if (count > 0 && fringeSet.Contains(pos)) {
			fringeHeap.Add(entry); // include collapsed if needed
		}
	}

	public void AddToFringe((int, int) pos) {
		if (!fringeSet.Add(pos)) return; // already present

		if (entryMap.TryGetValue(pos, out var entry)) {
			fringeHeap.Add(entry); // include collapsed tiles
		}
	}

	public void RemoveFromFringe((int, int) pos) {
		if (!fringeSet.Remove(pos)) return;

		if (entryMap.TryGetValue(pos, out var entry)) {
			fringeHeap.Remove(entry);
		}
	}

	public void ClearFringe() {
		fringeHeap.Clear();
		fringeSet.Clear();
	}

	public void Clear() {
		ClearFringe();
		globalHeap.Clear();
		entryMap.Clear();
	}

	public bool TryGetFringeLEP(out (int, int) pos) {
		if (fringeHeap.Count == 0) {
			pos = default;
			return false;
		}
		pos = fringeHeap.Min.pos;
		return true;
	}

	public bool TryGetGlobalLEP(out (int, int) pos) {
		if (globalHeap.Count == 0) {
			pos = default;
			return false;
		}
		pos = globalHeap.Min.pos;
		return true;
	}

	public bool TryGetEntropy((int, int) pos, out int entropy) {
		if (entryMap.TryGetValue(pos, out var entry)) {
			entropy = entry.entropy;
			return true;
		}
		entropy = int.MaxValue;
		return false;
	}


	private record struct Entry((int X, int Y) pos, int entropy, int count);

	private class EntryComparer : IComparer<Entry> {
		public static readonly EntryComparer Instance = new();

		public int Compare(Entry a, Entry b) {
			int cmp = a.entropy.CompareTo(b.entropy);
			if (cmp != 0) return cmp;
			cmp = a.pos.X.CompareTo(b.pos.X);
			return cmp != 0 ? cmp : a.pos.Y.CompareTo(b.pos.Y);
		}
	}
}



}