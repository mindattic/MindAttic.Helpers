using MindAttic.Helpers.Ascii;
using NUnit.Framework;

namespace MindAttic.Helpers.Tests;

[TestFixture]
public class BrailleCanvasTests
{
    // Mirrors BrailleCanvas's own private mapping (reused verbatim from AsciiCandleChart), so a
    // test can decode a rendered character back into the dots that produced it.
    private static int DotBit(int x, int y) => (x, y) switch
    {
        (0, 3) => 0x40,
        (1, 3) => 0x80,
        (0, _) => 1 << y,
        _ => 1 << (y + 3),
    };

    private static bool IsSet(string[] rows, int x, int y)
    {
        char c = rows[y / 4][x / 2];
        if (c == ' ') return false;
        int bits = c - 0x2800;
        return (bits & DotBit(x % 2, y % 4)) != 0;
    }

    [Test]
    public void ALoneDotRendersTheExactBrailleCharacter()
    {
        var canvas = new BrailleCanvas(2, 4);
        canvas.SetDot(1, 3);

        Assert.That(canvas.RenderRows()[0], Is.EqualTo(((char)(0x2800 + 0x80)).ToString()));
    }

    [Test]
    public void AnEmptyCanvasRendersAllSpaces()
    {
        var canvas = new BrailleCanvas(9, 10); // Columns = 5, Rows = 3

        foreach (var row in canvas.RenderRows())
            Assert.That(row, Is.EqualTo(new string(' ', canvas.Columns)));
    }

    [Test]
    public void RenderRowsShapeMatchesColumnsAndRows()
    {
        var canvas = new BrailleCanvas(17, 13); // Columns = 9, Rows = 4
        var rows = canvas.RenderRows();

        Assert.That(canvas.Columns, Is.EqualTo(9));
        Assert.That(canvas.Rows, Is.EqualTo(4));
        Assert.That(rows.Length, Is.EqualTo(canvas.Rows));
        foreach (var row in rows)
            Assert.That(row.Length, Is.EqualTo(canvas.Columns));
    }

    [Test]
    public void RenderJoinsRenderRowsWithNewlines()
    {
        var canvas = new BrailleCanvas(4, 8);
        canvas.SetDot(0, 0);

        Assert.That(canvas.Render(), Is.EqualTo(string.Join('\n', canvas.RenderRows())));
    }

    [Test]
    public void SetDotOutOfRangeDoesNotThrow()
    {
        var canvas = new BrailleCanvas(4, 4);

        Assert.DoesNotThrow(() =>
        {
            canvas.SetDot(-1, 0);
            canvas.SetDot(0, -1);
            canvas.SetDot(100, 0);
            canvas.SetDot(0, 100);
        });
        foreach (var row in canvas.RenderRows())
            Assert.That(row, Is.EqualTo(new string(' ', canvas.Columns)));
    }

    [Test]
    public void DrawRectOutlineLeavesTheInteriorUnset()
    {
        var canvas = new BrailleCanvas(8, 8);
        canvas.DrawRect(0, 0, 8, 8);
        var rows = canvas.RenderRows();

        Assert.That(IsSet(rows, 3, 3), Is.False, "interior dot should be unset for an outline rect");
        Assert.That(IsSet(rows, 0, 0), Is.True);
        Assert.That(IsSet(rows, 7, 7), Is.True);
    }

    [Test]
    public void DrawRectFilledSetsTheInterior()
    {
        var canvas = new BrailleCanvas(8, 8);
        canvas.DrawRect(0, 0, 8, 8, filled: true);
        var rows = canvas.RenderRows();

        Assert.That(IsSet(rows, 3, 3), Is.True);
    }

    [Test]
    public void DrawLineHorizontal()
    {
        var canvas = new BrailleCanvas(8, 1);
        canvas.DrawLine(0, 0, 7, 0);
        var rows = canvas.RenderRows();

        for (int x = 0; x < 8; x++)
            Assert.That(IsSet(rows, x, 0), Is.True, $"x={x}");
    }

    [Test]
    public void DrawLineVertical()
    {
        var canvas = new BrailleCanvas(1, 8);
        canvas.DrawLine(0, 0, 0, 7);
        var rows = canvas.RenderRows();

        for (int y = 0; y < 8; y++)
            Assert.That(IsSet(rows, 0, y), Is.True, $"y={y}");
    }

    [Test]
    public void DrawLineDiagonal()
    {
        var canvas = new BrailleCanvas(4, 4);
        canvas.DrawLine(0, 0, 3, 3);
        var rows = canvas.RenderRows();

        for (int i = 0; i < 4; i++)
            Assert.That(IsSet(rows, i, i), Is.True, $"i={i}");
    }

    [Test]
    public void ConstructorRejectsNonPositiveDotWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrailleCanvas(0, 4));
    }

    [Test]
    public void ConstructorRejectsNonPositiveDotHeight()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BrailleCanvas(4, 0));
    }
}
