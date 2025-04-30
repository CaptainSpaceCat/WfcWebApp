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
                return SelectStepResult(StepResult.BackpropConvergence);

            case StepResult.BackpropConvergence:
                // Backprop just converged (or wave was just initialized)
                // Find least entropy position in wave, and collapse it
                if (GetLeastEntropyPosition(out int lx, out int ly)) {
                    // collapse the wave at this position
                    Console.WriteLine($"Collapsing wave at {(lx, ly)}");
                    CollapseWaveAt(lx, ly);
                    
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
                    BackpropFringe.Clear();
                    BackpropVisited.Clear();
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
    private HashSet<(int, int)> BackpropFringe = new();
    private HashSet<(int, int)> BackpropVisited = new();

    private void CollapseWaveAt(int x, int y) {
        using var watch = new ScopedStopwatch();

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
            throw new InvalidOperationException($"Failed to collapse wave at {x}, {y} with entropy {GetEntropy(x, y)}");
        }
        
        int choiceIndex = IndexKeyCounter.Sample();
        //Console.WriteLine(Palette.GetPatternFromIndex(choiceIndex));
        Wave.CollapseWave(x, y, choiceIndex);
        BackpropFringe.Add((x, y));
        minEntropyMarker.Reset();
        minUncollapsedEntropyMarker.Reset();
        EntropyCache.Remove((x, y));
    }

    // Gets the LEP from the wave based on the palette's pattern indexer
    private bool GetLeastEntropyPosition(out int minX, out int minY,
    bool includeCollapsed = false, IEnumerable<(int x, int y)>? positionFilter = null)
    {
        using var watch = new ScopedStopwatch();

        if (includeCollapsed) {
            if (minEntropyMarker.Ready) {
                minX = minEntropyMarker.X;
                minY = minEntropyMarker.Y;
                return true;
            }
        } else if (minUncollapsedEntropyMarker.Ready) {
            minX = minUncollapsedEntropyMarker.X;
            minY = minUncollapsedEntropyMarker.Y;
            return true;
        }
            
        minX = minY = 0;
        int minEntropy = int.MaxValue;
        PositionKeyCounter.Clear();
        PositionCandidates.Clear();

        if (positionFilter == null) {
            positionFilter = AllGridPositions(Wave.Width, Wave.Height);
        }

        foreach ((int x, int y) in positionFilter) {
            SparsePatternSet patternSet = Wave.AccessPatternSet(x, y);
            if (!patternSet.IsContradiction && (!patternSet.IsCollapsed || includeCollapsed)) {
                // we need to calculate entropy
                int entropy = GetEntropy(x, y);
                if (entropy <= minEntropy) {
                    if (entropy < minEntropy) {
                        minEntropy = entropy;
                        PositionCandidates.Clear();
                    }
                    PositionCandidates.Add((x, y));
                }
            }
        }
        
        if (PositionCandidates.Count > 0) {
            (minX, minY) = RandomUtils.Choice(PositionCandidates);
            return true;
        }
        return false;

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

    private EntropyMarker minUncollapsedEntropyMarker = new();
    private EntropyMarker minEntropyMarker = new();

    private Dictionary<(int, int), int> EntropyCache = new();
    public int GetEntropy(int x, int y) {
        if (EntropyCache.TryGetValue((x, y), out int entropy)) {
            return entropy;
        }
        
        SparsePatternSet patternSet = Wave.AccessPatternSet(x, y);
        if (patternSet.IsUnobserved) {
            return int.MaxValue;
        } else if (patternSet.IsContradiction) {
            return 0;
        }

        // this is the slowdown we want to avoid whenever possible
        entropy = 0;
        foreach (int patternIndex in patternSet) {
            entropy += Palette.GetWeightFromIndex(patternIndex);
        }
        EntropyCache[(x, y)] = entropy;
        if (entropy > 0) {
            if (entropy < minEntropyMarker.Entropy) {
                minEntropyMarker.X = x;
                minEntropyMarker.Y = y;
                minEntropyMarker.Entropy = entropy;
            }
            if (!patternSet.IsCollapsed && entropy < minUncollapsedEntropyMarker.Entropy) {
                minUncollapsedEntropyMarker.X = x;
                minUncollapsedEntropyMarker.Y = y;
                minUncollapsedEntropyMarker.Entropy = entropy;
            }
        }
        return entropy;
    }

    
    private int current_iteration = 0;
    // Performs a single step of backprop
    // Returns true if backprop should continue
    // Returns false if convergence was reached or horizon was exceeded
    private bool SingleBackpropStep() {
        

        //Console.WriteLine($"Performing backprop iteration {current_iteration} with fringe of size {BackpropFringe.Count}");
		                                                                    // if it's -1, ignore the horizon
		if (BackpropFringe.Count > 0 && (backpropHorizon > current_iteration++ || backpropHorizon < 0)) {
            if (GetLeastEntropyPosition(out int lx, out int ly, true, BackpropFringe)) {
                using var watch = new ScopedStopwatch();
                BackpropFringe.Remove((lx, ly));

                // for each cardinal direction
                for (int r = 0; r < 4; r++) {
                    // Calculate neighbor offset position
                    (int nx, int ny) = r switch {
                        1 => (lx + 1, ly),
                        2 => (lx, ly + 1),
                        3 => (lx - 1, ly),
                        _ => (lx, ly - 1),
                    };

                    SparsePatternSet patternsThatMatch = new();

                    using (var watch2 = new ScopedStopwatch("pattern matching")) {
                        // For each pattern that fits at this position in the wave
                        foreach (int patternIndex in Wave.AllPatternsAtPosition(lx, ly)) {
                            // for each pattern that overlaps (lx, ly) in direction r
                            //Console.WriteLine($"Direction {r}, Template pattern: {Palette.GetPatternFromIndex(patternIndex)}");
                            SparsePatternSet subset = Palette.MatchingPatternSet(patternIndex, r);
                            patternsThatMatch.UnionWith(subset);
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
                        
                        //SparsePatternSet temp = Wave.AccessPatternSet(nx, ny).Copy();
                        int prev_entropy = 0, new_entropy = 0;
                        using (var watch3 = new ScopedStopwatch("intersection")) {
                            prev_entropy = GetEntropy(nx, ny);
                            Wave.AccessPatternSet(nx, ny).IntersectWith(patternsThatMatch);
                            EntropyCache.Remove((nx, ny));
                            new_entropy = GetEntropy(nx, ny);
                            minEntropyMarker.Reset();
                            minUncollapsedEntropyMarker.Reset();
                        }

                        if (new_entropy != prev_entropy) {
                            if (new_entropy == 0) {
                                //TODO self-healing
                            } else {
                                // if something changed and we're not at a contradiction, continue propagation
                                BackpropFringe.Add((nx, ny));
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