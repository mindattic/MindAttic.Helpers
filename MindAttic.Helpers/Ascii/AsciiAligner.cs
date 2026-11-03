namespace MindAttic.Helpers.Ascii;

/// <summary>
/// Pads ASCII box/table diagrams so every line in a block ends at the same column (M.HELP-T13).
///
/// <para>LLMs reliably miscount characters when hand-aligning ASCII art, so a box's right-hand
/// border drifts a column or two partway down. This fixes it mechanically instead: find the widest
/// line in each blank-line-separated block, then extend every other line in that block out to that
/// width — by lengthening its own trailing rule/fill run (so a <c>-----+</c> or
/// <c>-- Stats ---------+</c> stays a clean rule) or, for a plain content row, by inserting spaces
/// just before its own trailing wall character (<c>| text |</c> stays <c>| text   |</c>).</para>
///
/// <para>Ported from <c>MindAttic.Orchestrator/tools/ascii_align.py</c> (M.ORCH-T1305); that script
/// is retired once this version is verified working, since this repo is the one referenced by every
/// other repo's CLI.</para>
/// </summary>
public static class AsciiAligner
{
    /// <summary>
    /// Characters a horizontal rule/fill run is built from: extending one of these just lengthens it.
    /// </summary>
    private static readonly HashSet<char> RuleChars = new("-=_.~─━═╌╍┄┅┈┉·");

    /// <summary>
    /// Characters that close off the right side of a box row (a wall, corner or junction): padding
    /// goes in just before one of these rather than after it.
    /// </summary>
    private static readonly HashSet<char> RightEdgeChars = new("|│┃｜+┐┘┤╮╯╢┫║╗╝╣╬>");

    /// <summary>
    /// Aligns every blank-line-separated block of <paramref name="text"/> independently; blank
    /// lines pass through unchanged. Preserves whether the input ends with a trailing newline.
    /// </summary>
    public static string Align(string text)
    {
        bool hadTrailingNewline = text.EndsWith('\n');
        string[] inputLines = text.Split('\n');

        var output = new List<string>();
        var block = new List<string>();
        foreach (string line in inputLines)
        {
            if (line.Trim().Length == 0)
            {
                if (block.Count > 0)
                {
                    output.AddRange(AlignBlock(block));
                    block.Clear();
                }
                output.Add(line);
            }
            else
            {
                block.Add(line);
            }
        }

        if (block.Count > 0)
        {
            output.AddRange(AlignBlock(block));
        }

        string result = string.Join('\n', output);
        if (hadTrailingNewline && !result.EndsWith('\n'))
        {
            result += "\n";
        }

        return result;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="text"/> is already perfectly aligned,
    /// i.e. <see cref="Align"/> would leave it unchanged.
    /// </summary>
    public static bool IsAligned(string text) => Align(text) == text;

    /// <summary>Pads every line in one blank-line-free block out to its own widest line.</summary>
    private static List<string> AlignBlock(List<string> lines)
    {
        int width = 0;
        foreach (string line in lines)
        {
            width = Math.Max(width, line.TrimEnd().Length);
        }

        var result = new List<string>(lines.Count);
        foreach (string line in lines)
        {
            result.Add(PadLine(line, width));
        }

        return result;
    }

    /// <summary>Extends <paramref name="line"/> (already known to be no wider than <paramref name="width"/>) out to <paramref name="width"/>.</summary>
    private static string PadLine(string line, int width)
    {
        // Keep a CRLF line's '\r' (Align splits on '\n' only), so CRLF text round-trips instead of
        // coming back with mixed endings and never passing IsAligned.
        string lineEnd = line.EndsWith('\r') ? "\r" : "";
        string body = line.TrimEnd('\r', '\n');
        string stripped = body.TrimEnd();
        if (stripped.Length >= width)
        {
            return body + lineEnd;
        }

        int need = width - stripped.Length;
        string trailing = "";
        if (stripped.Length > 0 && RightEdgeChars.Contains(stripped[^1]))
        {
            trailing = stripped[^1].ToString();
            stripped = stripped[..^1];
        }

        if (stripped.Length > 0 && RuleChars.Contains(stripped[^1]))
        {
            stripped += new string(stripped[^1], need);
        }
        else
        {
            stripped += new string(' ', need);
        }

        return stripped + trailing + lineEnd;
    }
}
