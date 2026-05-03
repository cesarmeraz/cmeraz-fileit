using FileIt.Domain.Entities.Complex;
using FileIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.Data;

[TestClass]
public class TestComplexDocumentRepo
{
    public required CommonDbContext _db;
    public required Mock<ILogger<ComplexDocumentRepo>> _loggerMock;
    public required ComplexDocumentRepo target;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CommonDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new CommonDbContext(options);
        _loggerMock = new Mock<ILogger<ComplexDocumentRepo>>();
        target = new ComplexDocumentRepo(_db, _loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private static ComplexDocument BuildDoc(
        string name = "doc.txt",
        string contentType = "text/plain",
        Guid? publicId = null,
        string createdBy = "tester",
        DateTime? deletedUtc = null)
    {
        return new ComplexDocument
        {
            PublicId = publicId ?? Guid.NewGuid(),
            Name = name,
            ContentType = contentType,
            SizeBytes = 100,
            Content = "test content",
            CreatedBy = createdBy,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
            DeletedUtc = deletedUtc,
        };
    }

    [TestMethod]
    public async Task AddAsync_HappyPath_PersistsRow()
    {
        var doc = BuildDoc();

        var result = await target.AddAsync(doc);

        Assert.AreNotEqual(0L, result.DocumentId);
        var fetched = await target.GetByPublicIdAsync(doc.PublicId);
        Assert.IsNotNull(fetched);
        Assert.AreEqual(doc.Name, fetched!.Name);
    }

    [TestMethod]
    public async Task GetByPublicIdAsync_Existing_ReturnsRow()
    {
        var doc = BuildDoc(name: "specific.txt");
        await target.AddAsync(doc);

        var found = await target.GetByPublicIdAsync(doc.PublicId);

        Assert.IsNotNull(found);
        Assert.AreEqual("specific.txt", found!.Name);
    }

    [TestMethod]
    public async Task GetByPublicIdAsync_Missing_ReturnsNull()
    {
        var found = await target.GetByPublicIdAsync(Guid.NewGuid());

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task ListAsync_NoFilter_ReturnsAllNonDeleted()
    {
        await target.AddAsync(BuildDoc(name: "a.txt"));
        await target.AddAsync(BuildDoc(name: "b.txt"));
        await target.AddAsync(BuildDoc(name: "c.txt", deletedUtc: DateTime.UtcNow));

        var list = await target.ListAsync(null, skip: 0, take: 100, includeDeleted: false);

        Assert.AreEqual(2, list.Count);
    }

    [TestMethod]
    public async Task ListAsync_IncludeDeleted_ReturnsAll()
    {
        await target.AddAsync(BuildDoc(name: "a.txt"));
        await target.AddAsync(BuildDoc(name: "b.txt", deletedUtc: DateTime.UtcNow));

        var list = await target.ListAsync(null, skip: 0, take: 100, includeDeleted: true);

        Assert.AreEqual(2, list.Count);
    }

    [TestMethod]
    public async Task ListAsync_Pagination_RespectsSkipAndTake()
    {
        for (int i = 0; i < 5; i++)
        {
            await target.AddAsync(BuildDoc(name: $"doc-{i}.txt"));
        }

        var page = await target.ListAsync(null, skip: 2, take: 2, includeDeleted: false);

        Assert.AreEqual(2, page.Count);
    }

    [TestMethod]
    public async Task ListAsync_OrdersByModifiedUtcDescending()
    {
        var older = BuildDoc(name: "older.txt");
        older.ModifiedUtc = DateTime.UtcNow.AddHours(-2);
        var newer = BuildDoc(name: "newer.txt");
        newer.ModifiedUtc = DateTime.UtcNow;
        await target.AddAsync(older);
        await target.AddAsync(newer);

        var list = await target.ListAsync(null, skip: 0, take: 10, includeDeleted: false);

        Assert.AreEqual("newer.txt", list[0].Name);
        Assert.AreEqual("older.txt", list[1].Name);
    }

    [TestMethod]
    public async Task SoftDeleteAsync_Existing_StampsDeletedUtcAndReturnsTrue()
    {
        var doc = BuildDoc();
        await target.AddAsync(doc);

        var result = await target.SoftDeleteAsync(doc.PublicId);

        Assert.IsTrue(result);
        var fetched = await target.GetByPublicIdAsync(doc.PublicId);
        Assert.IsNotNull(fetched);
        Assert.IsNotNull(fetched!.DeletedUtc);
    }

    [TestMethod]
    public async Task SoftDeleteAsync_Missing_ReturnsFalse()
    {
        var result = await target.SoftDeleteAsync(Guid.NewGuid());

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task SoftDeleteAsync_AlreadyDeleted_ReturnsFalse()
    {
        var doc = BuildDoc(deletedUtc: DateTime.UtcNow);
        await target.AddAsync(doc);

        var result = await target.SoftDeleteAsync(doc.PublicId);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ExportAsync_OrdersByCreatedUtcAscending()
    {
        var older = BuildDoc(name: "older.txt");
        older.CreatedUtc = DateTime.UtcNow.AddHours(-2);
        var newer = BuildDoc(name: "newer.txt");
        newer.CreatedUtc = DateTime.UtcNow;
        await target.AddAsync(older);
        await target.AddAsync(newer);

        var list = await target.ExportAsync(includeDeleted: false);

        Assert.AreEqual("older.txt", list[0].Name);
        Assert.AreEqual("newer.txt", list[1].Name);
    }

    [TestMethod]
    public async Task ExportAsync_ExcludesDeletedByDefault()
    {
        await target.AddAsync(BuildDoc(name: "a.txt"));
        await target.AddAsync(BuildDoc(name: "b.txt", deletedUtc: DateTime.UtcNow));

        var list = await target.ExportAsync(includeDeleted: false);

        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("a.txt", list[0].Name);
    }
}
