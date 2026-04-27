using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using MockPaste.Resources;

namespace MockPaste.Localization;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    private static readonly ResourceManager ResourceManager =
        new("MockPaste.Resources.Strings", typeof(LocalizationManager).Assembly);

    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    public static LocalizationManager Instance { get; } = new();

    private LocalizationManager()
    {
        Strings.Culture = _currentCulture;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CultureChanged;

    public CultureInfo CurrentCulture => _currentCulture;

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        return ResourceManager.GetString(key, _currentCulture) ?? key;
    }

    public string Format(string key, params object[] args)
    {
        var format = GetString(key);
        return string.Format(_currentCulture, format, args);
    }

    public void SetCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (Equals(_currentCulture, culture))
        {
            return;
        }

        _currentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Strings.Culture = culture;

        OnPropertyChanged(nameof(CurrentCulture));
        OnPropertyChanged("Item[]");
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
