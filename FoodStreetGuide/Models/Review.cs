using SQLite;

namespace doanC_.Models
{
    [Table("Review")]
    public class Review
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public int UserId { get; set; }

        public int? LocationPointId { get; set; }

        public int? DishId { get; set; }

        [NotNull]
        public int Rating { get; set; }

        public string Comment { get; set; }

        public string Image { get; set; }

        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}