using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Data;
using System.Globalization;

namespace LightNovel.Controls
{
	/// <summary>
	/// A type converter for visibility and boolean values.
	/// </summary>
	public class BooleanToVisibilityConverter : IValueConverter
	{	
		public BooleanToVisibilityConverter ()
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
		public object Convert(
			object value,
			Type targetType,
			object parameter,
			CultureInfo culture)
		{
			bool visibility = (bool)value == VisiableValue;
			return visibility ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(
			object value,
			Type targetType,
			object parameter,
			CultureInfo culture)
		{
			Visibility visibility = (Visibility)value;
			return (visibility == Visibility.Visible) == VisiableValue;
		}
	}

	public class ProgressRing : Control
	{
		bool hasAppliedTemplate = false;

		public ProgressRing()
		{
			this.DefaultStyleKey = typeof(ProgressRing);
			TemplateSettings = new TemplateSettingValues(60);
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			hasAppliedTemplate = true;
			UpdateState(this.IsActive);
		}

		void UpdateState(bool isActive)
		{
			if (hasAppliedTemplate)
			{
				string state = isActive ? "Active" : "Inactive";
				System.Windows.VisualStateManager.GoToState(this, state, true);
			}
		}

		protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
		{
			var width = 100D;
			if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
				width = this.Width != double.NaN ? this.Width : availableSize.Width;
			TemplateSettings = new TemplateSettingValues(width);
			return base.MeasureOverride(availableSize);
		}

		public bool IsActive
		{
			get { return (bool)GetValue(IsActiveProperty); }
			set { SetValue(IsActiveProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsActive.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.Register("IsActive", typeof(bool), typeof(ProgressRing), new PropertyMetadata(false, new PropertyChangedCallback(IsActiveChanged)));

		private static void IsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
		{
			var pr = (ProgressRing)d;
			var isActive = (bool)args.NewValue;
			pr.UpdateState(isActive);
		}

		public TemplateSettingValues TemplateSettings
		{
			get { return (TemplateSettingValues)GetValue(TemplateSettingsProperty); }
			set { SetValue(TemplateSettingsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TemplateSettings.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TemplateSettingsProperty =
			DependencyProperty.Register("TemplateSettings", typeof(TemplateSettingValues), typeof(ProgressRing), new PropertyMetadata(new TemplateSettingValues(100)));


		public class TemplateSettingValues : System.Windows.DependencyObject
		{
			public TemplateSettingValues(double width)
			{
				MaxSideLength = 400;
				EllipseDiameter = width / 10;
				EllipseOffset = new System.Windows.Thickness(EllipseDiameter);
			}

			public double MaxSideLength
			{
				get { return (double)GetValue(MaxSideLengthProperty); }
				set { SetValue(MaxSideLengthProperty, value); }
			}

			// Using a DependencyProperty as the backing store for MaxSideLength.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty MaxSideLengthProperty =
				DependencyProperty.Register("MaxSideLength", typeof(double), typeof(TemplateSettingValues), new PropertyMetadata(0D));

			public double EllipseDiameter
			{
				get { return (double)GetValue(EllipseDiameterProperty); }
				set { SetValue(EllipseDiameterProperty, value); }
			}

			// Using a DependencyProperty as the backing store for EllipseDiameter.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty EllipseDiameterProperty =
				DependencyProperty.Register("EllipseDiameter", typeof(double), typeof(TemplateSettingValues), new PropertyMetadata(0D));

			public Thickness EllipseOffset
			{
				get { return (Thickness)GetValue(EllipseOffsetProperty); }
				set { SetValue(EllipseOffsetProperty, value); }
			}

			// Using a DependencyProperty as the backing store for EllipseOffset.  This enables animation, styling, binding, etc...
			public static readonly DependencyProperty EllipseOffsetProperty =
				DependencyProperty.Register("EllipseOffset", typeof(Thickness), typeof(TemplateSettingValues), new PropertyMetadata(new Thickness()));
		}
	}
}
