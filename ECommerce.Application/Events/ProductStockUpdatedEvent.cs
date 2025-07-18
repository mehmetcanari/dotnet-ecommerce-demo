using MediatR;

namespace ECommerce.Application.Events;

public class ProductStockUpdatedEvent : INotification
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public double Price { get; set; }
    public double DiscountRate { get; set; }
    public string ImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public int CategoryId { get; set; }
    public DateTime ProductCreated { get; set; }
    public DateTime ProductUpdated { get; set; }
} 