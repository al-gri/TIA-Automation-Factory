using TiaAutomationFactory.Domain;

namespace TiaAutomationFactory.PlcCompiler;

public sealed record PlcIrField(string Name, AutomationType Type);

public sealed record PlcIrDataType(string Name, IReadOnlyList<PlcIrField> Fields);
