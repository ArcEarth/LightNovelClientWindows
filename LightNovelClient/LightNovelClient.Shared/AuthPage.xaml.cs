using LightNovel.Common;
using LightNovel.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
			ChangeUserAgent(LightKindomHtmlClient.UserAgentString);
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += navigationHelper_LoadState;
			this.navigationHelper.SaveState += navigationHelper_SaveState; 
			
			webView.NavigationCompleted += webView_NavigationCompleted;
		}

		[DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
		private static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);

		const int URLMON_OPTION_USERAGENT = 0x10000001;
		public void ChangeUserAgent(string Agent)
		{
			UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, Agent, Agent.Length, 0);
		}


		async void webView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
            if (args.Uri.AbsolutePath.EndsWith("/userauth/index.html"))
            {
                if (args.IsSuccess)
                {
                    webView.Visibility = Visibility.Visible;
                    progressRing.Visibility = Visibility.Collapsed;
                    progressRing.IsActive = false;
                    RetryHintTextBlock.Visibility = Visibility.Collapsed;
                } else
                {
                    AuthPageLoadFailedHint.Visibility = Visibility.Visible;
                }
            }
			if (args.Uri.AbsolutePath.EndsWith("getautha.html"))
			{
                var content = await webView.InvokeScriptAsync("eval", new string[] { "document.body.innerHTML;" });
                if (content.Contains("验证失败"))
                {
                    webView.Visibility = Visibility.Collapsed;
                    progressRing.IsActive = true;
                    RetryHintTextBlock.Visibility = Visibility.Visible;
                    progressRing.Visibility = Visibility.Visible;
                    if (webView.CanGoBack)
                    {
                        webView.GoBack(); // Try Again
                        webView.Refresh();
                    }
                    else
                    {
                        webView.Navigate(new Uri("http://lknovel.lightnovel.cn/userauth/index.html"));
                    }
                }
                else
                    Frame.GoBack(); // Return to HubPage

                //.CaptureSelectedContentToDataPackageAsync();
                //var text = await datapackage.GetView().GetTextAsync();
                //webView.Navigate(new Uri("http://lknovel.lightnovel.cn/main/login.html?from=lknovel.lightnovel.cn/"));
                //var filter = new HttpBaseProtocolFilter();
                //var cookieManager = filter.CookieManager;
                //var cookies = cookieManager.GetCookies(new Uri("http://lknovel.lightnovel.cn/"));
                //var ci_session = cookies.FirstOrDefault(ck => ck.Name == "ci_session_3");
                //foreach(var ck in cookies)
                //{
                //	if (ck != ci_session)
                //		cookieManager.DeleteCookie(ck);
                //}
                //this.Frame.GoBack();
                //using (var client = new HttpClient())
                //{
                //	client.DefaultRequestHeaders.Add("User-Agent", LightKindomHtmlClient.UserAgentString);
                //	var str = await client.GetStringAsync(new Uri("http://lknovel.lightnovel.cn/main/login.html?from=lknovel.lightnovel.cn/"));
                //	Debug.WriteLine(str);
                //}
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
            //webView.Navigate(new Uri("http://lknovel.lightnovel.cn/main/login.html?from=lknovel.lightnovel.cn/"));
            //webView.Navigate(new Uri("http://whatsmyuseragent.com/"));
            webView.Visibility = Visibility.Collapsed;
            progressRing.IsActive = true;
            progressRing.Visibility = Visibility.Visible;
            RetryHintTextBlock.Visibility = Visibility.Collapsed;
            webView.Navigate(new Uri("http://lknovel.lightnovel.cn/userauth/index.html"));
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
