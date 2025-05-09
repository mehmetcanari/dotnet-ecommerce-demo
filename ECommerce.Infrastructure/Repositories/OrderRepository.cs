using ECommerce.Application.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;
using ECommerce.Infrastructure.DatabaseContext;
using ECommerce.Application.Interfaces.Service;
using ECommerce.Domain.Model;

namespace ECommerce.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly StoreDbContext _context;
    private readonly ILoggingService _logger;

    public OrderRepository(StoreDbContext context, ILoggingService logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Order>> Read()
    {
        try
        {
            IQueryable<Order> query = _context.Orders;

            var orders = await query
            .AsNoTracking()
            .Include(o => o.BasketItems)
            .ToListAsync();

            return orders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task Create(Order order)
    {
        try
        {
            await _context.Orders.AddAsync(order);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unexpected error occurred");
            throw new Exception(exception.Message);
        }
    }

    public void Update(Order order)
    {
        try
        {
            _context.Orders.Update(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public void Delete(Order order)
    {
        try
        {
            _context.Orders.Remove(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task<Order> GetOrderById(int id)
    {
        try
        {
            IQueryable<Order> query = _context.Orders;

            var order = await query
            .AsNoTracking()
            .Include(o => o.BasketItems)
            .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                throw new Exception("Order not found");
            }

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred");
            throw new Exception("An unexpected error occurred", ex);
        }
    }
}