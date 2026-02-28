namespace FileIt.Domain.Interfaces;

public interface IAuditable
{
    int Id { get; set; }
    DateTime? CreatedOn { get; set; }
    DateTime? ModifiedOn { get; set; }
}
