namespace MindAttic.Helpers.Ascii;

/// <summary>One labeled value to plot as a bar (M.HELP-T14).</summary>
public readonly record struct ChartPoint(string Label, double Value);

/// <summary>
/// Renders a labeled series of values as an ASCII horizontal bar chart (M.HELP-T14) — the
/// companion to <see cref="AsciiAligner"/>: where that fixes up hand-drawn diagrams, this builds
/// one mechanically from data in the first place, so bar lengths and the target-line marker are
/// never hand-counted and never drift.
/// </summary>
public static class AsciiChart
{
    /// <summary>
    /// Renders <paramref name="points"/> as one bar per line: a right-aligned label, a bar scaled
    /// to <paramref name="maxBarWidth"/> characters, and the formatted value.
    /// </summary>
    /// <param name="points">The series to plot, in display order. Must not be empty.</param>
    /// <param name="maxBarWidth">The character width of the longest bar.</param>
    /// <param name="scaleMax">
    /// The value a full-width bar represents. Defaults to the largest value in <paramref
    /// name="points"/>; pass an explicit value to keep the scale stable across charts drawn at
    /// different times (e.g. a fixed 0–200% axis) or to make room for <paramref
    /// name="markerValue"/> when it exceeds every point's value.
    /// </param>
    /// <param name="fillChar">The character a bar is drawn from.</param>
    /// <param name="markerValue">
    /// When set, overlays <paramref name="markerChar"/> at this value's position on every bar —
    /// a vertical reference line such as a target or threshold.
    /// </param>
    /// <param name="markerChar">The character used to draw <paramref name="markerValue"/>.</param>
    /// <param name="valueFormat">
    /// A numeric format string (e.g. "0", "0.0", "0%") applied to each value; null uses the
    /// round-trip default. Values are always formatted with the invariant culture, so the chart is
    /// identical on every machine (M.HELP-LAW-2): "0.0" gives "1.5", never "1,5". Note that the
    /// invariant "P0" renders 0.5 as "50 %" (with a space); use the custom "0%" for "50%".
    /// </param>
    public static string BarChart(
        IReadOnlyList<ChartPoint> points,
        int maxBarWidth = 40,
        double? scaleMax = null,
        char fillChar = '#',
        double? markerValue = null,
        char markerChar = '|',
        string? valueFormat = null)
    {
        if (points.Count == 0)
        {
            throw new ArgumentException("At least one point is required.", nameof(points));
        }

        if (maxBarWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBarWidth), maxBarWidth, "Must be positive.");
        }

        double max = scaleMax ?? points.Max(p => p.Value);
        if (markerValue.HasValue)
        {
            max = Math.Max(max, markerValue.Value);
        }

        if (max <= 0)
        {
            max = 1;
        }

        int labelWidth = points.Max(p => p.Label.Length);

        var lines = new List<string>(points.Count);
        foreach (ChartPoint point in points)
        {
            string label = point.Label.PadLeft(labelWidth);
            string bar = RenderBar(point.Value, max, maxBarWidth, fillChar, markerValue, markerChar);
            string value = valueFormat is not null
                ? point.Value.ToString(valueFormat, System.Globalization.CultureInfo.InvariantCulture)
                : point.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            lines.Add($"{label} | {bar} {value}");
        }

        return AsciiAligner.Align(string.Join('\n', lines));
    }

    private static string RenderBar(
        double value,
        double max,
        int maxBarWidth,
        char fillChar,
        double? markerValue,
        char markerChar)
    {
        double fraction = Math.Clamp(value / max, 0, 1);
        int filled = (int)Math.Round(fraction * maxBarWidth, MidpointRounding.AwayFromZero);

        char[] bar = new char[maxBarWidth];
        for (int i = 0; i < maxBarWidth; i++)
        {
            bar[i] = i < filled ? fillChar : ' ';
        }

        if (markerValue.HasValue)
        {
            double markerFraction = Math.Clamp(markerValue.Value / max, 0, 1);
            int markerPos = (int)Math.Round(markerFraction * maxBarWidth, MidpointRounding.AwayFromZero);
            if (markerPos >= maxBarWidth)
            {
                markerPos = maxBarWidth - 1;
            }

            bar[markerPos] = markerChar;
        }

        return new string(bar);
    }
}
