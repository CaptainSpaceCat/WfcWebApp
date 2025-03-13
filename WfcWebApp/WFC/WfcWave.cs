namespace WfcWebApp.Wfc {
    
public class WfcWave
{
    // Dictionary to store bitmasks at specific coordinates
    private Dictionary<Vector2I, int> waveDict = new Dictionary<Vector2I, int>();

    private static Random random = new Random();

    //public Dictionary<Vector2I, int> GetFullWave() => waveDict;


    // Clears any part of the wave, collapsed or otherwise,
    // whose position fails the provided includePosition delegate (causes it to return false)
    public void CullWave(Func<Vector2I, bool> includePosition) {
        List<Vector2I> toRemove = new List<Vector2I>();
        foreach (Vector2I pos in waveDict.Keys) {
            if (!includePosition(pos)) {
                toRemove.Add(pos);
            }
        }
        foreach (Vector2I pos in toRemove) {
            waveDict.Remove(pos);
        }
    }

    // Method to set a bitmask at a specific position
    public void SetBitmask(Vector2I pos, int bitmask)
    {
        waveDict[pos] = bitmask;
    }

    // Method to get a bitmask at a specific position
    public int GetBitmask(Vector2I pos)
    {
        if (waveDict.TryGetValue(pos, out var value))
        {
            return value;
        }
        return ~0; 
    }

	public int GetEntropy(Vector2I pos) {
        int mask = GetBitmask(pos);
		return System.Numerics.BitOperations.PopCount((uint)mask);
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

    public IEnumerable<Vector2I> FindContradictions() {
        foreach (var kvp in waveDict) {
            if (kvp.Value == 0) {
                yield return kvp.Key;
            }
        }
    }

    public int GetEntropyWindow(Vector2I pos, int size) {
        Vector2I offset = new();
        int center = (size-1)/2;
        int total_entropy = 0;
		for (int x = -center; x < size-center; x++) {
            for (int y = -center; y < size-center; y++) {
                offset.X = x;
                offset.Y = y;
                int entropy = GetEntropy(pos + offset);
                total_entropy += entropy;
            }
        }
        return total_entropy;
	}

    public bool GetRandomUncollapsedPosition(out Vector2I random_pos, IEnumerable<Vector2I> all_positions) {
        position_candidates.Clear();
        foreach (Vector2I pos in all_positions) {
			if (IsUncollapsed(pos)) {
                position_candidates.Add(pos);
            }
        }
        if (position_candidates.Count == 0) {
            random_pos = Vector2I.Zero;
            return false;
        }
        random_pos = position_candidates[random.Next(position_candidates.Count)];
        return true;
    }

    List<Vector2I> position_candidates = new List<Vector2I>();
    public bool GetLeastEntropyPosition(out Vector2I least_entropy_pos, IEnumerable<Vector2I> all_positions, int conv_size) {
		int current_min = 9999; //start at a value larger than entropy could ever be (max actual entropy = #tile types * 9)
		position_candidates.Clear();
		least_entropy_pos = new Vector2I();
		foreach (Vector2I pos in all_positions) {
			if (IsUncollapsed(pos)) {
                int entropy_window = GetEntropyWindow(pos, conv_size);
				if (entropy_window <= current_min) {
					if (entropy_window < current_min) {
						position_candidates.Clear();
						current_min = entropy_window;
					}
					position_candidates.Add(pos);
				}
			}
		}
		if (position_candidates.Count > 0){
			// choose random candidate out of all tiles with equal minimum entropy
			least_entropy_pos = position_candidates[random.Next(position_candidates.Count)];
			return true;
		}
		// all provided positions have entropy <= 1
		return false;
	}

    // Method to check if a specific position has a bitmask set
    public bool HasBitmask(Vector2I pos)
    {
        return waveDict.ContainsKey(pos);
    }

	public void PrintBitmask(Vector2I pos) {
		if (!HasBitmask(pos)) {
			Console.WriteLine("Position contains no bitmask");
		} else {
			string binary = Convert.ToString(GetBitmask(pos), 2); // Convert the integer to a binary string
			int entropy = GetEntropy(pos);
			Console.WriteLine($"Bitmask at <{pos}>: {binary} | Entropy: {entropy}");
		}
	}

    public WaveSliceView GetSliceView(Vector2I center, int size) {
        return new WaveSliceView(this, center, size);
    }

}

public class WaveSliceView : ReferenceView{
	private readonly WfcWave waveRef;
	private readonly Vector2I topLeft;
    public readonly Vector2I localCenter;
    public WaveSliceView(WfcWave waveRef, Vector2I center, int size = 3) {
        this.waveRef = waveRef;
        this.localCenter = Vector2I.One * ((size-1)/2);
        this.topLeft = center - this.localCenter;
        this.size = size;
    }

    public bool InBounds(Vector2I pos) {
		return pos.X >= 0 && pos.Y >= 0 && pos.X < size && pos.Y < size;
	}

	protected override int GetMaskInternal(Vector2I pos) {
		if (!InBounds(pos)) {
			throw new IndexOutOfRangeException($"Position {pos} falls outside the wave slice.");
		}
        return waveRef.GetBitmask(pos + topLeft);
	}

    public void SetBitmask(Vector2I pos, int mask) {
        waveRef.SetBitmask(pos + topLeft, mask);
    }

    public int GetEntropy(Vector2I pos) {
        return waveRef.GetEntropy(pos + topLeft);
    }
}


}
