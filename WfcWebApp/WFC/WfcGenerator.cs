
namespace WfcWebApp.Wfc
{


public class WfcGenerator
{
	public WfcPalette wfcPalette;
	public WfcWave wfcWave;
	private int backpropHorizon = -1; // Max number of backprop iterations before moving on
	private int backpropMaxDistance = -1; // backprop won't continue past this radius around each collapse position
	private int backpropEntropyThreshold = 30; //maximum number of unique patterns we're willing to analyze during backprop
	// setting this value to 10 would force the wave to stay fully uncollapsed at a position if there were 11 or more possible patterns there
	// tuning this properly can vastly speed up the algorithm without losing much accuracy
	
	

	public void SetDependencies(WfcWave wave, WfcPalette palette, int randomSeed = 0) {
		wfcWave = wave;
		wfcPalette = palette;
	}

	public bool RunSingle() {
		if (wfcWave.GetLeastEntropyPosition(out var collapse_pos)) {
			Console.WriteLine($"Selected position {collapse_pos} with entropy {wfcWave.GetEntropy(collapse_pos)} for collapse.");
			SingleGenerationCycleBounded(collapse_pos);
			return true;
		}
		Console.WriteLine($"Failed to select collapse position. Perhaps it's all collapsed?");
		return false;
	}

	RandomUtils.WeightedKeyCounter<Pattern> keyCounter = new();

	// Collapses the wfcWave at the specified collapse position
	// Performs up to backpropHorizon iterations of backprop around this position
	// This backprop extends up to <radius> distance away
	private void SingleGenerationCycleBounded(Vector2I collapse_pos) {
		if (wfcWave.IsUnobserved(collapse_pos)) {
			//if the tile is unobserved, we can short-circuit the random selection
			// by searching the trie for a ticket chosen randomly out of [0, cumulative weight]
			// TODO

			// ROUGH APPROXIMATION
			// just do a single unweighted monte carlo style rollout and return what you get
			Pattern chosenPattern = wfcPalette.GetRandomPattern();
			wfcWave.GetOrCreatePatternSet(collapse_pos, out SparsePatternSet waveSet);
			waveSet.Add(chosenPattern.Index);

			PerformBackpropBounded(collapse_pos);

		} else if (wfcWave.IsUncollapsed(collapse_pos)) { // this tile can still be collapsed
			keyCounter.Clear();
			foreach (int index in wfcWave.EnumeratePatternSet(collapse_pos)) {
				Pattern pattern = wfcPalette.PatternFromIndex(index);
				// each will have a weight in the trie
				// need to make a dict of pattern -> weight, populate it pattern by pattern, then call getweightedrandomkey
				keyCounter.AddWeightedKey(pattern, pattern.Weight);
			}
			Pattern chosenPattern = keyCounter.Sample();
			keyCounter.Clear();
			
			// empty the available patterns at the collapse pos, and add back in only the single pattern we chose
			wfcWave.GetOrCreatePatternSet(collapse_pos, out SparsePatternSet waveSet);
			waveSet.Clear(); // even if the waveSet was just created and is empty, this is still fine
			waveSet.Add(chosenPattern.Index);
			// waveSet is a reference to the actual hashset stored in the wfcWave object, so changing it here should be reflected in the wave


			TimerUtility.StartTimer("Full Backprop");
			// perform backprop within the specified radius of the collpase pos
			PerformBackpropBounded(collapse_pos);
			TimerUtility.StopTimer("Full Backprop");
			TimerUtility.PrintElapsed("Full Backprop");
		}
	}

	HashSet<Vector2I> backpropFringe = new HashSet<Vector2I>();
	HashSet<Vector2I> backpropVisited = new HashSet<Vector2I>();

	private void PerformBackpropBounded(Vector2I collapse_pos) {
		backpropFringe.Clear();
		backpropVisited.Clear();
		// if the max distance is -1, treat it as if the distance is effectively infinite
		Circle boundary = new Circle(collapse_pos, backpropMaxDistance == -1 ? int.MaxValue : backpropMaxDistance);
		int current_iteration = 0;

		
		// start by adding the spot we just collapsed to the fringe
		backpropFringe.Add(collapse_pos);
		backpropVisited.Add(collapse_pos);
		// while fringe isnt empty and we haven't exceeded backprop horizon:       // if it's -1, ignore the horizon
		while (backpropFringe.Count > 0 && (backpropHorizon > current_iteration++ || backpropHorizon < 0)) {
			// find the least entropy position frm all positions in the fringe, including collapsed ones
			if (wfcWave.GetLeastEntropyPosition(out Vector2I backprop_pos, backpropFringe, true)) {
				// pop least that position from the fringe
				backpropFringe.Remove(backprop_pos);

				TimerUtility.StartTimer("SingleBackpropStep");
				// perform backprop at that position
				SingleBackpropStep(backprop_pos, boundary);
				// based on the rules of backprop, the fringe may have been expanded
				TimerUtility.StopTimer("SingleBackpropStep");
				//TimerUtility.PrintElapsed("SingleBackpropStep");
			} else {
				// if we don't find a valid least entropy position
				// it means everything remaining in the fringe is a contradiction
				break;
			}
			
		}
		backpropFringe.Clear();
		backpropVisited.Clear();
		Console.WriteLine($"Backprop took {current_iteration-1} iterations!");
	}
	
	private void SingleBackpropStep(Vector2I backprop_pos, Circle boundary) {
		//Console.WriteLine($"Performing backprop at {backprop_pos} with entropy {wfcWave.GetEntropy(backprop_pos)}");
		// we just changed the content of the wave at backprop_pos
		// we need to perform backrop for all 4 neighbors, and add them to the fringe if they changed

		// for each adjacent neighbor to backprop_pos
		foreach (Vector2I neighbor_pos in Vector2I.Neighbors(backprop_pos)) {
			//Console.WriteLine($"backpropping at neighbor {neighbor_pos}");
			int direction = backprop_pos.DirectionTo(neighbor_pos);

			SparsePatternSet matchPatternSet = new();
			//for each pattern that fits at backprop_pos
			foreach (int index in wfcWave.EnumeratePatternSet(backprop_pos)) {
				Pattern backpropPattern = wfcPalette.PatternFromIndex(index);
				// get all patterns that fit at neighbor_pos assuming we apply backpropPattern at backprop_pos
				// aggregate them into the union set
				//Console.WriteLine($"Using index {index} to match pattern: \n{backpropPattern} in direction {direction}");
				//unionPatternSet.UnionWith(wfcPalette.EnumerateMatchingPatterns(backpropPattern, direction));
				foreach (Pattern match in wfcPalette.EnumerateMatchingPatterns(backpropPattern, direction)) {
					matchPatternSet.Add(match.Index);
				}

				if (matchPatternSet.Count > backpropEntropyThreshold) {
					// Optimization:
					// these pattern sets we're storing are sets of patterns that fit at each position in the output
					// this implies that the more patterns we store, the more options we have at that spot
					// more options = less likely to lead to a contradiction, and avoiding those is our primary goal
					// this means it's FAR more important to store pattern sets when they have few entries (low entropy)
					// fortunately for us, fewer entries also means less memory overhead and FAR less iteration time during backprop
					// thus, the optimization. define a maximum number of patterns we're willing to store for a position
					// if our unionPatternSet is too large, we simply discard it and move on without adding more to the fringe
					// we can assume further backprop from here will just lead into increasingly uncollapsed territory
					//Console.WriteLine($"Fast-exiting backprop at position {backprop_pos}, dir {direction}");
					return; //return to the main backprop loop to move to the next iteration
				}
			}

			TimerUtility.StartTimer("end section");
			bool changed_flag = false;
			// check the set of patterns that fit at that neighbor already
			if (!wfcWave.GetOrCreatePatternSet(neighbor_pos, out var wavePatternSet)) {
				// if this fails, it's because the neihbor_pos is out of bounds
				// this means we continue to the next neighbor_pos
				continue;
			}
			if (!wavePatternSet.IsUnobserved) {
				// there's already a set of patterns here, intersect it with the union set we just constructed
				int original_count = wavePatternSet.Count;
				wavePatternSet.IntersectWith(matchPatternSet);
				if (wavePatternSet.Count != original_count) {
					changed_flag = true;
				}
			} else {
				// if this set is newly created, just fill it with the values from the union set
				// this operation represents restricting the amount of patterns at this spot from all of them down to just this set
				wavePatternSet.UnionWith(matchPatternSet);
				changed_flag = true;
			}

			if (wavePatternSet.Count == 0) {
				// So, here is the meat of the problem: Contradiction!
				// whether the union set itself was empty, or if intersecting it with the wave set emptied it,
				// we're in the same case here
				// for now, ignore it and just don't add it to the fringe
			} else if (changed_flag) {
				//if anything changed, add this neighbor to the fringe to perform backprop on it!
				TryAddToBackpropFringe(neighbor_pos, boundary);
			}
			TimerUtility.StopTimer("end section");
			//TimerUtility.PrintElapsed("end section");
		}
	}

	private void TryAddToBackpropFringe(Vector2I neighbor_pos, Circle boundary) {
		// first, the position must be within range of the original collapse pos (center)
		// then, the position must not have already been visited
		if (MathUtils.IsPointInCircle(neighbor_pos, boundary)
			&& !backpropVisited.Contains(neighbor_pos)) {

				//TODO wrap the position if the output is in wrap mode, otherwise skip it if out's out of bounds
			// if all conditions pass, add to the fringe
			backpropFringe.Add(neighbor_pos);
			// add it to visited as well, so we don't accidentally add this position to the fringe again before it gets processed
			backpropVisited.Add(neighbor_pos);
		}
	}

}
}




