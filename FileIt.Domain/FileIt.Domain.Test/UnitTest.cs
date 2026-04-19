namespace FileIt.Domain.Test;

public class UnitTest
{
    [Test]
    public async Task TestMethod1()
    {
        // Arrange / Act
        var result = 1 + 1;

        // Assert
        await Assert.That(result).IsEqualTo(2);
    }
}
