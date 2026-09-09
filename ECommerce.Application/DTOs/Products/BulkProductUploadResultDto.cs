namespace ECommerce.Application.DTOs.Products;

public class BulkProductUploadResultDto
{
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int FailedRows { get; set; }
    public List<string> Errors { get; set; } = new();
}