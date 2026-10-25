using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Builder
{
    internal static class CLIConstance
    {
        public static ConsoleColor CMDBGColor => ConsoleColor.DarkGray;
        public static ConsoleColor CMDStartColor => ConsoleColor.Green;

        public static ConsoleColor CMDInputColor => ConsoleColor.DarkYellow;
        public static ConsoleColor CMDRestateColor => ConsoleColor.DarkCyan;

        public static string LineStart => "CLI Builder :> ";

        public static Dictionary<string, Dictionary<string, (string Name, string[] Aliases)>> Commands = new Dictionary<string, Dictionary<string, (string Name, string[] Aliases)>>
        {
            ["run"] = new()
            {
                ["pipeline"] = ("-pipeline", new[] { "-p" }),
                ["pipelineTags"] = ("-pipelineTags", new[] { "-pt" }),
                ["stage"] = ("-stage", new[] { "-s" }),
                ["stageTags"] = ("-stageTags", new[] { "-st" }),
                ["action"] = ("-action", new[] { "-a", "-i" }),
                ["fileTags"] = ("-fileTags", new[] { "-ft", "-f" }),
                ["paramTags"] = ("-paramTags", new[] { "-pt", "-p" }),
            },
            ["file"] = new()
            {
                ["file"] = ("file", new string[0]),
                ["tags"] = ("-tags", new[] { "-t" })
            },
            ["param"] = new()
            {
                ["name"] = ("name", new string[0]),
                ["value"] = ("value", new string[0]),
                ["tags"] = ("-tags", new[] { "-t" })
            },
            ["trigger"] = new()
            {
                ["path"] = ("path", new string[0]),
                ["pipeline"] = ("-pipeline", new[] { "-p" }),
                ["changeTags"] = ("-changeTags", new[] { "-ct" }),
                ["fileTags"] = ("-fileTags", new[] { "-ft" }),
                ["tags"] = ("-tags", new[] { "-t" })
            },
            ["pipeline"] = new()
            {
                ["name"] = ("name", new string[0]),
                ["tags"] = ("-tags", new[] { "-t" })
            },
            ["stage"] = new()
            {
                ["name"] = ("name", new string[0]),
                ["pipeline"] = ("-pipeline", new string[0]),
                ["validValues"] = ("-validReturns", new[] { "-values", "-returns", "-r", "-v" }),
                ["tags"] = ("-tags", new[] { "-t" })
            },
            ["action"] = new()
            {
                ["pipeline"] = ("pipeline", new string[0]),
                ["stage"] = ("stage", new string[0]),
                ["scriptTags"] = ("-fileTags", new[] {"-f", "-ft" } ),
                ["validValues"] = ("-validReturns", new[] { "-values", "-returns", "-r", "-v" }),
                ["tags"] = ("-tags", new[] { "-t" })
            }
        };
    }
}
