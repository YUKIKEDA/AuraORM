namespace System.Runtime.CompilerServices;

/// <summary>
/// Lets records and <c>init</c> accessors compile for netstandard2.0, which does not define this type.
/// </summary>
internal static class IsExternalInit { }
