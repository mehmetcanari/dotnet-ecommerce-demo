using ECommerce.API.DTO.Request.Order;
using ECommerce.API.DTO.Response.Order;
using ECommerce.API.DTO.Response.OrderItem;
using ECommerce.API.Repositories.Account;
using ECommerce.API.Repositories.Order;
using ECommerce.API.Repositories.OrderItem;
using ECommerce.API.Services.OrderItem;
using ECommerce.API.Repositories.Product;

namespace ECommerce.API.Services.Order;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemService _orderItemService;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository, 
        IOrderItemService orderItemService,
        IOrderItemRepository orderItemRepository,
        IAccountRepository accountRepository, 
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _accountRepository = accountRepository;
        _orderItemRepository = orderItemRepository;
        _orderItemService = orderItemService;
        _logger = logger;
    }

    public async Task AddOrderAsync(OrderCreateRequestDto createRequestOrderDto, string email)
    {
        try
        {
            var orderItems = await _orderItemRepository.Read();
            var accounts = await _accountRepository.Read();

            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                               throw new Exception("User not found");
            var userOrderItems = orderItems.Where(oi => oi.AccountId == tokenAccount.AccountId).ToList();

            List<Model.OrderItem> newOrderItems = userOrderItems
                .Select(orderItem => new Model.OrderItem
                {
                    AccountId = orderItem.AccountId,
                    ProductId = orderItem.ProductId,
                    Quantity = orderItem.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    ProductName = orderItem.ProductName
                }).ToList();

            if (newOrderItems.Count == 0)
            {
                throw new Exception("No items in cart");
            }

            Model.Order order = new Model.Order
            {
                AccountId = tokenAccount.AccountId,
                ShippingAddress = createRequestOrderDto.ShippingAddress,
                BillingAddress = createRequestOrderDto.BillingAddress,
                PaymentMethod = createRequestOrderDto.PaymentMethod,
                OrderItems = newOrderItems
            };

            await _orderItemService.DeleteAllOrderItemsAsync(email);
            await _orderRepository.Create(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while adding order: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task CancelOrderAsync(string email)
    {
        try
        {
            var orders = await _orderRepository.Read();
            var accounts = await _accountRepository.Read();

            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                               throw new Exception("Account not found");
            var order = orders.FirstOrDefault(o => o.AccountId == tokenAccount.AccountId) ??
                        throw new Exception("Order not found");
            await _orderRepository.Delete(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting order: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task DeleteOrderByIdAsync(int id)
    {
        try
        {
            var orders = await _orderRepository.Read();
            var orderToDelete = orders.FirstOrDefault(o => o.OrderId == id) ??
                                throw new Exception("Order not found");
            await _orderRepository.Delete(orderToDelete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while deleting order: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
    {
        try
        {
            var orders = await _orderRepository.Read();

            return orders.Select(o => new OrderResponseDto
            {
                AccountId = o.AccountId,
                OrderItems = o.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    AccountId = oi.AccountId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    ProductName = oi.ProductName
                }).ToList(),
                OrderDate = o.OrderDate,
                ShippingAddress = o.ShippingAddress,
                BillingAddress = o.BillingAddress,
                PaymentMethod = o.PaymentMethod,
                Status = o.Status
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching all orders: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task<OrderResponseDto> GetOrdersAsync(string email)
    {
        try
        {
            var orders = await _orderRepository.Read();
            var accounts = await _accountRepository.Read();

            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                               throw new Exception("Account not found");
            var order = orders.FirstOrDefault(o => o.AccountId == tokenAccount.AccountId) ??
                        throw new Exception("Order not found");

            if (order == null)
                throw new Exception("Order not found");

            OrderResponseDto orderResponseDto = new()
            {
                AccountId = order.AccountId,
                OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    AccountId = oi.AccountId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    ProductName = oi.ProductName
                }).ToList(),
                OrderDate = order.OrderDate,
                ShippingAddress = order.ShippingAddress,
                BillingAddress = order.BillingAddress,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status
            };

            return orderResponseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching order with id: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task<OrderResponseDto> GetOrderByIdAsync(int id)
    {
        try
        {
            var orders = await _orderRepository.Read();
            var order = orders.FirstOrDefault(o => o.OrderId == id) ??
                        throw new Exception("Order not found");

            OrderResponseDto orderResponseDto = new()
            {
                AccountId = order.AccountId,
                OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    AccountId = oi.AccountId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    ProductName = oi.ProductName
                }).ToList(),
                OrderDate = order.OrderDate,
                ShippingAddress = order.ShippingAddress,
                BillingAddress = order.BillingAddress,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status
            };

            return orderResponseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching order with id: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }

    public async Task UpdateOrderStatusByAccountIdAsync(int accountId, OrderUpdateRequestDto orderUpdateRequestDto)
    {
        try
        {
            var orders = await _orderRepository.Read();
            var order = orders.FirstOrDefault(o => o.AccountId == accountId) ?? throw new Exception("Order not found");
            order.Status = orderUpdateRequestDto.Status;
            await _orderRepository.Update(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while updating order status: {Message}", ex.Message);
            throw new Exception("An unexpected error occurred", ex);
        }
    }
}