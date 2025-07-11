using ECommerce.Application.Abstract.Service;
using ECommerce.Application.DTO.Response.Category;
using ECommerce.Application.DTO.Response.Product;
using ECommerce.Application.Utility;
using ECommerce.Domain.Abstract.Repository;
using MediatR;

namespace ECommerce.Application.Queries.Category;

public class GetCategoryByIdQuery : IRequest<Result<CategoryResponseDto>>
{
    public required int CategoryId { get; set; }
}

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryResponseDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILoggingService _logger;
    private readonly ICacheService _cacheService;
    private const string CategoryCacheKey = "category:{0}";
    private const int CacheExpirationMinutes = 60;

    public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository, IProductRepository productRepository, ILoggingService logger, ICacheService cacheService)
    {
        _cacheService = cacheService;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<Result<CategoryResponseDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cachedCategory = await GetCachedCategory(request.CategoryId);
            if (cachedCategory != null)
            {
                return Result<CategoryResponseDto>.Success(cachedCategory);
            }

            var categoryTask = _categoryRepository.GetCategoryById(request.CategoryId);
            var productsTask = _productRepository.GetProductsByCategoryId(request.CategoryId);

            await Task.WhenAll(categoryTask, productsTask);

            var category = await categoryTask;
            var categoryProducts = await productsTask;

            if (category == null)
            {
                return Result<CategoryResponseDto>.Failure("Category not found");
            }

            if (categoryProducts.Count == 0)
            {
                return Result<CategoryResponseDto>.Failure("No products found for this category");
            }

            var categoryResponseDto = MapToResponseDto(category, categoryProducts);

            await CacheCategory(request.CategoryId, categoryResponseDto);
    
            _logger.LogInformation("Category retrieved successfully");
            return Result<CategoryResponseDto>.Success(categoryResponseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", request.CategoryId);
            return Result<CategoryResponseDto>.Failure("An unexpected error occurred while retrieving the category");
        }
    }

    private async Task<CategoryResponseDto?> GetCachedCategory(int categoryId)
    {
        return await _cacheService.GetAsync<CategoryResponseDto>(string.Format(CategoryCacheKey, categoryId));
    }

    private async Task CacheCategory(int categoryId, CategoryResponseDto categoryDto)
    {
        var expirationTime = TimeSpan.FromMinutes(CacheExpirationMinutes);
        await _cacheService.SetAsync(string.Format(CategoryCacheKey, categoryId), categoryDto, expirationTime);
    }

    private static CategoryResponseDto MapToResponseDto(Domain.Model.Category category, List<Domain.Model.Product> products)
    {
        return new CategoryResponseDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            Products = products.Select(MapToProductDto).ToList()
        };
    }

    private static ProductResponseDto MapToProductDto(Domain.Model.Product product)
    {
        return new ProductResponseDto
        {
            ProductName = product.Name,
            Description = product.Description,
            Price = product.Price,
            DiscountRate = product.DiscountRate,
            ImageUrl = product.ImageUrl,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId
        };
    }
}