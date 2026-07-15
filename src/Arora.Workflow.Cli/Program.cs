using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Arora.Workflow.Internal.Engine.Graph;
using Arora.Workflow.Tooling.Diagnostics;
using Arora.Workflow.Tooling.Export;

namespace Arora.Workflow.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h" || args[0] == "--help" || args[0] == "help")
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "workflow":
                return HandleWorkflowCommand(args.Skip(1).ToArray());
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Unknown command '{command}'");
                Console.ResetColor();
                ShowHelp();
                return 1;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Arora Workflow CLI Tool");
        Console.WriteLine("Usage: arora <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  workflow lint <path>      Runs static diagnostics on workflow definitions in an assembly or JSON file.");
        Console.WriteLine("  workflow doctor <path>    Runs validation and prints detailed suggestions for structural fixes.");
        Console.WriteLine("  workflow export <path>    Exports definitions in Mermaid format.");
        Console.WriteLine("  workflow diff <v1> <v2>   Diffs transitions between two assembly versions.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help                Show help details.");
    }

    private static int HandleWorkflowCommand(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 1;
        }

        var subCommand = args[0].ToLowerInvariant();
        var remainingArgs = args.Skip(1).ToArray();

        switch (subCommand)
        {
            case "lint":
                return RunLint(remainingArgs, isDoctor: false);
            case "doctor":
                return RunLint(remainingArgs, isDoctor: true);
            case "export":
                return RunExport(remainingArgs);
            case "diff":
                return RunDiff(remainingArgs);
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Unknown sub-command '{subCommand}' for arora workflow");
                Console.ResetColor();
                return 1;
        }
    }

    private static int RunLint(string[] args, bool isDoctor)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Missing target file path (assembly .dll or definition .json).");
            return 1;
        }

        var targetPath = args[0];
        var definitions = LoadDefinitions(targetPath);

        if (definitions.Count == 0)
        {
            Console.WriteLine("No workflow definitions discovered.");
            return 0;
        }

        int exitCode = 0;

        foreach (var def in definitions)
        {
            Console.WriteLine($"--------------------------------------------------");
            Console.WriteLine($"Analyzing Workflow: {def.Name}");
            Console.WriteLine($"--------------------------------------------------");

            var diagnostics = WorkflowDiagnosticsEngine.Analyze(def.Graph);
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
            var suggestions = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Suggestion || d.Severity == DiagnosticSeverity.Info).ToList();

            if (diagnostics.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✓ Validation clean. Zero structural errors or warnings.");
                Console.ResetColor();
                continue;
            }

            foreach (var diag in diagnostics)
            {
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("  [Error]   ");
                    exitCode = 1; // Mark build failure
                }
                else if (diag.Severity == DiagnosticSeverity.Warning)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("  [Warning] ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("  [Info]    ");
                }

                Console.ResetColor();
                Console.WriteLine($"{diag.Code}: {diag.Message} {(diag.NodeName != null ? $"[Node: {diag.NodeName}]" : "")}");

                if (isDoctor && !string.IsNullOrEmpty(diag.Suggestion))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"            💡 Suggestion: {diag.Suggestion}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Summary: {errors.Count} Errors, {warnings.Count} Warnings, {suggestions.Count} Suggestions.");
        }

        return exitCode;
    }

    private static int RunExport(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: Missing target file path.");
            return 1;
        }

        var targetPath = args[0];
        var format = "mermaid";

        // Check if format argument is provided (e.g., --format sequence)
        var formatArgIdx = Array.IndexOf(args, "--format");
        if (formatArgIdx >= 0 && formatArgIdx < args.Length - 1)
        {
            format = args[formatArgIdx + 1].ToLowerInvariant();
        }

        var definitions = LoadDefinitions(targetPath);

        if (definitions.Count == 0)
        {
            Console.WriteLine("No workflow definitions discovered to export.");
            return 1;
        }

        foreach (var def in definitions)
        {
            Console.WriteLine($"%% Workflow: {def.Name} %%");
            if (format == "sequence")
            {
                Console.WriteLine(MermaidExporter.ToSequenceDiagram(def.Graph));
            }
            else
            {
                Console.WriteLine(MermaidExporter.ToFlowchart(def.Graph));
            }
            Console.WriteLine();
        }

        return 0;
    }

    private static int RunDiff(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: arora workflow diff <v1-assembly-path> <v2-assembly-path>");
            return 1;
        }

        var v1Defs = LoadDefinitions(args[0]).ToDictionary(d => d.Name, d => d.Graph, StringComparer.OrdinalIgnoreCase);
        var v2Defs = LoadDefinitions(args[1]).ToDictionary(d => d.Name, d => d.Graph, StringComparer.OrdinalIgnoreCase);

        var allNames = v1Defs.Keys.Union(v2Defs.Keys, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var name in allNames)
        {
            Console.WriteLine($"=== Diffs for Workflow: {name} ===");

            if (!v1Defs.ContainsKey(name))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  [NEW] Workflow added in V2.");
                Console.ResetColor();
                continue;
            }

            if (!v2Defs.ContainsKey(name))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  [DELETED] Workflow removed in V2.");
                Console.ResetColor();
                continue;
            }

            var g1 = v1Defs[name];
            var g2 = v2Defs[name];

            // Compute structural transition diffs
            var t1 = GetTransitionsList(g1);
            var t2 = GetTransitionsList(g2);

            var added = t2.Except(t1).ToList();
            var deleted = t1.Except(t2).ToList();

            if (added.Count == 0 && deleted.Count == 0)
            {
                Console.WriteLine("  No changes detected in transition flow.");
                continue;
            }

            foreach (var item in added)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  + Added: {item}");
                Console.ResetColor();
            }

            foreach (var item in deleted)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  - Removed: {item}");
                Console.ResetColor();
            }
        }

        return 0;
    }

    private static HashSet<string> GetTransitionsList(WorkflowGraph graph)
    {
        var set = new HashSet<string>();
        foreach (var nodeKv in graph.Nodes)
        {
            foreach (var t in nodeKv.Value.Transitions)
            {
                var cond = string.IsNullOrEmpty(t.Condition) ? "unconditional" : $"trigger '{t.Condition}'";
                set.Add($"{nodeKv.Key} -> {t.TargetNode} via {cond}");
            }
        }
        return set;
    }

    private static List<(string Name, WorkflowGraph Graph)> LoadDefinitions(string path)
    {
        var result = new List<(string Name, WorkflowGraph Graph)>();

        if (!File.Exists(path))
        {
            Console.WriteLine($"Error: Target file '{path}' does not exist.");
            return result;
        }

        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var json = File.ReadAllText(path);
                var graph = WorkflowGraph.Parse(json);
                result.Add((Path.GetFileNameWithoutExtension(path), graph));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading JSON definition: {ex.Message}");
            }
        }
        else
        {
            // Load assembly and scan
            try
            {
                var assembly = Assembly.LoadFrom(Path.GetFullPath(path));
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray()!;
                }

                foreach (var type in types)
                {
                    var method = type.GetMethod("GetDefinition", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                    if (method != null && method.GetParameters().Length == 0)
                    {
                        try
                        {
                            var instance = method.IsStatic ? null : Activator.CreateInstance(type);
                            var obj = method.Invoke(instance, null);
                            if (obj != null)
                            {
                                var resultType = obj.GetType();
                                if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,>))
                                {
                                    var jsonField = resultType.GetField("Item4");
                                    var json = jsonField?.GetValue(obj) as string;
                                    var nameField = resultType.GetField("Item1");
                                    var name = nameField?.GetValue(obj) as string ?? type.Name;

                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        var graph = WorkflowGraph.Parse(json);
                                        result.Add((name, graph));
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Skip loading issues for specific scanned types
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning assembly definitions: {ex.Message}");
            }
        }

        return result;
    }
}
