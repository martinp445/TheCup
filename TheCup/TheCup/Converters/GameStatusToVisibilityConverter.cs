using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TheCup_Domain.Enums;

namespace TheCup_Presentation.Converters;

public sealed class GameStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GameStatus status)
        {
            if (parameter is string raw)
            {
                var param = raw.Trim();
                if (string.Equals(param, "Scheduled", StringComparison.OrdinalIgnoreCase))
                {
                    return status == GameStatus.Scheduled ? Visibility.Visible : Visibility.Collapsed;
                }

                if (string.Equals(param, "Ongoing", StringComparison.OrdinalIgnoreCase))
                {
                    return status == GameStatus.Ongoing ? Visibility.Visible : Visibility.Collapsed;
                }

                if (string.Equals(param, "Editable", StringComparison.OrdinalIgnoreCase))
                {
                    // Editable when a game is Scheduled (before start) or Ongoing (in progress)
                    return status == GameStatus.Scheduled || status == GameStatus.Ongoing
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }

            // Default: only show for ongoing games
            return status == GameStatus.Ongoing ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
