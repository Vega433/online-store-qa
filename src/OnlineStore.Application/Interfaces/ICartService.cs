using OnlineStore.Application.DTOs.Carts;

namespace OnlineStore.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> CreateAsync(CancellationToken cancellationToken = default);

    Task<CartDto> GetByIdAsync(Guid cartId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid cartId, CancellationToken cancellationToken = default);

    Task<CartDto> AddItemsAsync(Guid cartId, AddCartItemsRequest request, CancellationToken cancellationToken = default);

    Task<CartDto> UpdateItemAsync(Guid cartId, Guid productId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);

    Task<CartDto> RemoveItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default);
}
