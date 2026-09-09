using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces;

public interface IBulkProductUploadService
{
    Task<BulkProductUploadResultDto> UploadAsync(Stream fileStream);
    byte[] GenerateTemplate();
}