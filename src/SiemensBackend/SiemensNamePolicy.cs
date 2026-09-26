using System.Collections.Generic;

namespace TiaAutomationFactory.SiemensBackend;

public static class SiemensNamePolicy
{
    public static void ValidateUdtName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new SiemensNamePolicyException(SiemensNamePolicyErrorCode.UdtNameNullOrEmpty, "UDT name cannot be null or empty.");

        if (!IsValidSiemensIdentifier(name))
            throw new SiemensNamePolicyException(SiemensNamePolicyErrorCode.UdtNameInvalidCharacters, $"UDT name '{name}' contains invalid characters for Siemens identifier.");
    }

    public static void ValidateFieldNames(IReadOnlyList<string> fieldNames)
    {
        ArgumentNullException.ThrowIfNull(fieldNames);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in fieldNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new SiemensNamePolicyException(SiemensNamePolicyErrorCode.FieldNameNullOrEmpty, "Field name cannot be null or empty.");

            if (!IsValidSiemensIdentifier(name))
                throw new SiemensNamePolicyException(SiemensNamePolicyErrorCode.FieldNameInvalidCharacters, $"Field name '{name}' contains invalid characters for Siemens identifier.");

            if (!seen.Add(name))
                throw new SiemensNamePolicyException(SiemensNamePolicyErrorCode.FieldNameCaseInsensitiveDuplicate, $"Duplicate field name (case-insensitive): '{name}'.");
        }
    }

    private static bool IsValidSiemensIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (name.Length > 128)
            return false;

        var firstChar = name[0];
        if (!char.IsLetter(firstChar) && firstChar != '_')
            return false;

        foreach (var c in name.AsSpan(1))
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }
}

public enum SiemensNamePolicyErrorCode
{
    UdtNameNullOrEmpty = 1001,
    UdtNameInvalidCharacters = 1002,
    FieldNameNullOrEmpty = 2001,
    FieldNameInvalidCharacters = 2002,
    FieldNameCaseInsensitiveDuplicate = 2003
}

public sealed class SiemensNamePolicyException : Exception
{
    public SiemensNamePolicyErrorCode ErrorCode { get; }

    public SiemensNamePolicyException(SiemensNamePolicyErrorCode errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}