using System.Reflection;

namespace AuraORM.Integration.Tests;

public sealed class RequiredDatabaseTests
{
    /// <summary>
    /// Every database that a CI job names in <c>AURA_TEST_REQUIRE</c> has test classes in its xUnit collection, so a job
    /// cannot pass with none of that database's tests (a renamed class, a changed collection).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <c>AURA_TEST_REQUIRE</c> as the job sets it; empty locally, where the test checks nothing.
    ///
    /// Steps:
    /// - For each name, find the test classes of this assembly in the collection of that name.
    ///
    /// Expected:
    /// - At least one class for each name.
    /// </remarks>
    [Fact]
    public void RequiredDatabases_HaveTestClasses()
    {
        string[] required = (Environment.GetEnvironmentVariable("AURA_TEST_REQUIRE") ?? string.Empty).Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        foreach (string name in required)
        {
            IEnumerable<Type> classes = typeof(RequiredDatabaseTests)
                .Assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<CollectionAttribute>()?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true);
            Assert.True(classes.Any(), $"AURA_TEST_REQUIRE names {name}, but no test class is in the collection {name}.");
        }
    }
}
