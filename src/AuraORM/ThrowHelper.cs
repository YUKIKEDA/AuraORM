using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace AuraORM;

/// <summary>
/// Throws the exceptions of the library, so that callers stay small (<c>.cursor/rules/perf.mdc</c>).
/// </summary>
[StackTraceHidden]
internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowTooManyParameters(int count, int max, string dialect) =>
        throw new ArgumentException(
            string.Format(
                CultureInfo.InvariantCulture,
                "The statement has {0} parameters, which is more than the {1} that {2} accepts in one statement.",
                count,
                max,
                dialect
            )
        );

    public static NotSupportedException UnsupportedDialect(Type type) =>
        new(string.Format(CultureInfo.InvariantCulture, "{0} is not a dialect that AuraORM supports.", type.FullName));

    [DoesNotReturn]
    public static void ThrowArgumentOutOfRange(string paramName, string message) => throw new ArgumentOutOfRangeException(paramName, message);
}
