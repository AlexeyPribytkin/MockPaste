using System.Reflection;
using System.Windows;

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
        var versionFormat = System.Windows.Application.Current.Resources["StringAboutVersion"] as string ?? "Version {0}";
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        return string.Format(versionFormat, version);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
