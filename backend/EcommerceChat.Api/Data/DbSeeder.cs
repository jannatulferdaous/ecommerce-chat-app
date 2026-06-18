using EcommerceChat.Api.Models;

namespace EcommerceChat.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Products.Any()) return; // already seeded

        var products = new List<Product>
        {
            new Product
            {
                Name = "Nike Dri-FIT Running T-Shirt",
                Brand = "Nike",
                Category = ProductCategories.TShirt,
                Price = 29.99m,
                Description = "Lightweight breathable t-shirt designed for running and training.",
                ImageUrl = "https://placehold.co/400x400?text=Nike+Running+Tee",
                Variants = MakeVariants(s: 10, m: 15, l: 0, xl: 8, xxl: 0) // L and XXL out of stock
            },
            new Product
            {
                Name = "Nike Sportswear Essential T-Shirt",
                Brand = "Nike",
                Category = ProductCategories.TShirt,
                Price = 24.99m,
                Description = "Classic cotton t-shirt with the Nike logo, everyday casual wear.",
                ImageUrl = "https://placehold.co/400x400?text=Nike+Essential+Tee",
                Variants = MakeVariants(s: 12, m: 20, l: 18, xl: 10, xxl: 5)
            },
            new Product
            {
                Name = "Adidas Run Icons T-Shirt",
                Brand = "Adidas",
                Category = ProductCategories.TShirt,
                Price = 27.50m,
                Description = "Moisture-wicking running t-shirt with reflective details for visibility.",
                ImageUrl = "https://placehold.co/400x400?text=Adidas+Run+Tee",
                Variants = MakeVariants(s: 8, m: 14, l: 14, xl: 6, xxl: 0)
            },
            new Product
            {
                Name = "Adidas Originals Trefoil T-Shirt",
                Brand = "Adidas",
                Category = ProductCategories.TShirt,
                Price = 22.00m,
                Description = "Iconic trefoil logo t-shirt, soft cotton fabric for everyday comfort.",
                ImageUrl = "https://placehold.co/400x400?text=Adidas+Trefoil+Tee",
                Variants = MakeVariants(s: 10, m: 10, l: 10, xl: 10, xxl: 10)
            },
            new Product
            {
                Name = "Puma Active Training T-Shirt",
                Brand = "Puma",
                Category = ProductCategories.TShirt,
                Price = 19.99m,
                Description = "Stretchy training t-shirt, great for the gym or a quick run.",
                ImageUrl = "https://placehold.co/400x400?text=Puma+Training+Tee",
                Variants = MakeVariants(s: 6, m: 12, l: 12, xl: 4, xxl: 0)
            },
            new Product
            {
                Name = "Nike Dri-FIT Running Track Pants",
                Brand = "Nike",
                Category = ProductCategories.Pants,
                Price = 54.99m,
                Description = "Tapered running track pants with zip pockets and breathable fabric.",
                ImageUrl = "https://placehold.co/400x400?text=Nike+Running+Pants",
                Variants = MakeVariants(s: 5, m: 10, l: 10, xl: 0, xxl: 0) // XL, XXL out of stock
            },
            new Product
            {
                Name = "Adidas Tiro Track Pants",
                Brand = "Adidas",
                Category = ProductCategories.Pants,
                Price = 49.99m,
                Description = "Classic three-stripe track pants for training and running.",
                ImageUrl = "https://placehold.co/400x400?text=Adidas+Track+Pants",
                Variants = MakeVariants(s: 8, m: 12, l: 12, xl: 8, xxl: 4)
            },
            new Product
            {
                Name = "Levi's 511 Slim Fit Jeans",
                Brand = "Levi's",
                Category = ProductCategories.Pants,
                Price = 69.99m,
                Description = "Slim fit denim jeans with a classic five-pocket styling.",
                ImageUrl = "https://placehold.co/400x400?text=Levis+511+Jeans",
                Variants = MakeVariants(s: 4, m: 10, l: 10, xl: 6, xxl: 0)
            },
            new Product
            {
                Name = "Puma Essential Sweatpants",
                Brand = "Puma",
                Category = ProductCategories.Pants,
                Price = 39.99m,
                Description = "Comfortable fleece sweatpants for casual everyday wear.",
                ImageUrl = "https://placehold.co/400x400?text=Puma+Sweatpants",
                Variants = MakeVariants(s: 10, m: 14, l: 14, xl: 10, xxl: 6)
            },
            new Product
            {
                Name = "H&M Slim Chino Pants",
                Brand = "H&M",
                Category = ProductCategories.Pants,
                Price = 34.99m,
                Description = "Smart-casual slim chino pants suitable for work or weekends.",
                ImageUrl = "https://placehold.co/400x400?text=HM+Chino+Pants",
                Variants = MakeVariants(s: 6, m: 12, l: 12, xl: 8, xxl: 0)
            },
            new Product
            {
                Name = "Under Armour Tech Running T-Shirt",
                Brand = "Under Armour",
                Category = ProductCategories.TShirt,
                Price = 26.00m,
                Description = "Quick-dry running shirt with anti-odor technology, ideal for long runs.",
                ImageUrl = "https://placehold.co/400x400?text=UA+Running+Tee",
                Variants = MakeVariants(s: 9, m: 9, l: 9, xl: 9, xxl: 0)
            },
            new Product
            {
                Name = "New Balance Impact Run Pants",
                Brand = "New Balance",
                Category = ProductCategories.Pants,
                Price = 47.50m,
                Description = "Lightweight woven running pants with elastic waistband.",
                ImageUrl = "https://placehold.co/400x400?text=NB+Run+Pants",
                Variants = MakeVariants(s: 5, m: 8, l: 8, xl: 5, xxl: 3)
            }
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }

    private static List<ProductVariant> MakeVariants(int s, int m, int l, int xl, int xxl) => new()
    {
        new ProductVariant { Size = SizeOptions.S, Stock = s },
        new ProductVariant { Size = SizeOptions.M, Stock = m },
        new ProductVariant { Size = SizeOptions.L, Stock = l },
        new ProductVariant { Size = SizeOptions.XL, Stock = xl },
        new ProductVariant { Size = SizeOptions.XXL, Stock = xxl }
    };
}
