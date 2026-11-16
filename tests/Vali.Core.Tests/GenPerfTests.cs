using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

[Collection("StaticState")]
public class GenPerfTests
{
    [Fact]
    public void BuildGcLine_formats_configuration_collections_and_pause_share()
    {
        var configuration = new Dictionary<string, object>
        {
            ["ServerGC"] = true,
            ["HeapCount"] = 32L,
            ["GCDynamicAdaptationMode"] = 1L,
        };

        var line = GenPerf.BuildGcLine(configuration, gen0: 412, gen1: 37, gen2: 5, pauseSeconds: 12.3, wallSeconds: 72.0);

        line.ShouldContain("Server, 32 heaps, DATAS on");
        line.ShouldContain("412 gen0 / 37 gen1 / 5 gen2");
        line.ShouldContain("paused 12.30 s (17% of wall)");
    }

    [Fact]
    public void BuildGcLine_describes_workstation_without_datas()
    {
        var configuration = new Dictionary<string, object>
        {
            ["ServerGC"] = false,
            ["HeapCount"] = 1L,
            ["GCDynamicAdaptationMode"] = 0L,
        };

        GenPerf.BuildGcLine(configuration, gen0: 0, gen1: 0, gen2: 0, pauseSeconds: 0, wallSeconds: 0)
            .ShouldContain("Workstation, 1 heap, DATAS off");
    }

    [Fact]
    public void AddCountryWork_accumulates_per_country()
    {
        GenPerf.Reset();
        GenPerf.AddCountryWork("US", TimeSpan.FromSeconds(1));
        GenPerf.AddCountryWork("US", TimeSpan.FromSeconds(2));
        GenPerf.AddCountryWork("FR", TimeSpan.FromSeconds(5));

        var work = GenPerf.CountryWork().ToDictionary(x => x.cc, x => x.seconds);
        work["US"].ShouldBe(3.0, 0.0001);
        work["FR"].ShouldBe(5.0, 0.0001);
    }

    [Fact]
    public void BuildReport_formats_phases_shares_parallelism_and_memory()
    {
        // deserialize..store/projection; sum = 54.00 s
        var phaseSeconds = new[] { 40.0, 8.0, 1.6, 3.4, 0.2, 0.8 };
        var countryWalls = new (string cc, double seconds)[] { ("NZ", 1.4), ("KE", 2.1) };

        var report = GenPerf.BuildReport(
            phaseSeconds,
            countryWalls,
            wallSeconds: 5.4,
            processorCount: 32,
            peakWorkingSetBytes: 2L * 1024 * 1024 * 1024,
            allocatedBytes: 6L * 1024 * 1024 * 1024);

        report.ShouldContain("deserialize");
        report.ShouldContain("40.00 s");
        report.ShouldContain("74%");        // 40 / 54
        report.ShouldContain("CPU summed");
        report.ShouldContain("54.00 s");
        report.ShouldContain("10.0x");      // 54 / 5.4
        report.ShouldContain("of 32 cores");
        report.ShouldContain("2.00 GB");
        report.ShouldContain("6.00 GB");
        report.ShouldContain("KE 2.10s");
        report.ShouldContain("NZ 1.40s");
        // Per-country ordered by descending wall: KE (2.10) before NZ (1.40)
        report.IndexOf("KE", StringComparison.Ordinal)
            .ShouldBeLessThan(report.IndexOf("NZ", StringComparison.Ordinal));
    }

    [Fact]
    public void Measure_accumulates_time_when_enabled()
    {
        GenPerf.Reset();
        GenPerf.Enabled = true;
        try
        {
            using (GenPerf.Measure(GenPerf.Phase.Filter))
            {
                Thread.Sleep(5);
            }

            GenPerf.PhaseSeconds(GenPerf.Phase.Filter).ShouldBeGreaterThan(0);
        }
        finally
        {
            GenPerf.Enabled = false;
            GenPerf.Reset();
        }
    }

    [Fact]
    public void Measure_is_a_no_op_when_disabled()
    {
        GenPerf.Reset();
        GenPerf.Enabled = false;

        using (GenPerf.Measure(GenPerf.Phase.Filter))
        {
            Thread.Sleep(5);
        }

        GenPerf.PhaseSeconds(GenPerf.Phase.Filter).ShouldBe(0);
    }

    [Fact]
    public void Reset_clears_phase_ticks_and_country_work()
    {
        GenPerf.Enabled = true;
        try
        {
            using (GenPerf.Measure(GenPerf.Phase.Distribute))
            {
                Thread.Sleep(5);
            }

            GenPerf.AddCountryWork("KE", TimeSpan.FromSeconds(2));

            GenPerf.Reset();

            GenPerf.PhaseSeconds(GenPerf.Phase.Distribute).ShouldBe(0);
            GenPerf.CountryWork().ShouldBeEmpty();
        }
        finally
        {
            GenPerf.Enabled = false;
            GenPerf.Reset();
        }
    }
    [Fact]
    public void Report_prints_neighbor_work_line()
    {
        GenPerf.Reset();
        GenPerf.Enabled = true;
        try
        {
            Parallel.For(0, 4, new ParallelOptions { MaxDegreeOfParallelism = 4 }, _ =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    GenPerf.AddNeighborEvaluation(5, 10);
                }
            });

            GenPerf.NeighborWork().ShouldBe((4000L, 20000L));
            var report = GenPerf.BuildReport(new double[8], [], wallSeconds: 1, processorCount: 32, peakWorkingSetBytes: 0, allocatedBytes: 0);
            report.ShouldContain("Neighbor work:");
            report.ShouldContain("4,000 evaluations");
            report.ShouldContain("20,000 neighbor visits");
        }
        finally
        {
            GenPerf.Enabled = false;
            GenPerf.Reset();
        }
    }

    [Fact]
    public void Neighbor_phases_are_labelled_as_nested_and_left_out_of_the_cpu_sum()
    {
        // deserialize..store/projection = 10 s; neighbor grid 1 s and neighbor eval 3 s run inside filter/distribute.
        var phaseSeconds = new[] { 4.0, 0.0, 0.0, 3.0, 3.0, 0.0, 1.0, 3.0 };

        var report = GenPerf.BuildReport(phaseSeconds, [], wallSeconds: 1, processorCount: 32, peakWorkingSetBytes: 0, allocatedBytes: 0);

        report.ShouldContain("neighbor grid");
        report.ShouldContain("neighbor eval");
        report.ShouldContain("(inside filter/distribute)");
        report.ShouldContain("10.00 s");
    }
}
