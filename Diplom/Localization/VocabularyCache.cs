using Diplom.Data;
using Diplom.Models.Tools;

namespace Diplom.Localization
{
    public class VocabularyCache
    {
        private Dictionary<string, Dictionary<string, string>> _data
            = new Dictionary<string, Dictionary<string, string>>();

        public VocabularyCache()
        {
        }

        public void Load(ApplicationDbContext context)
        {
            _data.Clear();

            var list = context.Vocabulary.ToList();

            foreach (var item in list)
            {
                if (!_data.ContainsKey(item.Key))
                {
                    _data[item.Key] =
                        new Dictionary<string, string>();
                }

                _data[item.Key][item.Language] = item.Value;
            }
        }

        public string Get(string key, string lang)
        {
            if (_data.TryGetValue(key, out var langs))
            {
                if (langs.TryGetValue(lang, out var value))
                    return string.IsNullOrWhiteSpace(value) ? key : value;
            }

            return key;
        }
    }
}