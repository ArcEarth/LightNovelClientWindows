using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace LightNovel.Controls
{
	/// <summary>
	/// Wrapper for <see cref="RichTextBlock"/> that creates as many additional overflow
	/// columns as needed to fit the available content.
	/// </summary>
	/// <example>
	/// The following creates a collection of 400-pixel wide columns spaced 50 pixels apart
	/// to contain arbitrary data-bound content:
	/// <code>
	/// <RichTextColumns>
	///     <RichTextColumns.ColumnTemplate>
	///         <DataTemplate>
	///             <RichTextBlockOverflow Width="400" Margin="50,0,0,0"/>
	///         </DataTemplate>
	///     </RichTextColumns.ColumnTemplate>
	///     
	///     <RichTextBlock Width="400">
	///         <Paragraph>
	///             <Run Text="{Binding Content}"/>
	///         </Paragraph>
	///     </RichTextBlock>
	/// </RichTextColumns>
	/// </code>
	/// </example>
	/// <remarks>Typically used in a horizontally scrolling region where an unbounded amount of
	/// space allows for all needed columns to be created.  When used in a vertically scrolling
	/// space there will never be any additional columns.</remarks>
	[Windows.UI.Xaml.Markup.ContentProperty(Name = "RichTextContent")]
	public sealed class RichTextColumns : Panel , IScrollSnapPointsInfo
	{
		/// <summary>
		/// Identifies the <see cref="Orientation"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register("Orientation", typeof(Orientation),
			typeof(RichTextColumns), new PropertyMetadata(Orientation.Horizontal, ResetOverflowLayout));

		/// <summary>
		/// Identifies the <see cref="RichTextContent"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty RichTextContentProperty =
			DependencyProperty.Register("RichTextContent", typeof(RichTextBlock),
			typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

		/// <summary>
		/// Identifies the <see cref="ColumnTemplate"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ColumnTemplateProperty =
			DependencyProperty.Register("ColumnTemplate", typeof(DataTemplate),
			typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

		/// <summary>
		/// Identifies the <see cref="ColumnWidth"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ColumnWidthProperty =
			DependencyProperty.Register("ColumnWidth", typeof(double),
			typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

		/// <summary>
		/// Identifies the <see cref="ColumnHeight"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ColumnHeightProperty =
			DependencyProperty.Register("ColumnHeight", typeof(double),
			typeof(RichTextColumns), new PropertyMetadata(null, ResetOverflowLayout));

		/// <summary>
		/// Identifies the <see cref="ColumnsPerPage"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ColumnsPerPageProperty =
			DependencyProperty.Register("ColumnsPerPage", typeof(int),
			typeof(RichTextColumns), new PropertyMetadata(1, NotifySnapPointsChanged));

		/// <summary>
		/// Initializes a new instance of the <see cref="RichTextColumns"/> class.
		/// </summary>
		public RichTextColumns()
		{
			this.HorizontalAlignment = HorizontalAlignment.Left;
		}

		public bool IsLayoutValiad { get; private set; }

		/// <summary>
		/// Gets or sets the initial rich text content to be used as the first column.
		/// </summary>
		public RichTextBlock RichTextContent
		{
			get { return (RichTextBlock)GetValue(RichTextContentProperty); }
			set { SetValue(RichTextContentProperty, value); }
		}

		/// <summary>
		/// Gets or sets the template used to create additional
		/// <see cref="RichTextBlockOverflow"/> instances.
		/// </summary>
		public DataTemplate ColumnTemplate
		{
			get { return (DataTemplate)GetValue(ColumnTemplateProperty); }
			set { SetValue(ColumnTemplateProperty, value); }
		}
		public double ColumnWidth
		{
			get { return (double)GetValue(ColumnWidthProperty); }
			set {
				if (Math.Abs(value - (double)GetValue(ColumnWidthProperty)) > 0.5)
					SetValue(ColumnWidthProperty, value);
			}
		}

		public double ColumnHeight
		{
			get { return (double)GetValue(ColumnHeightProperty); }
			set {
				if (Math.Abs(value - (double)GetValue(ColumnHeightProperty)) > 0.5)
					SetValue(ColumnHeightProperty, value);
			}
		}

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set
			{
				if (value != (Orientation)GetValue(OrientationProperty))
				{
					SetValue(OrientationProperty, value);
				}
			}
		}

		public int ColumnsPerPage
		{
			get { return (int)GetValue(ColumnsPerPageProperty); }
			set
			{
				if (value != (int)GetValue(ColumnsPerPageProperty))
				{
					SetValue(ColumnsPerPageProperty, value);
				}
			}
		}
		/// <summary>
		/// Invoked when the content or overflow template is changed to recreate the column layout.
		/// </summary>
		/// <param name="d">Instance of <see cref="RichTextColumns"/> where the change
		/// occurred.</param>
		/// <param name="e">Event data describing the specific change.</param>
		public static void ResetOverflowLayout(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			// When dramatic changes occur, rebuild the column layout from scratch
			var target = d as RichTextColumns;
			if (target != null)
			{
				if (target.RichTextContent != null)
				{
					if (!Double.IsNaN(target.ColumnWidth))
					{
						target.RichTextContent.Width = target.ColumnWidth;
						target.RichTextContent.MaxWidth = target.ColumnWidth;
					}
					if (!Double.IsNaN(target.ColumnHeight))
					{
						target.RichTextContent.Height = target.ColumnHeight;
						target.RichTextContent.MaxHeight = target.ColumnHeight;
					}
				}
				target.IsLayoutValiad = false;
				target.InvalidateMeasure();
				target.InvalidateArrange();
			}
		}

		/// <summary>
		/// Lists overflow columns already created.  Must maintain a 1:1 relationship with
		/// instances in the <see cref="Panel.Children"/> collection following the initial
		/// RichTextBlock child.
		/// </summary>
		private List<RichTextBlockOverflow> _overflowColumns = null;

		/// <summary>
		/// Determines whether additional overflow columns are needed and if existing columns can
		/// be removed.
		/// </summary>
		/// <param name="availableSize">The size of the space available, used to constrain the
		/// number of additional columns that can be created.</param>
		/// <returns>The resulting size of the original content plus any extra columns.</returns>
		protected override Size MeasureOverride(Size availableSize)
		{
			//availableSize = base.MeasureOverride(availableSize);
			Debug.WriteLine("RichTextColumn_Measure , AvailableSize {0}", availableSize);
			if (this.RichTextContent == null) return new Size(0, 0);

			// Make sure the RichTextBlock is a child, using the lack of
			// a list of additional columns as a sign that this hasn't been
			// done yet
			if (this._overflowColumns == null)
			{
				Children.Add(this.RichTextContent);
				this._overflowColumns = new List<RichTextBlockOverflow>();
			}

			// Start by measuring the original RichTextBlock content
			this.RichTextContent.Measure(availableSize);
			var maxWidth = this.RichTextContent.DesiredSize.Width;
			var maxHeight = this.RichTextContent.DesiredSize.Height;
			var hasOverflow = this.RichTextContent.HasOverflowContent;

			// Make sure there are enough overflow columns
			int overflowIndex = 0;
			bool condition = hasOverflow && this.ColumnTemplate != null;
			if (Orientation == Windows.UI.Xaml.Controls.Orientation.Horizontal)
				condition = condition && maxWidth < availableSize.Width;
			else
				condition = condition && maxHeight < availableSize.Height;
			while (condition)
			{
				// Use existing overflow columns until we run out, then create
				// more from the supplied template
				RichTextBlockOverflow overflow;
				if (this._overflowColumns.Count > overflowIndex)
				{
					overflow = this._overflowColumns[overflowIndex];
				}
				else
				{
					overflow = (RichTextBlockOverflow)this.ColumnTemplate.LoadContent();
					this._overflowColumns.Add(overflow);
					this.Children.Add(overflow);
					if (overflowIndex == 0)
					{
						this.RichTextContent.OverflowContentTarget = overflow;
					}
					else
					{
						this._overflowColumns[overflowIndex - 1].OverflowContentTarget = overflow;
					}
				}

				// Measure the new column and prepare to repeat as necessary
				overflow.Width = ColumnWidth;
				overflow.Height = ColumnHeight;

				if (Orientation == Windows.UI.Xaml.Controls.Orientation.Horizontal)
				{
					overflow.Measure(new Size(availableSize.Width - maxWidth, availableSize.Height));
					hasOverflow = overflow.HasOverflowContent;
					if (double.IsNaN(ColumnWidth))
						maxWidth += overflow.DesiredSize.Width;
					else
						maxWidth += ColumnWidth;
					maxHeight = Math.Max(maxHeight, overflow.DesiredSize.Height);
					condition = hasOverflow && maxWidth < availableSize.Width;
				}
				else // Vertical
				{
					overflow.Measure(new Size(availableSize.Width, availableSize.Height - maxHeight));
					hasOverflow = overflow.HasOverflowContent;
					maxWidth = Math.Max(maxWidth, overflow.DesiredSize.Width);
					if (double.IsNaN(ColumnHeight))
						maxHeight += overflow.DesiredSize.Height;
					else
						maxHeight += ColumnHeight;
					condition = hasOverflow && maxHeight < availableSize.Height;
				}
				overflowIndex++;
			}

			// Disconnect extra columns from the overflow chain, remove them from our private list
			// of columns, and remove them as children
			if (this._overflowColumns.Count > overflowIndex)
			{
				if (overflowIndex == 0)
				{
					this.RichTextContent.OverflowContentTarget = null;
				}
				else
				{
					this._overflowColumns[overflowIndex - 1].OverflowContentTarget = null;
				}
				while (this._overflowColumns.Count > overflowIndex)
				{
					this._overflowColumns.RemoveAt(overflowIndex);
					this.Children.RemoveAt(overflowIndex + 1);
				}
			}

			if (Orientation == Windows.UI.Xaml.Controls.Orientation.Horizontal && HorizontalSnapPointsChanged != null)
				HorizontalSnapPointsChanged(this, null);
			if (Orientation == Windows.UI.Xaml.Controls.Orientation.Vertical && VerticalSnapPointsChanged != null)
				VerticalSnapPointsChanged(this, null);
			if (ColumnsChanged != null)
				ColumnsChanged(this, null);

			// Report final determined size
			return new Size(maxWidth, maxHeight);
		}

		/// <summary>
		/// Arranges the original content and all extra columns.
		/// </summary>
		/// <param name="finalSize">Defines the size of the area the children must be arranged
		/// within.</param>
		/// <returns>The size of the area the children actually required.</returns>
		protected override Size ArrangeOverride(Size finalSize)
		{
			Debug.WriteLine("RichTextColumn_Arrage , FinalSize {0}",finalSize);
			double maxWidth = 0;
			double maxHeight = 0;
			foreach (var child in Children)
			{
				try
				{
					if (Orientation == Windows.UI.Xaml.Controls.Orientation.Horizontal)
					{
						child.Arrange(new Rect(maxWidth, 0, ColumnWidth, finalSize.Height));
						maxWidth += ColumnWidth;
						maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
					}
					else
					{
						child.Arrange(new Rect(0, maxHeight, finalSize.Width, ColumnHeight));
						maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
						//maxHeight += child.DesiredSize.Height;
						maxHeight += ColumnHeight;
					}
				}
				catch (Exception exception)
				{
					Debug.WriteLine(exception.Message);
				}
			}
			IsLayoutValiad = true;
			return new Size(maxWidth, maxHeight);
		}

		public bool AreHorizontalSnapPointsRegular
		{
			get { 
				return true;
			}
		}

		public bool AreVerticalSnapPointsRegular
		{
			get {
				return true;
			}
		}

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		{
			throw new NotImplementedException();
		}

		public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
		{
			if (orientation == Orientation.Horizontal)
			{
				offset = (float)Margin.Left;
				return (float)ColumnWidth * ColumnsPerPage;
			}
			else
			{
				offset = (float)Margin.Top;
				return (float)ColumnHeight * ColumnsPerPage;
			}
		}
		public static void NotifySnapPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var target = d as RichTextColumns;
			if (target.Orientation == Windows.UI.Xaml.Controls.Orientation.Horizontal && target.HorizontalSnapPointsChanged != null)
				target.HorizontalSnapPointsChanged(target, null);
			if (target.Orientation == Windows.UI.Xaml.Controls.Orientation.Vertical && target.VerticalSnapPointsChanged != null)
				target.VerticalSnapPointsChanged(target, null);
		}

		public event EventHandler<object> HorizontalSnapPointsChanged;

		public event EventHandler<object> VerticalSnapPointsChanged;

		public event EventHandler<object> ColumnsChanged;
	}

}
