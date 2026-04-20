using SQLite;
using doanC_.Models;
using System.Diagnostics;

namespace doanC_.Services.Data
{
    public class SQLiteService
    {
        private const string DbFileName = "foodstreet.db3";
        private string _dbPath;
        private SQLiteAsyncConnection _database;

        public SQLiteAsyncConnection Database
        {
            get
            {
                if (_database == null)
                {
                    _database = new SQLiteAsyncConnection(_dbPath);
                }
                return _database;
            }
        }

        public SQLiteService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
        }

        public async Task InitializeAsync()
        {
            try
            {
                await Database.CreateTableAsync<User>();
                await Database.CreateTableAsync<LocationPoint>();
                await Database.CreateTableAsync<Dish>();
                await Database.CreateTableAsync<Review>();

                Debug.WriteLine($"[SQLite] Database initialized at: {_dbPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] Initialization error: {ex.Message}");
                throw;
            }
        }

        // ============ USER OPERATIONS ============
        public async Task<int> AddUserAsync(User user)
        {
            try
            {
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                return await Database.InsertAsync(user);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] AddUser error: {ex.Message}");
                throw;
            }
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            try
            {
                return await Database.GetAsync<User>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetUserById error: {ex.Message}");
                return null;
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await Database.Table<User>()
                    .Where(u => u.Username == username)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetUserByUsername error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                return await Database.Table<User>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetAllUsers error: {ex.Message}");
                return new List<User>();
            }
        }

        public async Task<int> UpdateUserAsync(User user)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                return await Database.UpdateAsync(user);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] UpdateUser error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteUserAsync(int id)
        {
            try
            {
                return await Database.DeleteAsync<User>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] DeleteUser error: {ex.Message}");
                throw;
            }
        }

        // ============ LOCATION POINT OPERATIONS ============
        public async Task<int> AddLocationPointAsync(LocationPoint location)
        {
            try
            {
                location.CreatedAt = DateTime.UtcNow;
                location.UpdatedAt = DateTime.UtcNow;
                return await Database.InsertAsync(location);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] AddLocationPoint error: {ex.Message}");
                throw;
            }
        }

        // 👉 THÊM METHOD NÀY - LƯU NHIỀU ĐỊA ĐIỂM
        public async Task<int> SaveLocationPointsAsync(List<LocationPoint> locations)
        {
            try
            {
                // Xóa tất cả cũ
                await Database.DeleteAllAsync<LocationPoint>();

                // Thêm mới
                var count = await Database.InsertAllAsync(locations);
                Debug.WriteLine($"[SQLite] 💾 Saved {count} locations to cache");
                return count;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] SaveLocationPoints error: {ex.Message}");
                return 0;
            }
        }

        public async Task<LocationPoint> GetLocationPointByIdAsync(int id)
        {
            try
            {
                return await Database.GetAsync<LocationPoint>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetLocationPointById error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<LocationPoint>> GetAllLocationPointsAsync()
        {
            try
            {
                return await Database.Table<LocationPoint>().ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetAllLocationPoints error: {ex.Message}");
                return new List<LocationPoint>();
            }
        }

        public async Task<List<LocationPoint>> GetLocationPointsByCategoryAsync(string category)
        {
            try
            {
                return await Database.Table<LocationPoint>()
                    .Where(l => l.Category == category)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetLocationPointsByCategory error: {ex.Message}");
                return new List<LocationPoint>();
            }
        }

        public async Task<int> UpdateLocationPointAsync(LocationPoint location)
        {
            try
            {
                location.UpdatedAt = DateTime.UtcNow;
                return await Database.UpdateAsync(location);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] UpdateLocationPoint error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteLocationPointAsync(int id)
        {
            try
            {
                // Delete associated dishes first
                var dishes = await Database.Table<Dish>()
                    .Where(d => d.LocationPointId == id)
                    .ToListAsync();

                foreach (var dish in dishes)
                {
                    await DeleteDishAsync(dish.Id);
                }

                return await Database.DeleteAsync<LocationPoint>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] DeleteLocationPoint error: {ex.Message}");
                throw;
            }
        }

        // ============ DISH OPERATIONS ============
        public async Task<int> AddDishAsync(Dish dish)
        {
            try
            {
                dish.CreatedAt = DateTime.UtcNow;
                dish.UpdatedAt = DateTime.UtcNow;
                return await Database.InsertAsync(dish);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] AddDish error: {ex.Message}");
                throw;
            }
        }

        public async Task<Dish> GetDishByIdAsync(int id)
        {
            try
            {
                return await Database.GetAsync<Dish>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetDishById error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Dish>> GetDishesByLocationAsync(int locationPointId)
        {
            try
            {
                return await Database.Table<Dish>()
                    .Where(d => d.LocationPointId == locationPointId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetDishesByLocation error: {ex.Message}");
                return new List<Dish>();
            }
        }

        public async Task<List<Dish>> GetDishesByCategoryAsync(string category)
        {
            try
            {
                return await Database.Table<Dish>()
                    .Where(d => d.Category == category)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetDishesByCategory error: {ex.Message}");
                return new List<Dish>();
            }
        }

        public async Task<int> UpdateDishAsync(Dish dish)
        {
            try
            {
                dish.UpdatedAt = DateTime.UtcNow;
                return await Database.UpdateAsync(dish);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] UpdateDish error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteDishAsync(int id)
        {
            try
            {
                // Delete associated reviews first
                var reviews = await Database.Table<Review>()
                    .Where(r => r.DishId == id)
                    .ToListAsync();

                foreach (var review in reviews)
                {
                    await DeleteReviewAsync(review.Id);
                }

                return await Database.DeleteAsync<Dish>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] DeleteDish error: {ex.Message}");
                throw;
            }
        }

        // ============ REVIEW OPERATIONS ============
        public async Task<int> AddReviewAsync(Review review)
        {
            try
            {
                review.CreatedAt = DateTime.UtcNow;
                review.UpdatedAt = DateTime.UtcNow;
                return await Database.InsertAsync(review);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] AddReview error: {ex.Message}");
                throw;
            }
        }

        public async Task<Review> GetReviewByIdAsync(int id)
        {
            try
            {
                return await Database.GetAsync<Review>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetReviewById error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Review>> GetReviewsByLocationAsync(int locationPointId)
        {
            try
            {
                return await Database.Table<Review>()
                    .Where(r => r.LocationPointId == locationPointId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetReviewsByLocation error: {ex.Message}");
                return new List<Review>();
            }
        }

        public async Task<List<Review>> GetReviewsByDishAsync(int dishId)
        {
            try
            {
                return await Database.Table<Review>()
                    .Where(r => r.DishId == dishId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetReviewsByDish error: {ex.Message}");
                return new List<Review>();
            }
        }

        public async Task<List<Review>> GetReviewsByUserAsync(int userId)
        {
            try
            {
                return await Database.Table<Review>()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetReviewsByUser error: {ex.Message}");
                return new List<Review>();
            }
        }

        public async Task<int> UpdateReviewAsync(Review review)
        {
            try
            {
                review.UpdatedAt = DateTime.UtcNow;
                return await Database.UpdateAsync(review);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] UpdateReview error: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteReviewAsync(int id)
        {
            try
            {
                return await Database.DeleteAsync<Review>(id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] DeleteReview error: {ex.Message}");
                throw;
            }
        }

        // ============ UTILITY METHODS ============
        public async Task<int> GetLocationPointCountAsync()
        {
            try
            {
                return await Database.Table<LocationPoint>().CountAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] GetLocationPointCount error: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> ClearAllLocationPointsAsync()
        {
            try
            {
                await Database.DeleteAllAsync<LocationPoint>();
                Debug.WriteLine("[SQLite] 🗑️ All location points cleared");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQLite] ClearAll error: {ex.Message}");
                return false;
            }
        }
    }
}