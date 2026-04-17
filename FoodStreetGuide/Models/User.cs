using SQLite;

namespace doanC_.Models
{
    [Table("User")]
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull, Unique]
        public string Username { get; set; }

        [NotNull, Unique]
        public string Email { get; set; }

        [NotNull]
        public string Password { get; set; }

        public string FullName { get; set; }

        public string Avatar { get; set; }

        [NotNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}