using System.Text;
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

        // Redirected stdin/stdout use the OEM code page on Windows, which mangles box-drawing
        // characters (one '─' decodes as several chars, so widths are miscounted). Piped text is
        // read and written as UTF-8 instead; an interactive console keeps its own encoding.
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        bool fileHadBom = false;
        string text;
        if (path is not null)
        {
            byte[] bytes = File.ReadAllBytes(path);
            fileHadBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            text = File.ReadAllText(path);
        }
        else if (Console.IsInputRedirected)
        {
            using var stdin = new StreamReader(Console.OpenStandardInput(), utf8);
            text = stdin.ReadToEnd();
        }
        else
        {
            text = Console.In.ReadToEnd();
        }

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
                // Keep the file's UTF-8 BOM (or lack of one) instead of silently stripping it.
                File.WriteAllText(path!, aligned, new UTF8Encoding(encoderShouldEmitUTF8Identifier: fileHadBom));
            }

            return 0;
        }

        if (Console.IsOutputRedirected)
        {
            using var stdout = new StreamWriter(Console.OpenStandardOutput(), utf8);
            stdout.Write(aligned);
        }
        else
        {
            Console.Out.Write(aligned);
        }

        return 0;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("usage: ascii-align [--check | --write] [path]");
    }
}
