using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using LightNovel.Controls;

// A Custom implementation to the Windows 8.1 SearchBox control in windows phone
namespace Windows.UI.Xaml.Controls
{
	// Summary:
	//     Represents a control that can be used to enter search query text.
	public class SearchBox : Control
	{
		// Summary:
		//     Initializes a new instance of the SearchBox class.

		public SearchBox() { }

		// Summary:
		//     Gets or sets a value that determines whether the suggested search query is
		//     activated when the user presses Enter.
		//
		// Returns:
		//     true if the suggested search query is activated when the user presses Enter;
		//     otherwise, false. The default is false.
		
		public bool ChooseSuggestionOnEnter { get; set; }
		//
		// Summary:
		//     Identifies the ChooseSuggestionOnEnter dependency property.
		//
		// Returns:
		//     The identifier for the ChooseSuggestionOnEnter dependency property.
		static DependencyProperty _ChooseSuggestionOnEnterProperty = null;
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
		
		public bool FocusOnKeyboardInput { get; set; }
		//
		// Summary:
		//     Identifies the FocusOnKeyboardInput dependency property.
		//
		// Returns:
		//     The identifier for the FocusOnKeyboardInput dependency property.

		static DependencyProperty _FocusOnKeyboardInputProperty = null;
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
		
		public string PlaceholderText { get; set; }
		//
		// Summary:
		//     Identifies the PlaceholderText dependency property.
		//
		// Returns:
		//     The identifier for the PlaceholderText dependency property.
		static DependencyProperty _PlaceholderTextProperty = null;

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
		
		public string QueryText { get; set; }
		//
		// Summary:
		//     Identifies the QueryText dependency property.
		//
		// Returns:
		//     The identifier for the QueryText dependency property.

		static DependencyProperty _QueryTextProperty = null;
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
		
		public string SearchHistoryContext { get; set; }
		//
		// Summary:
		//     Identifies the SearchHistoryContext dependency property.
		//
		// Returns:
		//     The identifier for the SearchHistoryContext dependency property.

		static DependencyProperty _SearchHistoryContextProperty = null;
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
		
		public bool SearchHistoryEnabled { get; set; }
		//
		// Summary:
		//     Identifies the SearchHistoryEnabled dependency property.
		//
		// Returns:
		//     The identifier for the SearchHistoryEnabled dependency property.

		static DependencyProperty _SearchHistoryEnabledProperty = null;
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
		
		public event TypedEventHandler<SearchBox, RoutedEventArgs> PrepareForFocusOnKeyboardInput;
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
		
		public event TypedEventHandler<SearchBox, SearchBoxResultSuggestionChosenEventArgs> ResultSuggestionChosen;
		//
		// Summary:
		//     Occurs when the user's query text changes and the app needs to provide new
		//     suggestions to display in the search pane.
		
		public event TypedEventHandler<SearchBox, SearchBoxSuggestionsRequestedEventArgs> SuggestionsRequested;

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
	}

	public class LocalContentSuggestionSettings
	{
	}

	public class SearchBoxResultSuggestionChosenEventArgs
	{
	}

	public class SearchBoxQueryChangedEventArgs
	{
	}
	public class SearchBoxQuerySubmittedEventArgs
	{
	}
	public class SearchBoxSuggestionsRequestedEventArgs
	{
	}
}
