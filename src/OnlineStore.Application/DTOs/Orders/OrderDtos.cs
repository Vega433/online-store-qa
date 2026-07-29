using OnlineStore.Domain.Enums;

namespace OnlineStore.Application.DTOs.Orders;

public record OrderDto(
    Guid Id,
    Guid? CartId,
    decimal Total,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal Price,
    int Quantity,
    decimal LineTotal);

public record CreateOrderRequest(Guid CartId);

public record UpdateOrderStatusRequest(OrderStatus Status);
