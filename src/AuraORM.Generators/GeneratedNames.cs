namespace AuraORM.Generators;

/// <summary>
/// The identifiers that generated code uses. A member name is always written verbatim (<c>@name</c>), so a member named like a
/// keyword compiles, and is reached through <c>this.@name</c>, so a parameter cannot hide it. Locals, fields, and parameters
/// are numbered and start with <see cref="ReservedPrefix"/>, which a member name cannot (AURA0104).
/// </summary>
internal static class GeneratedNames
{
    public const string ReservedPrefix = "__aura";

    public const string TableParameter = ReservedPrefix + "Table";

    public const string ReaderParameter = ReservedPrefix + "Reader";

    public static string Id(string memberName) => "@" + memberName;

    public static string This(string memberName) => "this.@" + memberName;

    public static string Local(int index) => ReservedPrefix + "Value" + index;

    public static string Field(int index) => ReservedPrefix + "Field" + index;
}
