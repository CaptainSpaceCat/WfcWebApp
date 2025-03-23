namespace WfcWebApp.Wfc
{
    
public class WfcWave
{
    //TODO store collapsed position tracker in here instead
    // add function to 


    // Dictionary to store sets of possible patterns at each position
    private Dictionary<Vector2I, PatternSet> waveDict = new(); //TODO optimize by storing patterns in a HashTrie instead
    const int maxEntropy = int.MaxValue;

    public bool Wrap = true;

    public Vector2I? boundaryCorner = null;
    public Vector2I boundaryShape;

    private bool InBounds(Vector2I pos) {
        // if the boundaries of the wave are set, perform the check
        if (boundaryCorner != null) {
            Vector2I posAdjusted = pos - boundaryCorner.Value;
            return !(posAdjusted.X < 0 || posAdjusted.Y < 0
            || posAdjusted.X >= boundaryShape.X || posAdjusted.Y >= boundaryShape.Y);
        }
        return true; //no boundaries = always in bounds!
    }

    private Vector2I BoundaryWrap(Vector2I pos) {
        // if the boundaries are set and Wrap is true, perform the wrapping
        if (boundaryCorner != null && Wrap) {
            Vector2I posAdjusted = pos - boundaryCorner.Value;
            posAdjusted = ((posAdjusted % boundaryShape) + boundaryShape) % boundaryShape;
            return posAdjusted + boundaryCorner.Value;
        }
        // if there's no boundary or wrap is false, just return the same pos
        return pos;
    }

    private bool BoundaryCheck(Vector2I pos, out Vector2I outPos) {
        outPos = BoundaryWrap(pos);
        if (!InBounds(outPos)) {
            //Console.WriteLine($"pos {pos} outpos {outPos} OUT OF BOUNDS");
            return false;
        }
        //Console.WriteLine($"pos {pos} outpos {outPos} in bounds");
        return true;
    }


    public IEnumerable<Pattern> EnumeratePatternSet(Vector2I pos) {
        if (TryAccessWave(pos, out PatternSet patternSet)) {
            foreach (Pattern p in patternSet) {
                yield return p;
            }
        }
    }



    public bool GetOrCreatePatternSet(Vector2I pos, out PatternSet patternSet) {
        if (BoundaryCheck(pos, out Vector2I bounded_pos)) {
            if (waveDict.TryGetValue(bounded_pos, out patternSet)) {
                return true;
            }
            patternSet = waveDict[bounded_pos] = new();
            return true;
        }
        patternSet = null;
        return false;
    }


    public void Clear() {
        waveDict.Clear();
    }

	public int GetEntropy(Vector2I pos) {
        if (TryAccessWave(pos, out var patternSet)) {
            return patternSet.Count;
        }
        return maxEntropy;
	}

    private Vector2I boundedPos;
    private bool TryAccessWave(Vector2I pos, out PatternSet patternSet) {
        if (BoundaryCheck(pos, out Vector2I boundedPos)) {
            return waveDict.TryGetValue(boundedPos, out patternSet);
        }
        patternSet = null;
        return false;
    }
    
    public int GetMaxObservedEntropy() {
        int max_seen = 0;
        foreach (Vector2I pos in waveDict.Keys) {
            if (IsUncollapsed(pos)) {
                int entropy = GetEntropy(pos);
                if (entropy > max_seen) {
                    max_seen = entropy;
                }
            }
        }
        return max_seen;
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
                //Console.WriteLine($"position candidate {pos}");
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
        if (TryAccessWave(pos, out PatternSet waveSet)) {
            return waveSet.IsUnobserved;
        }
        return true;
    }

    public bool IsContradiction(Vector2I pos) {
        if (TryAccessWave(pos, out PatternSet waveSet)) {
            return !waveSet.IsUnobserved && GetEntropy(pos) == 0;
        }
        return false;
    }

    public bool GetRandomUncollapsedPosition(out Vector2I random_pos, IEnumerable<Vector2I>? positionFilter = null) {
        random_pos = Vector2I.Zero;
        if (positionFilter == null) {
            positionFilter = waveDict.Keys; // default to searching the entire wave
        }
        positionCandidates.Clear();
        foreach (Vector2I pos in positionFilter) {
            if (BoundaryCheck(pos, out Vector2I wrapped)) {
                if (IsUncollapsed(pos)) {
                    positionCandidates.Add(wrapped);
                }
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
