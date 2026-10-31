using System.Text;

namespace MindAttic.Helpers.Ascii;

/// <summary>
/// A dot-addressable drawing surface that packs a 2-wide x 4-tall grid of dots into every output
/// character, using the Unicode Braille Patterns block (<c>U+2800</c> plus the dot bits) as a
/// high-resolution "pixel" font: 8 sub-cells per character instead of 1, so a line or box drawn
/// here is 8x sharper than one glyph per cell while every line still lays out as plain, single-width
/// text (M.HELP-T20).
///
/// <para>Extracted from the private bit-packing <see cref="AsciiCandleChart"/> used for its Braille
/// candle mode, so that mode and <see cref="AsciiMockup"/> (and anything else wanting Braille-dot
/// resolution) share one implementation instead of two (M.HELP-LAW-1/2). The dot-to-bit mapping is
/// reused verbatim from that original code, so <see cref="AsciiCandleChart"/>'s output is unchanged.</para>
///
/// <para>Instance-based rather than static because a canvas accumulates dots as it is drawn to, but
/// it still holds no I/O and no non-deterministic state: the same sequence of calls always renders
/// the same text (M.HELP-LAW-2).</para>
/// </summary>
public sealed class BrailleCanvas
{
    private readonly int[,] _bits;

    /// <summary>The canvas width in dots (2 dots per output column).</summary>
    public int DotWidth { get; }

    /// <summary>The canvas height in dots (4 dots per output row).</summary>
    public int DotHeight { get; }

    /// <summary>Output columns: <c>ceil(DotWidth / 2)</c>.</summary>
    public int Columns { get; }

    /// <summary>Output rows: <c>ceil(DotHeight / 4)</c>.</summary>
    public int Rows { get; }

    /// <summary>Creates an empty (all dots off) canvas of <paramref name="dotWidth"/> x <paramref name="dotHeight"/> dots.</summary>
    /// <param name="dotWidth">Width in dots; must be positive. Needn't be a multiple of 2 — a partial last column is padded with unset dots.</param>
    /// <param name="dotHeight">Height in dots; must be positive. Needn't be a multiple of 4 — a partial last row is padded with unset dots.</param>
    public BrailleCanvas(int dotWidth, int dotHeight)
    {
        if (dotWidth <= 0) throw new ArgumentOutOfRangeException(nameof(dotWidth), dotWidth, "Must be positive.");
        if (dotHeight <= 0) throw new ArgumentOutOfRangeException(nameof(dotHeight), dotHeight, "Must be positive.");

        DotWidth = dotWidth;
        DotHeight = dotHeight;
        Columns = (dotWidth + 1) / 2;
        Rows = (dotHeight + 3) / 4;
        _bits = new int[Rows, Columns];
    }

    /// <summary>
    /// Turns one dot on (or off) at dot coordinates (<paramref name="x"/>, <paramref name="y"/>).
    /// A coordinate outside <see cref="DotWidth"/> x <see cref="DotHeight"/> is a silent no-op —
    /// chart and mockup code routinely draws right up to (and occasionally past) the edge, and
    /// that should never throw.
    /// </summary>
    public void SetDot(int x, int y, bool on = true)
    {
        if (x < 0 || x >= DotWidth || y < 0 || y >= DotHeight) return;

        int row = y / 4, col = x / 2;
        int bit = DotBit(x % 2, y % 4);
        if (on) _bits[row, col] |= bit;
        else _bits[row, col] &= ~bit;
    }

    /// <summary>Draws a straight line of dots from (<paramref name="x0"/>, <paramref name="y0"/>) to (<paramref name="x1"/>, <paramref name="y1"/>) using Bresenham's algorithm, in dot coordinates.</summary>
    public void DrawLine(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        int x = x0, y = y0;

        while (true)
        {
            SetDot(x, y);
            if (x == x1 && y == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }

    /// <summary>
    /// Draws a rectangle whose top-left corner is at dot coordinates (<paramref name="x"/>,
    /// <paramref name="y"/>) and whose size is <paramref name="width"/> x <paramref name="height"/>
    /// dots. Draws only the outline unless <paramref name="filled"/> is <see langword="true"/>, in
    /// which case the interior is filled too. A non-positive <paramref name="width"/> or
    /// <paramref name="height"/> draws nothing.
    /// </summary>
    public void DrawRect(int x, int y, int width, int height, bool filled = false)
    {
        if (filled)
        {
            for (int yy = y; yy < y + height; yy++)
                for (int xx = x; xx < x + width; xx++)
                    SetDot(xx, yy);
            return;
        }

        for (int xx = x; xx < x + width; xx++)
        {
            SetDot(xx, y);
            SetDot(xx, y + height - 1);
        }

        for (int yy = y; yy < y + height; yy++)
        {
            SetDot(x, yy);
            SetDot(x + width - 1, yy);
        }
    }

    /// <summary>
    /// Renders the canvas as <see cref="Rows"/> strings, each exactly <see cref="Columns"/>
    /// characters wide. A cell with no dots set renders as a plain space, not the blank Braille
    /// character <c>U+2800</c>, matching the original <see cref="AsciiCandleChart"/> behaviour.
    /// </summary>
    public string[] RenderRows()
    {
        var rows = new string[Rows];
        for (int r = 0; r < Rows; r++)
        {
            var sb = new StringBuilder(Columns);
            for (int c = 0; c < Columns; c++)
            {
                int bits = _bits[r, c];
                sb.Append(bits == 0 ? ' ' : (char)(0x2800 + bits));
            }
            rows[r] = sb.ToString();
        }
        return rows;
    }

    /// <summary>The canvas as one string, rows joined by <c>'\n'</c>. Equivalent to <c>string.Join('\n', RenderRows())</c>.</summary>
    public string Render() => string.Join('\n', RenderRows());

    /// <summary>
    /// The Unicode Braille dot bit for dot (<paramref name="x"/>, <paramref name="y"/>) within a
    /// 2(x)x4(y) cell — reused verbatim from <see cref="AsciiCandleChart"/>'s original private
    /// mapping, so output stays bit-for-bit identical (M.HELP-LAW-3).
    /// </summary>
    private static int DotBit(int x, int y) => (x, y) switch
    {
        (0, 3) => 0x40,
        (1, 3) => 0x80,
        (0, _) => 1 << y,
        _ => 1 << (y + 3),
    };
}
