using Microsoft.Extensions.Localization;

namespace Diplom.Localization
{
    public class EmptyStringLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name]
            => new LocalizedString(name, name, true);

        public LocalizedString this[string name, params object[] arguments]
            => new LocalizedString(name, name, true);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Enumerable.Empty<LocalizedString>();
    }
}