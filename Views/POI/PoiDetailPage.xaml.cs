namespace doanC_.Views;

[QueryProperty(nameof(PoiId), "poiId")]
public partial class PoiDetailPage : ContentPage
{
    private bool isPlaying = false;
    private string poiId;

    public string PoiId
    {
        get => poiId;
      set
        {
          poiId = value;
            LoadPoiDetails();
        }
    }

    public PoiDetailPage()
    {
        InitializeComponent();
    }

 private void LoadPoiDetails()
    {
   // Load chi ti?t POI t? database ho?c API
     PoiNameLabel.Text = PoiId;
        DescriptionLabel.Text = "?ây là mô t? chi ti?t v? " + PoiId + ". Thông tin v? l?ch s?, ??c ?i?m, và nh?ng ?i?u thú v?...";
  }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        // Thay ??i file audio theo ngôn ng? ???c ch?n
        var selectedLanguage = AudioLanguagePicker.SelectedItem?.ToString();
   // Load audio file t??ng ?ng
    }

    private void OnPlayPauseClicked(object sender, EventArgs e)
    {
        isPlaying = !isPlaying;
        
        if (isPlaying)
 {
     PlayPauseButton.Text = "? T?m d?ng";
      // B?t ??u phát audio
        }
    else
      {
            PlayPauseButton.Text = "? Phát";
         // T?m d?ng audio
        }
    }

    private async void OnGetDirectionsClicked(object sender, EventArgs e)
    {
        // M? Google Maps ho?c Apple Maps ?? ch? ???ng
        await DisplayAlert("Ch? ???ng", "Tính n?ng ch? ???ng ?ang ???c phát tri?n", "OK");
    }
}
