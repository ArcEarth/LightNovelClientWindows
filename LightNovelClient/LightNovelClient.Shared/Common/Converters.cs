using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace LightNovel.Common
{
	/// <summary>
	/// Value converter that translates true to <see cref="Visibility.Visible"/> and false to
	/// <see cref="Visibility.Collapsed"/>.
	/// </summary>
	public sealed class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value is bool && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return value is Visibility && (Visibility)value == Visibility.Visible;
		}
	}

	/// <summary>
	/// Value converter that translates true to false and vice versa.
	/// </summary>
	public sealed class BooleanNegationConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return !(value is bool && (bool)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return !(value is bool && (bool)value);
		}
	}

	public sealed class OrientationToVisibilityConverter : IValueConverter
	{
		public Orientation VisibleOrientation { get; set; }
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (Orientation)value == VisibleOrientation ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException("This convert don't support convert back");
		}
	}

	public class StringToSymbolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var name = value as string;
			if (name.Contains("新番"))
				return Symbol.Play;
			switch (name)
			{
				case "即将动画化":
					return Symbol.Emoji;
				case "热门轻小说":
					return Symbol.Like;
				case "最近更新轻小说":
					return Symbol.Calendar;
				default:
					return Symbol.Library;
			}
		}

		public object ConvertBack(
			object value,
			Type targetType,
			object parameter,
			string language)
		{
			throw new NotImplementedException();
		}
	}


}
