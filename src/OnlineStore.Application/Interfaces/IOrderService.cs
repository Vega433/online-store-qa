using OnlineStore.Application.DTOs.Orders;
using OnlineStore.Domain.Enums;

namespace OnlineStore.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateFromCartAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<OrderDto> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken cancellationToken = default);
}
