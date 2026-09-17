namespace TiaAutomationFactory.Domain;

public sealed record AutomationField(string Name, AutomationType Type);

public sealed record AutomationDevice(string Name, IReadOnlyList<AutomationField> Fields);
