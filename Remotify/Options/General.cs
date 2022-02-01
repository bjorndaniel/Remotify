using System.ComponentModel;

namespace Remotify
{
    internal partial class OptionsProvider
    {
        // Register the options with these attributes on your package class:
        // [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "Remotify", "General", 0, 0, true)]
        // [ProvideProfile(typeof(OptionsProvider.GeneralOptions), "Remotify", "General", 0, 0, true)]
        public class GeneralOptions : BaseOptionPage<General> { }
    }

    public class General : BaseOptionModel<General>
    {
        [Category("Remotify")]
        [DisplayName("HasRevoked")]
        [Description("Bool if user has revoked access")]
        [DefaultValue(false)]
        public bool HasRevokedAccess { get; set; } = false;
    }
}
