using doanC_.Services.Api;
using doanC_.Services.Offline;
using Microsoft.Maui.Controls;

namespace doanC_
{
    public partial class MainPage : ContentPage
    {
        private readonly ApiService _apiService;
        int count = 0;

        public MainPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;

            // Test API khi khởi động
            LoadData();
        }

        private async void LoadData()
        {
            // Hiển thị loading
            var loadingLabel = new Label { Text = "Đang tải dữ liệu...", HorizontalOptions = LayoutOptions.Center };
            Content = loadingLabel;

            // Gọi API
            var locations = await _apiService.GetLocationPointsAsync();

            if (locations != null && locations.Any())
            {
                // Hiển thị danh sách
                var stackLayout = new VerticalStackLayout();
                foreach (var loc in locations.Take(5))
                {
                    stackLayout.Add(new Label
                    {
                        Text = $"📍 {loc.Name}\n   {loc.Address}\n   ⭐ {loc.Rating}\n",
                        Margin = 10
                    });
                }
                Content = new ScrollView { Content = stackLayout };
            }
            else
            {
                Content = new Label
                {
                    Text = "❌ Không có dữ liệu.\nKiểm tra:\n1. doanC_Admin đang chạy?\n2. Cổng 5225?\n3. URL trong ApiConfig đúng?",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
            }
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;
            CounterBtn.Text = $"Clicked {count} times";
        }
    }
}