using System.Numerics;
using System.Text;

namespace MindAttic.Helpers;

/// <summary>
/// Computes the decimal digits of π one at a time, to a requested number of
/// decimal places, and bows out early if the machine starts running low on RAM.
///
/// <para>Digits are produced with Jeremy Gibbons' <em>unbounded spigot</em>
/// algorithm (<c>q, r, t, k, n, l</c> state over <see cref="BigInteger"/>), which
/// streams one correct digit per step without ever needing to know the final
/// length up front. That streaming shape is exactly what lets us check the memory
/// budget between digits and stop cleanly: the digits already produced are always
/// correct, never a half-finished approximation.</para>
///
/// <para><b>About the memory guard.</b> Arbitrary-precision π is a genuine memory
/// hog — the working integers <c>q</c>, <c>r</c>, <c>t</c> grow roughly linearly
/// with the digit count, so tens of millions of digits really can exhaust RAM. We
/// read the <em>system-wide</em> physical-memory load via
/// <see cref="GC.GetGCMemoryInfo(GCKind)"/> (<see cref="GCMemoryInfo.MemoryLoadBytes"/>
/// vs <see cref="GCMemoryInfo.TotalAvailableMemoryBytes"/>) and abort once free
/// RAM drops below <c>minFreeMemoryFraction</c> (default 33%). Two honest caveats:
/// that reading is refreshed at the last garbage collection, so it's a recent
/// snapshot rather than a live gauge; and on a container it reflects the cgroup
/// limit, not the host. It's a safety valve to avoid thrashing/OOM, not a precise
/// allocator.</para>
/// </summary>
public static class PiHelper
{
    /// <summary>
    /// How many decimal places to emit between memory checks. Querying the GC on
    /// every single digit would dominate the runtime, so we batch — small enough
    /// to react well before the limit, large enough to be free in practice.
    /// </summary>
    public const int MemoryCheckInterval = 256;

    /// <summary>The outcome of a <see cref="Calculate(int, double)"/> run.</summary>
    /// <param name="Value">
    /// The digits produced, formatted as <c>"3.1415…"</c> (or a bare <c>"3"</c>
    /// when zero decimal places were requested). Always a correct prefix of π.
    /// </param>
    /// <param name="DecimalPlacesProduced">
    /// Count of digits emitted <em>after</em> the decimal point. Equals the
    /// requested count unless <see cref="StoppedForMemory"/> is true.
    /// </param>
    /// <param name="StoppedForMemory">
    /// <c>true</c> if the run halted early because free system RAM fell below the
    /// requested fraction; <c>false</c> if it produced every requested place.
    /// </param>
    /// <param name="FreeMemoryFraction">
    /// The fraction of system RAM that was free at the final memory check (0–1).
    /// </param>
    public readonly record struct PiResult(
        string Value,
        int DecimalPlacesProduced,
        bool StoppedForMemory,
        double FreeMemoryFraction);

    /// <summary>
    /// Computes π to <paramref name="decimalPlaces"/> digits after the decimal
    /// point (with the leading <c>3</c> always present, so <c>decimalPlaces = 4</c>
    /// yields <c>"3.1415"</c> and <c>decimalPlaces = 0</c> yields <c>"3"</c>),
    /// stopping early if free system RAM drops below
    /// <paramref name="minFreeMemoryFraction"/>.
    /// </summary>
    /// <param name="decimalPlaces">Digits after the decimal point to produce. Must be ≥ 0.</param>
    /// <param name="minFreeMemoryFraction">
    /// Abort once the free-RAM fraction falls below this (0–1). Default <c>0.33</c>
    /// (stop with less than a third of RAM free). Pass <c>0</c> to disable the
    /// guard and compute the full count regardless.
    /// </param>
    /// <returns>A <see cref="PiResult"/> describing what was produced and why it stopped.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="decimalPlaces"/> &lt; 0, or <paramref name="minFreeMemoryFraction"/>
    /// is outside [0, 1].
    /// </exception>
    public static PiResult Calculate(int decimalPlaces, double minFreeMemoryFraction = 0.33)
    {
        if (decimalPlaces < 0)
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), decimalPlaces, "Cannot be negative.");
        if (minFreeMemoryFraction is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(minFreeMemoryFraction), minFreeMemoryFraction, "Must be between 0 and 1.");

        // Gibbons' unbounded spigot state.
        BigInteger q = 1, r = 0, t = 1, k = 1, n = 3, l = 3;

        var sb = new StringBuilder(decimalPlaces + 2);
        var fractional = 0;       // digits emitted after the decimal point
        var emittedInteger = false;
        var stopped = false;
        var freeFraction = 1.0;

        while (fractional < decimalPlaces)
        {
            if (4 * q + r - t < n * t)
            {
                // A digit is settled: emit it, then advance the state. Every new
                // value is computed from the *current* state before any overwrite,
                // mirroring the algorithm's simultaneous assignment.
                if (!emittedInteger)
                {
                    sb.Append((char)('0' + (int)n));    // the leading 3
                    if (decimalPlaces > 0)
                        sb.Append('.');
                    emittedInteger = true;
                }
                else
                {
                    sb.Append((char)('0' + (int)n));
                    fractional++;
                }

                var nr = 10 * (r - n * t);
                n = 10 * (3 * q + r) / t - 10 * n;
                q *= 10;
                r = nr;

                // Periodic safety valve, skipped entirely when the guard is off.
                if (minFreeMemoryFraction > 0 && fractional > 0 && fractional % MemoryCheckInterval == 0)
                {
                    freeFraction = FreeMemoryFraction();
                    if (freeFraction < minFreeMemoryFraction)
                    {
                        stopped = true;
                        break;
                    }
                }
            }
            else
            {
                // Not enough precision to settle the next digit yet: pull in more
                // terms of the series. Again, temps first, then assign.
                var nr = (2 * q + r) * l;
                n = (q * (7 * k + 2) + r * l) / (t * l);
                q *= k;
                t *= l;
                l += 2;
                k += 1;
                r = nr;
            }
        }

        // decimalPlaces == 0 never enters the loop body's integer branch, so emit
        // the bare "3" here.
        if (!emittedInteger)
            sb.Append('3');

        return new PiResult(sb.ToString(), fractional, stopped, freeFraction);
    }

    /// <summary>
    /// Fraction of system physical RAM currently free (0–1), per the latest GC's
    /// memory-load reading. Returns <c>1.0</c> when the runtime can't report a
    /// total (treated as "plenty free" so the guard never blocks spuriously).
    /// </summary>
    private static double FreeMemoryFraction()
    {
        var info = GC.GetGCMemoryInfo();
        var total = info.TotalAvailableMemoryBytes;
        if (total <= 0)
            return 1.0;

        var free = total - info.MemoryLoadBytes;
        if (free < 0)
            free = 0;
        return (double)free / total;
    }
}
