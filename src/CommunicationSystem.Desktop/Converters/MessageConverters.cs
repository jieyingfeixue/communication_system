using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using CommunicationSystem.Shared.Enums;

namespace CommunicationSystem.Desktop.Converters;

public class UrlToImageConverter : IValueConverter
{
    private static readonly Dictionary<string, BitmapImage> Cache = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class MessageTypeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MessageType type || parameter is not string expected)
            return Visibility.Collapsed;

        return type.ToString().Equals(expected, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
