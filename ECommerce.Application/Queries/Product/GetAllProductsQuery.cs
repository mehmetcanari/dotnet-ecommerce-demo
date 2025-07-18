using ECommerce.Application.DTO.Response.Product;
using MediatR;
using ECommerce.Application.Abstract.Service;
using ECommerce.Application.Utility;
using ECommerce.Domain.Abstract.Repository;

namespace ECommerce.Application.Queries.Product;

public class GetAllProductsQuery : IRequest<Result<List<ProductResponseDto>>>{}

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, Result<List<ProductResponseDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly ILoggingService _logger;
    private const string AllProductsCacheKey = "products";
    private const int CacheDurationInMinutes = 60;

    public GetAllProductsQueryHandler(
        IProductRepository productRepository,
        ICacheService cacheService,
        ILoggingService logger)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<List<ProductResponseDto>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var expirationTime = TimeSpan.FromMinutes(CacheDurationInMinutes);
            var cachedProducts = await _cacheService.GetAsync<List<ProductResponseDto>>(AllProductsCacheKey);

            if (cachedProducts is { Count: > 0 })
            {
                _logger.LogInformation("Retrieved {Count} products from cache", cachedProducts.Count);
                return Result<List<ProductResponseDto>>.Success(cachedProducts);
            }

            var products = await _productRepository.Read();

            if (products.Count == 0)
            {
                throw new Exception("No products found");
            }

            var productResponses = products.Select(p => new ProductResponseDto
            {
                ProductName = p.Name,
                Description = p.Description,
                Price = p.Price,
                DiscountRate = p.DiscountRate,
                ImageUrl = p.ImageUrl,
                StockQuantity = p.StockQuantity,
                CategoryId = p.CategoryId
            }).ToList();

            await _cacheService.SetAsync(AllProductsCacheKey, productResponses, expirationTime);
            _logger.LogInformation("Successfully cached {Count} products", productResponses.Count);
            
            return Result<List<ProductResponseDto>>.Success(productResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching all products: {Message}", ex.Message);
            return Result<List<ProductResponseDto>>.Failure(ex.Message);
        }
    }
}