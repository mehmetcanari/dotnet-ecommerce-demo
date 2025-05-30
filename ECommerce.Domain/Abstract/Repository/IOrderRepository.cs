using ECommerce.Domain.Model;

namespace ECommerce.Domain.Abstract.Repository;

public interface IOrderRepository
{
    Task Create(Order order);
    Task<List<Order>> Read(int pageNumber = 1, int pageSize = 50);
    Task<List<Order>> GetAccountPendingOrders(int accountId);
    Task<Order?> GetOrderById(int id);
    Task<Order?> GetOrderByAccountId(int accountId);
    Task<List<Order?>> GetAccountOrders(int accountId);
    void Update(Order order);
    void Delete(Order order);
}