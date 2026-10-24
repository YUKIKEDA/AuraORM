namespace AuraORM;

/// <summary>
/// The messages of the <c>[Obsolete(error: true)]</c> members of <c>UPDATE</c> and <c>DELETE</c>, which exist only so that a
/// call that is not accepted fails with CS0619 and says how to fix it (<c>docs/design.ja.md</c> §7, §14.1).
/// </summary>
internal static class MutationMessages
{
    public const string OptionalCondition =
        "UPDATE and DELETE do not take a condition that may be empty (the And or Or of a list), because an empty one would change every row; check the list, and write AllRows() for every row.";

    public const string OptionalUpsertCondition =
        "DO UPDATE does not take a condition that may be empty (the And or Or of a list); check the list, and leave out Where to update every conflicting row.";
}
