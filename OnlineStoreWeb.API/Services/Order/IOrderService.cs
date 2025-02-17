using OnlineStoreWeb.API.DTO.Order;

namespace OnlineStoreWeb.API.Services.Order;

public interface IOrderService
{
    Task<List<Model.Order>> GetAllOrdersAsync();
    Task<Model.Order> GetOrderWithIdAsync(int id);
    Task AddOrderAsync(OrderCreateDto createOrderDto);
    Task DeleteOrderAsync(int userId);
    Task UpdateOrderStatusAsync(int id, OrderUpdateDto orderUpdateDto);
}