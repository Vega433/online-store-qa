using Microsoft.EntityFrameworkCore;
using OnlineStore.Application.DTOs.Carts;
using OnlineStore.Application.Exceptions;
using OnlineStore.Application.Interfaces;
using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Infrastructure.Services;

public class CartService(StoreDbContext db) : ICartService
{
    public async Task<CartDto> CreateAsync(CancellationToken cancellationToken = default)
    {
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        db.Carts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);
        return Map(cart);
    }

    public async Task<CartDto> GetByIdAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var cart = await LoadCartAsync(cartId, cancellationToken);
        return Map(cart);
    }

    public async Task DeleteAsync(Guid cartId, CancellationToken cancellationToken = default)
    {
        var cart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);

        if (cart is null)
        {
            throw new NotFoundException($"Cart '{cartId}' was not found.");
        }

        db.Carts.Remove(cart);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CartDto> AddItemsAsync(
        Guid cartId,
        AddCartItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new BusinessException("At least one item is required.");
        }

        if (request.Items.Any(i => i.Quantity <= 0))
        {
            throw new BusinessException("Quantity must be greater than zero for every item.");
        }

        // Merge duplicate productIds in one request (sum quantities).
        var requested = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        var cart = await LoadCartAsync(cartId, cancellationToken);
        var productIds = requested.Select(r => r.ProductId).ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var line in requested)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                throw new NotFoundException($"Product '{line.ProductId}' was not found.");
            }

            if (!product.IsActive)
            {
                throw new BusinessException($"Cannot add an inactive product to the cart: '{product.Name}'.");
            }

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == line.ProductId);
            var newQuantity = (existing?.Quantity ?? 0) + line.Quantity;

            if (newQuantity > product.Stock)
            {
                throw new BusinessException(
                    $"Not enough stock for '{product.Name}'. Available: {product.Stock}, requested total: {newQuantity}.");
            }

            if (existing is null)
            {
                db.CartItems.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = product.Id,
                    Quantity = line.Quantity
                });
            }
            else
            {
                existing.Quantity = newQuantity;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Map(await LoadCartAsync(cartId, cancellationToken));
    }

    public async Task<CartDto> UpdateItemAsync(Guid cartId, Guid productId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new BusinessException("Quantity must be greater than zero.");
        }

        var cart = await LoadCartAsync(cartId, cancellationToken);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            throw new NotFoundException($"Product '{productId}' was not found in cart '{cartId}'.");
        }

        if (request.Quantity > item.Product.Stock)
        {
            throw new BusinessException($"Not enough stock for '{item.Product.Name}'. Available: {item.Product.Stock}.");
        }

        if (!item.Product.IsActive)
        {
            throw new BusinessException("Cannot update quantity for an inactive product.");
        }

        item.Quantity = request.Quantity;
        await db.SaveChangesAsync(cancellationToken);
        return Map(await LoadCartAsync(cartId, cancellationToken));
    }

    public async Task<CartDto> RemoveItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default)
    {
        var cart = await LoadCartAsync(cartId, cancellationToken);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null)
        {
            throw new NotFoundException($"Product '{productId}' was not found in cart '{cartId}'.");
        }

        db.CartItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return Map(await LoadCartAsync(cartId, cancellationToken));
    }

    private async Task<Cart> LoadCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);

        if (cart is null)
        {
            throw new NotFoundException($"Cart '{cartId}' was not found.");
        }

        return cart;
    }

    private static CartDto Map(Cart cart)
    {
        var items = cart.Items
            .Select(i => new CartItemDto(
                i.ProductId,
                i.Product.Name,
                i.Product.Price,
                i.Quantity,
                i.Product.Price * i.Quantity))
            .ToList();

        return new CartDto(cart.Id, cart.CreatedAt, items, items.Sum(i => i.LineTotal));
    }
}
