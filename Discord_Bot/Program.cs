using System.IO;
using Newtonsoft.Json;

namespace Discord_Bot
{
	public class Settings
	{
		public static readonly string _CurrentSettingsVersion = "0.1";

		public string settingsVer = _CurrentSettingsVersion;
		public string botPrefix = "b.";
		public string botToken = null;
		public string botToken_dev = null;
	}

	class Program
	{
		static void Main(string[] args)
		{
			Settings settings = GetSettings();

			// indicates settings file was just made or version is out of date
			if (settings.settingsVer == "0")
				return;

			new Bot.Core().StartBot(settings).Wait();
		}

		static Settings GetSettings()
		{
			FileStream file;
			if (!File.Exists("settings.json"))
			{
				file = new FileStream("settings.json", FileMode.Create, FileAccess.Write);
				StreamWriter writer = new(file);

				Settings defaultSettings = new();
				string settingsString = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);

				writer.Write(settingsString);

				writer.Close();
				file.Close();

				defaultSettings.settingsVer = "0";

				return defaultSettings;
			}

			file = new("settings.json", FileMode.Open, FileAccess.Read);
			StreamReader reader = new(file);

			Settings settings = JsonConvert.DeserializeObject<Settings>(reader.ReadToEnd());

			if (settings.settingsVer != Settings._CurrentSettingsVersion)
			{
				reader.Close();
				file.Close();

				file = new("settings.json", FileMode.Create, FileAccess.Write);
				StreamWriter writer = new(file);

				settings.settingsVer = Settings._CurrentSettingsVersion;

				string settingsString = JsonConvert.SerializeObject(settings, Formatting.Indented);

				writer.Write(settingsString);

				writer.Close();
				file.Close();

				settings.settingsVer = "0";

				return settings;
			}

			return settings;
		}
	}
}
