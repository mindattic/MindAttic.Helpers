using System.Text.RegularExpressions;
using MindAttic.Helpers.Ascii;
using NUnit.Framework;

namespace MindAttic.Helpers.Tests;

[TestFixture]
public class AsciiCandleChartTests
{
    private static readonly DateTime Thu = new(2026, 10, 29);
    private static readonly DateTime Fri = new(2026, 10, 30);

    private static List<ChartSession> TwoDays() =>
    [
        .. new[] { Thu, Fri }.SelectMany(d => new[]
        {
            new ChartSession("PRE", d.AddHours(4), d.AddHours(9.5), d.ToString("ddd M/d")),
            new ChartSession("REGULAR", d.AddHours(9.5), d.AddHours(16), d.ToString("ddd M/d")),
            new ChartSession("AFTER", d.AddHours(16), d.AddHours(20), d.ToString("ddd M/d")),
        }),
    ];

    private static List<OhlcBar> Walk(IEnumerable<ChartSession> sessions, int seed = 7)
    {
        var rnd = new Random(seed);
        var bars = new List<OhlcBar>();
        double p = 20;
        foreach (var s in sessions)
        {
            for (var t = s.Start; t < s.End; t = t.AddMinutes(1))
            {
                double o = p;
                p += (rnd.NextDouble() - 0.5) * 0.06;
                bars.Add(new OhlcBar(t, o, Math.Max(o, p) + 0.01, Math.Min(o, p) - 0.01, p, 1000 + rnd.Next(5000)));
            }
        }
        return bars;
    }

    private static bool IsBraille(char c) => c is >= '⠀' and <= '⣿';

    [TestCase(60, CandleChartMode.Braille)]
    [TestCase(90, CandleChartMode.Braille)]
    [TestCase(118, CandleChartMode.Braille)]
    [TestCase(160, CandleChartMode.Braille)]
    [TestCase(60, CandleChartMode.Candles)]
    [TestCase(118, CandleChartMode.Candles)]
    public void EveryLineIsExactlyTheRequestedWidth(int width, CandleChartMode mode)
    {
        var sessions = TwoDays();
        string chart = AsciiCandleChart.Render(Walk(sessions), sessions, new CandleChartOptions
        {
            Title = "TEST",
            Mode = mode,
            Width = width,
            ReferenceLines = [new ChartReferenceLine("Thu close", 20.1)],
        });

        Assert.That(chart.Split('\n').Select(l => l.Length).Distinct(), Is.EqualTo(new[] { width }));
    }

    [Test]
    public void CandlesModeIsPureAsciiAndDeterministic()
    {
        var sessions = TwoDays();
        var bars = Walk(sessions);
        var candles = new CandleChartOptions { Mode = CandleChartMode.Candles };
        string a = AsciiCandleChart.Render(bars, sessions, candles);
        string b = AsciiCandleChart.Render(bars.AsEnumerable().Reverse().ToList(), sessions, candles);

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.All(c => c is '\n' or (>= ' ' and <= '~')), Is.True);
    }

    [Test]
    public void BrailleModeUsesOnlyBrailleBlocksAndAscii()
    {
        var sessions = TwoDays();
        string chart = AsciiCandleChart.Render(Walk(sessions), sessions);

        Assert.That(chart.Count(c => c > '⠀' && IsBraille(c)), Is.GreaterThan(100));
        Assert.That(chart.All(c => c is '\n' or (>= ' ' and <= '~') or (>= '▁' and <= '█') || IsBraille(c)), Is.True);
    }

    [Test]
    public void BrailleModeHasFourTimesTheVerticalResolution()
    {
        // A bar whose range is a quarter of the axis fills about a quarter of the dot rows.
        var session = new ChartSession("S", Thu.AddHours(10), Thu.AddHours(11));
        var bars = new List<OhlcBar> { new(Thu.AddHours(10), 10, 10.5, 10, 10.5), new(Thu.AddHours(10.5), 10, 12, 10, 10) };
        string chart = AsciiCandleChart.Render(bars, [session], new CandleChartOptions
        {
            BucketSize = TimeSpan.FromMinutes(30), Height = 4, VolumeHeight = 0, MarkLast = false, Width = 30,
        });
        var cells = chart.Split('\n').Take(4).Select(l => l[l.IndexOf('|') + 2]).ToArray();
        int leftDots = cells.Sum(c => IsBraille(c) ? new[] { 0x01, 0x02, 0x04, 0x40 }.Count(bit => ((c - 0x2800) & bit) != 0) : 0);
        int rightDots = cells.Sum(c => IsBraille(c) ? new[] { 0x08, 0x10, 0x20, 0x80 }.Count(bit => ((c - 0x2800) & bit) != 0) : 0);

        Assert.That(rightDots, Is.EqualTo(16)); // 10..12 spans the whole 10..12 axis: all 16 dot rows
        Assert.That(leftDots, Is.InRange(4, 5)); // 10..10.5 is a quarter of it
    }

    [TestCase(CandleChartMode.Braille)]
    [TestCase(CandleChartMode.Candles)]
    public void SessionBreaksLineUpInEveryPaneRow(CandleChartMode mode)
    {
        var sessions = TwoDays();
        string[] lines = AsciiCandleChart.Render(Walk(sessions), sessions, new CandleChartOptions { Title = "T", Mode = mode }).Split('\n');
        var paneRows = lines.Skip(1).Take(20 + 4).ToList();
        var breaks = Enumerable.Range(0, paneRows[0].Length).Where(c => paneRows[0][c] == ':').ToList();

        Assert.That(breaks, Has.Count.EqualTo(5));
        foreach (var row in paneRows)
            Assert.That(breaks.All(c => row[c] == ':'), Is.True, row);
        string axis = lines[1 + 24];
        Assert.That(breaks.All(c => axis[c] == '+'), Is.True, axis);
    }

    [Test]
    public void AutoBucketPicksTheSmallestThatFits()
    {
        var sessions = TwoDays();
        int Minutes(int width) => int.Parse(Regex.Match(
            AsciiCandleChart.Render(Walk(sessions), sessions, new CandleChartOptions { Width = width, Mode = CandleChartMode.Candles }),
            @"(\d+)-min bars").Groups[1].Value);

        Assert.That(Minutes(80), Is.GreaterThan(Minutes(220)));
        Assert.That(Minutes(220), Is.EqualTo(10)); // 2 days x 96 ten-minute columns + 5 breaks = 197
    }

    [Test]
    public void AggregatesBarsIntoBucketCandles()
    {
        var session = new ChartSession("S", Thu.AddHours(10), Thu.AddHours(11));
        var bars = new List<OhlcBar>
        {
            new(Thu.AddHours(10), 10, 11, 9, 10.5, 100),
            new(Thu.AddHours(10).AddMinutes(30), 10.5, 12, 10, 11, 100),
        };
        string chart = AsciiCandleChart.Render(bars, [session], new CandleChartOptions
        {
            Mode = CandleChartMode.Candles, BucketSize = TimeSpan.FromHours(1), Height = 7, VolumeHeight = 0, Grid = '\0', MarkLast = false, Width = 40,
        });
        var plot = chart.Split('\n').Take(7).Select(l => l[l.IndexOf('|') + 2]).ToArray();

        // One up candle 9-12: a wick above and below a '#' body, nothing else.
        Assert.That(new string(plot).Trim(), Does.Match(@"^\|+#+\|+$"));
    }

    [TestCase(CandleChartMode.Braille)]
    [TestCase(CandleChartMode.Candles)]
    public void ReferenceLineIsDrawnAndNamedAtTheRightEdge(CandleChartMode mode)
    {
        var sessions = TwoDays();
        string chart = AsciiCandleChart.Render(Walk(sessions), sessions, new CandleChartOptions
        {
            Mode = mode,
            ReferenceLines = [new ChartReferenceLine("Thu close", 20.1)],
        });
        string line = chart.Split('\n').Single(l => l.Contains("<- Thu close 20.10"));

        Assert.That(line.Count(c => c == '-' || IsBraille(c)), Is.GreaterThan(20));
    }

    [Test]
    public void LabelsSessionsAndDays()
    {
        var sessions = TwoDays();
        string chart = AsciiCandleChart.Render(Walk(sessions), sessions);

        Assert.That(chart, Does.Contain("REGULAR"));
        Assert.That(chart, Does.Contain(" Thu 10/29 "));
        Assert.That(chart, Does.Contain(" Fri 10/30 "));
        Assert.That(chart, Does.Contain("9:30a"));
    }

    [Test]
    public void RejectsAChartThatCannotFit()
    {
        var sessions = TwoDays();
        Assert.Throws<ArgumentException>(() => AsciiCandleChart.Render(Walk(sessions), sessions,
            new CandleChartOptions { Width = 60, BucketSize = TimeSpan.FromMinutes(1) }));
    }

    [TestCase(0.37, 0.5)]
    [TestCase(1.2, 2)]
    [TestCase(2.1, 2.5)]
    [TestCase(0.04, 0.05)]
    [TestCase(7, 10)]
    public void NiceStepRoundsUpToOneTwoTwoPointFiveOrFive(double rough, double expected) =>
        Assert.That(AsciiCandleChart.NiceStep(rough), Is.EqualTo(expected).Within(1e-12));

    [TestCase(4, 0, "4a")]
    [TestCase(9, 30, "9:30a")]
    [TestCase(12, 0, "12p")]
    [TestCase(16, 0, "4p")]
    public void CompactClockReadsLikeATapeTime(int hour, int minute, string expected) =>
        Assert.That(AsciiCandleChart.CompactClock(new DateTime(2026, 1, 1, hour, minute, 0)), Is.EqualTo(expected));
}
