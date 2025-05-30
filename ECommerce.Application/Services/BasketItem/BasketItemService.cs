﻿using ECommerce.Application.Abstract.Service;
using ECommerce.Application.DTO.Request.BasketItem;
using ECommerce.Application.DTO.Response.BasketItem;
using ECommerce.Application.Services.Base;
using ECommerce.Application.Utility;
using ECommerce.Domain.Abstract.Repository;

namespace ECommerce.Application.Services.BasketItem;

public class BasketItemService : ServiceBase, IBasketItemService
{
    private readonly IBasketItemRepository _basketItemRepository;
    private readonly ICurrentUserService _currentUserService;
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
        IUnitOfWork unitOfWork,
        IServiceProvider serviceProvider, 
        ICurrentUserService currentUserService) : base(serviceProvider)
    {
        _basketItemRepository = basketItemRepository;
        _productRepository = productRepository;
        _accountRepository = accountRepository;
        _logger = logger;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<BasketItemResponseDto>>> GetAllBasketItemsAsync()
    {
        try
        {
            var emailResult = _currentUserService.GetCurrentUserEmail();
            if (emailResult is { IsSuccess: false, Error: not null })
            {
                _logger.LogWarning("Failed to get current user email: {Error}", emailResult.Error);
                return Result<List<BasketItemResponseDto>>.Failure(emailResult.Error);
            }
            
            TimeSpan cacheDuration = TimeSpan.FromMinutes(10);
            var cachedItems = await _cacheService.GetAsync<List<BasketItemResponseDto>>(GetAllBasketItemsCacheKey);
            if (cachedItems != null)
            {
                _logger.LogInformation("Basket items fetched from cache");
                return Result<List<BasketItemResponseDto>>.Success(cachedItems);
            }
            
            if (emailResult.Data == null)
            {
                _logger.LogWarning("User email is null");
                return Result<List<BasketItemResponseDto>>.Failure("Email is not available");
            }
            
            var account = await _accountRepository.GetAccountByEmail(emailResult.Data);
            if (account == null)
            {
                return Result<List<BasketItemResponseDto>>.Failure("Account not found");
            }
            
            var nonOrderedBasketItems = await _basketItemRepository.GetNonOrderedBasketItems(account);
            if (nonOrderedBasketItems.Count == 0)
            {
                return Result<List<BasketItemResponseDto>>.Failure("No basket items found.");
            }

            var clientResponseBasketItems = nonOrderedBasketItems
            .Select(basketItem => new BasketItemResponseDto
            {
                AccountId = basketItem.AccountId,
                Quantity = basketItem.Quantity,
                UnitPrice = basketItem.UnitPrice,
                ProductId = basketItem.ProductId,
                ProductName = basketItem.ProductName
            }).ToList();

            await _cacheService.SetAsync(GetAllBasketItemsCacheKey, clientResponseBasketItems, cacheDuration);

            return Result<List<BasketItemResponseDto>>.Success(clientResponseBasketItems);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while fetching all basket items");
            return Result<List<BasketItemResponseDto>>.Failure("An unexpected error occurred");
        }
    }

    public async Task<Result> CreateBasketItemAsync(CreateBasketItemRequestDto createBasketItemRequestDto)
    {
        try
        {
            var emailResult = _currentUserService.GetCurrentUserEmail();
            if (emailResult is { IsSuccess: false, Error: not null })
            {
                _logger.LogWarning("Failed to get current user email: {Error}", emailResult.Error);
                return Result.Failure(emailResult.Error);
            }
            
            if (emailResult.Data == null)
            {
                _logger.LogWarning("User email is null");
                return Result.Failure("Email is not available");
            }
            
            var validationResult = await ValidateAsync(createBasketItemRequestDto);
            if (validationResult is { IsSuccess: false, Error: not null }) 
                return Result.Failure(validationResult.Error);

            var product = await _productRepository.GetProductById(createBasketItemRequestDto.ProductId);
            var userAccount = await _accountRepository.GetAccountByEmail(emailResult.Data);
            
            if (userAccount == null)
                return Result.Failure("Account not found");
            
            if (product == null)
                return Result.Failure("Product not found");

            if (createBasketItemRequestDto.Quantity > product.StockQuantity)
                return Result.Failure("Not enough stock");

            var basketItem = new Domain.Model.BasketItem
            {
                AccountId = userAccount.Id,
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
            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating basket item");
            return Result.Failure(exception.Message);
        }
    }

    public async Task<Result> UpdateBasketItemAsync(UpdateBasketItemRequestDto updateBasketItemRequestDto)
    {
        try
        {
            var emailResult = _currentUserService.GetCurrentUserEmail();
            if (emailResult is { IsSuccess: false, Error: not null })
            {
                _logger.LogWarning("Failed to get current user email: {Error}", emailResult.Error);
                return Result.Failure(emailResult.Error);
            }
            if (emailResult.Data == null)
            {
                _logger.LogWarning("User email is null");
                return Result.Failure("Email is not available");
            }
            
            var validationResult = await ValidateAsync(updateBasketItemRequestDto);
            if (validationResult is { IsSuccess: false, Error: not null }) 
                return Result.Failure(validationResult.Error);
            
            var account = await _accountRepository.GetAccountByEmail(emailResult.Data);
            if (account == null)
                return Result.Failure("Account not found");
            
            var basketItem = await _basketItemRepository.GetSpecificAccountBasketItemWithId(updateBasketItemRequestDto.BasketItemId, account);
            if (basketItem == null)
                return Result.Failure("Basket item not found");

            var product = await _productRepository.GetProductById(updateBasketItemRequestDto.ProductId);
            if (product == null)
                return Result.Failure("Product not found");

            if (product.StockQuantity < updateBasketItemRequestDto.Quantity)
            {
                return Result.Failure("Not enough stock");
            }

            basketItem.Quantity = updateBasketItemRequestDto.Quantity;
            basketItem.ProductId = updateBasketItemRequestDto.ProductId;
            basketItem.ProductName = product.Name;
            basketItem.UnitPrice = product.Price;
            basketItem.IsOrdered = false;

            _basketItemRepository.Update(basketItem);
            await ClearBasketItemsCacheAsync();
            await _unitOfWork.Commit();

            _logger.LogInformation("Basket item updated successfully: {BasketItem}", basketItem);
            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating basket item");
            throw;
        }
    }

    public async Task<Result> DeleteAllNonOrderedBasketItemsAsync()
    {
        try
        {
            var emailResult = _currentUserService.GetCurrentUserEmail();
            if (emailResult is { IsSuccess: false, Error: not null })
            {
                _logger.LogWarning("Failed to get current user email: {Error}", emailResult.Error);
                return Result.Failure(emailResult.Error);
            }
            
            if (emailResult.Data == null)
            {
                _logger.LogWarning("User email is null");
                return Result.Failure("Email is not available");
            }
            
            var tokenAccount = await _accountRepository.GetAccountByEmail(emailResult.Data);
            if (tokenAccount == null)
                return Result.Failure("Account not found");
            
            var nonOrderedBasketItems = await _basketItemRepository.GetNonOrderedBasketItems(tokenAccount);

            if (nonOrderedBasketItems.Count == 0)
                return Result.Failure("No basket items found to delete");

            foreach (var basketItem in nonOrderedBasketItems)
            {
                _basketItemRepository.Delete(basketItem);
            }

            await ClearBasketItemsCacheAsync();
            await _unitOfWork.Commit();

            _logger.LogInformation("All basket items deleted successfully");
            return Result.Success();
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
            var nonOrderedBasketItems = await _basketItemRepository.GetNonOrderedBasketItemIncludeSpecificProduct(updatedProduct.ProductId);
            if (nonOrderedBasketItems == null || nonOrderedBasketItems.Count == 0)
            {
                _logger.LogInformation("No non-ordered basket items found for product: {ProductId}", updatedProduct.ProductId);
                return;
            }

            foreach (var basketItem in nonOrderedBasketItems)
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

    private async Task ClearBasketItemsCacheAsync()
    {
        await _cacheService.RemoveAsync(GetAllBasketItemsCacheKey);
    }
}