using ECommerce.Application.Abstract.Service;
using ECommerce.Application.DTO.Response.Product;
using ECommerce.Application.Utility;
using MediatR;

namespace ECommerce.Application.Queries.Product;
public class ProductSearchQuery : IRequest<Result<List<ProductResponseDto>>>
{
    public string Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public ProductSearchQuery(string query, int page = 1, int pageSize = 10)
    {
        Query = query;
        Page = page;
        PageSize = pageSize;
    }
}
public class ProductSearchQueryHandler : IRequestHandler<ProductSearchQuery, Result<List<ProductResponseDto>>>
{
    private readonly IProductSearchService _productSearchService;
    private readonly ILoggingService _logger;

    public ProductSearchQueryHandler(IProductSearchService productSearchService, ILoggingService logger)
    {
        _productSearchService = productSearchService;
        _logger = logger;
    }

    public async Task<Result<List<ProductResponseDto>>> Handle(ProductSearchQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _productSearchService.SearchProductsAsync(request.Query, request.Page, request.PageSize);

            if (result.Data == null || !result.Data.Any())
            {
                _logger.LogInformation("No products found for query: {Query}", request.Query);
                return Result<List<ProductResponseDto>>.Success(new List<ProductResponseDto>());
            }

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Product search failed: {Error}", result.Error);
                return Result<List<ProductResponseDto>>.Failure(result.Error);
            }

            _logger.LogInformation("Product search completed successfully. Found {Count} products", result.Data.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while searching for products: {Message}", ex.Message);
            return Result<List<ProductResponseDto>>.Failure("An error occurred while searching for products");
        }
    }
}