using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AuraORM.Generators;

/// <summary>
/// A source location that can be cached in the incremental pipeline (a <see cref="Location"/> holds the syntax tree).
/// </summary>
internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public static LocationInfo? From(Location? location) =>
        location is { IsInSource: true } ? new LocationInfo(location.SourceTree!.FilePath, location.SourceSpan, location.GetLineSpan().Span) : null;

    public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
}
