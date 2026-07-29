using OnlineStore.Domain.Entities;
using OnlineStore.Infrastructure.Persistence;

namespace OnlineStore.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(StoreDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Products.Any())
        {
            return;
        }

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse with USB receiver",
                Price = 29.99m,
                Stock = 50,
                Category = "Electronics",
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Mechanical Keyboard",
                Description = "RGB mechanical keyboard, blue switches",
                Price = 89.99m,
                Stock = 25,
                Category = "Electronics",
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Coffee Mug",
                Description = "Ceramic mug 350ml",
                Price = 12.50m,
                Stock = 100,
                Category = "Home",
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Notebook A5",
                Description = "Lined notebook, 96 pages",
                Price = 5.99m,
                Stock = 200,
                Category = "Office",
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Discontinued Headphones",
                Description = "Old model, not for sale",
                Price = 49.99m,
                Stock = 0,
                Category = "Electronics",
                IsActive = false
            }
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);
    }
}
