using LightNovel.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace LightNovel
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class AuthPage : Page
	{
		private NavigationHelper navigationHelper;
		public NavigationHelper NavigationHelper
		{
			get { return this.navigationHelper; }
		}

		public AuthPage()
		{
			this.InitializeComponent();
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += navigationHelper_LoadState;
			this.navigationHelper.SaveState += navigationHelper_SaveState; 
			
			webView.NavigationCompleted += webView_NavigationCompleted;
		}

		async void webView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{


			string IE11UserAgentString = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

			if (args.Uri.AbsolutePath.EndsWith("getauth.html"))
			{
				//var datapackage = await webView.CaptureSelectedContentToDataPackageAsync();
				//var text = await datapackage.GetView().GetTextAsync();
				//webView.Navigate(new Uri("http://lknovel.lightnovel.cn/main/view/30151.html"));
				var filter = new HttpBaseProtocolFilter();
				var cookieManager = filter.CookieManager;
				var cookies = cookieManager.GetCookies(new Uri("http://lknovel.lightnovel.cn/"));
				var ci_session = cookies.FirstOrDefault(ck => ck.Name == "ci_session_3");
				foreach(var ck in cookies)
				{
					if (ck != ci_session)
						cookieManager.DeleteCookie(ck);
				}

				using (var client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("User-Agent", IE11UserAgentString);
					var str = await client.GetStringAsync(new Uri("http://lknovel.lightnovel.cn/main/view/30151.html"));
					Debug.WriteLine(str);
				}
			}
		}
		/// <summary>
		/// Populates the page with content passed during navigation.  Any saved state is also
		/// provided when recreating a page from a prior session.
		/// </summary>
		/// <param name="navigationParameter">The parameter value passed to
		/// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
		/// </param>
		/// <param name="pageState">A dictionary of state preserved by this page during an earlier
		/// session.  This will be null the first time a page is visited.</param>
		private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
		{
			webView.Navigate(new Uri("http://lknovel.lightnovel.cn/main/view/30151.html"));
			//webView.Navigate(new Uri("http://lknovel.lightnovel.cn/userauth/index.html"));
		}
		private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
		}

		#region NavigationHelper registration

		/// The methods provided in this section are simply used to allow
		/// NavigationHelper to respond to the page's navigation methods.
		/// 
		/// Page specific logic should be placed in event handlers for the  
		/// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
		/// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
		/// The navigation parameter is available in the LoadState method 
		/// in addition to page state preserved during an earlier session.

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			navigationHelper.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			navigationHelper.OnNavigatedFrom(e);
		}

		#endregion
	}
}
