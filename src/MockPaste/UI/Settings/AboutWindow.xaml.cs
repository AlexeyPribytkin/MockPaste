using System.Reflection;
using System.Windows;
using MockPaste.Resources;

namespace MockPaste.UI.Settings;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = GetVersionText();
    }

    private static string GetVersionText()
    {
        var versionFormat = Strings.StringAboutVersion;
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        return string.Format(versionFormat, version);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
