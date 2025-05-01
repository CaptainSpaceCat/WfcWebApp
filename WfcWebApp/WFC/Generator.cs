using System.Collections.Specialized;
using WfcWebApp.Utils;

namespace WfcWebApp.Wfc
{
public class Generator
{
    public BoundedWave Wave = new BoundedWave();
    public Palette Palette = new();
    private int backpropHorizon = -1; // Max number of backprop iterations before moving on
    //private int backpropMaxDistance = -1; // backprop won't continue past this radius around each collapse position
    private int backpropSizeThreshold = 50; //maximum number of unique patterns we're willing to analyze during backprop
    // setting this value to 10 would force the wave to stay fully uncollapsed at a position if there were 11 or more possible patterns there
    // tuning this properly can vastly speed up the algorithm without losing much accuracy

    public enum StepResult
    {
        Initialize, //algorithm has yet to do anything
        CollapseStep, //a position in the wave was just collapsed
        BackpropStep, //one step of backprop was just performed
        BackpropConvergence, //backprop has reached convergence
        Completed, //generation is fully complete
        Error //an error was encountered
    }

    private StepResult PreviousStepResult = StepResult.Initialize;

    public void Reset() {
        PreviousStepResult = StepResult.Initialize;
        EntropyTracker.Clear();
        _initialized = false;
    }

    // Super EZ wrapper that just returns back the input result,
    // but also assigns it to PreviousStepResult first
    private StepResult SelectStepResult(StepResult result) {
        //Console.WriteLine($"Selected step result {result}");
        PreviousStepResult = result;
        return result;
    }

    private bool _initialized = false;
    public void Initialize(WfcConfig config, IndexedImage paletteImage) {
        Wave.Clear();
        Wave.Resize(config.OutputWidth, config.OutputHeight);

        Palette.SetImage(paletteImage);
        Palette.SetParams(3, config.Wrap, config.RotationalSymmetry); //TODO add a slider for conv size
        Palette.Preprocess();
        _initialized = true;

        Console.WriteLine($"Palette contains {Palette.PatternCount} unique patterns");
    }

    public StepResult Next() {
        switch (PreviousStepResult)
        {
            case StepResult.Initialize:
                if (!_initialized) {
                    throw new Exception("Generator called before initialization.");
                }
                // randomly collapse first pattern
                CollapseWaveAt(RandomUtils.Random.Next(Wave.Width), RandomUtils.Random.Next(Wave.Height));
                return SelectStepResult(StepResult.BackpropStep);

            case StepResult.BackpropConvergence:
                // Backprop just converged (or wave was just initialized)
                // Find least entropy position in wave, and collapse it
                if (EntropyTracker.TryGetGlobalLEP(out (int X, int Y) pos)) {
                    // collapse the wave at this position
                    Console.WriteLine($"Collapsing wave at {pos}");
                    CollapseWaveAt(pos.X, pos.Y);
                    
                    return SelectStepResult(StepResult.CollapseStep);
                }
                // if we don't find any position in the wave we can still collapse, we are done!
                return SelectStepResult(StepResult.Completed);
            

            case StepResult.CollapseStep:
            case StepResult.BackpropStep:
                // After a collapse step or a backprop step, we must continue backprop
                
                bool continue_backprop = SingleBackpropStep();
                if (!continue_backprop) {
                    // backprop has converged
                    EntropyTracker.ClearFringe();
                    current_iteration = 0;
                    return SelectStepResult(StepResult.BackpropConvergence);
                }

                // backprop isn't completed yet
                return SelectStepResult(StepResult.BackpropStep);
            
            
            case StepResult.Completed:
                // This shouldn't ever be reached, we shouldn't be calling Next() again once it returns Completed
                throw new InvalidOperationException("Generation process already complete!");
        }
        throw new InvalidOperationException("Unknown stepResult");
    }
    
    private RandomUtils.WeightedKeyCounter<(int, int)> PositionKeyCounter = new();
    private RandomUtils.WeightedKeyCounter<int> IndexKeyCounter = new();
    private List<(int, int)> PositionCandidates = new();
    private EntropyTracker EntropyTracker = new();

    private void CollapseWaveAt(int x, int y) {
        IndexKeyCounter.Clear();
        //ask the wave to wrap these coords, then use them here as keys to the fringe
        (x, y) = Wave.WrapPosition(x, y);
        if (Wave.IsUnobserved(x, y)) {
            // choose randomly from all patterns
            foreach (var (patternIndex, weight) in Palette.AllUniqueWeightedPatterns()) {
                IndexKeyCounter.AddWeightedKey(patternIndex, weight);
            }
        } else if (Wave.IsUncollapsed(x, y)) {
            foreach (int patternIndex in Wave.AllPatternsAtPosition(x, y)) {
                IndexKeyCounter.AddWeightedKey(patternIndex, Palette.GetWeightFromIndex(patternIndex));
            }
        } else {
            throw new InvalidOperationException(
                $"Failed to collapse wave at {x}, {y} with entropy {GetEntropy(x, y)} and count {Wave.AccessPatternSet(x, y).Count}");
        }
        
        int choiceIndex = IndexKeyCounter.Sample();
        //Console.WriteLine(Palette.GetPatternFromIndex(choiceIndex));
        Wave.CollapseWave(x, y, choiceIndex);

        EntropyTracker.AddOrUpdate((x, y), GetEntropy(x, y), 1);
        EntropyTracker.AddToFringe((x, y));

    }

    private struct EntropyMarker {
        public int X, Y;
        public int Entropy;

        public EntropyMarker() {
            Reset();
        }

        public void Reset() {
            X = Y = -1;
            Entropy = int.MaxValue;
        }

        public bool Ready => X >= 0 && Y >= 0;
    }

    private int RecalculateEntropy(SparsePatternSet patternSet) {
        int entropy = 0;
        foreach (int patternIndex in patternSet) {
            entropy += Palette.GetWeightFromIndex(patternIndex);
        }
        return entropy;
    }

    public int GetEntropy(int x, int y) {
        var pos = Wave.WrapPosition(x, y);
        if (EntropyTracker.TryGetEntropy(pos, out int cachedEntropy)) {
            return cachedEntropy;
        }
        
        SparsePatternSet patternSet = Wave.AccessPatternSet(x, y);
        int entropy;

        if (patternSet.IsUnobserved) {
            entropy = int.MaxValue;
        } else if (patternSet.IsContradiction) {
            entropy = 0;
        } else {
            entropy = RecalculateEntropy(patternSet);
        }

        // Count is the number of valid patterns left (used for collapsed check)
        int count = patternSet.Count;

        // Store in tracker
        EntropyTracker.AddOrUpdate(pos, entropy, count);

        return entropy;
    }

    
    private static readonly (int dx, int dy)[] DirectionOffsets = {
        (0, -1), (1, 0), (0, 1), (-1, 0)
    };

    private SparsePatternSet patternsThatMatch = new();
    private int current_iteration = 0;
    // Performs a single step of backprop
    // Returns true if backprop should continue
    // Returns false if convergence was reached or horizon was exceeded
    private bool SingleBackpropStep() {
        //Console.WriteLine($"Performing backprop iteration {current_iteration} with fringe of size {BackpropFringe.Count}");
		if (EntropyTracker.FringeCount > 0 &&
        (backpropHorizon > current_iteration++ || backpropHorizon < 0)) { // if it's -1, ignore the horizon
            if (EntropyTracker.TryGetFringeLEP(out (int, int) pos)) {
                using var watch = new ScopedStopwatch();
                (int lx, int ly) = pos;
                EntropyTracker.RemoveFromFringe(pos);

                // for each cardinal direction
                for (int r = 0; r < 4; r++) {
                    // Calculate neighbor offset position
                    (int nx, int ny) = Wave.WrapPosition(lx + DirectionOffsets[r].dx, ly + DirectionOffsets[r].dy);

                    patternsThatMatch.Clear();

                    using (var watch2 = new ScopedStopwatch("pattern matching")) {
                        // For each pattern that fits at this position in the wave
                        foreach (int patternIndex in Wave.AllPatternsAtPosition(lx, ly)) {
                            // for each pattern that overlaps (lx, ly) in direction r
                            //Console.WriteLine($"Direction {r}, Template pattern: {Palette.GetPatternFromIndex(patternIndex)}");
                            patternsThatMatch.UnionWith(Palette.MatchingPatternSet(patternIndex, r));
                        }

                        // catch the edge case where there are no patterns that match
                        // force the patternSet to be observed even if it's still empty
                        patternsThatMatch.Observe(); 
                    }

                    // skip this next step for anywhere that:
                    // allows all patterns (no new info gained)
                    // allows too many patterns for the threshold (too long to compute)
                    int n_matches = patternsThatMatch.Count;
                    if ((n_matches < Palette.PatternCount)
                        && (backpropSizeThreshold == -1 || n_matches <= backpropSizeThreshold)) {

                        // intersect the wave at the neighbor offset pos with the patterns that fit
                        using (var watch3 = new ScopedStopwatch("intersection")) {
                            SparsePatternSet neighborPatternSet = Wave.AccessPatternSet(nx, ny);
                            int prev_entropy = GetEntropy(nx, ny);
                            if (neighborPatternSet.IntersectWith(patternsThatMatch)) {
                                //if something changed during intersection, recompute entropy
                                int new_entropy = RecalculateEntropy(neighborPatternSet);
                                int count = neighborPatternSet.Count;
                                EntropyTracker.AddOrUpdate((nx, ny), new_entropy, count);
                                // and add to fringe
                                if (new_entropy > 0)
                                    EntropyTracker.AddToFringe((nx, ny));
                            }
                        }
                    }
                }
                return true;
            }
            // if we fail to find LEP, it means nothing still in the fringe is able to be collapsed
            return false;
        }
        // if fringe is empty or we exceeded backprop horizon, end backprop
        return false;
    }

    private IEnumerable<(int, int)> AllGridPositions(int w, int h) {
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                yield return (x, y);
            }
        }
    }
}


}