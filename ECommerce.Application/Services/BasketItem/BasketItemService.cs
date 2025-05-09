﻿using ECommerce.Application.DTO.Request.BasketItem;
using ECommerce.Application.DTO.Response.BasketItem;
using ECommerce.Application.Interfaces.Repository;
using ECommerce.Application.Interfaces.Service;

namespace ECommerce.Application.Services.BasketItem;

public class BasketItemService : IBasketItemService
{
    private readonly IBasketItemRepository _basketItemRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly ILoggingService _logger;
    private readonly IUnitOfWork _unitOfWork;
    private const string GetAllBasketItemsCacheKey = "GetAllBasketItems";

    public BasketItemService(
        IBasketItemRepository basketItemRepository, 
        IProductRepository productRepository,
        IAccountRepository accountRepository,
        ILoggingService logger,
        ICacheService cacheService,
        IUnitOfWork unitOfWork)
    {
        _basketItemRepository = basketItemRepository;
        _productRepository = productRepository;
        _accountRepository = accountRepository;
        _logger = logger;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<BasketItemResponseDto>> GetAllBasketItemsAsync(string email)
    {
        try
        {
            var cacheKey = GetAllBasketItemsCacheKey;
            TimeSpan cacheDuration = TimeSpan.FromMinutes(10);
            var cachedItems = await _cacheService.GetAsync<List<BasketItemResponseDto>>(cacheKey);
            if (cachedItems != null)
            {
                _logger.LogInformation("Basket items fetched from cache");
                return cachedItems;
            }

            var accounts = await _accountRepository.Read();
            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                        throw new Exception("Account not found");
            var basketItems = await _basketItemRepository.Read();
            var nonOrderedBasketItems = basketItems.Where(o => o.AccountId == tokenAccount.Id && o.IsOrdered == false).ToList();
            if (nonOrderedBasketItems.Count == 0)
            {
                throw new Exception("No basket items found.");
            }

            List<BasketItemResponseDto> clientResponseBasketItems = nonOrderedBasketItems
            .Select(basketItem => new BasketItemResponseDto
            {
                AccountId = basketItem.AccountId,
                Quantity = basketItem.Quantity,
                UnitPrice = basketItem.UnitPrice,
                ProductId = basketItem.ProductId,
                ProductName = basketItem.ProductName
            }).ToList();

            await _cacheService.SetAsync(cacheKey, clientResponseBasketItems, cacheDuration);

            return clientResponseBasketItems;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while fetching all basket items");
            throw;
        }
    }

    public async Task CreateBasketItemAsync(CreateBasketItemRequestDto createBasketItemRequestDto, string email)
    {
        try
        {
            var products = await _productRepository.Read();
            var accounts = await _accountRepository.Read();

            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ?? throw new Exception("Account not found");
            var product = products.FirstOrDefault(p => p.ProductId == createBasketItemRequestDto.ProductId) ?? throw new Exception("Product not found");

            if (createBasketItemRequestDto.Quantity > product.StockQuantity)
                throw new Exception("Not enough stock");

            Domain.Model.BasketItem basketItem = new Domain.Model.BasketItem
            {
                AccountId = tokenAccount.Id,
                ExternalId = Guid.NewGuid().ToString(),
                Quantity = createBasketItemRequestDto.Quantity,
                ProductId = product.ProductId,
                UnitPrice = product.Price,
                ProductName = product.Name,
                IsOrdered = false
            };

            await _basketItemRepository.Create(basketItem);
            await ClearBasketItemsCacheAsync();
            await _unitOfWork.Commit();

            _logger.LogInformation("Basket item created successfully: {BasketItem}", basketItem);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating basket item");
            throw;
        }
    }

    public async Task UpdateBasketItemAsync(UpdateBasketItemRequestDto updateBasketItemRequestDto, string email)
    {
        try
        {
            var products = await _productRepository.Read();
            var basketItems = await _basketItemRepository.Read();
            var accounts = await _accountRepository.Read();

            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                        throw new Exception("Account not found");

            var basketItem = basketItems.FirstOrDefault(p =>
                                p.BasketItemId == updateBasketItemRequestDto.BasketItemId &&
                                p.AccountId == tokenAccount.Id) ??
                            throw new Exception("Basket item not found");

            var updatedProduct = products.FirstOrDefault(p => p.ProductId == updateBasketItemRequestDto.ProductId) ??
                          throw new Exception("Product not found");

            if (updatedProduct.StockQuantity < updateBasketItemRequestDto.Quantity)
            {
                throw new Exception("Not enough stock");
            }

            basketItem.Quantity = updateBasketItemRequestDto.Quantity;
            basketItem.ProductId = updateBasketItemRequestDto.ProductId;
            basketItem.ProductName = updatedProduct.Name;
            basketItem.UnitPrice = updatedProduct.Price;
            basketItem.IsOrdered = false;

            _basketItemRepository.Update(basketItem);
            await ClearBasketItemsCacheAsync();
            await _unitOfWork.Commit();

            _logger.LogInformation("Basket item updated successfully: {BasketItem}", basketItem);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating basket item");
            throw;
        }
    }

    public async Task DeleteAllBasketItemsAsync(string email)
    {
        try
        {
            var basketItems = await _basketItemRepository.Read();
            var accounts = await _accountRepository.Read();
            var tokenAccount = accounts.FirstOrDefault(a => a.Email == email) ??
                        throw new Exception("Account not found");
            var nonOrderedBasketItems = basketItems.Where(o => o.AccountId == tokenAccount.Id && o.IsOrdered == false).ToList();

            if (nonOrderedBasketItems.Count == 0)
                throw new Exception("No basket items found to delete");

            foreach (var basketItem in nonOrderedBasketItems)
            {
                _basketItemRepository.Delete(basketItem);
            }

            await ClearBasketItemsCacheAsync();
            await _unitOfWork.Commit();

            _logger.LogInformation("All basket items deleted successfully");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while deleting basket items");
            throw;
        }
    }

    public async Task ClearBasketItemsIncludeOrderedProductAsync(Domain.Model.Product updatedProduct)
    {
        try
        {
            var basketItems = await _basketItemRepository.Read();
            var nonOrderedBasketItems = basketItems.Where(o => o.IsOrdered == false).ToList();

            var basketItem = nonOrderedBasketItems.FirstOrDefault(i => i.ProductId == updatedProduct.ProductId);
            if (basketItem != null)
            {
                _basketItemRepository.Delete(basketItem);
            }

            await ClearBasketItemsCacheAsync();
            await _unitOfWork.Commit();
            _logger.LogInformation("Basket items cleared successfully for product: {ProductId}", updatedProduct.ProductId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while clearing basket items include product");
            throw;
        }
    }

    public async Task ClearBasketItemsCacheAsync()
    {
        await _cacheService.RemoveAsync(GetAllBasketItemsCacheKey);
    }
}