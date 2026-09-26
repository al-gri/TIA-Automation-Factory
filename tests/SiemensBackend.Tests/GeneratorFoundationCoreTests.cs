using System;
using Xunit;
using TiaAutomationFactory.Domain;
using TiaAutomationFactory.PlcCompiler;
using TiaAutomationFactory.SiemensBackend;

namespace TiaAutomationFactory.SiemensBackend.Tests;

public sealed class GeneratorFoundationCoreTests
{
    [Fact]
    public void CanonicalIdentity_EquivalentModels_ProduceSameHash()
    {
        var device1 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool),
                new AutomationField("Running", AutomationType.Bool),
                new AutomationField("Speed", AutomationType.Real)
            });

        var device2 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool),
                new AutomationField("Running", AutomationType.Bool),
                new AutomationField("Speed", AutomationType.Real)
            });

        var ir1 = AutomationCompiler.Compile(device1);
        var ir2 = AutomationCompiler.Compile(device2);

        var identity1 = CanonicalInputIdentity.Compute(ir1);
        var identity2 = CanonicalInputIdentity.Compute(ir2);

        Assert.Equal(identity1, identity2);
        Assert.Equal(64, identity1.Length);
    }

    [Fact]
    public void CanonicalIdentity_FieldOrderChange_ProducesDifferentHash()
    {
        var device1 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool),
                new AutomationField("Running", AutomationType.Bool),
                new AutomationField("Speed", AutomationType.Real)
            });

        var device2 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Running", AutomationType.Bool),
                new AutomationField("Start", AutomationType.Bool),
                new AutomationField("Speed", AutomationType.Real)
            });

        var ir1 = AutomationCompiler.Compile(device1);
        var ir2 = AutomationCompiler.Compile(device2);

        var identity1 = CanonicalInputIdentity.Compute(ir1);
        var identity2 = CanonicalInputIdentity.Compute(ir2);

        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void CanonicalIdentity_FieldNameChange_ProducesDifferentHash()
    {
        var device1 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool),
                new AutomationField("Running", AutomationType.Bool)
            });

        var device2 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Begin", AutomationType.Bool),
                new AutomationField("Running", AutomationType.Bool)
            });

        var ir1 = AutomationCompiler.Compile(device1);
        var ir2 = AutomationCompiler.Compile(device2);

        var identity1 = CanonicalInputIdentity.Compute(ir1);
        var identity2 = CanonicalInputIdentity.Compute(ir2);

        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void CanonicalIdentity_FieldTypeChange_ProducesDifferentHash()
    {
        var device1 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Speed", AutomationType.Real)
            });

        var device2 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Speed", AutomationType.Int)
            });

        var ir1 = AutomationCompiler.Compile(device1);
        var ir2 = AutomationCompiler.Compile(device2);

        var identity1 = CanonicalInputIdentity.Compute(ir1);
        var identity2 = CanonicalInputIdentity.Compute(ir2);

        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void CanonicalIdentity_DeviceNameChange_ProducesDifferentHash()
    {
        var device1 = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool)
            });

        var device2 = new AutomationDevice(
            "Pump",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool)
            });

        var ir1 = AutomationCompiler.Compile(device1);
        var ir2 = AutomationCompiler.Compile(device2);

        var identity1 = CanonicalInputIdentity.Compute(ir1);
        var identity2 = CanonicalInputIdentity.Compute(ir2);

        Assert.NotEqual(identity1, identity2);
    }

    [Fact]
    public void CanonicalIdentity_IsLowercaseHex()
    {
        var device = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool)
            });

        var ir = AutomationCompiler.Compile(device);
        var identity = CanonicalInputIdentity.Compute(ir);

        Assert.All(identity, c => Assert.True(char.IsLower(c) || char.IsDigit(c)));
    }

    [Fact]
    public void SiemensNamePolicy_ValidUdtName_Passes()
    {
        SiemensNamePolicy.ValidateUdtName("Motor");
        SiemensNamePolicy.ValidateUdtName("_Motor");
        SiemensNamePolicy.ValidateUdtName("Motor_123");
        SiemensNamePolicy.ValidateUdtName("M");
    }

    [Fact]
    public void SiemensNamePolicy_NullUdtName_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateUdtName(null!));
        Assert.Equal(SiemensNamePolicyErrorCode.UdtNameNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_EmptyUdtName_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateUdtName(""));
        Assert.Equal(SiemensNamePolicyErrorCode.UdtNameNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_WhitespaceUdtName_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateUdtName("   "));
        Assert.Equal(SiemensNamePolicyErrorCode.UdtNameNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_UdtNameStartingWithDigit_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateUdtName("123Motor"));
        Assert.Equal(SiemensNamePolicyErrorCode.UdtNameInvalidCharacters, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_UdtNameWithInvalidChars_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateUdtName("Motor-Test"));
        Assert.Equal(SiemensNamePolicyErrorCode.UdtNameInvalidCharacters, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_ValidFieldNames_Pass()
    {
        SiemensNamePolicy.ValidateFieldNames(new[] { "Start", "Running", "Speed" });
        SiemensNamePolicy.ValidateFieldNames(new[] { "_field", "field_123", "Field" });
    }

    [Fact]
    public void SiemensNamePolicy_NullFieldName_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateFieldNames(new[] { "Start", null!, "Speed" }));
        Assert.Equal(SiemensNamePolicyErrorCode.FieldNameNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_EmptyFieldName_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateFieldNames(new[] { "Start", "", "Speed" }));
        Assert.Equal(SiemensNamePolicyErrorCode.FieldNameNullOrEmpty, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_FieldNameWithInvalidChars_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateFieldNames(new[] { "Start", "Run-ning", "Speed" }));
        Assert.Equal(SiemensNamePolicyErrorCode.FieldNameInvalidCharacters, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_CaseInsensitiveDuplicateFieldNames_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateFieldNames(new[] { "Start", "start", "Speed" }));
        Assert.Equal(SiemensNamePolicyErrorCode.FieldNameCaseInsensitiveDuplicate, ex.ErrorCode);
    }

    [Fact]
    public void SiemensNamePolicy_CaseInsensitiveDuplicateDifferentCase_Throws()
    {
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SiemensNamePolicy.ValidateFieldNames(new[] { "START", "Start", "Speed" }));
        Assert.Equal(SiemensNamePolicyErrorCode.FieldNameCaseInsensitiveDuplicate, ex.ErrorCode);
    }

    [Fact]
    public void SclDataTypeGenerator_ValidMotor_GeneratesExpectedOutput()
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
    public void SclDataTypeGenerator_ValidValveConfig_GeneratesExpectedOutput()
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

    [Fact]
    public void SclDataTypeGenerator_InvalidUdtName_ThrowsSiemensNamePolicyException()
    {
        var device = new AutomationDevice(
            "123Invalid",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool)
            });

        var ir = AutomationCompiler.Compile(device);
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SclDataTypeGenerator.Generate(ir));
        Assert.Equal(SiemensNamePolicyErrorCode.UdtNameInvalidCharacters, ex.ErrorCode);
    }

    [Fact]
    public void SclDataTypeGenerator_InvalidFieldName_ThrowsSiemensNamePolicyException()
    {
        var device = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Run-ning", AutomationType.Bool)
            });

        var ir = AutomationCompiler.Compile(device);
        var ex = Assert.Throws<SiemensNamePolicyException>(() => SclDataTypeGenerator.Generate(ir));
        Assert.Equal(SiemensNamePolicyErrorCode.FieldNameInvalidCharacters, ex.ErrorCode);
    }

    [Fact]
    public void SclDataTypeGenerator_CaseInsensitiveDuplicateFieldNames_ThrowsAtCompilerLevel()
    {
        var device = new AutomationDevice(
            "Motor",
            new[]
            {
                new AutomationField("Start", AutomationType.Bool),
                new AutomationField("start", AutomationType.Bool)
            });

        var ex = Assert.Throws<InvalidOperationException>(() => AutomationCompiler.Compile(device));
        Assert.Contains("Duplicate field", ex.Message);
    }
}