using System.Text.Json;
using TiaAutomationFactory.Domain;
using TiaAutomationFactory.PlcCompiler;
using TiaAutomationFactory.SiemensBackend;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: GeneratorCli <input.json> <output-directory>");
    return 2;
}

var json = await File.ReadAllTextAsync(args[0]);
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var device = JsonSerializer.Deserialize<AutomationDevice>(json, options)
    ?? throw new InvalidOperationException("Input JSON did not contain an AutomationDevice.");

var ir = AutomationCompiler.Compile(device);
var scl = SclDataTypeGenerator.Generate(ir);

Directory.CreateDirectory(args[1]);
var output = Path.Combine(args[1], $"UDT_{ir.Name}.scl");
await File.WriteAllTextAsync(output, scl);
Console.WriteLine(output);
return 0;
