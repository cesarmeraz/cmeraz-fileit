// This is the message payload we put on the service bus queue
// when a new GL Account CSV file is ready to be transformed.
// It carries just enough info for the transform handler to find the file.
namespace FileIt.Module.DataFlow.App
{
    public class DataFlowMessage
    {
        // The name of the blob file sitting in the working container
        // waiting to be parsed and transformed
        public string BlobName { get; set; } = string.Empty;

        // The number of rows we found in the CSV when we first picked it up
        // useful for validation later — if we transformed fewer rows than we ingested, something went wrong
        public int RowCount { get; set; }
    }
}
