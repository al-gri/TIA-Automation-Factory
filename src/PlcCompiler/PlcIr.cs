using TiaAutomationFactory.Domain;

namespace TiaAutomationFactory.PlcCompiler;

public sealed record PlcIrField(string Name, AutomationType Type);

public sealed record PlcIrDataType
{
    public string Name { get; init; }
    public IReadOnlyList<PlcIrField> Fields { get; init; }

    public PlcIrDataType(string name, IReadOnlyList<PlcIrField> fields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fields);

        var duplicate = fields
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate field: {duplicate.Key}");
        }

        Name = name;
        Fields = fields;
    }
}
