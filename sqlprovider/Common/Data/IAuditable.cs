namespace FileIt.SqlProvider.Data
{
    public interface IAuditable
    {
        int Id { get; set; }
        DateTime? CreatedOn { get; set; }
        DateTime? ModifiedOn { get; set; }
    }
}
