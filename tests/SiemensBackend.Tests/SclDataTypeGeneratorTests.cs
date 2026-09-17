using Xunit;
using TiaAutomationFactory.Domain;
using TiaAutomationFactory.PlcCompiler;
using TiaAutomationFactory.SiemensBackend;

namespace TiaAutomationFactory.SiemensBackend.Tests;

public sealed class SclDataTypeGeneratorTests
{
    [Fact]
    public void GeneratesExpectedMotorType()
    {
        var device = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool),
                new AutomationField("Running", AutomationType.Bool),
                new AutomationField("Speed", AutomationType.Real)
            });

        var ir = AutomationCompiler.Compile(device);
        var scl = SclDataTypeGenerator.Generate(ir);

        Assert.Contains("TYPE \"UDT_Motor\"", scl);
        Assert.Contains("Start : Bool;", scl);
        Assert.Contains("Running : Bool;", scl);
        Assert.Contains("Speed : Real;", scl);
        Assert.Contains("END_TYPE", scl);
    }

    [Fact]
    public void GeneratesExpectedValveType()
    {
        var device = new AutomationDevice(
            "Valve",
            new[]
            {
                new AutomationField("Open", AutomationType.Bool),
                new AutomationField("Close", AutomationType.Bool),
                new AutomationField("Opened", AutomationType.Bool),
                new AutomationField("Closed", AutomationType.Bool),
                new AutomationField("Fault", AutomationType.Bool)
            });

        var ir = AutomationCompiler.Compile(device);
        var scl = SclDataTypeGenerator.Generate(ir);

        Assert.Contains("TYPE \"UDT_Valve\"", scl);
        Assert.Contains("Open : Bool;", scl);
        Assert.Contains("Close : Bool;", scl);
        Assert.Contains("Opened : Bool;", scl);
        Assert.Contains("Closed : Bool;", scl);
        Assert.Contains("Fault : Bool;", scl);
        Assert.Contains("END_TYPE", scl);
    }
}
