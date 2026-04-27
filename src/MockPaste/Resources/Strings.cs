using System.Globalization;
using System.Resources;

namespace MockPaste.Resources;

public static class Strings
{
    private static readonly ResourceManager ResourceManagerInstance =
        new("MockPaste.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static ResourceManager ResourceManager => ResourceManagerInstance;

    public static string Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        return ResourceManagerInstance.GetString(key, Culture) ?? key;
    }

    public static string StringAppName => Get(nameof(StringAppName));
    public static string StringButtonHistory => Get(nameof(StringButtonHistory));
    public static string StringPopupBackFormat => Get(nameof(StringPopupBackFormat));
    public static string StringTrayTooltipEnabled => Get(nameof(StringTrayTooltipEnabled));
    public static string StringTrayTooltipDisabled => Get(nameof(StringTrayTooltipDisabled));
    public static string StringStatusCapturePrompt => Get(nameof(StringStatusCapturePrompt));
    public static string StringStatusHotkeySet => Get(nameof(StringStatusHotkeySet));
    public static string StringStatusHotkeyReset => Get(nameof(StringStatusHotkeyReset));
    public static string StringStatusSaved => Get(nameof(StringStatusSaved));
    public static string StringStatusSaveFailed => Get(nameof(StringStatusSaveFailed));
    public static string StringStatusHotkeyModifierRequired => Get(nameof(StringStatusHotkeyModifierRequired));
    public static string StringUnitMilliseconds => Get(nameof(StringUnitMilliseconds));
    public static string StringUnitItem => Get(nameof(StringUnitItem));
    public static string StringUnitItems => Get(nameof(StringUnitItems));
    public static string StringDiscardChangesMessage => Get(nameof(StringDiscardChangesMessage));
    public static string StringAboutVersion => Get(nameof(StringAboutVersion));
    public static string StringMessageAlreadyRunning => Get(nameof(StringMessageAlreadyRunning));
    public static string StringMessageFailedToStartFormat => Get(nameof(StringMessageFailedToStartFormat));
    public static string StringTitleError => Get(nameof(StringTitleError));
    public static string StringMessageHotkeyRegisterFailedFormat => Get(nameof(StringMessageHotkeyRegisterFailedFormat));
}
