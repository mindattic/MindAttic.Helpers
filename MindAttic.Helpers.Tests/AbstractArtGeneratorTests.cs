using System.Text;
using MindAttic.Helpers;
using NUnit.Framework;

namespace MindAttic.Helpers.Tests;

/// <summary>
/// Locks the abstract-art generator: determinism (the whole point — a seed must
/// always yield the same image), well-formed output, distinctness across seeds,
/// and the initial-letter override.
/// </summary>
[TestFixture]
public class AbstractArtGeneratorTests
{
    [Test]
    public void Svg_IsDeterministicForTheSameSeed()
    {
        Assert.That(AbstractArtGenerator.Svg("persona-0042"), Is.EqualTo(AbstractArtGenerator.Svg("persona-0042")));
        Assert.That(AbstractArtGenerator.DataUri("persona-0042"), Is.EqualTo(AbstractArtGenerator.DataUri("persona-0042")));
    }

    [Test]
    public void DifferentSeeds_ProduceDifferentArt()
    {
        Assert.That(AbstractArtGenerator.Svg("persona-0001"), Is.Not.EqualTo(AbstractArtGenerator.Svg("persona-0002")));
    }

    [Test]
    public void Svg_IsWellFormedAndUsesAPaletteGradient()
    {
        var svg = AbstractArtGenerator.Svg("persona-0500");
        Assert.That(svg, Does.StartWith("<svg "));
        Assert.That(svg, Does.EndWith("</svg>"));
        Assert.That(svg, Does.Contain("<linearGradient"));
        Assert.That(svg, Does.Contain("viewBox=\"0 0 300 300\""));
        Assert.That(svg, Does.Contain("</text>"));
    }

    [Test]
    public void DataUri_IsBase64SvgThatRoundTrips()
    {
        const string prefix = "data:image/svg+xml;base64,";
        var uri = AbstractArtGenerator.DataUri("hello-world");
        Assert.That(uri, Does.StartWith(prefix));
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(uri[prefix.Length..]));
        Assert.That(decoded, Is.EqualTo(AbstractArtGenerator.Svg("hello-world")));
    }

    [Test]
    public void Initial_OverridesTheOverlaidLetter()
    {
        // Seed slug starts with 'p' but we want the display-name initial 'M'.
        var svg = AbstractArtGenerator.Svg("persona-0500", initial: 'M');
        Assert.That(svg, Does.Contain(">M</text>"));
    }

    [Test]
    public void Initial_DefaultsToFirstAlphanumericOfSeed()
    {
        Assert.That(AbstractArtGenerator.Svg("persona-0500"), Does.Contain(">P</text>"));
        Assert.That(AbstractArtGenerator.Svg("---"), Does.Contain(">?</text>"));
    }

    [Test]
    public void Palettes_AreSixteenTriples()
    {
        Assert.That(AbstractArtGenerator.Palettes, Has.Count.EqualTo(16));
        Assert.That(AbstractArtGenerator.Palettes, Is.All.Length.EqualTo(3));
    }
}
