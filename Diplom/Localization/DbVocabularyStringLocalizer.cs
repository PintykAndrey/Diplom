using Diplom.Data;
using Diplom.Models.Tools;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Diplom.Localization
{
    public class DbVocabularyStringLocalizer : IStringLocalizer
    {
        private readonly VocabularyCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;

        private static object _lock = new();

        public DbVocabularyStringLocalizer(
            VocabularyCache cache,
            IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        public LocalizedString this[string name]
        {
            get
            {
                var lang =
                    CultureInfo.CurrentUICulture
                    .TwoLetterISOLanguageName;

                var value = _cache.Get(name, lang);

                if (value == name)
                {
                    AddKeyIfMissing(name);
                    value = _cache.Get(name, lang);
                }

                return new LocalizedString(name, value);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var lang =
                    CultureInfo.CurrentUICulture
                    .TwoLetterISOLanguageName;

                var format = _cache.Get(name, lang);

                if (format == name)
                {
                    AddKeyIfMissing(name);
                    format = _cache.Get(name, lang);
                }

                var value = string.Format(format, arguments);

                return new LocalizedString(name, value);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Enumerable.Empty<LocalizedString>();
        }

        private void AddKeyIfMissing(string key)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (context.Vocabulary.Any(v => v.Key == key))
                    return;

                var langs = new[] { "en", "ru", "uk", "bg" };

                foreach (var lang in langs)
                {
                    context.Vocabulary.Add(new Vocabulary
                    {
                        Key = key,
                        Language = lang,
                        Value = lang == "en" ? key : ""
                    });
                }

                context.SaveChanges();

                _cache.Load(context);
            }
        }
    }
}