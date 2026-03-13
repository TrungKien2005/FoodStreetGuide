namespace doanC_.Views;

public partial class QrScannerPage : ContentPage
{
    public QrScannerPage()
    {
        InitializeComponent();
    }

    private async void OnStartScanClicked(object sender, EventArgs e)
    {
// Ki?m tra quy?n camera
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        
     if (status != PermissionStatus.Granted)
        {
 status = await Permissions.RequestAsync<Permissions.Camera>();
   }

        if (status == PermissionStatus.Granted)
        {
  // Kh?i ??ng QR Scanner
  // S? tích h?p th? vi?n nh? ZXing.Net.Maui ho?c BarcodeScanner.Mobile
      await DisplayAlert("QR Scanner", "Tính n?ng quét QR ?ang ???c phát tri?n", "OK");
      }
  else
        {
        await DisplayAlert("Quy?n truy c?p", "C?n c?p quy?n camera ?? quét mã QR", "OK");
        }
    }

    // Callback khi quét thành công
    private async void OnQrCodeDetected(string qrData)
    {
        // Parse QR data ?? l?y POI ID
    // Chuy?n ??n trang chi ti?t POI
        await Shell.Current.GoToAsync($"//PoiDetailPage?poiId={qrData}");
    }
}
