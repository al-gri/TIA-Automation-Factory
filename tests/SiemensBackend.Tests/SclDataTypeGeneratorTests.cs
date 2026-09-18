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
    public void GeneratesTimeTypeForValveConfig()
    {
        var device = new AutomationDevice(
            "ValveConfig",
            new[]
            {
                new AutomationField("Timeout", AutomationType.Time),
                new AutomationField("Mode", AutomationType.Int),
                new AutomationField("Enable", AutomationType.Bool),
                new AutomationField("CommandWork", AutomationType.Bool)
            });

        var ir = AutomationCompiler.Compile(device);
        var scl = SclDataTypeGenerator.Generate(ir);

        Assert.Contains("TYPE \"UDT_ValveConfig\"", scl);
        Assert.Contains("Timeout : Time;", scl);
        Assert.Contains("Mode : Int;", scl);
        Assert.Contains("Enable : Bool;", scl);
        Assert.Contains("CommandWork : Bool;", scl);
        Assert.Contains("END_TYPE", scl);
    }
}
