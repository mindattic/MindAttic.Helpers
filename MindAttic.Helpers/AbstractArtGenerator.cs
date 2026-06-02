using System.Globalization;
using System.Text;

namespace MindAttic.Helpers;

/// <summary>
/// Deterministic abstract-art generator: turns any string seed into a stable
/// 300×300 SVG "fingerprint" — a two-stop linear gradient overlaid with 5–8
/// translucent shapes and a single bold initial letter. The same seed always
/// produces the same image, so it's ideal for avatars / project tiles keyed by
/// a slug.
///
/// <para>This is a faithful C# port of the <c>generateProjectArt</c> function on
/// mindattic.com (FNV-1a hash → LCG RNG → one of 16 palettes → gradient + shapes
/// + letter), so output matches the site's house style. The 16 palettes are
/// reused verbatim and the RNG is consumed in the same order.</para>
/// </summary>
public static class AbstractArtGenerator
{
    /// <summary>
    /// The 16 palettes, each <c>[gradientStart, gradientEnd, accent]</c> — deep
    /// teal/indigo/purple/charcoal grounds with a single neon accent (the
    /// MindAttic look). Reused verbatim from mindattic.com.
    /// </summary>
    public static readonly IReadOnlyList<string[]> Palettes = new[]
    {
        new[] { "#0f2027", "#2c5364", "#71f0c8" },
        new[] { "#3a1c71", "#d76d77", "#ffaf7b" },
        new[] { "#1a2980", "#26d0ce", "#ffffff" },
        new[] { "#232526", "#414345", "#f59f00" },
        new[] { "#000428", "#004e92", "#7afcff" },
        new[] { "#0f0c29", "#302b63", "#ff6b6b" },
        new[] { "#134e5e", "#71b280", "#fff700" },
        new[] { "#43cea2", "#185a9d", "#ffffff" },
        new[] { "#5b247a", "#1bcedf", "#ffe66d" },
        new[] { "#373b44", "#4286f4", "#80f8ff" },
        new[] { "#2c003e", "#7700ff", "#ffd166" },
        new[] { "#1f4037", "#99f2c8", "#ffeaa7" },
        new[] { "#1d2671", "#c33764", "#f7d8ba" },
        new[] { "#08313a", "#2980b9", "#f1c40f" },
        new[] { "#093028", "#237a57", "#fffae5" },
        new[] { "#360033", "#0b8793", "#a0ffe6" },
    };

    /// <summary>
    /// The generated SVG as a base64 <c>data:image/svg+xml</c> URI — drop
    /// straight into <c>&lt;img src&gt;</c> or a CSS <c>background-image: url(...)</c>.
    /// </summary>
    public static string DataUri(string seed, char? initial = null) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Svg(seed, initial)));

    /// <summary>
    /// The raw 300×300 SVG markup for <paramref name="seed"/>. The overlaid
    /// letter defaults to the first alphanumeric character of the seed; pass
    /// <paramref name="initial"/> to override it (e.g. a display-name initial
    /// when the seed is an opaque slug).
    /// </summary>
    public static string Svg(string seed, char? initial = null)
    {
        var rng = new Rng(seed);

        var p = Palettes[(int)(rng.Next() * Palettes.Count)];
        string c1 = p[0], c2 = p[1], accent = p[2];

        // Gradient direction (endpoints expressed as percentages around centre).
        var ang = rng.Next() * Math.PI * 2;
        var dx = Math.Cos(ang) * 50;
        var dy = Math.Sin(ang) * 50;
        var x1 = Pct(50 - dx); var y1 = Pct(50 - dy);
        var x2 = Pct(50 + dx); var y2 = Pct(50 + dy);

        var shapes = new StringBuilder();
        var count = 5 + (int)(rng.Next() * 4);          // 5–8 shapes
        for (var s = 0; s < count; s++)
        {
            var sx = Round(rng.Next() * 360 - 30);      // allowed to bleed past the 0–300 edges
            var sy = Round(rng.Next() * 360 - 30);
            var col = (rng.Next() * 3) switch { < 1 => c1, < 2 => c2, _ => accent };
            var op = (rng.Next() * 0.45 + 0.18).ToString("F2", CultureInfo.InvariantCulture);
            var kind = (int)(rng.Next() * 3);
            if (kind == 0)
            {
                var rr = Round(rng.Next() * 90 + 25);
                shapes.Append($"<circle cx=\"{sx}\" cy=\"{sy}\" r=\"{rr}\" fill=\"{col}\" opacity=\"{op}\"/>");
            }
            else if (kind == 1)
            {
                var w = Round(rng.Next() * 160 + 50);
                var h = Round(rng.Next() * 160 + 50);
                var rot = (int)(rng.Next() * 360);
                shapes.Append($"<rect x=\"{sx}\" y=\"{sy}\" width=\"{w}\" height=\"{h}\" fill=\"{col}\" opacity=\"{op}\" transform=\"rotate({rot} {sx} {sy})\"/>");
            }
            else
            {
                var p1x = Round(sx + (rng.Next() - 0.5) * 220);
                var p1y = Round(sy + (rng.Next() - 0.5) * 220);
                var p2x = Round(sx + (rng.Next() - 0.5) * 220);
                var p2y = Round(sy + (rng.Next() - 0.5) * 220);
                shapes.Append($"<polygon points=\"{sx},{sy} {p1x},{p1y} {p2x},{p2y}\" fill=\"{col}\" opacity=\"{op}\"/>");
            }
        }

        var letter = FirstAlnum(initial?.ToString() ?? seed);
        return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 300 300\" preserveAspectRatio=\"xMidYMid slice\">" +
               $"<defs><linearGradient id=\"g\" x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\">" +
               $"<stop offset=\"0\" stop-color=\"{c1}\"/><stop offset=\"1\" stop-color=\"{c2}\"/></linearGradient></defs>" +
               "<rect width=\"300\" height=\"300\" fill=\"url(#g)\"/>" +
               shapes +
               $"<text x=\"150\" y=\"190\" font-family=\"'Outfit',system-ui,sans-serif\" font-size=\"130\" font-weight=\"800\" fill=\"{accent}\" fill-opacity=\"0.92\" text-anchor=\"middle\">{letter}</text>" +
               "</svg>";
    }

    private static string Pct(double v) => Round(v).ToString(CultureInfo.InvariantCulture) + "%";

    private static int Round(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);

    private static string FirstAlnum(string s)
    {
        foreach (var ch in s)
            if (char.IsLetterOrDigit(ch))
                return char.ToUpperInvariant(ch).ToString();
        return "?";
    }

    /// <summary>
    /// FNV-1a (32-bit) seed over the string's UTF-16 code units, advanced by a
    /// Numerical-Recipes LCG — bit-for-bit the same stream as the mindattic.com
    /// JS (<c>Math.imul</c> semantics reproduced by unchecked 32-bit multiply).
    /// </summary>
    private struct Rng
    {
        private uint seed;

        public Rng(string s)
        {
            seed = 2166136261u;
            foreach (var ch in s)
            {
                seed ^= ch;
                seed = unchecked(seed * 16777619u);
            }
        }

        public double Next()
        {
            seed = unchecked(seed * 1664525u + 1013904223u);
            return seed / 4294967296.0;
        }
    }
}
