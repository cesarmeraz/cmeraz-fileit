using FileIt.Domain.Entities.Complex;
using FileIt.Domain.Interfaces;
using FileIt.Module.Complex.App.Errors;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App.Commands;

public sealed record CreateDocumentResult(
    bool Success,
    DocumentResponse? Document,
    ProblemDetails? Problem);

public interface ICreateDocumentCommand
{
    Task<CreateDocumentResult> ExecuteAsync(
        CreateDocumentRequest request,
        CancellationToken cancellationToken = default);
}

public class CreateDocumentCommand : ICreateDocumentCommand
{
    private const int MaxNameLength = 260;
    private const int MaxContentBytes = 10 * 1024 * 1024; // 10 MiB sanity cap

    private readonly IComplexDocumentRepo _repo;
    private readonly ILogger<CreateDocumentCommand> _logger;

    public CreateDocumentCommand(
        IComplexDocumentRepo repo,
        ILogger<CreateDocumentCommand> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<CreateDocumentResult> ExecuteAsync(
        CreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate
        var fieldErrors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            fieldErrors["name"] = new[] { "Name is required." };
        }
        else if (request.Name.Length > MaxNameLength)
        {
            fieldErrors["name"] = new[] { $"Name length exceeds maximum of {MaxNameLength}." };
        }

        if (string.IsNullOrEmpty(request.ContentType))
        {
            fieldErrors["contentType"] = new[] { "ContentType is required." };
        }

        if (request.Content is null)
        {
            fieldErrors["content"] = new[] { "Content is required (use empty string for empty body)." };
        }
        else
        {
            // UTF-8 byte length check
            var byteLen = System.Text.Encoding.UTF8.GetByteCount(request.Content);
            if (byteLen > MaxContentBytes)
            {
                _logger.LogWarning(ComplexEvents.ContentTooLarge,
                    "Content too large: {Bytes} bytes > {Max}", byteLen, MaxContentBytes);
                return new CreateDocumentResult(
                    Success: false,
                    Document: null,
                    Problem: ProblemDetailsFactory.PayloadTooLarge(
                        $"Content exceeds maximum size of {MaxContentBytes} bytes."));
            }
        }

        if (fieldErrors.Count > 0)
        {
            _logger.LogWarning(ComplexEvents.InvalidRequest,
                "CreateDocument validation failed: {@FieldErrors}", fieldErrors);
            return new CreateDocumentResult(
                Success: false,
                Document: null,
                Problem: ProblemDetailsFactory.BadRequest(
                    "Request validation failed.",
                    errors: fieldErrors));
        }

        var entity = new ComplexDocument
        {
            PublicId = Guid.NewGuid(),
            Name = request.Name,
            ContentType = request.ContentType,
            Content = request.Content,
            SizeBytes = System.Text.Encoding.UTF8.GetByteCount(request.Content ?? string.Empty),
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
            CreatedBy = "complex-api",
        };

        var saved = await _repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(ComplexEvents.DocumentCreated,
            "Document created: {PublicId} ({Name}, {SizeBytes} bytes)",
            saved.PublicId, saved.Name, saved.SizeBytes);

        return new CreateDocumentResult(
            Success: true,
            Document: ToResponse(saved),
            Problem: null);
    }

    private static DocumentResponse ToResponse(ComplexDocument d) => new()
    {
        Id = d.PublicId,
        Name = d.Name,
        ContentType = d.ContentType,
        SizeBytes = d.SizeBytes,
        Content = d.Content,
        CreatedUtc = d.CreatedUtc,
        ModifiedUtc = d.ModifiedUtc,
    };
}
