using System.Text.Json;
using System.Windows;
using Shared.Core.ErrorHandling;

namespace Shared.Core.Services
{
    public class LocalizationManager
    {
        private readonly ILogger _logger = Logging.Create<LocalizationManager>();

        private Dictionary<string, string> _strings = [];
        private string _selectedLangauge = "Not set";

        public string SelectedLangauge { get => _selectedLangauge; }

        public List<string> GetPossibleLanguages()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var langaugeFiles = Directory.GetFiles(currentDirectory, "Language_*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => name!.Substring("Language_".Length).ToLower())
                .ToList();

            _logger.Here().Information($"Found language files: {string.Join(", ", langaugeFiles)}");    
            return langaugeFiles;
        }

        public void LoadLanguage(string languageCode)
        {
            _selectedLangauge = languageCode.ToLower();

            var languageFile= $"Language_{_selectedLangauge}.json";
            if (File.Exists(languageFile) == false)
            {
                MessageBox.Show($"Language file for code '{_selectedLangauge}' not found.");
                _logger.Here().Error($"Language file for code '{_selectedLangauge}' not found. in {Directory.GetCurrentDirectory()}");
                return;
            }

            try
            {
                var json = File.ReadAllText(languageFile);
                var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (strings == null || strings.Count == 0)
                {
                    MessageBox.Show($"Failed to parse langauge file {_selectedLangauge}");
                    _logger.Here().Error($"Failed to parse langauge file {_selectedLangauge}");

                    _strings = [];
                    return;
                }

                _strings = strings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load language file: {ex.Message}");
                _logger.Here().Error($"Failed to load language file: {ex.Message}");    
            }
        }

        public  string Get(string key)
        {
            if (_strings.TryGetValue(key, out var value))
                return value;

            _logger.Here().Error($"Failed to load language code {key} for language {_selectedLangauge}");
            return key;
        }
    }
}
