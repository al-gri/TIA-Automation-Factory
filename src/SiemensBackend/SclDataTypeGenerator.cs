using System.Text;
using TiaAutomationFactory.Domain;
using TiaAutomationFactory.PlcCompiler;

namespace TiaAutomationFactory.SiemensBackend;

public static class SclDataTypeGenerator
{
    public static string Generate(PlcIrDataType dataType)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"TYPE \"UDT_{dataType.Name}\"");
        sb.AppendLine("VERSION : 0.1");
        sb.AppendLine("   STRUCT");

        foreach (var field in dataType.Fields)
            sb.AppendLine($"      {field.Name} : {MapType(field.Type)};");

        sb.AppendLine("   END_STRUCT;");
        sb.AppendLine("END_TYPE");
        return sb.ToString();
    }

    private static string MapType(AutomationType type) => type switch
    {
        AutomationType.Bool => "Bool",
        AutomationType.Int => "Int",
        AutomationType.DInt => "DInt",
        AutomationType.Real => "Real",
        AutomationType.String => "String[254]",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
