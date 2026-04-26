using System.Windows.Input;

namespace MockPaste.UI;

/// <summary>
/// Minimal <see cref="ICommand"/> implementation backed by delegates.
/// Raises <see cref="CanExecuteChanged"/> explicitly via <see cref="NotifyCanExecuteChanged"/>.
/// </summary>
internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    /// <inheritdoc/>
    public void Execute(object? parameter) => execute();

    /// <summary>Raises <see cref="CanExecuteChanged"/> so bound controls refresh their enabled state.</summary>
    public void NotifyCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
