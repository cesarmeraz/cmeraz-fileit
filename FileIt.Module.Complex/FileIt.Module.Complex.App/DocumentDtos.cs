namespace FileIt.Module.Complex.App;

/// <summary>
/// Request body for POST /api/documents.
/// </summary>
public sealed class CreateDocumentRequest
{
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain";
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response body for POST /api/documents (201 Created) and
/// GET /api/documents/{id} (200 OK).
/// </summary>
public sealed class DocumentResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
}

/// <summary>
/// Wraps the list endpoint payload so we can include paging metadata
/// alongside items without leaking domain shape.
/// </summary>
public sealed class DocumentListResponse
{
    public IReadOnlyList<DocumentResponse> Items { get; set; } = Array.Empty<DocumentResponse>();
    public int Skip { get; set; }
    public int Take { get; set; }
    public bool IncludeDeleted { get; set; }
    public string? NameFilter { get; set; }
}

/// <summary>
/// Bulk export payload. Same shape as list but no paging.
/// </summary>
public sealed class DocumentExportResponse
{
    public IReadOnlyList<DocumentResponse> Documents { get; set; } = Array.Empty<DocumentResponse>();
    public DateTime ExportedAtUtc { get; set; }
    public int Count { get; set; }
}
