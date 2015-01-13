using LightNovel.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LightNovel.Controls
{
	class LineViewDataTemplateSelector : DataTemplateSelector
	{
		public DataTemplate ImageDataTemplate { get; set; }
		public DataTemplate TextDataTemplate { get; set; }
		protected override DataTemplate SelectTemplateCore(object item)
		{
			var lvm = item as LineViewModel;
			if (lvm != null)
			{
				if (lvm.IsImage)
					return ImageDataTemplate;
				else
					return TextDataTemplate;
			}
			return base.SelectTemplate(item);
		}
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			var lvm = item as LineViewModel;
			if (lvm != null)
			{
				return SelectTemplateCore(item);
			}
			return base.SelectTemplate(item, container);
		}

	}
}
