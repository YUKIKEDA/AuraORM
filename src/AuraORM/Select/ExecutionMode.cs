namespace AuraORM;

/// <summary>
/// Whether a statement can be run: <see cref="Executable"/> for statements from a <see cref="Database{TDialect}"/> or a
/// <see cref="Transaction{TDialect}"/>, <see cref="SqlOnly"/> for statements from a <see cref="SqlBuilder{TDialect}"/>, which
/// has no connection. The run methods exist only for <see cref="Executable"/> statements (<c>docs/design.ja.md</c> §12.3).
/// </summary>
/// <remarks>
/// The set is closed: only AuraORM declares modes.
/// </remarks>
public abstract class ExecutionMode
{
    private protected ExecutionMode() { }
}
