using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AuraORM;

/// <summary>
/// Writes SQL text into a buffer rented from <see cref="ArrayPool{T}.Shared"/>.
/// </summary>
internal sealed class SqlWriter : IDisposable
{
    private char[] _buffer;
    private int _length;

    public SqlWriter()
    {
        _buffer = ArrayPool<char>.Shared.Rent(256);
    }

    public void Append(char value)
    {
        if (_length == _buffer.Length)
        {
            Grow(1);
        }

        _buffer[_length++] = value;
    }

    public void Append(string value)
    {
        Append(value.AsSpan());
    }

    public void Append(ReadOnlySpan<char> value)
    {
        if (_length + value.Length > _buffer.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_buffer.AsSpan(_length));
        _length += value.Length;
    }

    public void Append(int value)
    {
        Span<char> digits = stackalloc char[11];
        value.TryFormat(digits, out int written, provider: CultureInfo.InvariantCulture);
        Append(digits[..written]);
    }

    public override string ToString() => new(_buffer, 0, _length);

    public void Dispose()
    {
        char[] buffer = _buffer;
        _buffer = [];
        _length = 0;
        if (buffer.Length > 0)
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additional)
    {
        char[] larger = ArrayPool<char>.Shared.Rent(Math.Max(_buffer.Length * 2, _length + additional));
        _buffer.AsSpan(0, _length).CopyTo(larger);
        ArrayPool<char>.Shared.Return(_buffer);
        _buffer = larger;
    }
}
