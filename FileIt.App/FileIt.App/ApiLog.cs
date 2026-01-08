using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FileIt.Common.Data;

namespace FileIt.App
{
    public class ApiLog : IAuditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? ClientRequestId { get; set; }
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
