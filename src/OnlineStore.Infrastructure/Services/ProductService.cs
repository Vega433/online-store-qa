using Microsoft.EntityFrameworkCore;
using OnlineStore.Application.DTOs.Products;
using OnlineStore.Application.Exceptions;
using OnlineStore.Application.Interfaces;
using OnlineStore.Domain.Entities;
using OnlineStore.Domain.Enums;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Infrastructure.Services;

public class ProductService(StoreDbContext db) : IProductService
{
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        if (minPrice is < 0 || maxPrice is < 0)
        {
            throw new BusinessException("Price filters cannot be negative.");
        }

        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
        {
            throw new BusinessException("minPrice cannot be greater than maxPrice.");
        }

        var query = db.Products.AsNoTracking().AsQueryable();

        if (onlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(Map).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product '{id}' was not found.");
        }

        return Map(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Price, request.Stock, request.Category);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Price = request.Price,
            Stock = request.Stock,
            Category = request.Category.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return Map(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Price, request.Stock, request.Category);

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{id}' was not found.");
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.Category = request.Category.Trim();
        product.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return Map(product);
    }

    public async Task<ProductDto> PatchAsync(Guid id, PatchProductRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Name is null
            && request.Description is null
            && request.Price is null
            && request.Category is null
            && request.IsActive is null)
        {
            throw new BusinessException("At least one field must be provided for patch.");
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{id}' was not found.");
        }

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new BusinessException("Product name is required.");
            }

            product.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            product.Description = request.Description.Trim();
        }

        if (request.Price is not null)
        {
            if (request.Price <= 0)
            {
                throw new BusinessException("Product price must be greater than zero.");
            }

            product.Price = request.Price.Value;
        }

        if (request.Category is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Category))
            {
                throw new BusinessException("Product category is required.");
            }

            product.Category = request.Category.Trim();
        }

        if (request.IsActive is not null)
        {
            product.IsActive = request.IsActive.Value;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Map(product);
    }

    public async Task<ProductDto> AdjustStockAsync(Guid id, AdjustStockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity < 0)
        {
            throw new BusinessException("Quantity cannot be negative.");
        }

        if (request.Operation is StockOperation.Increase or StockOperation.Decrease && request.Quantity == 0)
        {
            throw new BusinessException("Quantity must be greater than zero for increase/decrease.");
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{id}' was not found.");
        }

        var previousStock = product.Stock;

        product.Stock = request.Operation switch
        {
            StockOperation.Set => request.Quantity,
            StockOperation.Increase => product.Stock + request.Quantity,
            StockOperation.Decrease => product.Stock - request.Quantity,
            _ => throw new BusinessException($"Unknown stock operation '{request.Operation}'.")
        };

        if (product.Stock < 0)
        {
            throw new BusinessException(
                $"Stock cannot become negative. Current: {previousStock}, decrease: {request.Quantity}.");
        }

        await db.SaveChangesAsync(cancellationToken);
        return Map(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Product '{id}' was not found.");
        }

        product.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(string name, decimal price, int stock, string category)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessException("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new BusinessException("Product category is required.");
        }

        if (price <= 0)
        {
            throw new BusinessException("Product price must be greater than zero.");
        }

        if (stock < 0)
        {
            throw new BusinessException("Product stock cannot be negative.");
        }
    }

    private static ProductDto Map(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.Stock,
        product.Category,
        product.IsActive,
        product.CreatedAt);
}
