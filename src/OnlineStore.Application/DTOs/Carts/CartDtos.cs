namespace OnlineStore.Application.DTOs.Carts;

public record CartDto(Guid Id, DateTime CreatedAt, IReadOnlyList<CartItemDto> Items, decimal Total);

public record CartItemDto(Guid ProductId, string ProductName, decimal Price, int Quantity, decimal LineTotal);

public record AddCartItemRequest(Guid ProductId, int Quantity);

/// <summary>
/// Add one or more products in a single request.
/// Duplicate productIds in the list are merged (quantities summed).
/// </summary>
public record AddCartItemsRequest(IReadOnlyList<AddCartItemRequest> Items);

public record UpdateCartItemRequest(int Quantity);
