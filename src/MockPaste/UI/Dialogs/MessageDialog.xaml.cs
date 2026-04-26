using System.Windows;

namespace MockPaste.UI.Dialogs;

/// <summary>Themed replacement for <see cref="MessageBox"/> that matches the MockPaste visual style.</summary>
public partial class MessageDialog : Window
{
    public bool Result { get; private set; }

    private MessageDialog(string message, string title, DialogKind kind)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;

        Tag = kind.ToString();
    }

    /// <summary>Shows an informational/warning/error dialog with a single OK button.</summary>
    internal static void Show(string message, string title = "MockPaste", DialogKind kind = DialogKind.Information, Window? owner = null)
        => Create(message, title, kind, owner).ShowDialog();

    /// <summary>Shows a Yes/No confirmation dialog. Returns <c>true</c> when the user clicks Yes.</summary>
    internal static bool Confirm(string message, string title = "MockPaste", Window? owner = null)
    {
        var dlg = Create(message, title, DialogKind.Question, owner);
        dlg.ShowDialog();
        return dlg.Result;
    }

    private static MessageDialog Create(string message, string title, DialogKind kind, Window? owner)
        => new(message, title, kind)
        {
            Owner = owner ?? System.Windows.Application.Current?.MainWindow
        };

    private void Yes_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void No_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

internal enum DialogKind
{
    Information,
    Warning,
    Error,
    Question,
}
