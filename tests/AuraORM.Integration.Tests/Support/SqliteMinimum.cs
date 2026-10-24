#if AURA_SQLITE_MINIMUM
using System.Runtime.CompilerServices;

namespace AuraORM.Integration.Tests;

/// <summary>
/// Sets the SQLite provider in a build with <c>-p:AuraSqliteMinimum=true</c>, which has no bundle to set it
/// (<c>docs/design.ja.md</c> §17).
/// </summary>
internal static class SqliteMinimum
{
#pragma warning disable CA2255 // The initializer is in a test program, not a library, and must run before the first connection.
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void SetProvider() => SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
}
#endif
