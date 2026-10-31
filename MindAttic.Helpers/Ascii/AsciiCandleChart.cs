using System.Globalization;
using System.Text;

namespace MindAttic.Helpers.Ascii;

/// <summary>One open/high/low/close/volume bar to plot (M.HELP-T18). <see cref="Time"/> is the bar's start, in whatever clock the sessions use.</summary>
public readonly record struct OhlcBar(DateTime Time, double Open, double High, double Low, double Close, double Volume = 0);

/// <summary>
/// A labeled stretch of time drawn as one contiguous block of columns (M.HELP-T18), e.g. a
/// premarket, regular or after-hours session. Time between sessions is not drawn: consecutive
/// sessions sit side by side with a one-column separator, so an overnight gap costs no width.
/// Sessions sharing a <see cref="Group"/> (e.g. "Thu 10/29") get one span label beneath them.
/// </summary>
public sealed record ChartSession(string Label, DateTime Start, DateTime End, string Group = "");

/// <summary>A horizontal reference line (prior close, a target, a stop) drawn through the plot and named at the right edge (M.HELP-T18).</summary>
public sealed record ChartReferenceLine(string Label, double Value, char LineChar = '-');

/// <summary>How the price pane is drawn (M.HELP-T18).</summary>
public enum CandleChartMode
{
    /// <summary>
    /// High resolution: each character is a 2x4 Braille dot cell, so it carries two time slots and
    /// four price levels, with the high-low range of every slot filled in. Volume uses the eight
    /// block heights. Unicode, but every glyph is single-width, so lines still align.
    /// </summary>
    Braille,

    /// <summary>Pure ASCII candles, one per character: <c>|</c> wick, <c>#</c> up body, <c>=</c> down body.</summary>
    Candles,
}

/// <summary>Layout and glyph options for <see cref="AsciiCandleChart.Render"/> (M.HELP-T18).</summary>
public sealed record CandleChartOptions
{
    /// <summary>A first line above the chart; null for none.</summary>
    public string? Title { get; init; }

    /// <summary>Braille (high resolution, the default) or pure-ASCII candles.</summary>
    public CandleChartMode Mode { get; init; } = CandleChartMode.Braille;

    /// <summary>The width every output line is padded to; the plot gets what the gutter and right-hand notes leave.</summary>
    public int Width { get; init; } = 120;

    /// <summary>Rows in the price pane.</summary>
    public int Height { get; init; } = 20;

    /// <summary>Rows in the volume pane; 0 hides it.</summary>
    public int VolumeHeight { get; init; } = 4;

    /// <summary>
    /// The time one slot covers (a character in <see cref="CandleChartMode.Candles"/>, half of one in
    /// <see cref="CandleChartMode.Braille"/>); null picks the smallest of 1, 2, 3, 5, 10, 15, 20, 25,
    /// 30, 40, 45, 60, 90, 120 or 240 minutes that fits.
    /// </summary>
    public TimeSpan? BucketSize { get; init; }

    /// <summary>Numeric format for prices on the axis and in the right-hand notes.</summary>
    public string PriceFormat { get; init; } = "0.00";

    /// <summary>Horizontal reference lines.</summary>
    public IReadOnlyList<ChartReferenceLine> ReferenceLines { get; init; } = [];

    /// <summary>Adds a "last" note at the row of the final close.</summary>
    public bool MarkLast { get; init; } = true;

    /// <summary>Formats an axis tick time; null gives a compact clock such as <c>4a</c>, <c>9:30a</c>, <c>4p</c>.</summary>
    public Func<DateTime, string>? TickFormat { get; init; }

    /// <summary>Candles mode: body of a candle that closed at or above its open.</summary>
    public char UpBody { get; init; } = '#';

    /// <summary>Candles mode: body of a candle that closed below its open.</summary>
    public char DownBody { get; init; } = '=';

    /// <summary>Candles mode: the high-low wick.</summary>
    public char Wick { get; init; } = '|';

    /// <summary>The column between two sessions.</summary>
    public char Separator { get; init; } = ':';

    /// <summary>Candles mode: dots drawn every other column on price-tick rows; '\0' turns the grid off.</summary>
    public char Grid { get; init; } = '.';

    /// <summary>Candles mode: the volume pane's bar glyph.</summary>
    public char VolumeBar { get; init; } = '|';
}

/// <summary>
/// Renders OHLC bars as a text chart across labeled sessions (M.HELP-T18): a price pane with a
/// nice-step axis, an optional volume pane, a time axis, session labels and group spans. Braille
/// mode (the default) packs 2 time slots x 4 price levels into every character; Candles mode is
/// pure ASCII. Deterministic (invariant culture), and every line comes out exactly
/// <see cref="CandleChartOptions.Width"/> characters wide, so nothing drifts. It is the
/// time-series companion to <see cref="AsciiChart"/>.
/// </summary>
public static class AsciiCandleChart
{
    private static readonly int[] AutoBucketMinutes = [1, 2, 3, 5, 10, 15, 20, 25, 30, 40, 45, 60, 90, 120, 240];

    /// <summary>Volume heights in eighths of a row.</summary>
    private const string Blocks = "▁▂▃▄▅▆▇█";

    /// <summary>" | " between the axis labels and the plot, so the first mark never touches the axis.</summary>
    private const int Prefix = 3;

    private enum ColumnKind { Plot, Separator }

    private sealed class Column(ColumnKind kind, int session, DateTime start, DateTime end, int slots)
    {
        public ColumnKind Kind { get; } = kind;
        public int Session { get; } = session;
        public DateTime Start { get; } = start;
        public DateTime End { get; } = end;
        public OhlcBar?[] Slots { get; } = new OhlcBar?[slots];
        public double Volume => Slots.Sum(s => s?.Volume ?? 0);
    }

    /// <summary>Renders <paramref name="bars"/> (any order; bars outside every session are ignored) across <paramref name="sessions"/> (in display order, non-overlapping).</summary>
    public static string Render(IReadOnlyList<OhlcBar> bars, IReadOnlyList<ChartSession> sessions, CandleChartOptions? options = null)
    {
        var o = options ?? new CandleChartOptions();
        if (sessions.Count == 0) throw new ArgumentException("At least one session is required.", nameof(sessions));
        if (sessions.Any(s => s.End <= s.Start)) throw new ArgumentException("Every session must end after it starts.", nameof(sessions));
        if (o.Height < 3) throw new ArgumentOutOfRangeException(nameof(options), o.Height, "Height must be at least 3.");
        if (o.VolumeHeight < 0) throw new ArgumentOutOfRangeException(nameof(options), o.VolumeHeight, "VolumeHeight can't be negative.");

        var inv = CultureInfo.InvariantCulture;
        bool braille = o.Mode == CandleChartMode.Braille;
        int slotsPerColumn = braille ? 2 : 1;
        int levelsPerRow = braille ? 4 : 1;
        int levels = o.Height * levelsPerRow;
        string Price(double p) => p.ToString(o.PriceFormat, inv);

        // The price range covers the bars that land in a session plus every reference line.
        var inSession = bars.Where(b => SessionOf(sessions, b.Time) >= 0).OrderBy(b => b.Time).ToList();
        var values = inSession.SelectMany(b => new[] { b.High, b.Low }).Concat(o.ReferenceLines.Select(r => r.Value)).ToList();
        double min = values.Count > 0 ? values.Min() : 0, max = values.Count > 0 ? values.Max() : 1;
        double step = NiceStep((max - min) / Math.Max(1, (o.Height - 1) / 3.0));
        double lo = Math.Floor(min / step) * step, hi = Math.Ceiling(max / step) * step;
        if (hi - lo < step) hi = lo + step;
        int Level(double p) => Math.Clamp((int)Math.Round((hi - p) / (hi - lo) * (levels - 1)), 0, levels - 1);
        int Row(double p) => Level(p) / levelsPerRow;

        // Left gutter: the widest axis label. Right side: the widest note.
        var tickRows = new SortedDictionary<int, string>();
        for (int i = 0; lo + i * step <= hi + step / 2; i++)
            tickRows.TryAdd(Row(lo + i * step), Price(lo + i * step));

        var notes = new SortedDictionary<int, List<string>>();
        foreach (var line in o.ReferenceLines)
            AddNote(notes, Row(line.Value), $"{line.Label} {Price(line.Value)}");
        if (o.MarkLast && inSession.Count > 0)
            AddNote(notes, Row(inSession[^1].Close), $"last {Price(inSession[^1].Close)}");
        var noteText = notes.ToDictionary(n => n.Key, n => "<- " + string.Join("; ", n.Value));
        int noteWidth = noteText.Count > 0 ? noteText.Values.Max(n => n.Length) + 1 : 0;

        // The volume label isn't known until bucketing, but it is at most 6 wide ("999.9K").
        int gutter = Math.Max(tickRows.Values.Max(l => l.Length), o.VolumeHeight > 0 ? 6 : 0);
        int plotWidth = o.Width - gutter - Prefix - noteWidth;
        var bucket = o.BucketSize ?? PickBucket(sessions, plotWidth, slotsPerColumn);
        if (bucket <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options), bucket, "BucketSize must be positive.");

        var columns = BuildColumns(sessions, bucket, slotsPerColumn);
        if (columns.Count > plotWidth)
            throw new ArgumentException(
                $"{columns.Count} columns of {bucket.TotalMinutes.ToString("0.##", inv)} min don't fit in {plotWidth} plot characters; widen the chart or use a larger BucketSize.",
                nameof(options));
        Aggregate(inSession, sessions, columns, bucket, slotsPerColumn);

        var lines = new List<string>();
        if (!string.IsNullOrEmpty(o.Title)) lines.Add(o.Title);
        var pane = braille ? BraillePane(columns, o, levels, Level) : CandlePane(columns, o, tickRows, Row);
        for (int r = 0; r < o.Height; r++)
        {
            var label = tickRows.TryGetValue(r, out var t) ? t : "";
            var note = noteText.TryGetValue(r, out var n) ? " " + n : "";
            lines.Add(label.PadLeft(gutter) + " | " + pane[r] + note);
        }

        // Volume pane: each column's total volume against the busiest column, labeled with that max.
        double maxVolume = columns.Max(c => c.Volume);
        for (int r = 0; r < o.VolumeHeight; r++)
        {
            var row = new StringBuilder(columns.Count);
            foreach (var col in columns)
            {
                if (col.Kind == ColumnKind.Separator) { row.Append(o.Separator); continue; }
                row.Append(VolumeCell(col.Volume, maxVolume, o.VolumeHeight - 1 - r, o.VolumeHeight, braille, o.VolumeBar));
            }
            var label = r == 0 ? Compact(maxVolume) : r == o.VolumeHeight - 1 ? "vol" : "";
            lines.Add(label.PadLeft(gutter) + " | " + row);
        }

        // Time axis: '+' at session breaks and ticks, tick labels, session names, group spans, legend.
        var axis = Filled(columns.Count, '-');
        var tickLabels = Filled(columns.Count, ' ');
        for (int c = 0; c < columns.Count; c++)
            if (columns[c].Kind == ColumnKind.Separator) axis[c] = '+';
        PlaceTicks(columns, sessions, bucket * slotsPerColumn, o.TickFormat ?? CompactClock, axis, tickLabels);
        var indent = new string(' ', gutter + Prefix);
        lines.Add(new string(' ', gutter) + " +-" + new string(axis));
        lines.Add(indent + new string(tickLabels));
        lines.Add(indent + SessionRow(columns, sessions));
        if (sessions.Any(s => s.Group.Length > 0))
            lines.Add(indent + GroupRow(columns, sessions));
        var minutes = bucket.TotalMinutes.ToString("0.##", inv);
        lines.Add(indent + (braille
            ? $"⣿ high-low range, 2 x {minutes} min per character  {o.Separator} session break  ⠤⠤ reference"
            : $"{o.UpBody} up  {o.DownBody} down  {o.Wick} high-low  {o.Separator} session break  {minutes}-min bars"));

        return string.Join('\n', lines.Select(l => Fit(l, o.Width)));
    }

    /// <summary>1, 2, 2.5 or 5 x 10^n: the tidiest step at or above <paramref name="rough"/>.</summary>
    public static double NiceStep(double rough)
    {
        if (rough <= 0 || double.IsNaN(rough)) return 1;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        foreach (var m in new[] { 1, 2, 2.5, 5 })
            if (m * magnitude >= rough - 1e-12) return m * magnitude;
        return 10 * magnitude;
    }

    /// <summary>4a, 9:30a, 12p, 4p, 8p.</summary>
    public static string CompactClock(DateTime t) =>
        (t.Minute == 0 ? (t.Hour % 12 == 0 ? 12 : t.Hour % 12).ToString(CultureInfo.InvariantCulture)
                       : t.ToString("h:mm", CultureInfo.InvariantCulture)) + (t.Hour < 12 ? "a" : "p");

    /// <summary>1.2M, 850K, 3.4B, 120.</summary>
    internal static string Compact(double value)
    {
        var inv = CultureInfo.InvariantCulture;
        return value switch
        {
            >= 1e9 => (value / 1e9).ToString("0.#", inv) + "B",
            >= 1e6 => (value / 1e6).ToString("0.#", inv) + "M",
            >= 1e3 => (value / 1e3).ToString("0.#", inv) + "K",
            _ => value.ToString("0", inv),
        };
    }

    /// <summary>Braille pane: every slot's high-low range filled as dots; reference lines dashed through it.</summary>
    private static string[] BraillePane(List<Column> columns, CandleChartOptions o, int levels, Func<double, int> level)
    {
        var bits = new int[o.Height, columns.Count];
        void Dot(int c, int slot, int y) => bits[y / 4, c] |= DotBit(slot, y % 4);

        for (int c = 0; c < columns.Count; c++)
        {
            if (columns[c].Kind != ColumnKind.Plot) continue;
            foreach (var line in o.ReferenceLines)
                Dot(c, 0, level(line.Value));
            for (int slot = 0; slot < columns[c].Slots.Length; slot++)
            {
                if (columns[c].Slots[slot] is not { } b) continue;
                for (int y = level(b.High); y <= level(b.Low); y++) Dot(c, slot, y);
            }
        }

        var rows = new string[o.Height];
        for (int r = 0; r < o.Height; r++)
        {
            var sb = new StringBuilder(columns.Count);
            for (int c = 0; c < columns.Count; c++)
                sb.Append(columns[c].Kind == ColumnKind.Separator ? o.Separator
                    : bits[r, c] == 0 ? ' ' : (char)(0x2800 + bits[r, c]));
            rows[r] = sb.ToString();
        }
        return rows;
    }

    /// <summary>The Unicode Braille bit for dot (x, y) of a 2x4 cell.</summary>
    private static int DotBit(int x, int y) => (x, y) switch
    {
        (0, 3) => 0x40,
        (1, 3) => 0x80,
        (0, _) => 1 << y,
        _ => 1 << (y + 3),
    };

    /// <summary>Candles pane: separators, grid dots, reference lines, then one candle per character on top.</summary>
    private static string[] CandlePane(List<Column> columns, CandleChartOptions o, SortedDictionary<int, string> tickRows, Func<double, int> row)
    {
        var grid = new char[o.Height, columns.Count];
        for (int r = 0; r < o.Height; r++)
            for (int c = 0; c < columns.Count; c++)
                grid[r, c] = columns[c].Kind == ColumnKind.Separator ? o.Separator
                    : o.Grid != '\0' && tickRows.ContainsKey(r) && c % 2 == 1 ? o.Grid : ' ';
        foreach (var line in o.ReferenceLines)
        {
            int r = row(line.Value);
            for (int c = 0; c < columns.Count; c++)
                if (columns[c].Kind == ColumnKind.Plot) grid[r, c] = line.LineChar;
        }
        for (int c = 0; c < columns.Count; c++)
        {
            if (columns[c].Kind != ColumnKind.Plot || columns[c].Slots[0] is not { } b) continue;
            for (int r = row(b.High); r <= row(b.Low); r++) grid[r, c] = o.Wick;
            char body = b.Close >= b.Open ? o.UpBody : o.DownBody;
            for (int r = row(Math.Max(b.Open, b.Close)); r <= row(Math.Min(b.Open, b.Close)); r++) grid[r, c] = body;
        }

        var rows = new string[o.Height];
        for (int r = 0; r < o.Height; r++)
        {
            var sb = new StringBuilder(columns.Count);
            for (int c = 0; c < columns.Count; c++) sb.Append(grid[r, c]);
            rows[r] = sb.ToString();
        }
        return rows;
    }

    /// <summary>One volume cell, <paramref name="fromBottom"/> rows up: eighth-blocks in Braille mode, a whole glyph per row in Candles mode.</summary>
    private static char VolumeCell(double volume, double maxVolume, int fromBottom, int height, bool eighths, char bar)
    {
        if (maxVolume <= 0 || volume <= 0) return ' ';
        int units = eighths ? 8 : 1;
        int filled = Math.Max(1, (int)Math.Round(volume / maxVolume * height * units));
        int here = Math.Clamp(filled - fromBottom * units, 0, units);
        return here == 0 ? ' ' : eighths ? Blocks[here - 1] : bar;
    }

    private static int SessionOf(IReadOnlyList<ChartSession> sessions, DateTime time)
    {
        for (int i = 0; i < sessions.Count; i++)
            if (time >= sessions[i].Start && time < sessions[i].End) return i;
        return -1;
    }

    private static int ColumnsFor(ChartSession s, TimeSpan bucket, int slotsPerColumn) =>
        (int)Math.Ceiling(Math.Ceiling((s.End - s.Start) / bucket - 1e-9) / slotsPerColumn);

    private static TimeSpan PickBucket(IReadOnlyList<ChartSession> sessions, int plotWidth, int slotsPerColumn)
    {
        foreach (var minutes in AutoBucketMinutes)
        {
            var bucket = TimeSpan.FromMinutes(minutes);
            if (sessions.Sum(s => ColumnsFor(s, bucket, slotsPerColumn)) + sessions.Count - 1 <= plotWidth) return bucket;
        }
        return TimeSpan.FromMinutes(AutoBucketMinutes[^1]);
    }

    private static List<Column> BuildColumns(IReadOnlyList<ChartSession> sessions, TimeSpan bucket, int slotsPerColumn)
    {
        var columns = new List<Column>();
        var span = bucket * slotsPerColumn;
        for (int i = 0; i < sessions.Count; i++)
        {
            var s = sessions[i];
            if (i > 0) columns.Add(new Column(ColumnKind.Separator, i, s.Start, s.Start, 0));
            for (int k = 0; k < ColumnsFor(s, bucket, slotsPerColumn); k++)
            {
                var start = s.Start + span * k;
                columns.Add(new Column(ColumnKind.Plot, i, start, start + span < s.End ? start + span : s.End, slotsPerColumn));
            }
        }
        return columns;
    }

    private static void Aggregate(List<OhlcBar> ordered, IReadOnlyList<ChartSession> sessions, List<Column> columns, TimeSpan bucket, int slotsPerColumn)
    {
        var firstColumn = new int[sessions.Count];
        for (int c = columns.Count - 1; c >= 0; c--)
            if (columns[c].Kind == ColumnKind.Plot) firstColumn[columns[c].Session] = c;
        foreach (var b in ordered)
        {
            int s = SessionOf(sessions, b.Time);
            int slot = (int)((b.Time - sessions[s].Start) / bucket);
            var col = columns[firstColumn[s] + slot / slotsPerColumn];
            ref var cell = ref col.Slots[slot % slotsPerColumn];
            cell = cell is { } a
                ? a with { High = Math.Max(a.High, b.High), Low = Math.Min(a.Low, b.Low), Close = b.Close, Volume = a.Volume + b.Volume }
                : b;
        }
    }

    private static void PlaceTicks(List<Column> columns, IReadOnlyList<ChartSession> sessions, TimeSpan columnSpan,
        Func<DateTime, string> format, char[] axis, char[] labels)
    {
        // Session starts first (they matter most), then every k-th whole hour, k the smallest of
        // 1, 2, 3, 4, 6, 12 that leaves at least 6 columns between hourly ticks.
        double columnsPerHour = TimeSpan.FromHours(1) / columnSpan;
        int every = new[] { 1, 2, 3, 4, 6, 12 }.FirstOrDefault(k => columnsPerHour * k >= 6, 24);
        var candidates = new List<(int Column, DateTime Time, bool Priority)>();
        for (int c = 0; c < columns.Count; c++)
        {
            var col = columns[c];
            if (col.Kind != ColumnKind.Plot) continue;
            if (c == 0 || columns[c - 1].Kind == ColumnKind.Separator)
            {
                candidates.Add((c, sessions[col.Session].Start, true));
                continue;
            }
            var hour = new DateTime(col.Start.Year, col.Start.Month, col.Start.Day, col.Start.Hour, 0, 0, col.Start.Kind);
            if (hour < col.Start) hour = hour.AddHours(1);
            if (hour < col.End && hour.Hour % every == 0) candidates.Add((c, hour, false));
        }
        var taken = new bool[columns.Count];
        foreach (var (column, time, _) in candidates.OrderByDescending(x => x.Priority).ThenBy(x => x.Column))
        {
            var text = format(time);
            if (column + text.Length > columns.Count) continue;
            bool free = true; // a blank column on each side of every label
            for (int i = Math.Max(0, column - 1); i <= Math.Min(columns.Count - 1, column + text.Length); i++)
                free &= !taken[i];
            if (!free) continue;
            for (int i = 0; i < text.Length; i++) { labels[column + i] = text[i]; taken[column + i] = true; }
            axis[column] = '+';
        }
    }

    private static string SessionRow(List<Column> columns, IReadOnlyList<ChartSession> sessions)
    {
        var row = Filled(columns.Count, ' ');
        for (int s = 0; s < sessions.Count; s++)
        {
            int session = s;
            var (from, to) = Span(columns, c => c.Kind == ColumnKind.Plot && c.Session == session);
            Center(row, from, to, sessions[s].Label);
        }
        return new string(row);
    }

    private static string GroupRow(List<Column> columns, IReadOnlyList<ChartSession> sessions)
    {
        var row = Filled(columns.Count, ' ');
        for (int s = 0; s < sessions.Count;)
        {
            int e = s;
            while (e + 1 < sessions.Count && sessions[e + 1].Group == sessions[s].Group) e++;
            if (sessions[s].Group.Length > 0)
            {
                int first = s, last = e;
                var (from, to) = Span(columns, c => c.Kind == ColumnKind.Plot && c.Session >= first && c.Session <= last);
                if (to > from)
                {
                    for (int c = from; c <= to; c++) row[c] = '-';
                    row[from] = '<';
                    row[to] = '>';
                    Center(row, from + 1, to - 1, $" {sessions[s].Group} ");
                }
            }
            s = e + 1;
        }
        return new string(row);
    }

    private static (int From, int To) Span(List<Column> columns, Func<Column, bool> match)
    {
        int from = -1, to = -1;
        for (int c = 0; c < columns.Count; c++)
            if (match(columns[c])) { if (from < 0) from = c; to = c; }
        return (from, to);
    }

    /// <summary>Writes <paramref name="text"/> centered in [from, to], cut to fit.</summary>
    private static void Center(char[] row, int from, int to, string text)
    {
        if (from < 0 || to < from) return;
        int width = to - from + 1;
        if (text.Length > width) text = text[..width];
        int start = from + (width - text.Length) / 2;
        for (int i = 0; i < text.Length; i++) row[start + i] = text[i];
    }

    private static void AddNote(SortedDictionary<int, List<string>> notes, int row, string text)
    {
        if (!notes.TryGetValue(row, out var list)) notes[row] = list = [];
        list.Add(text);
    }

    private static char[] Filled(int count, char c)
    {
        var a = new char[count];
        Array.Fill(a, c);
        return a;
    }

    /// <summary>Pads or cuts a line to exactly <paramref name="width"/> characters.</summary>
    private static string Fit(string line, int width) =>
        line.Length >= width ? line[..width] : line.PadRight(width);
}
