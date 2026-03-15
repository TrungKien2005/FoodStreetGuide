using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;
using Mapsui.Tiling;
using Mapsui.Layers;
using Mapsui.Features;
using Mapsui.Styles;
using doanC_.Models;

// tránh xung đột MAUI
using MapsuiBrush = Mapsui.Styles.Brush;
using MapsuiColor = Mapsui.Styles.Color;

namespace doanC_.Services;

public class MapService
{
    private MemoryLayer? userLayer;
    private MemoryLayer? poiLayer;

    public void InitializeMap(MapControl map)
    {
        map.Map = new Mapsui.Map();
        map.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
    }

    public void FocusToLocation(MapControl map, double latitude, double longitude)
    {
        var spherical = SphericalMercator.FromLonLat(longitude, latitude);
        var point = new MPoint(spherical.x, spherical.y);

        map.Map.Navigator.CenterOn(point);
        map.Map.Navigator.ZoomTo(15000);
    }

    public void ShowUserLocation(MapControl map, double latitude, double longitude)
    {
        var spherical = SphericalMercator.FromLonLat(longitude, latitude);

        var feature = new PointFeature(new MPoint(spherical.x, spherical.y));

        feature.Styles.Add(new SymbolStyle
        {
            Fill = new MapsuiBrush(MapsuiColor.Blue),
            SymbolScale = 1
        });

        userLayer = new MemoryLayer
        {
            Name = "UserLocation",
            Features = new List<IFeature> { feature }
        };

        map.Map.Layers.Add(userLayer);

        map.Refresh();
    }

    public void UpdateUserLocation(MapControl map, double latitude, double longitude)
    {
        if (userLayer == null) return;

        var spherical = SphericalMercator.FromLonLat(longitude, latitude);

        var feature = new PointFeature(new MPoint(spherical.x, spherical.y));

        feature.Styles.Add(new SymbolStyle
        {
            Fill = new MapsuiBrush(MapsuiColor.Blue),
            SymbolScale = 1
        });

        userLayer.Features = new List<IFeature> { feature };

        map.Refresh();
    }

    public void AddLocationPoints(MapControl map, List<LocationPoint> points)
    {
        var features = new List<IFeature>();

        foreach (var p in points)
        {
            var spherical = SphericalMercator.FromLonLat(p.Longitude, p.Latitude);

            var feature = new PointFeature(new MPoint(spherical.x, spherical.y));

            feature.Styles.Add(new SymbolStyle
            {
                Fill = new MapsuiBrush(MapsuiColor.Red),
                SymbolScale = 0.8
            });

            features.Add(feature);
        }

        poiLayer = new MemoryLayer
        {
            Name = "POI",
            Features = features
        };

        map.Map.Layers.Add(poiLayer);

        map.Refresh();
    }

    public void ClearPOI(MapControl map)
    {
        if (poiLayer != null)
        {
            map.Map.Layers.Remove(poiLayer);
            poiLayer = null;

            map.Refresh();
        }
    }
}