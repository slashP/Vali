using Shouldly;
using Xunit;

namespace Vali.Core.Tests;

public class GenPerfTests
{
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
}
