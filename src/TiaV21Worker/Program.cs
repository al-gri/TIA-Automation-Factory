using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.ExternalSources;

namespace TiaAutomationFactory.TiaV21Worker
{
    internal static class Program
    {
        private const string OpennessFolder = @"C:\Program Files\Siemens\Automation\Portal V21\PublicAPI\V21\net48";

        static Program()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        public static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.Error.WriteLine("Usage: TiaV21Worker <absolute-source-file> <absolute-diagnostics-json> [absolute-work-root]");
                return 64;
            }

            string sourcePath = Path.GetFullPath(args[0]);
            string diagnosticsPath = Path.GetFullPath(args[1]);
            string workRoot = args.Length == 3
                ? Path.GetFullPath(args[2])
                : Path.Combine(Path.GetTempPath(), "TiaAutomationFactory");

            Directory.CreateDirectory(Path.GetDirectoryName(diagnosticsPath));
            Directory.CreateDirectory(workRoot);

            WorkerResult result;
            try
            {
                result = new TiaSmokeTest().Run(sourcePath, workRoot);
            }
            catch (Exception exception)
            {
                result = WorkerResult.FromException(exception);
            }

            File.WriteAllText(diagnosticsPath, ResultJson.Serialize(result), new UTF8Encoding(false));
            Console.WriteLine(File.ReadAllText(diagnosticsPath));
            return result.Success ? 0 : 1;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName requestedAssemblyName = new AssemblyName(args.Name);
            string filePath = Path.Combine(OpennessFolder, requestedAssemblyName.Name + ".dll");

            if (!requestedAssemblyName.Name.StartsWith("Siemens.Engineering.", StringComparison.Ordinal) || !File.Exists(filePath))
                return null;

            Assembly loadedAssembly = Assembly.LoadFrom(filePath);
            if (!string.Equals(requestedAssemblyName.FullName, loadedAssembly.GetName().FullName, StringComparison.Ordinal))
                throw new FileNotFoundException("TIA Portal Openness assembly version does not match the referenced version.", filePath);

            return loadedAssembly;
        }
    }

    internal sealed class TiaSmokeTest
    {
        private const string CpuTypeIdentifier = "OrderNumber:6ES7 516-3AP03-0AB0/V4.0";

        public WorkerResult Run(string sourcePath, string workRoot)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Generated PLC source file was not found.", sourcePath);

            string runName = "Smoke_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            DirectoryInfo projectsDirectory = Directory.CreateDirectory(Path.Combine(workRoot, "projects"));

            var output = new WorkerResult();

            using (TiaPortal portal = new TiaPortal(TiaPortalMode.WithUserInterface))
            using (ExclusiveAccess exclusiveAccess = portal.ExclusiveAccess("TIA Automation Factory smoke test"))
            {
                Project project = null;
                try
                {
                    project = portal.Projects.Create(projectsDirectory, runName);
                    output.ProjectPath = project.Path.FullName;

                    Device station = project.Devices.CreateWithItem(CpuTypeIdentifier, "PLC_1", "S7_1500_Station_1");
                    PlcSoftware plcSoftware = GetPlcSoftware(station);
                    if (plcSoftware == null)
                        throw new InvalidOperationException("No PlcSoftware target was found in the generated S7-1500 station.");

                    PlcExternalSource externalSource = plcSoftware.ExternalSourceGroup.ExternalSources.CreateFromFile(
                        "AutomationFactorySource",
                        sourcePath);

                    externalSource.GenerateBlocksFromSource();

                    ICompilable compilable = plcSoftware.GetService<ICompilable>();
                    if (compilable == null)
                        throw new InvalidOperationException("The PLC software does not expose the ICompilable service.");

                    CompilerResult compilerResult = compilable.Compile();
                    output.State = compilerResult.State.ToString();
                    output.WarningCount = compilerResult.WarningCount;
                    output.ErrorCount = compilerResult.ErrorCount;
                    AddMessages(compilerResult.Messages, output.Diagnostics);
                    output.Success = compilerResult.ErrorCount == 0;

                    return output;
                }
                finally
                {
                    if (project != null)
                        project.Close();
                }
            }
        }

        private static PlcSoftware GetPlcSoftware(HardwareObject hardwareObject)
        {
            var queue = new Queue<HardwareObject>();
            queue.Enqueue(hardwareObject);

            while (queue.Count > 0)
            {
                foreach (DeviceItem deviceItem in queue.Dequeue().Items)
                {
                    if (!deviceItem.IsPlugged)
                        continue;

                    SoftwareContainer softwareContainer = deviceItem.GetService<SoftwareContainer>();
                    if (deviceItem.Classification.HasFlag(DeviceItemClassifications.CPU)
                        && softwareContainer != null
                        && softwareContainer.Software is PlcSoftware)
                    {
                        return (PlcSoftware)softwareContainer.Software;
                    }

                    queue.Enqueue(deviceItem);
                }
            }

            return null;
        }

        private static void AddMessages(CompilerResultMessageComposition messages, IList<DiagnosticRecord> target)
        {
            foreach (CompilerResultMessage message in messages)
            {
                target.Add(new DiagnosticRecord
                {
                    Path = Convert.ToString(message.Path),
                    State = message.State.ToString(),
                    Description = message.Description,
                    WarningCount = message.WarningCount,
                    ErrorCount = message.ErrorCount
                });

                AddMessages(message.Messages, target);
            }
        }
    }

    internal sealed class WorkerResult
    {
        public bool Success { get; set; }
        public string ProjectPath { get; set; }
        public string State { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public string Failure { get; set; }
        public List<DiagnosticRecord> Diagnostics { get; } = new List<DiagnosticRecord>();

        public static WorkerResult FromException(Exception exception)
        {
            return new WorkerResult
            {
                Success = false,
                State = "Exception",
                ErrorCount = 1,
                Failure = exception.ToString()
            };
        }
    }

    internal sealed class DiagnosticRecord
    {
        public string Path { get; set; }
        public string State { get; set; }
        public string Description { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
    }

    internal static class ResultJson
    {
        public static string Serialize(WorkerResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            AppendProperty(builder, "success", result.Success ? "true" : "false", false, true);
            AppendProperty(builder, "projectPath", result.ProjectPath, true, true);
            AppendProperty(builder, "state", result.State, true, true);
            AppendProperty(builder, "warnings", result.WarningCount.ToString(), false, true);
            AppendProperty(builder, "errors", result.ErrorCount.ToString(), false, true);
            AppendProperty(builder, "failure", result.Failure, true, true);
            builder.AppendLine("  \"diagnostics\": [");

            for (int i = 0; i < result.Diagnostics.Count; i++)
            {
                DiagnosticRecord item = result.Diagnostics[i];
                builder.AppendLine("    {");
                AppendProperty(builder, "path", item.Path, true, true, 6);
                AppendProperty(builder, "state", item.State, true, true, 6);
                AppendProperty(builder, "description", item.Description, true, true, 6);
                AppendProperty(builder, "warnings", item.WarningCount.ToString(), false, true, 6);
                AppendProperty(builder, "errors", item.ErrorCount.ToString(), false, false, 6);
                builder.Append("    }");
                builder.AppendLine(i + 1 < result.Diagnostics.Count ? "," : string.Empty);
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendProperty(StringBuilder builder, string name, string value, bool quote, bool comma, int indent = 2)
        {
            builder.Append(' ', indent);
            builder.Append('"').Append(Escape(name)).Append("\": ");
            if (value == null)
                builder.Append("null");
            else if (quote)
                builder.Append('"').Append(Escape(value)).Append('"');
            else
                builder.Append(value);

            if (comma)
                builder.Append(',');
            builder.AppendLine();
        }

        private static string Escape(string value)
        {
            if (value == null)
                return null;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
