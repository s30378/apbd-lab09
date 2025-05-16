using ZadCw9.Models.DTOs;

namespace ZadCw9.Services;

public interface IWarehouseService
{
    Task<int> AddProductAsync(AddProductDto dto);
    Task<int> AddProductProcedureAsync(AddProductDto dto);
}