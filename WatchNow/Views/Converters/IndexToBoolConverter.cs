using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace WatchNow.Avalonia.Views.Converters
{
	public class IndexToBoolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return System.Convert.ToInt32(value) == System.Convert.ToInt32(parameter);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return AvaloniaProperty.UnsetValue;
		}
	}
}
