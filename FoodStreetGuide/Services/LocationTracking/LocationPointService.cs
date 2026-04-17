using doanC_.Helpers;
using doanC_.Models;
using System.Diagnostics;
using doanC_.Services.Data;

namespace doanC_.Services.LocationTracking;

public class LocationPointService
{
    private readonly SQLiteService _sqliteService;

    public LocationPointService()
    {
        _sqliteService = ServiceHelper.GetService<SQLiteService>();
    }

    public async Task<List<LocationPoint>> GetLocationsAsync()
    {
        try
        {
            var points = await _sqliteService.GetAllLocationPointsAsync();
            return points ?? new List<LocationPoint>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LocationPointService] Error: {ex.Message}");
            return new List<LocationPoint>();
        }
    }
}