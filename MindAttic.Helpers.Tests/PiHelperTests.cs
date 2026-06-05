using MindAttic.Helpers;
using NUnit.Framework;

namespace MindAttic.Helpers.Tests;

/// <summary>
/// Locks the π calculator: the digits it emits must actually be π (correctness is
/// the whole point), the output format must be stable, and the memory guard must
/// behave — both when disabled (run to completion) and when tripped immediately.
/// </summary>
[TestFixture]
public class PiHelperTests
{
    // π with 99 digits after the decimal point (100 significant digits) — the
    // reference prefix every run must match exactly.
    private const string Pi99Places =
        "3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067";

    [Test]
    public void Calculate_ZeroPlaces_IsBareThree()
    {
        var result = PiHelper.Calculate(0);
        Assert.That(result.Value, Is.EqualTo("3"));
        Assert.That(result.DecimalPlacesProduced, Is.EqualTo(0));
        Assert.That(result.StoppedForMemory, Is.False);
    }

    [Test]
    public void Calculate_FourPlaces_Is3Point1415()
    {
        var result = PiHelper.Calculate(4);
        Assert.That(result.Value, Is.EqualTo("3.1415"));
        Assert.That(result.DecimalPlacesProduced, Is.EqualTo(4));
    }

    [Test]
    public void Calculate_NinetyNinePlaces_MatchesKnownPi()
    {
        var result = PiHelper.Calculate(99);
        Assert.That(result.Value, Is.EqualTo(Pi99Places));
        Assert.That(result.DecimalPlacesProduced, Is.EqualTo(99));
        Assert.That(result.StoppedForMemory, Is.False);
    }

    [Test]
    public void Calculate_IsDeterministic()
    {
        Assert.That(PiHelper.Calculate(250).Value, Is.EqualTo(PiHelper.Calculate(250).Value));
    }

    [Test]
    public void Calculate_LongerRunExtendsTheShorterOne()
    {
        // Every prefix of π is a prefix of a longer computation of π.
        Assert.That(PiHelper.Calculate(500).Value, Does.StartWith(PiHelper.Calculate(200).Value));
    }

    [Test]
    public void Calculate_GuardDisabled_ProducesEveryRequestedPlace()
    {
        var result = PiHelper.Calculate(1000, minFreeMemoryFraction: 0);
        Assert.That(result.DecimalPlacesProduced, Is.EqualTo(1000));
        Assert.That(result.StoppedForMemory, Is.False);
    }

    [Test]
    public void Calculate_ImpossibleMemoryThreshold_StopsEarly()
    {
        // Demanding 100% free RAM can never be satisfied, so the run trips the
        // guard at the first check (MemoryCheckInterval places) instead of
        // reaching the requested 100k.
        var result = PiHelper.Calculate(100_000, minFreeMemoryFraction: 1.0);
        Assert.That(result.StoppedForMemory, Is.True);
        Assert.That(result.DecimalPlacesProduced, Is.LessThan(100_000));
        Assert.That(result.DecimalPlacesProduced, Is.EqualTo(PiHelper.MemoryCheckInterval));
        // Whatever it did produce is still a correct prefix of π.
        Assert.That(result.Value, Does.StartWith(Pi99Places));
    }

    [Test]
    public void Calculate_RejectsNegativePlaces()
    {
        Assert.That(() => PiHelper.Calculate(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Calculate_RejectsOutOfRangeThreshold()
    {
        Assert.That(() => PiHelper.Calculate(10, minFreeMemoryFraction: 1.5), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => PiHelper.Calculate(10, minFreeMemoryFraction: -0.1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
