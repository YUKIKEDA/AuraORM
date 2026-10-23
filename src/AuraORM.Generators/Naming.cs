using System.Text;

namespace AuraORM.Generators;

/// <summary>
/// The column naming rules of <c>docs/design.ja.md</c> §3.2.
/// </summary>
internal static class Naming
{
    /// <summary>
    /// Converts a member name to lower snake case: <c>CreatedAt</c> → <c>created_at</c>, <c>HTTPCode</c> → <c>http_code</c>.
    /// </summary>
    /// <param name="name">The member name.</param>
    /// <returns>The column name.</returns>
    public static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c))
            {
                char previous = name[i - 1];
                bool afterLowerOrDigit = char.IsLower(previous) || char.IsDigit(previous);
                bool endOfUpperRun = char.IsUpper(previous) && i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (afterLowerOrDigit || endOfUpperRun)
                {
                    builder.Append('_');
                }
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
