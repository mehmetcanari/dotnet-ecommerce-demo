namespace ECommerce.Application.DTO.Request.Category;

public record UpdateCategoryRequestDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
}