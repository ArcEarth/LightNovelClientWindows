using LightNovel.Common;
using LightNovel.Service;
using LightNovel.ViewModels;
using Q42.WinRT.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.Extensions;

namespace LightNovel
{
	public class StringToSymbolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var name = value as string;
			if (name == "Recent" || name.Contains("最近阅读"))
				return Symbol.Clock;
			if (name == "Favorite" || name.Contains("收藏"))
				return Symbol.Favorite;
			if (name.Contains("新番") || name.Contains("动画"))
				return Symbol.Play;
			if (name.Contains("热门"))
				return Symbol.Like;
			if (name.Contains("新"))
				return Symbol.Calendar;
			return Symbol.Library;
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
