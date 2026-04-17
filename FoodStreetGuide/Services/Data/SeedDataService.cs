//using doanC_.Models;
//using System.Diagnostics;

//namespace doanC_.Services.Data
//{
//    public class SeedDataService
//    {
//        private readonly SQLiteService _sqliteService;

//        public SeedDataService(SQLiteService sqliteService)
//        {
//            _sqliteService = sqliteService;
//        }

//        /// <summary>
//        /// Thêm dữ liệu mẫu vào database (chỉ chạy lần đầu)
//        /// </summary>
//        public async Task SeedAsync()
//        {
//            try
//            {
//                // Kiểm tra xem đã có dữ liệu chưa
//                var existingUsers = await _sqliteService.GetAllUsersAsync();
//                if (existingUsers.Count > 0)
//                {
//                    Debug.WriteLine("[Seed] Database already has data, skipping seed");
//                    return;
//                }

//                Debug.WriteLine("[Seed] Starting to seed database...");

//                // Thêm Users
//                await AddUsersAsync();

//                // Thêm LocationPoints
//                await AddLocationsAsync();

//                // Thêm Dishes
//                await AddDishesAsync();

//                // Thêm Reviews
//                await AddReviewsAsync();

//                Debug.WriteLine("[Seed] ✅ Seed completed successfully!");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Seed] ❌ Error: {ex.Message}");
//                Debug.WriteLine($"[Seed] Stack trace: {ex.StackTrace}");
//            }
//        }

//        private async Task AddUsersAsync()
//        {
//            var users = new List<User>
//            {
//                new User
//                {
//                    Username = "johndoe",
//                    Email = "john@example.com",
//                    Password = "password123",
//                    FullName = "John Doe",
//                    Avatar = "user1.jpg"
//                },
//                new User
//                {
//                    Username = "janedoe",
//                    Email = "jane@example.com",
//                    Password = "password456",
//                    FullName = "Jane Doe",
//                    Avatar = "user2.jpg"
//                },
//                new User
//                {
//                    Username = "bobsmith",
//                    Email = "bob@example.com",
//                    Password = "password789",
//                    FullName = "Bob Smith",
//                    Avatar = "user3.jpg"
//                }
//            };

//            foreach (var user in users)
//            {
//                await _sqliteService.AddUserAsync(user);
//                Debug.WriteLine($"[Seed] Added user: {user.Username}");
//            }
//        }

//        private async Task AddLocationsAsync()
//        {
//            var locations = new List<LocationPoint>
//            {
//                new LocationPoint
//                {
//                    Name = "Bánh tráng nướng Dì Đinh",
//                    Description = "Quán bánh tráng nướng nổi tiếng Đà Lạt với hương vị đậm đà, giòn rụm. Được nhiều du khách gọi là 'pizza Đà Lạt'.",
//                    Address = "26 Hoàng Diệu, TP. Đà Lạt",
//                    Latitude = 11.9295,
//                    Longitude = 108.4300,
//                    Category = "Food",
//                    Image = "banh_trang_nuong_di_dinh.jpg",
//                    Rating = 4.7,
//                    ReviewCount = 520,
//                    OpeningHours = "14:00 - 22:00",
//                    PriceRange = "Rẻ"
//                },
//                new LocationPoint
//                {
//                    Name = "Sữa đậu nành Hoa Sữa",
//                    Description = "Quán sữa đậu nành nóng nổi tiếng gần chợ đêm, thích hợp thưởng thức trong thời tiết lạnh.",
//                    Address = "64 Tăng Bạt Hổ, TP. Đà Lạt",
//                    Latitude = 11.9402,
//                    Longitude = 108.4385,
//                    Category = "Drink",
//                    Image = "sua_dau_nanh_hoa_sua.jpg",
//                    Rating = 4.6,
//                    ReviewCount = 430,
//                    OpeningHours = "17:00 - 01:00",
//                    PriceRange = "Rẻ"
//                },
//                new LocationPoint
//                {
//                    Name = "Nem nướng Bà Hùng",
//                    Description = "Quán nem nướng lâu đời với hương vị đặc trưng, ăn kèm rau sống và nước chấm đậm đà.",
//                    Address = "328 Phan Đình Phùng, TP. Đà Lạt",
//                    Latitude = 11.9490,
//                    Longitude = 108.4335,
//                    Category = "Food",
//                    Image = "nem_nuong.jpg",
//                    Rating = 4.5,
//                    ReviewCount = 150,
//                    OpeningHours = "06:00 - 18:00",
//                    PriceRange = "10,000 - 50,000 VND"
//                },


//                new LocationPoint
//                {
//                    Name = "Ốc 33",
//                    Description = "Quán ốc nổi tiếng với nhiều món như ốc len xào dừa, ốc hấp sả. Không gian bình dân, đông khách.",
//                    Address = "33 Hai Bà Trưng, TP. Đà Lạt",
//                    Latitude = 11.9368,
//                    Longitude = 108.4330,
//                    Category = "Food",
//                    Image = "oc_33.jpg",
//                    Rating = 4.5,
//                    ReviewCount = 410,
//                    OpeningHours = "15:00 - 22:00",
//                    PriceRange = "Trung bình"
//                }
//            };

//            foreach (var location in locations)
//            {
//                await _sqliteService.AddLocationPointAsync(location);
//                Debug.WriteLine($"[Seed] Added location: {location.Name}");
//            }
//        }

//        private async Task AddDishesAsync()
//        {
//            var dishes = new List<Dish>
//            {
//                new Dish
//                {
//                    LocationPointId = 4,
//                    Name = "Phở Bò",
//                    Description = "Phở bò truyền thống Hà Nội",
//                    Price = 5.50m,
//                    Category = "Soup",
//                    Image = "pho_bo.jpg",
//                    Rating = 4.6,
//                    ReviewCount = 45
//                },
//                new Dish
//                {
//                    LocationPointId = 4,
//                    Name = "Phở Gà",
//                    Description = "Phở gà nhân dân",
//                    Price = 4.50m,
//                    Category = "Soup",
//                    Image = "pho_ga.jpg",
//                    Rating = 4.5,
//                    ReviewCount = 32
//                },
//                new Dish
//                {
//                    LocationPointId = 1,
//                    Name = "Bánh Mì",
//                    Description = "Bánh mì Sài Gòn đặc sản",
//                    Price = 2.00m,
//                    Category = "Sandwich",
//                    Image = "banh_mi.jpg",
//                    Rating = 4.7,
//                    ReviewCount = 120
//                },
//                new Dish
//                {
//                    LocationPointId = 1,
//                    Name = "Cơm Tấm",
//                    Description = "Cơm tấm Sài Gòn",
//                    Price = 3.50m,
//                    Category = "Rice",
//                    Image = "com_tam.jpg",
//                    Rating = 4.4,
//                    ReviewCount = 88
//                },
//                new Dish
//                {
//                    LocationPointId = 1,
//                    Name = "Chè Ba Màu",
//                    Description = "Chè ba màu truyền thống",
//                    Price = 1.50m,
//                    Category = "Dessert",
//                    Image = "che_ba_mau.jpg",
//                    Rating = 4.8,
//                    ReviewCount = 95
//                }
//            };

//            foreach (var dish in dishes)
//            {
//                await _sqliteService.AddDishAsync(dish);
//                Debug.WriteLine($"[Seed] Added dish: {dish.Name}");
//            }
//        }

//        private async Task AddReviewsAsync()
//        {
//            var reviews = new List<Review>
//            {
//                new Review
//                {
//                    UserId = 1,
//                    LocationPointId = 1,
//                    Rating = 5,
//                    Comment = "Quán ăn rất ngon, tươi tươi!",
//                    Image = "review1.jpg"
//                },
//                new Review
//                {
//                    UserId = 2,
//                    LocationPointId = 1,
//                    Rating = 4,
//                    Comment = "Món ăn tốt nhưng đợi lâu",
//                    Image = "review2.jpg"
//                },
//                new Review
//                {
//                    UserId = 3,
//                    DishId = 1,
//                    Rating = 5,
//                    Comment = "Phở bò ngon tuyệt vời!",
//                    Image = "review3.jpg"
//                },
//                new Review
//                {
//                    UserId = 1,
//                    LocationPointId = 2,
//                    Rating = 5,
//                    Comment = "Địa điểm tuyệt đẹp để chụp ảnh",
//                    Image = "review4.jpg"
//                },
//                new Review
//                {
//                    UserId = 2,
//                    DishId = 3,
//                    Rating = 4,
//                    Comment = "Bánh mì rất tươi, giá cả hợp lý",
//                    Image = "review5.jpg"
//                }
//            };

//            foreach (var review in reviews)
//            {
//                await _sqliteService.AddReviewAsync(review);
//                Debug.WriteLine($"[Seed] Added review: {review.Id}");
//            }
//        }
//    }
//}