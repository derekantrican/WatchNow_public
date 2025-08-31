using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WatchNow.Avalonia.Views.Converters
{
	public class TabBackgroundColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((bool)value)
			{
				return new SolidColorBrush(Color.Parse("#FFFA8072"));
			}
			else
			{
				return new SolidColorBrush(Color.Parse("#1f1f1f"));
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return AvaloniaProperty.UnsetValue;
		}
	}
}
