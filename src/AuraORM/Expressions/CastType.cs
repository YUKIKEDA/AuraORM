namespace AuraORM;

/// <summary>
/// A target type of <c>CAST</c>. The instances are the properties of <see cref="SqlType"/>.
/// </summary>
/// <typeparam name="T">The CLR type that the result of the cast is read as.</typeparam>
public sealed class CastType<T>
{
    internal CastType(CastTarget target)
    {
        Target = target;
    }

    internal CastTarget Target { get; }
}
