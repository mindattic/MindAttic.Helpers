using MindAttic.Helpers.Ascii;
using NUnit.Framework;

namespace MindAttic.Helpers.Tests;

[TestFixture]
public class AsciiChartTests
{
    [Test]
    public void ScalesBarsToTheLargestValueByDefault()
    {
        var points = new[]
        {
            new ChartPoint("half", 50),
            new ChartPoint("full", 100),
        };

        string chart = AsciiChart.BarChart(points, maxBarWidth: 20);
        string[] lines = chart.Split('\n');

        int halfBar = lines[0].Count(c => c == '#');
        int fullBar = lines[1].Count(c => c == '#');
        Assert.That(fullBar, Is.EqualTo(20));
        Assert.That(halfBar, Is.EqualTo(10));
    }

    [Test]
    public void RightAlignsLabelsToTheWidestOne()
    {
        var points = new[]
        {
            new ChartPoint("a", 1),
            new ChartPoint("longer label", 1),
        };

        string[] lines = AsciiChart.BarChart(points).Split('\n');
        int barColumn = lines[0].IndexOf('|');
        Assert.That(lines.Select(l => l.IndexOf('|')).Distinct().Count(), Is.EqualTo(1));
        Assert.That(lines[0][..barColumn].TrimStart(), Is.EqualTo("a".PadLeft(barColumn)));
    }

    [Test]
    public void AnExplicitScaleMaxLeavesHeadroomPastTheLargestValue()
    {
        var points = new[] { new ChartPoint("one", 50) };

        string chart = AsciiChart.BarChart(points, maxBarWidth: 10, scaleMax: 200);
        int filled = chart.Count(c => c == '#');

        Assert.That(filled, Is.EqualTo(3), "50/200 of a 10-wide bar rounds to 2.5 -> 3");
    }

    [Test]
    public void AMarkerPastEveryValueWidensTheImplicitScale()
    {
        var points = new[] { new ChartPoint("one", 10) };

        string chart = AsciiChart.BarChart(points, maxBarWidth: 10, markerValue: 20);

        // The marker sits past the data, so the implicit max becomes 20 and the bar is half full.
        Assert.That(chart.Count(c => c == '#'), Is.EqualTo(5));
        Assert.That(chart, Does.Contain('|'));
    }

    [Test]
    public void MarkerOverlaysTheBarAtItsOwnPosition()
    {
        var points = new[] { new ChartPoint("one", 100) };

        string chart = AsciiChart.BarChart(points, maxBarWidth: 10, markerValue: 50);
        string bar = chart.Split('|')[1].TrimEnd();

        Assert.That(bar[4], Is.EqualTo('|'), bar);
    }

    [Test]
    public void ValueFormatIsAppliedToTheTrailingNumber()
    {
        var points = new[] { new ChartPoint("pace", 196.4) };

        string chart = AsciiChart.BarChart(points, valueFormat: "0");

        Assert.That(chart.TrimEnd(), Does.EndWith("196"));
    }

    [Test]
    public void EveryLineComesOutTheSameWidth()
    {
        var points = new[]
        {
            new ChartPoint("short", 1),
            new ChartPoint("a much longer label", 2),
        };

        string[] lines = AsciiChart.BarChart(points, valueFormat: "0").Split('\n');
        Assert.That(lines.Select(l => l.Length).Distinct().Count(), Is.EqualTo(1), string.Join('\n', lines));
    }

    [Test]
    public void RejectsAnEmptySeries()
    {
        Assert.Throws<ArgumentException>(() => AsciiChart.BarChart(Array.Empty<ChartPoint>()));
    }

    [Test]
    public void RejectsANonPositiveBarWidth()
    {
        var points = new[] { new ChartPoint("one", 1) };
        Assert.Throws<ArgumentOutOfRangeException>(() => AsciiChart.BarChart(points, maxBarWidth: 0));
    }
}
