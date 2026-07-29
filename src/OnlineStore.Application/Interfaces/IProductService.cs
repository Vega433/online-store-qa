using OnlineStore.Application.DTOs.Products;

namespace OnlineStore.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool onlyActive = true,
        CancellationToken cancellationToken = default);

    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> PatchAsync(Guid id, PatchProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> AdjustStockAsync(Guid id, AdjustStockRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
