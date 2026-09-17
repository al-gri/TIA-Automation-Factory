using TiaAutomationFactory.Domain;

namespace TiaAutomationFactory.PlcCompiler;

public static class AutomationCompiler
{
    public static PlcIrDataType Compile(AutomationDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (string.IsNullOrWhiteSpace(device.Name))
            throw new InvalidOperationException("Device name is required.");

        if (device.Fields.Count == 0)
            throw new InvalidOperationException("At least one field is required.");

        var duplicate = device.Fields
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate field: {duplicate.Key}");

        return new PlcIrDataType(
            device.Name,
            device.Fields.Select(f => new PlcIrField(f.Name, f.Type)).ToArray());
    }
}
