namespace doanC_.Views;

public partial class PoiListPage : ContentPage
{
    public PoiListPage()
    {
     InitializeComponent();
        LoadSampleData();
    }

    private void LoadSampleData()
    {
        // D? li?u m?u - sau này s? load t? database ho?c API
        var samplePois = new List<PoiItem>
        {
            new PoiItem { Name = "Quán Phở Hà Nội", Description = "Phở truyền thống chính gốc", Distance = 50, ImageUrl = "dotnet_bot.png" },
            new PoiItem { Name = "Bánh Mì Sài Gòn", Description = "Bánh mì đặc sản miền Nam", Distance = 120, ImageUrl = "dotnet_bot.png" },
     new PoiItem { Name = "Cà Phê Vẹt", Description = "Cà phê truyền thống Việt Nam", Distance = 200, ImageUrl = "dotnet_bot.png" },
            new PoiItem { Name = "Chùa Một Cột", Description = "Di tích lịcch sử nổi tiếng", Distance = 350, ImageUrl = "dotnet_bot.png" }
        };

        PoiCollection.ItemsSource = samplePois;  // gán danh sách các item
    }

  private void OnFilterChanged(object sender, EventArgs e)
    {
        // X? lý l?c d? li?u
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
   // X? lý tìm ki?m
    }

    private async void OnPoiSelected(object sender, SelectionChangedEventArgs e)
  {
 if (e.CurrentSelection.FirstOrDefault() is PoiItem selectedPoi)
     {
   await Shell.Current.GoToAsync($"//PoiDetailPage?poiId={selectedPoi.Name}");
 }
    }
}

public class PoiItem
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Distance { get; set; }
    public string ImageUrl { get; set; }
}
