using OnlineStore.Domain.Enums;

namespace OnlineStore.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid? CartId { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
