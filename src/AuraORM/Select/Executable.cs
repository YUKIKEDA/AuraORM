namespace AuraORM;

/// <summary>
/// The mode of a statement that can be run: it comes from a <see cref="Database{TDialect}"/> or a
/// <see cref="Transaction{TDialect}"/>.
/// </summary>
public sealed class Executable : ExecutionMode
{
    private Executable() { }
}
