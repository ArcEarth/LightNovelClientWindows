using LightNovel.Service;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace LightNovel.Common
{
	public enum ImageLoadingPolicy
	{
		Manual = 0,
		Adaptive = 1,
		Automatic = 2,
	}

	public class ApplicationSettings : INotifyPropertyChanged
	{
		readonly IPropertySet _appSettings;
		PasswordVault _Vault = new PasswordVault();
		PasswordCredential _Cred;
		public ApplicationSettings()
		{
			if (ApplicationData.Current.RoamingSettings.Containers.ContainsKey("AppSettings"))
			{
				_appSettings = ApplicationData.Current.RoamingSettings.Containers["AppSettings"].Values;
			} else
			{
				_appSettings = ApplicationData.Current.RoamingSettings.CreateContainer("AppSettings",ApplicationDataCreateDisposition.Always).Values;
			}
			if (!_appSettings.ContainsKey(EnableCommentsKey))
				_appSettings.Add(EnableCommentsKey, true);
			if (!_appSettings.ContainsKey(EnableLiveTileKey))
				_appSettings.Add(EnableLiveTileKey, true);
			if (!_appSettings.ContainsKey(BackgroundThemeKey))
				_appSettings.Add(BackgroundThemeKey, (int)Windows.UI.Xaml.ElementTheme.Default);
			if (!_appSettings.ContainsKey(ImageLoadingPolicyKey))
				_appSettings.Add(ImageLoadingPolicyKey, (int)ImageLoadingPolicy.Automatic);
			if (!_appSettings.ContainsKey(CredentialKey))
				_appSettings.Add(CredentialKey, JsonConvert.SerializeObject(new Session{ Expries = DateTime.Now.AddYears(-100),Key=""}));
			if (!_appSettings.ContainsKey(FontSizeKey))
				_appSettings.Add(FontSizeKey, 19.0);
			if (!_appSettings.ContainsKey(FontFamilyKey))
#if WINDOWS_PHONE_APP
				_appSettings.Add(FontFamilyKey, "Segoe WP"); 
#else
				_appSettings.Add(FontFamilyKey, "Segoe UI"); 
#endif
			if (!_appSettings.ContainsKey(SavedAppVersionKey))
			{
				_appSettings.Add(SavedAppVersionKey, "00.00.00.00");
			}
			try
			{
				var creds = _Vault.FindAllByResource("lightnovel.cn");
				if (creds.Count < 1)
					_Cred = null;
				else
				{
					_Cred = creds[0];
					_Cred = _Vault.Retrieve("lightnovel.cn", _Cred.UserName);
				}
			}
			catch (Exception ex) // Not found
			{
				Debug.WriteLine(ex.Message);
				_Cred = null;
			}
		}

		private const string EnableCommentsKey = "EnableComments";

		public bool EnableComments
		{
			get
			{
				return (bool)_appSettings[EnableCommentsKey];
			}
			set
			{
				_appSettings[EnableCommentsKey] = value;
				NotifyPropertyChanged();
			}
		}

		private const string EnableLiveTileKey = "EnableLiveTile";

		public bool EnableLiveTile
		{
			get
			{
				return (bool)_appSettings[EnableLiveTileKey];
			}
			set
			{
				_appSettings[EnableLiveTileKey] = value;
				NotifyPropertyChanged();
			}
		}

		private const string ImageLoadingPolicyKey = "ImageLoadingPolicy";

		public ImageLoadingPolicy ImageLoadingPolicy
		{
			get
			{
				return (ImageLoadingPolicy)_appSettings[ImageLoadingPolicyKey];
			}
			set
			{
				_appSettings[ImageLoadingPolicyKey] = (int)value;
				NotifyPropertyChanged();
			}
		}
		public int ImageLoadingPolicyBindingProperty
		{
			get
			{ return (int)ImageLoadingPolicy; }
			set
			{
				ImageLoadingPolicy = (ImageLoadingPolicy)value;
				NotifyPropertyChanged();
			}
		}

		private const string BackgroundThemeKey = "BackgroundTheme";

		public Windows.UI.Xaml.ElementTheme BackgroundTheme
		{
			get
			{
				return (Windows.UI.Xaml.ElementTheme)_appSettings[BackgroundThemeKey];
			}
			set
			{
				if ((Windows.UI.Xaml.ElementTheme)_appSettings[BackgroundThemeKey] != value)
				{
					_appSettings[BackgroundThemeKey] = (int)value;
					NotifyPropertyChanged();
				}
			}
		}

		public int BackgroundThemeIndexBindingProperty
		{
			get
			{ return (int)BackgroundTheme; }
			set
			{
				BackgroundTheme = (Windows.UI.Xaml.ElementTheme)value;
			}
		}

		private const string BackgroundKey = "Background";
		public Brush Background
		{
			get
			{
				if (!_appSettings.ContainsKey(BackgroundKey))
					if (App.Current.RequestedTheme == Windows.UI.Xaml.ApplicationTheme.Light)
						_appSettings.Add(BackgroundKey, JsonConvert.SerializeObject(Colors.White));
					else
						_appSettings.Add(BackgroundKey, JsonConvert.SerializeObject(Colors.Black));
				return new SolidColorBrush(JsonConvert.DeserializeObject<Color>((string)_appSettings[BackgroundKey]));
			}

			set
			{
				var color_str = JsonConvert.SerializeObject((value as SolidColorBrush).Color);
				_appSettings[BackgroundKey] = color_str;
				NotifyPropertyChanged();
			}
		}

		private const string ForegroundKey = "Foreground";
		public Brush Foreground
		{
			get
			{
				if (!_appSettings.ContainsKey(ForegroundKey))
					if (App.Current.RequestedTheme == Windows.UI.Xaml.ApplicationTheme.Light)
						_appSettings.Add(ForegroundKey, JsonConvert.SerializeObject(Colors.Black));
					else
						_appSettings.Add(ForegroundKey, JsonConvert.SerializeObject(Colors.White)); 
				return new SolidColorBrush(JsonConvert.DeserializeObject<Color>((string)_appSettings[ForegroundKey]));
			}

			set
			{
				var color_str = JsonConvert.SerializeObject((value as SolidColorBrush).Color);
				_appSettings[ForegroundKey] = color_str;
				NotifyPropertyChanged();
			}
		}

		private const string FontSizeKey = "FontSize";
		public double FontSize
		{
			get
			{
				var val = (double)_appSettings[FontSizeKey];
				return (double)val;
			}

			set
			{
				_appSettings[FontSizeKey] = value;
				NotifyPropertyChanged();
			}
		}

		private const string FontFamilyKey = "FontFamily";
		public FontFamily FontFamily
		{
			get
			{
				return new FontFamily((string)_appSettings[FontFamilyKey]);
			}

			set
			{
				_appSettings[FontFamilyKey] = value.Source;
				NotifyPropertyChanged();
			}
		}
		private const string SavedAppVersionKey = "SavedAppVersion";
		public string SavedAppVersion
		{
			get
			{
				var val = (string)_appSettings[SavedAppVersionKey];
				return (string)val;
			}

			private set
			{
				_appSettings[SavedAppVersionKey] = value;
				NotifyPropertyChanged();
			}
		}

		public void UpdateSavedAppVersion()
		{
			var CurrentVersion = Package.Current.Id.Version;
			SavedAppVersion = String.Format("{0}.{1}.{2}.{3}", CurrentVersion.Major.ToString("D2"), CurrentVersion.Minor.ToString("D2"), CurrentVersion.Revision.ToString("D2"), CurrentVersion.Build.ToString("D2"));
		}

		// String.Compare(SavedAppVersion, thresholdVersion) <= 0

		public bool IsSavedAppVersionLessThan(string thresholdVersion = "00.00.00.00")
		{
			return String.Compare(SavedAppVersion, thresholdVersion) < 0;
		}

		public void SetUserNameAndPassword(string userName,string password)
		{
			if (_Cred!=null)
			{
				_Vault.Remove(_Cred);
				_Cred = null;
			}
			if (!(String.IsNullOrEmpty(userName) || String.IsNullOrEmpty(password)))
			{
				_Cred = new PasswordCredential("lightnovel.cn", userName, password);
				_Vault.Add(_Cred);
			}
		}

		private const string UserNameKey = "UserName";
		public string UserName
		{
			get
			{
				return _Cred != null ? _Cred.UserName : null;
			}
		}

		private const string PasswordKey = "Password";
		public string Password
		{
			get
			{
				return _Cred != null ? _Cred.Password : null;
			}
		}

		private string CredentialKey = "CredentialCookie";

		public Session Credential
		{
			get
			{
				return JsonConvert.DeserializeObject<Session>((string)_appSettings[CredentialKey]);
			}
			set
			{
				_appSettings[CredentialKey] = JsonConvert.SerializeObject(value);
				NotifyPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
