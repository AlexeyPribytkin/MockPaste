using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MockPaste.Infrastructure;
using MockPaste.Resources;
using MockPaste.UI.Helpers;

namespace MockPaste.UI.Settings;

public partial class AboutWindow : Window
{
    private const string GitHubUrl = "https://github.com/AlexeyPribytkin/mockpaste";

    public AboutWindow()
    {
        InitializeComponent();
        PopulateVersion();
        PopulateLicenseContent();
    }

    private void PopulateVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        VersionText.Text = string.Format(Strings.StringAboutVersion, version);
    }

    // Consolas matches the font used by MarkdownFlowFormatter for license body text.
    private static readonly FontFamily ConsolasFont = new("Consolas");

    private void PopulateLicenseContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var license = EmbeddedResourceReader.Read(assembly, "LICENSE");
        var thirdParty = EmbeddedResourceReader.Read(assembly, "THIRD_PARTY_NOTICES.md");

        var document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            LineHeight = double.NaN
        };

        if (license is not null)
        {
            MarkdownFlowFormatter.AppendTo(document, license, ConsolasFont);
        }

        if (thirdParty is not null)
        {
            MarkdownFlowFormatter.AppendTo(document, thirdParty, ConsolasFont);
        }

        LicenseContent.Document = document;
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = GitHubUrl,
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            // Opening a browser is best-effort; ignore failures silently
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
