namespace ECommerce.Application.DTOs.Products;

public class BulkProductUploadResultDto
{
    public int TotalRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public int BatchSize { get; set; }
    public int BatchesProcessed { get; set; }
    public List<string> Errors { get; set; } = new();
}