using FileIt.Domain.Entities.Complex;
using FileIt.Domain.Interfaces;
using FileIt.Module.Complex.App;
using FileIt.Module.Complex.App.Commands;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileIt.Module.Complex.Test;

[TestClass]
public class CreateDocumentCommandTests
{
    [TestMethod]
    public async Task ValidRequest_Succeeds()
    {
        var repo = new FakeRepo();
        var sut = new CreateDocumentCommand(repo, NullLogger<CreateDocumentCommand>.Instance);

        var result = await sut.ExecuteAsync(new CreateDocumentRequest
        {
            Name = "doc.txt",
            ContentType = "text/plain",
            Content = "hello world",
        });

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Document);
        Assert.AreEqual("doc.txt", result.Document!.Name);
        Assert.AreEqual(11, result.Document.SizeBytes);
        Assert.IsNull(result.Problem);
    }

    [TestMethod]
    public async Task EmptyName_BadRequest()
    {
        var repo = new FakeRepo();
        var sut = new CreateDocumentCommand(repo, NullLogger<CreateDocumentCommand>.Instance);

        var result = await sut.ExecuteAsync(new CreateDocumentRequest
        {
            Name = "",
            Content = "x",
        });

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Problem);
        Assert.AreEqual(400, result.Problem!.Status);
        Assert.IsNotNull(result.Problem.Errors);
        Assert.IsTrue(result.Problem.Errors!.ContainsKey("name"));
    }

    [TestMethod]
    public async Task NameTooLong_BadRequest()
    {
        var repo = new FakeRepo();
        var sut = new CreateDocumentCommand(repo, NullLogger<CreateDocumentCommand>.Instance);

        var result = await sut.ExecuteAsync(new CreateDocumentRequest
        {
            Name = new string('x', 261),
            Content = "ok",
        });

        Assert.IsFalse(result.Success);
        Assert.AreEqual(400, result.Problem!.Status);
    }

    [TestMethod]
    public async Task ContentTooLarge_PayloadTooLarge()
    {
        var repo = new FakeRepo();
        var sut = new CreateDocumentCommand(repo, NullLogger<CreateDocumentCommand>.Instance);

        // 11 MiB
        var oversizeContent = new string('a', 11 * 1024 * 1024);
        var result = await sut.ExecuteAsync(new CreateDocumentRequest
        {
            Name = "huge.txt",
            Content = oversizeContent,
        });

        Assert.IsFalse(result.Success);
        Assert.AreEqual(413, result.Problem!.Status);
    }

    private sealed class FakeRepo : IComplexDocumentRepo
    {
        public Task<ComplexDocument> AddAsync(ComplexDocument document, CancellationToken ct = default)
        {
            document.DocumentId = 1;
            return Task.FromResult(document);
        }
        public Task<ComplexDocument?> GetByPublicIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<ComplexDocument?>(null);
        public Task<IReadOnlyList<ComplexDocument>> ListAsync(string? f, int s, int t, bool d, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ComplexDocument>>(Array.Empty<ComplexDocument>());
        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);
        public Task<IReadOnlyList<ComplexDocument>> ExportAsync(bool d, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ComplexDocument>>(Array.Empty<ComplexDocument>());
    }
}
