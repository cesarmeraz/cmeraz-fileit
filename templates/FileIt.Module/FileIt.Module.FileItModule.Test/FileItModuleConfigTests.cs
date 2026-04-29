namespace FileIt.Module.FileItModule.Test;

[TestClass]
public class FileItModuleConfigTests
{
    [TestMethod]
    public void Defaults_AreEmptyStrings()
    {
        var sut = new FileIt.Module.FileItModule.App.FileItModuleConfig();

        Assert.AreEqual(string.Empty, sut.QueueName);
        Assert.AreEqual(string.Empty, sut.SourceContainer);
        Assert.AreEqual(string.Empty, sut.WorkingContainer);
        Assert.AreEqual(string.Empty, sut.FinalContainer);
    }
}
