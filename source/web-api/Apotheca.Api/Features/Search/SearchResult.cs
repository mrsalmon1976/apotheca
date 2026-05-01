namespace Apotheca.Api.Features.Search;

public record SearchResult
{
    public required string ReferenceId   { get; init; }
    public required string ReferenceType { get; init; }
    public required string Title         { get; init; }
    public string?         Snippet       { get; init; }
    public string?         ProjectId     { get; init; }
}
