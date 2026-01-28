using System.Runtime.CompilerServices;
using System.Text;
using FileIt.Domain.Entities;

namespace FileIt.Domain.Interfaces;

public interface IHandleFiles
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    Task GetFileAsync(string filename, string location);

    /// <summary>
    /// Moves a blob from one container to another
    /// </summary>
    /// <param name="name"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    Task MoveAsync(string filename, string source, string destination);

    Task UploadAsync(Stream content, string filename, string location);
}
