using MindAttic.Helpers.Ascii;
using NUnit.Framework;

namespace MindAttic.Helpers.Tests;

[TestFixture]
public class AsciiAlignerTests
{
    [Test]
    public void PadsShortContentRowsToTheLongestRowInTheBlock()
    {
        string text = string.Join('\n', new[]
        {
            "+----------------+",
            "| short          |",
            "| a much longer line |",
            "+----------------+",
        });

        string aligned = AsciiAligner.Align(text);
        string[] lines = aligned.Split('\n');

        Assert.That(lines.Select(l => l.Length).Distinct().Count(), Is.EqualTo(1), aligned);
        foreach (string line in lines)
        {
            Assert.That(line.EndsWith('|') || line.EndsWith('+'), Is.True, line);
        }
    }

    [Test]
    public void ExtendsARuleLineInsteadOfInsertingAGap()
    {
        string text = string.Join('\n', new[]
        {
            "+-- Stats --+",
            "| a much longer row of content |",
        });

        string[] aligned = AsciiAligner.Align(text).Split('\n');
        Assert.That(aligned[0].Length, Is.EqualTo(aligned[1].Length));

        // The rule's own fill run lengthens; its "Stats" title and the run right after it are
        // untouched, and nothing but that fill character and the closing "+" was appended.
        Assert.That(aligned[0], Does.StartWith("+-- Stats --"), aligned[0]);
        Assert.That(aligned[0], Does.EndWith("-+"), aligned[0]);
        string added = aligned[0]["+-- Stats --".Length..^1];
        Assert.That(added.All(c => c == '-'), Is.True, aligned[0]);
    }

    [Test]
    public void BoxDrawingCharacters()
    {
        string text = string.Join('\n', new[]
        {
            "┌─────┐",
            "│ hi              │",
            "└─────┘",
        });

        string[] aligned = AsciiAligner.Align(text).Split('\n');
        Assert.That(aligned.Select(l => l.Length).Distinct().Count(), Is.EqualTo(1), string.Join('\n', aligned));
        Assert.That(aligned[0], Does.EndWith("─┐"));
        Assert.That(aligned[2], Does.EndWith("─┘"));
    }

    [Test]
    public void BlankLinesSeparateIndependentBlocks()
    {
        string text = string.Join('\n', new[]
        {
            "+-----+",
            "| one |",
            "+-----+",
            "",
            "+---------------+",
            "| two           |",
            "+---------------+",
        });

        Assert.That(AsciiAligner.Align(text), Is.EqualTo(text), "already-aligned blocks pass through unchanged");
    }

    [Test]
    public void AlreadyAlignedTextIsUnchanged()
    {
        const string text = "+-----+\n| hi  |\n+-----+\n";
        Assert.That(AsciiAligner.Align(text), Is.EqualTo(text));
        Assert.That(AsciiAligner.IsAligned(text), Is.True);
    }

    [Test]
    public void PreservesTrailingNewlinePresence()
    {
        const string withNewline = "| a   |\n| bb  |\n";
        const string withoutNewline = "| a   |\n| bb  |";
        Assert.That(AsciiAligner.Align(withNewline), Does.EndWith("\n"));
        Assert.That(AsciiAligner.Align(withoutNewline), Does.Not.EndWith("\n"));
    }

    [Test]
    public void PlainTextWithoutBordersJustGetsRightPadded()
    {
        const string text = "short\na much longer line";
        string[] aligned = AsciiAligner.Align(text).Split('\n');
        Assert.That(aligned[0].Length, Is.EqualTo(aligned[1].Length));
        Assert.That(aligned[0].TrimEnd(), Is.EqualTo("short"));
    }
}
