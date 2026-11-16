using Xunit;

namespace Vali.Core.Tests;

// Tests that set static switches or read static counters (GenPerf, DistanceLog, NeighborEvaluation,
// GenerationDeterminism) run one at a time, never beside each other.
[CollectionDefinition("StaticState", DisableParallelization = true)]
public class StaticStateCollection;
