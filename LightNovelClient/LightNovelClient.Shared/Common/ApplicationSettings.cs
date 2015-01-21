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
			//if (!_appSettings.ContainsKey(UserNameKey))
			//	_appSettings.Add(UserNameKey, "");
			////if (!_appSettings.ContainsKey(PasswordKey))
			////	_appSettings.Add(PasswordKey, "");
			if (!_appSettings.ContainsKey(CredentialKey))
				_appSettings.Add(CredentialKey, JsonConvert.SerializeObject(new Session{ Expries = DateTime.Now.AddYears(-100),Key=""}));
			if (!_appSettings.ContainsKey(BackgroundKey))
				_appSettings.Add(BackgroundKey, JsonConvert.SerializeObject(Colors.White));
			if (!_appSettings.ContainsKey(ForegroundKey))
				_appSettings.Add(ForegroundKey, JsonConvert.SerializeObject(Colors.Black));
			if (!_appSettings.ContainsKey(FontSizeKey))
				_appSettings.Add(FontSizeKey, 19.0);
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

		private const string BackgroundKey = "Background";
		public Brush Background
		{
			get
			{
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
