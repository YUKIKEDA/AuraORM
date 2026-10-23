using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace AuraORM.Generators;

/// <summary>
/// A diagnostic that can be cached in the incremental pipeline.
/// </summary>
internal sealed record DiagnosticInfo(string Id, LocationInfo? Location, EquatableArray<string> Arguments)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] arguments) =>
        new(descriptor.Id, LocationInfo.From(location), new EquatableArray<string>(ImmutableArray.Create(arguments)));

    public Diagnostic ToDiagnostic() => Diagnostic.Create(Diagnostics.ById(Id), Location?.ToLocation(), Arguments.Cast<object>().ToArray());
}
