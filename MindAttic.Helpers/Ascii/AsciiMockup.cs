namespace MindAttic.Helpers.Ascii;

/// <summary>One labeled rectangular region of a mockup, in character cells (not dots) (M.HELP-T20).</summary>
/// <param name="X">The panel's left edge, in character columns from the canvas's left edge.</param>
/// <param name="Y">The panel's top edge, in character rows from the canvas's top edge.</param>
/// <param name="Width">The panel's width in character columns; must be positive.</param>
/// <param name="Height">The panel's height in character rows; must be positive.</param>
/// <param name="Label">Text centered just inside the panel's top border; <see langword="null"/> for none.</param>
public readonly record struct MockupPanel(int X, int Y, int Width, int Height, string? Label = null);

/// <summary>
/// Renders app/UI wireframes — labeled rectangular panels laid out on a grid — at Braille dot
/// resolution (M.HELP-T20): 8x the edge detail of a plain box-drawing border, using
/// <see cref="BrailleCanvas"/> for the dot-packing so this never duplicates
/// <see cref="AsciiCandleChart"/>'s bit-packing logic. General-purpose: it draws panels and labels
/// for any app's screens, not any one app's layout.
/// </summary>
public static class AsciiMockup
{
    /// <summary>
    /// Renders <paramref name="panels"/> as a wireframe: each panel's border drawn as a Braille-dot
    /// rectangle outline (2x4 dots per character — 8x the edge resolution of a plain box-drawing
    /// border), with its label centered just inside the top edge. Coordinates are in character
    /// cells; every panel must fit inside the <paramref name="width"/> x <paramref name="height"/>
    /// character canvas. Deterministic; every output line is exactly <paramref name="width"/> characters.
    /// </summary>
    /// <param name="panels">The panels to draw. Must not be empty; every panel must have a positive size and fit entirely within the canvas.</param>
    /// <param name="width">The canvas width in character columns; must be positive.</param>
    /// <param name="height">The canvas height in character rows; must be positive.</param>
    /// <param name="title">An optional first line above the wireframe.</param>
    /// <exception cref="ArgumentException"><paramref name="panels"/> is empty, or a panel does not fit within the canvas.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/>, <paramref name="height"/>, or a panel's width/height is not positive.</exception>
    public static string Render(IReadOnlyList<MockupPanel> panels, int width, int height, string? title = null)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Must be positive.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Must be positive.");
        if (panels.Count == 0) throw new ArgumentException("At least one panel is required.", nameof(panels));

        for (int i = 0; i < panels.Count; i++)
        {
            var p = panels[i];
            if (p.Width <= 0 || p.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(panels), $"{p.Width}x{p.Height}", $"Panel {i} must have a positive width and height.");
            if (p.X < 0 || p.Y < 0 || p.X + p.Width > width || p.Y + p.Height > height)
                throw new ArgumentException($"Panel {i} ({p.X},{p.Y} {p.Width}x{p.Height}) does not fit in a {width}x{height} canvas.", nameof(panels));
        }

        var canvas = new BrailleCanvas(width * 2, height * 4);
        foreach (var p in panels)
            canvas.DrawRect(p.X * 2, p.Y * 4, p.Width * 2, p.Height * 4);

        var rows = canvas.RenderRows().Select(r => r.ToCharArray()).ToArray();
        foreach (var p in panels)
        {
            if (string.IsNullOrEmpty(p.Label) || p.Height < 3) continue;
            Center(rows[p.Y + 1], p.X + 1, p.X + p.Width - 2, p.Label);
        }

        var lines = new List<string>();
        if (!string.IsNullOrEmpty(title)) lines.Add(Fit(title, width));
        lines.AddRange(rows.Select(r => new string(r)));
        return string.Join('\n', lines);
    }

    /// <summary>Writes <paramref name="text"/> centered in [from, to], cut to fit — same logic as <see cref="AsciiCandleChart"/>'s private span-centering helper.</summary>
    private static void Center(char[] row, int from, int to, string text)
    {
        if (from < 0 || to < from) return;
        int width = to - from + 1;
        if (text.Length > width) text = text[..width];
        int start = from + (width - text.Length) / 2;
        for (int i = 0; i < text.Length; i++) row[start + i] = text[i];
    }

    /// <summary>Pads or cuts a line to exactly <paramref name="width"/> characters.</summary>
    private static string Fit(string line, int width) =>
        line.Length >= width ? line[..width] : line.PadRight(width);
}
