using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZadCw9.Models.DTOs;
using ZadCw9.Services;

namespace ZadCw9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct(AddProductDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var id = await _warehouseService.AddProductAsync(dto);
            return Ok(id);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("procedura")]
    public async Task<IActionResult> AddProductProcedure(AddProductDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var id = await _warehouseService.AddProductProcedureAsync(dto);
            return Ok(id);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}