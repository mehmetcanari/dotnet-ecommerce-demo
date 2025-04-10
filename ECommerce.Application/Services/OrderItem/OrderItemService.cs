﻿using ECommerce.Application.DTO.Request.OrderItem;
using ECommerce.Application.DTO.Response.OrderItem;
using ECommerce.Application.Interfaces.Repository;
using ECommerce.Application.Interfaces.Service;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services.OrderItem;

public class OrderItemService : IOrderItemService
{
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<OrderItemService> _logger;

    public OrderItemService(
        IOrderItemRepository orderItemRepository, 
        IProductRepository productRepository,
        IAccountRepository accountRepository,
        ILogger<OrderItemService> logger)
    {
        _orderItemRepository = orderItemRepository;
        _productRepository = productRepository;
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<List<OrderItemResponseDto>> GetAllOrderItemsAsync(string email)
    {
        try
        {
            var accounts = await _accountRepository.Read();
            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                        throw new Exception("Account not found");
            var orderItems = await _orderItemRepository.Read();
            var items = orderItems.Where(o => o.AccountId == tokenAccount.AccountId && o.IsOrdered == false).ToList();
            if (items.Count == 0)
            {
                throw new Exception("No order items found.");
            }

            return items.Select(orderItem => new OrderItemResponseDto
            {
                AccountId = orderItem.AccountId,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.ProductName
            }).ToList();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while fetching all order items");
            throw;
        }
    }

    public async Task CreateOrderItemAsync(CreateOrderItemRequestDto createOrderItemRequestDto, string email)
    {
        try
        {
            var products = await _productRepository.Read();
            var accounts = await _accountRepository.Read();

            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                        throw new Exception("Account not found");
            var product = products.FirstOrDefault(p => p.ProductId == createOrderItemRequestDto.ProductId) ??
                          throw new Exception("Product not found");

            if (accounts.FirstOrDefault(a => a.AccountId == tokenAccount.AccountId) == null)
                throw new Exception("Account not found");

            if (createOrderItemRequestDto.Quantity > product.StockQuantity)
            {
                throw new Exception("Not enough stock");
            }

            var orderItem = new Domain.Model.OrderItem
            {
                AccountId = tokenAccount.AccountId,
                Quantity = createOrderItemRequestDto.Quantity,
                ProductId = product.ProductId,
                UnitPrice = product.Price,
                ProductName = product.Name,
                IsOrdered = false
            };

            await _orderItemRepository.Create(orderItem);
            _logger.LogInformation("Order item created successfully");

            product.StockQuantity -= createOrderItemRequestDto.Quantity;
            await _productRepository.Update(product);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating order item");
            throw;
        }
    }

    public async Task UpdateOrderItemAsync(UpdateOrderItemRequestDto updateOrderItemRequestDto, string email)
    {
        try
        {
            var products = await _productRepository.Read();
            var orderItems = await _orderItemRepository.Read();
            var accounts = await _accountRepository.Read();

            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                        throw new Exception("Account not found");

            var orderItem = orderItems.FirstOrDefault(p =>
                                p.OrderItemId == updateOrderItemRequestDto.OrderItemId &&
                                p.AccountId == tokenAccount.AccountId) ??
                            throw new Exception("Order item not found");

            var product = products.FirstOrDefault(p => p.ProductId == updateOrderItemRequestDto.ProductId) ??
                          throw new Exception("Product not found");

            if (product.StockQuantity < updateOrderItemRequestDto.Quantity)
            {
                throw new Exception("Not enough stock");
            }

            orderItem.Quantity = updateOrderItemRequestDto.Quantity;
            orderItem.ProductId = updateOrderItemRequestDto.ProductId;
            orderItem.ProductName = product.Name;

            await _orderItemRepository.Update(orderItem);
            _logger.LogInformation("Order item updated successfully");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating order item");
            throw;
        }
    }

    public async Task DeleteAllOrderItemsAsync(string email)
    {
        try
        {
            var orderItems = await _orderItemRepository.Read();
            var accounts = await _accountRepository.Read();
            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                        throw new Exception("Account not found");
            var items = orderItems.Where(o => o.AccountId == tokenAccount.AccountId && o.IsOrdered == false).ToList();

            if (items.Count == 0)
            {
                throw new Exception("No order items found to delete");
            }

            _logger.LogInformation($"Attempting to delete {items.Count} order items");
            
            foreach (var item in items)
            {
                await _orderItemRepository.Delete(item);
                _logger.LogInformation($"Deleted order item with ID: {item.OrderItemId}");
            }

            _logger.LogInformation("All order items deleted successfully");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while deleting order items");
            throw;
        }
    }
}