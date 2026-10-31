using MindAttic.Helpers.Ascii;
using NUnit.Framework;

namespace MindAttic.Helpers.Tests;

[TestFixture]
public class AsciiMockupTests
{
    private static bool IsBraille(char c) => c is >= '⠀' and <= '⣿';

    [Test]
    public void SinglePanelRendersABorderAndCenteredLabel()
    {
        var panel = new MockupPanel(0, 0, 20, 5, "Login");
        string chart = AsciiMockup.Render([panel], 20, 5);
        string[] lines = chart.Split('\n');

        Assert.That(lines, Has.Length.EqualTo(5));
        Assert.That(lines[0].Any(IsBraille), Is.True, "top border should be drawn");
        Assert.That(lines[4].Any(IsBraille), Is.True, "bottom border should be drawn");
        Assert.That(lines[1], Does.Contain("Login"));
    }

    [Test]
    public void MultiplePanelsRenderIndependently()
    {
        var panels = new[]
        {
            new MockupPanel(0, 0, 10, 4, "A"),
            new MockupPanel(10, 0, 10, 4, "B"),
        };
        string chart = AsciiMockup.Render(panels, 20, 4);
        string[] lines = chart.Split('\n');

        Assert.That(lines[1], Does.Contain("A"));
        Assert.That(lines[1], Does.Contain("B"));
    }

    [TestCase(10, 5, null)]
    [TestCase(30, 8, "Screen 1")]
    public void EveryLineIsExactlyTheRequestedWidth(int width, int height, string? title)
    {
        var panel = new MockupPanel(0, 0, width, height, "Panel");
        string chart = AsciiMockup.Render([panel], width, height, title);

        Assert.That(chart.Split('\n').Select(l => l.Length).Distinct(), Is.EqualTo(new[] { width }));
    }

    [Test]
    public void RejectsAnEmptyPanelList()
    {
        Assert.Throws<ArgumentException>(() => AsciiMockup.Render(Array.Empty<MockupPanel>(), 10, 10));
    }

    [Test]
    public void RejectsAPanelThatDoesNotFit()
    {
        var panel = new MockupPanel(5, 5, 10, 10);
        Assert.Throws<ArgumentException>(() => AsciiMockup.Render([panel], 10, 10));
    }

    [Test]
    public void RejectsANegativeOriginPanel()
    {
        var panel = new MockupPanel(-1, 0, 5, 5);
        Assert.Throws<ArgumentException>(() => AsciiMockup.Render([panel], 10, 10));
    }

    [TestCase(0, 10)]
    [TestCase(10, 0)]
    public void RejectsNonPositiveCanvasDimensions(int width, int height)
    {
        var panel = new MockupPanel(0, 0, 1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => AsciiMockup.Render([panel], width, height));
    }

    [Test]
    public void RejectsAPanelWithNonPositiveSize()
    {
        var panel = new MockupPanel(0, 0, 0, 5);
        Assert.Throws<ArgumentOutOfRangeException>(() => AsciiMockup.Render([panel], 10, 10));
    }
}
