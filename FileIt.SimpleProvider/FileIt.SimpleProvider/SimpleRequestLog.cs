using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FileIt.Common.Data;

namespace FileIt.SimpleProvider
{
    public class SimpleRequestLog : IAuditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Environment { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public string? Agent { get; set; }
        public string? BlobName { get; set; }
        public string? ClientRequestId { get; set; }
        public string? Comment { get; set; }
        public int ApiId { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
