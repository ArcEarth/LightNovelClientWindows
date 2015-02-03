using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LightNovel.Controls
{
	public class LocalContentSuggestionSettings
	{
	}

	public class SearchBoxResultSuggestionChosenEventArgs
	{
	}

	public class SearchBoxQueryChangedEventArgs
	{
		public string QueryText { get; set; }
	}
	public class SearchBoxQuerySubmittedEventArgs
	{
		public string QueryText { get; set; }
	}
	public class SearchBoxSuggestionsRequestedEventArgs
	{
	}
	public sealed partial class SearchBox : UserControl
	{

		// Summary:
		//     Initializes a new instance of the SearchBox class.

		public SearchBox() {
			InitializeComponent();
		}

		static DependencyProperty _IsExpandedProperty =
			DependencyProperty.Register("IsExpanded", typeof(bool),
			typeof(SearchBox), new PropertyMetadata(false, OnIsExpandedChanged));

		public static DependencyProperty IsExpandedProperty
		{
			get
			{
				return _IsExpandedProperty;
			}
		}

		public bool IsExpanded
		{
			get
			{
				return (bool)GetValue(_IsExpandedProperty);
			}
			set
			{
				SetValue(_IsExpandedProperty, value);
			}
		}

		private static void OnIsExpandedChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var box = d as SearchBox;
			if ((bool)e.NewValue == true)
			{
				//box.ExpandingAnimation.To = box.Width;
				VisualStateManager.GoToState(box, "ExpandedState", true);
			}
			else
			{
				VisualStateManager.GoToState(box, "NonExpandedState", true);
			}
		}
		// Summary:
		//     Gets or sets a value that determines whether the suggested search query is
		//     activated when the user presses Enter.
		//
		// Returns:
		//     true if the suggested search query is activated when the user presses Enter;
		//     otherwise, false. The default is false.

		public bool ChooseSuggestionOnEnter
		{
			get
			{
				return (bool)GetValue(_ChooseSuggestionOnEnterProperty);
			}
			set
			{
				SetValue(_ChooseSuggestionOnEnterProperty, value);
			}
		}
		//
		// Summary:
		//     Identifies the ChooseSuggestionOnEnter dependency property.
		//
		// Returns:
		//     The identifier for the ChooseSuggestionOnEnter dependency property.
		static DependencyProperty _ChooseSuggestionOnEnterProperty =
			DependencyProperty.Register("ChooseSuggestionOnEnter", typeof(bool),
			typeof(SearchBox), new PropertyMetadata(false, null));

		public static DependencyProperty ChooseSuggestionOnEnterProperty
		{
			get
			{
				return _ChooseSuggestionOnEnterProperty;
			}
		}
		//
		// Summary:
		//     Gets or sets a value that determines whether a user can search by typing
		//     anywhere in the app.
		//
		// Returns:
		//     true if the user can search by typing anywhere in the app; otherwise, false.
		//     The default is false.

		public bool FocusOnKeyboardInput
		{
			get
			{
				return (bool)GetValue(_FocusOnKeyboardInputProperty);
			}
			set
			{
				SetValue(_FocusOnKeyboardInputProperty, value);
			}
		}
		//
		// Summary:
		//     Identifies the FocusOnKeyboardInput dependency property.
		//
		// Returns:
		//     The identifier for the FocusOnKeyboardInput dependency property.

		static DependencyProperty _FocusOnKeyboardInputProperty =
			DependencyProperty.Register("FocusOnKeyboardInput", typeof(bool),
			typeof(SearchBox), new PropertyMetadata(false, null));

		public static DependencyProperty FocusOnKeyboardInputProperty
		{
			get
			{
				return _FocusOnKeyboardInputProperty;
			}
		}
		//
		// Summary:
		//     Gets or sets the text that is displayed in the control until the value is
		//     changed by a user action or some other operation.
		//
		// Returns:
		//     The text that is displayed in the control when no value is entered. The default
		//     is an empty string ("").

		public string PlaceholderText
		{
			get
			{
				return (string)GetValue(_PlaceholderTextProperty);
			}
			set
			{
				SetValue(_PlaceholderTextProperty, value);
			}
		}
		//
		// Summary:
		//     Identifies the PlaceholderText dependency property.
		//
		// Returns:
		//     The identifier for the PlaceholderText dependency property.
		static DependencyProperty _PlaceholderTextProperty =
			DependencyProperty.Register("PlaceholderText", typeof(string),
			typeof(SearchBox), new PropertyMetadata(null, null));

		public static DependencyProperty PlaceholderTextProperty
		{
			get
			{
				return _PlaceholderTextProperty;
			}
		}

		//
		// Summary:
		//     Gets or sets the text contents of the search box.
		//
		// Returns:
		//     A string containing the text contents of the search box. The default is an
		//     empty string ("").

		public string QueryText
		{
			get
			{
				return (string)GetValue(_QueryTextProperty);
			}
			set
			{
				SetValue(_QueryTextProperty, value);
			}
		}
		//
		// Summary:
		//     Identifies the QueryText dependency property.
		//
		// Returns:
		//     The identifier for the QueryText dependency property.

		static DependencyProperty _QueryTextProperty =
			DependencyProperty.Register("QueryText", typeof(string),
			typeof(SearchBox), new PropertyMetadata(String.Empty, null));

		public static DependencyProperty QueryTextProperty
		{
			get
			{
				return _QueryTextProperty;
			}
		}
		//
		// Summary:
		//     Gets or sets a string that identifies the context of the search and is used
		//     to store the user's search history with the app.
		//
		// Returns:
		//     A string that identifies the context of the search. The default is an empty
		//     string ("").

		public string SearchHistoryContext
		{
			get
			{
				return (string)GetValue(_SearchHistoryContextProperty);
			}
			set
			{
				SetValue(_SearchHistoryContextProperty, value);
			}
		}
		// Summary:
		//     Identifies the SearchHistoryContext dependency property.
		//
		// Returns:
		//     The identifier for the SearchHistoryContext dependency property.

		static DependencyProperty _SearchHistoryContextProperty =
			DependencyProperty.Register("SearchHistoryContext", typeof(string),
			typeof(SearchBox), new PropertyMetadata(String.Empty, null));

		public static DependencyProperty SearchHistoryContextProperty
		{
			get
			{
				return _SearchHistoryContextProperty;
			}
		}

		//
		// Summary:
		//     Gets or sets a value that determines whether search suggestions are made
		//     from the search history.
		//
		// Returns:
		//     true if search suggestions are made from the search history; otherwise, false.
		//     The default is true.

		public bool SearchHistoryEnabled
		{
			get
			{
				return (bool)GetValue(_SearchHistoryEnabledProperty);
			}
			set
			{
				SetValue(_SearchHistoryEnabledProperty, value);
			}
		}
		//
		// Summary:
		//     Identifies the SearchHistoryEnabled dependency property.
		//
		// Returns:
		//     The identifier for the SearchHistoryEnabled dependency property.

		static DependencyProperty _SearchHistoryEnabledProperty =
			DependencyProperty.Register("SearchHistoryEnabled", typeof(bool),
			typeof(SearchBox), new PropertyMetadata(false, null));

		public static DependencyProperty SearchHistoryEnabledProperty
		{
			get
			{
				return _SearchHistoryEnabledProperty;
			}
		}

		// Summary:
		//     Occurs when the FocusOnKeyboardInput property is true and the app receives
		//     textual keyboard input.

		//public event TypedEventHandler<SearchBox, RoutedEventArgs> PrepareForFocusOnKeyboardInput;
		//
		// Summary:
		//     Occurs when the query text changes.

		public event TypedEventHandler<SearchBox, SearchBoxQueryChangedEventArgs> QueryChanged;
		//
		// Summary:
		//     Occurs when the user submits a search query.

		public event TypedEventHandler<SearchBox, SearchBoxQuerySubmittedEventArgs> QuerySubmitted;
		//
		// Summary:
		//     Occurs when the user picks a suggested search result.

		//public event TypedEventHandler<SearchBox, SearchBoxResultSuggestionChosenEventArgs> ResultSuggestionChosen;
		//
		// Summary:
		//     Occurs when the user's query text changes and the app needs to provide new
		//     suggestions to display in the search pane.

		//public event TypedEventHandler<SearchBox, SearchBoxSuggestionsRequestedEventArgs> SuggestionsRequested;

		// Summary:
		//     Specifies whether suggestions based on local files are automatically displayed
		//     in the search box suggestions, and defines the criteria that Windows uses
		//     to locate and filter these suggestions.
		//
		// Parameters:
		//   settings:
		//     The new settings for local content suggestions.

		public void SetLocalContentSuggestionSettings(LocalContentSuggestionSettings settings)
		{ }

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (!IsExpanded)
			{
				IsExpanded = true;
				InputBox.Focus(FocusState.Keyboard);
			}
			else
				if (!String.IsNullOrEmpty(QueryText))
				{
					QuerySubmitted(this, new SearchBoxQuerySubmittedEventArgs { QueryText = this.QueryText });
				}
		}

		private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if ((e.Key ==  Windows.System.VirtualKey.Accept || e.Key ==  Windows.System.VirtualKey.Enter) && !String.IsNullOrEmpty(QueryText) && QuerySubmitted != null)
			{
				QuerySubmitted(this, new SearchBoxQuerySubmittedEventArgs { QueryText = this.QueryText });
			}		
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			QueryText = ((TextBox)sender).Text;
			if (QueryChanged != null)
			{
				QueryChanged(this, new SearchBoxQueryChangedEventArgs { QueryText = this.QueryText });
			}
		}

		private void InputBox_LostFocus(object sender, RoutedEventArgs e)
		{
			IsExpanded = false;
		}

	}

}
