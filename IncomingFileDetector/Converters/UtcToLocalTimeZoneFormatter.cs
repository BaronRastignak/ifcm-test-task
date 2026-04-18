using System.Globalization;
using System.Windows.Data;

namespace IncomingFileDetector.Converters;

internal class UtcToLocalTimeZoneFormatter : IValueConverter
{
    #region Implementation of IValueConverter

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is DateTimeOffset dateTimeOffset ? dateTimeOffset.ToLocalTime() : value;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    #endregion
}