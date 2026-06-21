using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CommunicationSystem.Desktop.Services;

public static class ImageCache
{
    private static readonly Dictionary<string, ImageSource> Cache = new();

    public static ImageSource? Get(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (Cache.TryGetValue(url, out var cached))
            return cached;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(url, UriKind.Absolute);
            bitmap.DecodePixelWidth = 260;
            bitmap.CacheOption = BitmapCacheOption.OnDemand;
            bitmap.EndInit();
            Cache[url] = bitmap;
            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
