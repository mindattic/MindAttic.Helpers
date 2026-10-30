using MindAttic.Helpers.Ascii;

namespace MindAttic.Helpers.Ascii.Tool;

/// <summary>
/// Shell entry point for <see cref="AsciiAligner"/> (M.HELP-T13) — mirrors the usage of the
/// retired <c>MindAttic.Orchestrator/tools/ascii_align.py</c> (M.ORCH-T1305):
///
/// <code>
/// ascii-align &lt; diagram.txt          # aligned result to stdout
/// ascii-align diagram.txt
/// ascii-align --check diagram.txt      # validate only; exit 1 if misaligned
/// ascii-align --write diagram.txt      # rewrite the file in place
/// </code>
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        bool check = false;
        bool write = false;
        string? path = null;

        foreach (string arg in args)
        {
            switch (arg)
            {
                case "--check":
                    check = true;
                    break;
                case "--write":
                    write = true;
                    break;
                case "-h":
                case "--help":
                    PrintUsage();
                    return 0;
                default:
                    if (path is not null)
                    {
                        Console.Error.WriteLine($"ascii-align: unexpected argument '{arg}'");
                        PrintUsage();
                        return 2;
                    }
                    path = arg;
                    break;
            }
        }

        if (write && path is null)
        {
            Console.Error.WriteLine("ascii-align: --write requires a file path, not stdin");
            return 2;
        }

        string text = path is not null ? File.ReadAllText(path) : Console.In.ReadToEnd();
        string aligned = AsciiAligner.Align(text);

        if (check)
        {
            if (aligned != text)
            {
                Console.Error.WriteLine("ascii-align: misaligned — run without --check to see the fixed version");
                return 1;
            }

            return 0;
        }

        if (write)
        {
            if (aligned != text)
            {
                File.WriteAllText(path!, aligned);
            }

            return 0;
        }

        Console.Out.Write(aligned);
        return 0;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("usage: ascii-align [--check | --write] [path]");
    }
}
