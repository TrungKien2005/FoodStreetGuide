using System.Text.Json;
using doanC_.Models;
using doanC_.Services;
using ZXing.Net.Maui;
using doanC_.Services.Geo;

namespace doanC_.Views;

public partial class QrScannerPage : ContentPage
{
    private bool _isScanning = true;

    public QrScannerPage()
    {
        InitializeComponent();
    }

    // 🔥 Khi mở trang → xin quyền + bật scan
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Camera>();

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Lỗi", "Cần cấp quyền camera để quét QR", "OK");
            return;
        }

        cameraView.IsDetecting = true;
    }

    // 🔥 EVENT QUÉT QR (QUAN TRỌNG NHẤT)
    private void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (!_isScanning) return;

        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        _isScanning = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            cameraView.IsDetecting = false;

            await HandleQr(result.Value);

            // Cho phép quét lại
            _isScanning = true;
            cameraView.IsDetecting = true;
        });
    }

    // 🔥 XỬ LÝ QR
    private async Task HandleQr(string qrText)
    {
        try
        {
            // Parse JSON → LocationPoint
            var point = JsonSerializer.Deserialize<LocationPoint>(qrText);

            if (point != null)
            {
                await DisplayAlert("QR OK", $"Đã thêm: {point.Name}", "OK");

                GeoFenceService.Instance.AddPoint(point);
            }
            else
            {
                await DisplayAlert("QR", qrText, "OK");
            }
        }
        catch
        {
            await DisplayAlert("Lỗi", "QR không đúng định dạng JSON", "OK");
        }
    }

    // 🔥 NHẬP TAY (fallback)
    private async void OnManualInputClicked(object sender, EventArgs e)
    {
        string input = await DisplayPromptAsync("Nhập QR", "Dán nội dung QR:");

        if (!string.IsNullOrEmpty(input))
        {
            await HandleQr(input);
        }
    }
}