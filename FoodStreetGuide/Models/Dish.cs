using SQLite;

namespace doanC_.Models
{
    [Table("Dish")]
    public class Dish
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public int LocationPointId { get; set; }

        [NotNull]
        public string Name { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }

        [NotNull]
        public decimal Price { get; set; }

        public string Category { get; set; }

        public double Rating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;

        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}