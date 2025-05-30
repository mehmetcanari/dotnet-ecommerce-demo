using Asp.Versioning;
using ECommerce.Application.Abstract.Service;
using ECommerce.Application.DTO.Request.Category;
using ECommerce.Application.Validations.Attribute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/categories")]
[Authorize(Roles = "Admin")]
[ApiVersion("1.0")]
public class AdminCategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public AdminCategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequestDto request)
    {
        var result = await _categoryService.CreateCategoryAsync(request);
        return Ok(new { message = "Category created successfully", data = result });
    }

    [HttpDelete("delete/{id}")]
    [ValidateId]
    public async Task<IActionResult> DeleteCategory([FromRoute] int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);
        return Ok(new { message = "Category deleted successfully", data = result });
    }

    [HttpPut("update/{id}")]
    [ValidateId]
    public async Task<IActionResult> UpdateCategory([FromRoute] int id, [FromBody] UpdateCategoryRequestDto request)
    {
        var result = await _categoryService.UpdateCategoryAsync(id, request);
        return Ok(new { message = "Category updated successfully", data = result });
    }

    [HttpGet("{id}")]
    [ValidateId]
    public async Task<IActionResult> GetCategoryById([FromRoute] int id)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        return Ok(new { message = "Category retrieved successfully", data = category });
    }
}