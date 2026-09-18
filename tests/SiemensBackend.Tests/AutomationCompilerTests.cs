using Xunit;
using TiaAutomationFactory.Domain;
using TiaAutomationFactory.PlcCompiler;

namespace TiaAutomationFactory.PlcCompiler.Tests;

public sealed class AutomationCompilerTests
{
    [Fact]
    public void RejectsDuplicateFieldNamesCaseInsensitively()
    {
        var device = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool),
                new AutomationField("start", AutomationType.Bool)
            });

        var exception = Assert.Throws<InvalidOperationException>(() => AutomationCompiler.Compile(device));
        Assert.Contains("Duplicate field", exception.Message);
    }
}
