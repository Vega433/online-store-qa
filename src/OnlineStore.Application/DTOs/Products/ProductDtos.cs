using OnlineStore.Domain.Enums;

namespace OnlineStore.Application.DTOs.Products;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    bool IsActive,
    DateTime CreatedAt);

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category);

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    bool IsActive);

/// <summary>
/// Partial update: only non-null fields are applied. Stock is changed via AdjustStock.
/// </summary>
public record PatchProductRequest(
    string? Name = null,
    string? Description = null,
    decimal? Price = null,
    string? Category = null,
    bool? IsActive = null);

public record AdjustStockRequest(StockOperation Operation, int Quantity);
