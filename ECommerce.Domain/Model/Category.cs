namespace ECommerce.Domain.Model;

public class Category
{
    public int CategoryId { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
}
