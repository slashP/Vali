using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class GenerationConcurrencyTests
{
    [Theory]
    [InlineData(32, 1, 32)]   // a single work item gets the whole machine
    [InlineData(32, 2, 16)]
    [InlineData(32, 5, 6)]    // even split, rounded down
    [InlineData(32, 32, 1)]   // fully fanned-out outer pass ⇒ serial inner scan
    [InlineData(32, 64, 1)]   // more in flight than cores ⇒ still at least 1
    [InlineData(8, 3, 2)]
    [InlineData(1, 1, 1)]
    [InlineData(32, 0, 32)]   // defensive: never divide by zero
    public void InnerDegreeOfParallelism_splits_cores_across_in_flight_items(int processorCount, int inFlight, int expected)
    {
        GenerationConcurrency.InnerDegreeOfParallelism(processorCount, inFlight).ShouldBe(expected);
    }

    [Fact]
    public void InnerDegreeOfParallelism_is_always_at_least_one()
    {
        for (var inFlight = 0; inFlight <= 200; inFlight++)
        {
            GenerationConcurrency.InnerDegreeOfParallelism(16, inFlight).ShouldBeGreaterThanOrEqualTo(1);
        }
    }

    [Fact]
    public void EnterWorkItem_tracks_in_flight_count_and_releases_on_dispose()
    {
        var before = GenerationConcurrency.InFlight;
        using (GenerationConcurrency.EnterWorkItem())
        {
            GenerationConcurrency.InFlight.ShouldBe(before + 1);
            using (GenerationConcurrency.EnterWorkItem())
            {
                GenerationConcurrency.InFlight.ShouldBe(before + 2);
            }

            GenerationConcurrency.InFlight.ShouldBe(before + 1);
        }

        GenerationConcurrency.InFlight.ShouldBe(before);
    }
}
