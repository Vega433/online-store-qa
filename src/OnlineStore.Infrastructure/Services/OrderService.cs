using Microsoft.EntityFrameworkCore;
using OnlineStore.Application.DTOs.Orders;
using OnlineStore.Application.Exceptions;
using OnlineStore.Application.Interfaces;
using OnlineStore.Domain.Entities;
using OnlineStore.Domain.Enums;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Infrastructure.Services;

public class OrderService(StoreDbContext db) : IOrderService
{
    public async Task<OrderDto> CreateFromCartAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var cart = await db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == request.CartId, cancellationToken);

        if (cart is null)
        {
            throw new NotFoundException($"Cart '{request.CartId}' was not found.");
        }

        if (cart.Items.Count == 0)
        {
            throw new BusinessException("Cannot checkout an empty cart.");
        }

        foreach (var item in cart.Items)
        {
            if (!item.Product.IsActive)
            {
                throw new BusinessException($"Product '{item.Product.Name}' is inactive and cannot be ordered.");
            }

            if (item.Quantity > item.Product.Stock)
            {
                throw new BusinessException(
                    $"Not enough stock for '{item.Product.Name}'. Available: {item.Product.Stock}, requested: {item.Quantity}.");
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = cart.Items.Select(i => new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Price = i.Product.Price,
                Quantity = i.Quantity
            }).ToList()
        };

        order.Total = order.Items.Sum(i => i.Price * i.Quantity);

        foreach (var item in cart.Items)
        {
            item.Product.Stock -= item.Quantity;
        }

        db.Orders.Add(order);
        db.CartItems.RemoveRange(cart.Items);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Map(order);
    }

    public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order '{id}' was not found.");
        }

        return Map(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(Map).ToList();
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order '{id}' was not found.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new BusinessException("Cancelled orders cannot change status.");
        }

        if (order.Status == OrderStatus.Shipped && status == OrderStatus.Cancelled)
        {
            throw new BusinessException("Shipped orders cannot be cancelled.");
        }

        if (order.Status == status)
        {
            return Map(order);
        }

        if (status == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);
                if (product is not null)
                {
                    product.Stock += item.Quantity;
                }
            }
        }

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Map(order);
    }

    private static OrderDto Map(Order order)
    {
        var items = order.Items
            .Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                i.Price,
                i.Quantity,
                i.Price * i.Quantity))
            .ToList();

        return new OrderDto(
            order.Id,
            order.CartId,
            order.Total,
            order.Status,
            order.CreatedAt,
            order.UpdatedAt,
            items);
    }
}
