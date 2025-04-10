namespace ECommerce.Domain.Model
{
    public class Order
    {   
        public int OrderId { get; init; }
        public int AccountId { get; init; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public decimal TotalPrice => OrderItems.Sum(oi => oi.TotalPrice);
        public DateTime OrderDate { get; init; } = DateTime.UtcNow;
        public required string ShippingAddress { get; init; }
        public required string BillingAddress { get; init; }
        public PaymentMethod PaymentMethod { get; init; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
    }
}


public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered,
    Cancelled
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    ApplePay,
}