using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace LightNovel.Controls
{
	/// <summary>
	/// A type converter for visibility and boolean values.
	/// </summary>
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public BooleanToVisibilityConverter()
		{
			VisiableValue = true;
		}
		bool _visiable;
		public bool VisiableValue
		{
			get
			{ return _visiable; }
			set
			{ _visiable = value; }
		}

		public object Convert(object value, Type targetType, object parameter, string language){
			bool visibility = (bool)value == VisiableValue;
			return visibility ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(
			object value,
			Type targetType,
			object parameter,
			string language)
		{
			Visibility visibility = (Visibility)value;
			return (visibility == Visibility.Visible) == VisiableValue;
		}
	}
	//public class UriCacheConverter : IValueConverter
	//{
	//	public object Convert(object value, Type targetType, object parameter, string language)
	//	{
	//		if (value is Uri)
	//		{
	//			Q42.WinRT.Data.WebDataCache(new Uri(bookmark.DescriptionImageUri))

	//		}
	//		int term = int.Parse((string)parameter);
	//		int a = (int)value;
	//		return (a+term).ToString();
	//	}
	//}

	public class PlusConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			int term = int.Parse((string)parameter);
			int a = (int)value;
			return (a+term).ToString();
		}

		public object ConvertBack(
			object value,
			Type targetType,
			object parameter,
			string language)
		{
			try
			{
				return int.Parse((string)value) - int.Parse((string)parameter);
			}
			catch (Exception)
			{
				return 0;
			}
		}
	}
}
