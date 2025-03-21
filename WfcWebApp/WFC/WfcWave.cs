namespace WfcWebApp.Wfc
{
    
public class WfcWave
{
    // Dictionary to store sets of possible patterns at each position
    private Dictionary<Vector2I, HashSet<Pattern>> waveDict = new(); //TODO optimize by storing patterns in a HashTrie instead
    const int maxEntropy = int.MaxValue;

    public IEnumerable<Pattern> EnumeratePatternSet(Vector2I pos) {
        if (waveDict.TryGetValue(pos, out HashSet<Pattern> patternSet)) {
            foreach (Pattern p in patternSet) {
                yield return p;
            }
        }
    }

    // If a position key exists in the waveDict, this will offer the pattern hashset there, and return true (initialized)
    // If a key doesn't exist yet, this will create a pattern hashset at the position, and return false (uninitialized)
    public bool GetOrCreatePatternSet(Vector2I pos, out HashSet<Pattern> patternSet) {
        if (waveDict.TryGetValue(pos, out patternSet)) {
            return true;
        }
        patternSet = waveDict[pos] = new();
        return false;
    }

    // If a position key exists in the waveDict, this will offer the pattern hashset there, and return true
    // If a key doesn't exist yet, it will simply return false WITHOUT creating an entry
    public bool TryGetPatternSet(Vector2I pos, out HashSet<Pattern> patternSet) {
        if (waveDict.TryGetValue(pos, out patternSet)) {
            return true;
        }
        return false;
    }


    public void Clear() {
        waveDict.Clear();
    }

	public int GetEntropy(Vector2I pos) {
        if (waveDict.TryGetValue(pos, out var patternSet)) {
            return patternSet.Count;
        }
        return maxEntropy;
	}

    public bool IsEntropyValid(Vector2I pos, int threshold) {
        if (waveDict.TryGetValue(pos, out var patternSet)) {
            return patternSet.Count <= threshold;
        }
        // if the wave doesn't have an entry here, we definitely want to process this position
        return true;
    }

    private List<Vector2I> positionCandidates = new(); //store and reuse to avoid having to re-declare many times in extremely dense loops

    public bool GetLeastEntropyPosition(out Vector2I leastEntropyPos, IEnumerable<Vector2I>? positionFilter = null, bool includeCollapsed = false) {
        leastEntropyPos = Vector2I.Zero;
        if (positionFilter == null) {
            positionFilter = waveDict.Keys; // default to searching the entire wave
        }
        positionCandidates.Clear();
        int currentMinEntropy = maxEntropy;
        foreach (Vector2I pos in positionFilter) {
            int entropy = GetEntropy(pos);

            // entropy must be at least 2; a value of 0 is a contradiction, and value of 1 is collapsed
            // if includeCollapsed is set to true, we also accept entropy values of 1
            if ((entropy > 1 || (includeCollapsed && entropy == 1)) && entropy <= currentMinEntropy) {
                if (entropy < currentMinEntropy) {
                    currentMinEntropy = entropy;
                    positionCandidates.Clear();
                }
                positionCandidates.Add(pos);
                Console.WriteLine($"position candidate {pos}");
            }
        }
        if (positionCandidates.Count > 0) {
            leastEntropyPos = RandomUtils.Choice(positionCandidates);
            return true;
        }
        return false;
    }



    // Checks if a tile is EXACTLY collapsed (meaning it also returns false for contradictions)
    public bool IsCollapsed(Vector2I pos) {
        return GetEntropy(pos) == 1;
    }
    // Checks if a tile has AT LEAST 2 possibilities remaining and can still be collapsed
    // Crucially ignores both collapsed tiles and contradictions
    public bool IsUncollapsed(Vector2I pos) {
        return GetEntropy(pos) > 1;
    }

    public bool IsUnobserved(Vector2I pos) {
        return !waveDict.ContainsKey(pos);
    }

    public bool IsContradiction(Vector2I pos) {
        return GetEntropy(pos) == 0;
    }

    public bool GetRandomUncollapsedPosition(out Vector2I random_pos, IEnumerable<Vector2I>? positionFilter = null) {
        random_pos = Vector2I.Zero;
        if (positionFilter == null) {
            positionFilter = waveDict.Keys; // default to searching the entire wave
        }
        positionCandidates.Clear();
        foreach (Vector2I pos in positionFilter) {
			if (IsUncollapsed(pos)) {
                positionCandidates.Add(pos);
            }
        }
        if (positionCandidates.Count > 0) {
            random_pos = RandomUtils.Choice(positionCandidates);
            return true;
        }
        return false;
    }

}


}
