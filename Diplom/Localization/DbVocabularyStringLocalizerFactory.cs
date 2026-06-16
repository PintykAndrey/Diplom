using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Http;

namespace Diplom.Localization
{
    public class DbVocabularyStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DbVocabularyStringLocalizerFactory(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private IStringLocalizer CreateLocalizer()
        {
            var services =
                _httpContextAccessor.HttpContext?.RequestServices;

            if (services == null)
                return new EmptyStringLocalizer();

            return services.GetRequiredService<DbVocabularyStringLocalizer>();
        }

        public IStringLocalizer Create(Type resourceSource)
            => CreateLocalizer();

        public IStringLocalizer Create(string baseName, string location)
            => CreateLocalizer();
    }
}