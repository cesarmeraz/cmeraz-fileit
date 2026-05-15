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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task GetFileAsync(string filename, string location, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string filename, string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a blob from one container to another
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task MoveAsync(string filename, string source, string destination, CancellationToken cancellationToken = default);

    Task UploadAsync(Stream content, string filename, string location, CancellationToken cancellationToken = default);
}
