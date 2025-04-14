using WfcWebApp.Utils;

namespace WfcWebApp.Wfc
{
public class Generator
{
    private BoundedWave Wave = new BoundedWave();
    private Palette Palette = new();
    private int backpropHorizon = -1; // Max number of backprop iterations before moving on
    private int backpropMaxDistance = -1; // backprop won't continue past this radius around each collapse position
    private int backpropEntropyThreshold = 30; //maximum number of unique patterns we're willing to analyze during backprop
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
    }

    // Super EZ wrapper that just returns back the input result,
    // but also assigns it to PreviousStepResult first
    private StepResult SelectStepResult(StepResult result) {
        PreviousStepResult = result;
        return result;
    }

    private bool _initialized = false;
    public void Initialize(int waveWidth, int waveHeight, bool paletteWrap, bool paletteRotate, IndexedImage paletteImage) {
        Wave.Clear();
        Wave.Resize(waveWidth, waveHeight);

        Palette.SetImage(paletteImage);
        Palette.SetParams(3, paletteWrap, paletteRotate); //TODO add a slider for conv size
        Palette.Preprocess();
        _initialized = true;
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
                    CollapseWaveAt(lx, ly);
                    return SelectStepResult(StepResult.CollapseStep);
                }
                // if we don't find any position in the wave we can still collapse, we are done!
                return SelectStepResult(StepResult.Completed);
            

            case StepResult.CollapseStep:
            case StepResult.BackpropStep:
                // After a collapse step or a backprop step, we must continue backprop
                
                // return SelectStepResult(StepResult.BackpropStep) // if backprop isn't completed yet
                return SelectStepResult(StepResult.BackpropConvergence);
            
            
            case StepResult.Completed:
                // This shouldn't ever be reached, we shouldn't be calling Next() again once it returns Completed
                throw new InvalidOperationException("Generation process already complete!");
        }
        throw new InvalidOperationException("Unknown stepResult");
    }
    
    private RandomUtils.WeightedKeyCounter<(int, int)> KeyCounter = new();
    private List<(int, int)> PositionCandidates = new();
    private HashSet<(int, int)> BackpropFringe = new();
    private HashSet<(int, int)> BackpropVisited = new();

    private void CollapseWaveAt(int x, int y) {
        
    }

    // Gets the LEP from the wave based on the palette's pattern indexer
    private bool GetLeastEntropyPosition(out int minX, out int minY,
    bool includeCollapsed = false, IEnumerable<(int x, int y)>? positionFilter = null)
    {
        minX = minY = 0;
        int minEntropy = int.MaxValue;
        KeyCounter.Clear();
        PositionCandidates.Clear();

        if (positionFilter == null) {
            positionFilter = AllGridPositions(Wave.Width, Wave.Height);
        }

        foreach ((int x, int y) in positionFilter) {
            foreach (int patternIndex in Wave.EnumeratePatternsAtPosition(x, y)) {
                PatternView pattern = Palette.GetPatternFromIndex(patternIndex);
                int entropy = pattern.TotalWeight; //TODO if generator is set to ignore rotation, do pattern.weights[0] instead
                if ((entropy > 1 || (includeCollapsed && entropy == 1)) && entropy <= minEntropy) {
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

    private IEnumerable<(int, int)> AllGridPositions(int w, int h) {
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                yield return (x, y);
            }
        }
    }
}


}