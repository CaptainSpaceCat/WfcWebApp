namespace WfcWebApp.Wfc
{


/*
The purpose of this class is to run the WFC algorithm at specified positions
For this game, we want to give the generator a position and a radius,
and have it generate all tiles in that radius, not overwriting any existing tiles,
and clearing any existing tiles outside of the larger secondary radius (defined below)

Additionally, to avoid getting stuck in a "sweep" (backprop keeps generating the same similar subset of the input)
We should have a secondary radius larger than the first. Between the first radius and the second,
We will generate a single cycle at every position whose coordinates are both multiples of 20 (or around that)
This ensures that the algorithm has time to "preload" some of the material at periodic intervals before merging with the main "bubble"
*/

public class WfcGenerator
{

	private static Random random = new Random();
	public WfcPalette wfcPalette;
	public WfcWave wfcWave;

	public int convSize = 3;
	public bool rotationsEnabled = false;

	public float smallRadius;
	public float largeRadius;
	private int preloadFrequency = 20; // Grid spacing for preloading WFC generation in larger radius
	private int backpropHorizon = 50; // Max number of backprop iterations before moving on
	private int backpropMaxDistance = 10;
	private float randomEpsilon = 0.05f; // Probability of collapsing a random tile instead of the one with the lowest entropy
	


	

	HashSet<Vector2I> collapsedPositionTracker = new();
	private void StartGenerate(IEnumerable<Vector2I> positionIterator) {
		int c = 0;
		int max_backprop_distance = 10;
		Vector2I collapse_pos;
		collapsedPositionTracker.Clear();
		while (c++ < 1000) {
			if (TrySelectNextCollapsePosition(out collapse_pos, positionIterator)) {
				SingleGenerationCycleBounded(collapse_pos, max_backprop_distance);
			} else {
				break;
			}
		}

		Console.WriteLine($"GenerateWithinArea took {c} iterations!");
	}


	private bool TrySelectNextCollapsePosition(out Vector2I collapse_pos, IEnumerable<Vector2I> positionIterator) {
		bool success;
		if (random.NextDouble() < randomEpsilon) {
			//if we roll under epsilon, choose the next tile to collaps uniformly at random
			success = wfcWave.GetRandomUncollapsedPosition(out collapse_pos, positionIterator);
		} else {
			// find tile with smallest nonzero entropy
			success = wfcWave.GetLeastEntropyPosition(out collapse_pos, positionIterator, convSize);
		}
		if (!success) {
			// If we fail to find a single uncollapsed tile, we are done generating within the circle and can exit
			return false;
		}
		if (collapsedPositionTracker.Contains(collapse_pos)) {
			Console.WriteLine($"Stuck on pos {collapse_pos} with entropy {wfcWave.GetEntropy(collapse_pos)}");
			return false;
		}
		collapsedPositionTracker.Add(collapse_pos);
		return true;
	}

	// Collapses the wfcWave at the specified collapse position
	// Performs up to backpropHorizon iterations of backprop around this position
	// This backprop extends up to <radius> distance away
	private void SingleGenerationCycleBounded(Vector2I collapse_pos, float radius) {
		if (wfcWave.IsUncollapsed(collapse_pos)) { // this tile can still be collapsed
			int collapse_mask = ChooseCollapseMask(collapse_pos, DoesWaveSliceFit);
			
			// the chosen tile-type mask is assigned to the collapse_pos and wfcWave is collapsed
			// note that it could be assigning a value of 0 as the mask. if the algorith ran into a 
			// contradiction, it won't stop, it'll just try to generate around the contradiction
			wfcWave.SetBitmask(collapse_pos, collapse_mask);

			PerformBackpropBounded(collapse_pos, radius);
		}
	}


	private void PerformSelfHealing() {
		foreach (Vector2I collapse_pos in wfcWave.FindContradictions()) {
			int collapse_mask = ChooseCollapseMask(collapse_pos, ScoreWaveSliceFit);
			if (collapse_mask == 0) {
				// This is a rare edge case in which the biome map sampled at this position
				// somehow doesn't fit at all into the generated terrain
				// Usually at least one or two tiles match somewhere, but occasionally we need to fall back to local averaging
				collapse_mask = FallbackCollapseMask(collapse_pos);
			}
			wfcWave.SetBitmask(collapse_pos, collapse_mask);
		}
	}

	Dictionary<int, int> fitCounter = new Dictionary<int, int>();
	/// <summary>
	/// Chooses a one-hot bitmask to collapse the wave function at a single position.
	/// </summary>
	/// <param name="collapse_pos">Position at which to perform the collapse.</param>
	/// <param name="scoringFunction">The function used to evaluate each possible palette tile.</param>
	private int ChooseCollapseMask(Vector2I collapse_pos, Func<ReferenceView, ReferenceView, Vector2I, int> scoringFunction) {
		// we have: c*c window of the wfcWave, p*p window of palette
		// we need to slide the window along the palette and track the positions where it fits

		ReferenceView waveSlice = wfcWave.GetReferenceView(collapse_pos, convSize);
		fitCounter.Clear(); //keep this as a persistent dict since this function will need to be reused rather a lot

		Vector2I bounds = new Vector2I(wfcPalette.Width, wfcPalette.Height);
		if (!wfcPalette.Wrap) {
			bounds -= Vector2I.One*(convSize-1);
		}
		Vector2I offset = new();
		int n_rotations = rotationsEnabled ? 4 : 1;
		
		for (int i = 0; i < n_rotations; i++) {
			waveSlice.rotation = i;
			for (int x = 0; x < bounds.X; x++) {
				for (int y = 0; y < bounds.Y; y++) {
					offset.X = x - (convSize-1)/2;
					offset.Y = y - (convSize-1)/2;
					int score = scoringFunction(waveSlice, wfcPalette.GetReferenceView(), offset);
					if (score > 0) {
						int mask = wfcPalette.GetBitmask(offset);
						fitCounter[mask] = fitCounter.TryGetValue(mask, out int value) ? value + score : score;
					}
				}
			}
		}

		int collapse_mask = 0; //default to 0, representing a wfc contradiction
		if (fitCounter.Count > 0) {
			// randomly choose based on weights to collapse this tile
			collapse_mask = GetWeightedRandomKey(fitCounter);
		}

		return collapse_mask;
	}

	// Fallback option for self-healing in very rare cases
	// Default to whatever value is found most frequently in surroundings
	private int FallbackCollapseMask(Vector2I collapse_pos) {
		ReferenceView waveSlice = wfcWave.GetReferenceView(collapse_pos, convSize);
		fitCounter.Clear();
		Vector2I pos = new();
		for (int x = 0; x < waveSlice.size; x++) {
			for (int y = 0; y < waveSlice.size; y++) {
				pos.X=x;
				pos.Y=y;
				if (waveSlice.GetEntropy(pos) == 1) {
					int mask = waveSlice.GetBitmask(pos);
					fitCounter[mask] = fitCounter.TryGetValue(mask, out int value) ? value + 1 : 1;
				}
			}
		}
		if (fitCounter.Count > 0) {
			return GetHeaviestKey(fitCounter);
		}
		// If even this fails, we're just SOL
		return 0;
	}


	// Simulates an intersection between a waveSlice and an equal sized portion of a biomePallete at a specified center position
	// Counts the number of elements in the waveSlice that don't contradict with their corresponding position in the palette
	// Basically the more similar the slice is to the palette the higher the score
	private int ScoreWaveSliceFit(ReferenceView waveSlice, ReferenceView paletteSlice, Vector2I center) {
		int score = 0;
		Vector2I pos = new();
		for (int x = 0; x < waveSlice.size; x++) {
			for (int y = 0; y < waveSlice.size; y++) {
				pos.X = x - (convSize-1)/2;
				pos.Y = y - (convSize-1)/2;
				// waveSlice.GetMask() returns an indicator vector
				// biomePalette.GetMask() returns a one-hot vector
				int mask = waveSlice.GetBitmask(pos) & paletteSlice.GetBitmask(pos + center);
				if (mask > 0) {
					// If any of the active bits are shared between the wfcWave slice and biome palette masks,
					// we know this slice of the palette will fit into the wfcWave slice
					score++;
				}
			}
		}
		return score;
	}

	// This function operates the same as ScoreWaveSliceFit, but is optimized to fast-exit if it finds even one mismatch
	// Useful because the main algorithm relies heavily on looking for a complete fit, we can save lots of time by fast-exit here
	private int DoesWaveSliceFit(ReferenceView waveSlice, ReferenceView paletteSlice, Vector2I center) {
		Vector2I pos = new();
		for (int x = 0; x < waveSlice.size; x++) {
			for (int y = 0; y < waveSlice.size; y++) {
				pos.X = x - (convSize-1)/2;
				pos.Y = y - (convSize-1)/2;
				// waveSlice.GetMask() returns an indicator vector
				int mask = waveSlice.GetBitmask(pos);
				if (mask == 0) {
					// if the mask is 0 already, it's a contradiction in the wave
					// for the purposes of backprop, this means it fits all (pretend its not real and let self-healing handle it)
					continue;
				}
				// biomePalette.GetMask() returns a one-hot vector
				mask &= paletteSlice.GetBitmask(pos + center);
				if (mask == 0) {
					// If none of the active bits are shared between the wfcWave slice and biome palette masks,
					// we know this slice of the palette won't fit into the wfcWave slice
					return 0;
				}
			}
		}
		return 1;
	}
	
	HashSet<Vector2I> backpropFringe = new HashSet<Vector2I>();
	HashSet<Vector2I> backpropVisited = new HashSet<Vector2I>();

	private void PerformBackpropBounded(Vector2I collapse_pos, float radius) {
		backpropFringe.Clear();
		backpropVisited.Clear();

		int current_iteration = 0;
		// start by adding the spot we just collapsed to the fringe
		TryAddNeighborsToBackpropFringe(collapse_pos, collapse_pos, radius);
		// while fringe isnt empty and we haven't exceeded backprop horizon:
		while (backpropFringe.Count > 0 && backpropHorizon > current_iteration++) {
			if (wfcWave.GetLeastEntropyPosition(out Vector2I backprop_pos, backpropFringe.AsEnumerable(), convSize)) {
				// pop least entropy entry in fringe
				backpropFringe.Remove(backprop_pos);
				// add entry to Visited set
				backpropVisited.Add(backprop_pos);

				// pull full palette through the window around our backprop position
				//ReferenceView palette = wfcPalette.GetReferenceView();
				ReferenceView backprop_window = wfcWave.GetReferenceView(backprop_pos, convSize);
				BitmaskWindow new_window = new(convSize);

				int bounds = wfcPalette.Width - backprop_window.size; //TODO either make the palette not queare or change width to size
				Vector2I offset = new();
				bool foundFit = false;
				int n_rotations = rotationsEnabled ? 4 : 1;
				for (int i = 0; i < n_rotations; i++) {
					backprop_window.rotation = i;
					int inverse_rotation = (4-i)%4;
					for (int x = 0; x < bounds; x++) {
						for (int y = 0; y < bounds; y++) {
							offset.X = x + (convSize-1)/2;
							offset.Y = y + (convSize-1)/2;
							
							if (DoesWaveSliceFit(backprop_window, wfcPalette.GetReferenceView(), offset) == 1) {
								// construct combined but unweighted waveslice by OR-ing each convolution that fits
								new_window.AppendOR(wfcPalette.GetReferenceView(offset, new_window.size, inverse_rotation));
								foundFit = true;
							}
						}
					}
				}
				backprop_window.rotation = 0;
				
				if (!foundFit) {
					// if no areas of the biome palette fit into the waveSlice, we have reached a contradiction during backprop
					// TODO: decide what to do here. there are a few options
					// 		A: do nothing. continue performing backprop as if there's nothing wrong
					// will likely require the backprop algorithm to treat contradictions as completely open waves
					// or else contradictions will propagate
					// NOTE: this is the current approach. contradictions are treated as a completely open wave during backprop, then patched later
					//		B: early-halt backprop.
					// could help, but also might just kick the can down the road, as the area with contradictions is likely to have low entropy
					//		C: perform a patch here
					// instead of self-healing after the full output is generated, patch as we go
					// might improve things by giving the algorithm a new foothold to generate from
					// might cause issues by generating a tile that definitely doesn't exist in the input
					// could compromise by inserting a pattern rather than a specific tile
					// probably need to test both ways
				} else {
					// apply the resulting window to the wfcWave
					bool changedFlag = false;
					for (int x = 0; x < backprop_window.size; x++) {
						for (int y = 0; y < backprop_window.size; y++) {
							offset.X = x;
							offset.Y = y;
							int old_mask = backprop_window.GetBitmask(offset);
							int new_mask = new_window.Get(offset);
							backprop_window.SetBitmask(offset, old_mask & new_mask);
							changedFlag |= old_mask != new_mask;
						}
					}
					if (changedFlag) {
						// if anything changed, add unvisited neighbors to fringe
						// avoid straying too far geographically from the original collapse position
						if ((backprop_pos - collapse_pos).LengthSquared() < backpropMaxDistance * backpropMaxDistance) {
							TryAddNeighborsToBackpropFringe(backprop_pos, collapse_pos, radius);
						}
					}
				}

			} else {
				// we ran out of open tiles in the fringe somehow, just break
				break;
			}
			
		}
	}

	private void TryAddNeighborsToBackpropFringe(Vector2I pos, Vector2I center, float radius) {
		int conv_size = convSize;
		int conv_center = (conv_size-1)/2;
		Vector2I neighbor_pos = new();
		for (int x = -conv_center; x <= conv_size-conv_center; x++) {
			for (int y = -conv_center; y <= conv_size-conv_center; y++) {
				neighbor_pos.X = x;
				neighbor_pos.Y = y;
				neighbor_pos += pos;
				// first, the position must be within range of the original collapse pos (center)
				// then, the position must not have already been visited
				if (MathUtils.IsPointInCircle(neighbor_pos, center, radius) && !backpropVisited.Contains(neighbor_pos)) {
					// if both pass, add to the fringe
					backpropFringe.Add(neighbor_pos);
				}
			}
		}
	}

	private T GetWeightedRandomKey<T>(Dictionary<T, int> counter)
	{
		if (counter == null || counter.Count == 0)
			throw new InvalidOperationException("The dictionary is empty or null.");

		// Step 1: Calculate the total number of "tickets"
		int totalWeight = 0;
		foreach (var entry in counter)
		{
			if (entry.Value == 0) {
				throw new InvalidOperationException("Entry value is 0.");
			}
			totalWeight += entry.Value;
		}

		// Step 2: Generate a random number between 1 and totalWeight
		int randomTicket = random.Next(1, totalWeight + 1); // Random number in range [1, totalWeight]

		// Step 3: Traverse the dictionary to find the corresponding key
		int cumulativeWeight = 0;
		foreach (var entry in counter)
		{
			cumulativeWeight += entry.Value;
			if (randomTicket <= cumulativeWeight)
			{
				return entry.Key; // Return the key where the random ticket falls
			}
		}

		// In case something goes wrong (this should never happen if the logic is correct)
		throw new InvalidOperationException("Failed to select a key.");
	}

	private T GetHeaviestKey<T>(Dictionary<T, int> counter)
	{
		T heaviestKey = counter.GetEnumerator().Current.Key;
		int heaviestValue = -1;
		foreach (var entry in counter)
		{
			if (entry.Value > heaviestValue) {
				heaviestKey = entry.Key;
				heaviestValue = entry.Value;
			}
		}
		return heaviestKey;
	}

}




}

