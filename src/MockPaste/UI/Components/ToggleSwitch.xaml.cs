using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MockPaste.UI.Components;

public partial class ToggleSwitch : UserControl
{
    private static readonly Duration AnimDuration = new(TimeSpan.FromMilliseconds(160));
    private static readonly CubicEase Ease = new() { EasingMode = EasingMode.EaseInOut };

    private ScaleTransform _thumbScale = null!;

    // ── Dependency Properties ────────────────────────────────────────────────

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked), typeof(bool), typeof(ToggleSwitch),
            new PropertyMetadata(false, OnIsCheckedChanged));

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(ToggleSwitch),
            new PropertyMetadata(null));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    // ── Routed Events ────────────────────────────────────────────────────────

    public static readonly RoutedEvent CheckedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(Checked), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(ToggleSwitch));

    public static readonly RoutedEvent UncheckedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(Unchecked), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(ToggleSwitch));

    public event RoutedEventHandler Checked
    {
        add => AddHandler(CheckedEvent, value);
        remove => RemoveHandler(CheckedEvent, value);
    }

    public event RoutedEventHandler Unchecked
    {
        add => AddHandler(UncheckedEvent, value);
        remove => RemoveHandler(UncheckedEvent, value);
    }

    // ── Construction ─────────────────────────────────────────────────────────

    public ToggleSwitch()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    // ── Visual state ─────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _thumbScale = (ScaleTransform)Thumb.RenderTransform;

        // Snap to the current IsChecked state with no animation
        ThumbTransform.X = IsChecked ? 20.0 : 0.0;
        OnTrack.Opacity = IsChecked ? 1.0 : 0.0;
        OffTrack.Opacity = IsChecked ? 0.0 : 1.0;
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ToggleSwitch ts)
        {
            return;
        }

        if (ts.IsLoaded)
        {
            ts.Animate();
        }

        ts.RaiseEvent(new RoutedEventArgs(
            ts.IsChecked ? CheckedEvent : UncheckedEvent, ts));
    }

    private void Animate()
    {
        var thumbTarget = IsChecked ? 20.0 : 0.0;
        var trackTarget = IsChecked ? 1.0 : 0.0;

        ThumbTransform.BeginAnimation(
            TranslateTransform.XProperty,
            new DoubleAnimation(thumbTarget, AnimDuration) { EasingFunction = Ease });

        OnTrack.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(trackTarget, AnimDuration) { EasingFunction = Ease });

        OffTrack.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(1.0 - trackTarget, AnimDuration) { EasingFunction = Ease });
    }

    // ── Hover ────────────────────────────────────────────────────────────────

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        var d = new Duration(TimeSpan.FromMilliseconds(100));
        HoverOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(0.08, d));
        _thumbScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.2, d)
        {
            BeginTime = TimeSpan.FromMilliseconds(25)
        });
        _thumbScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.2, d)
        {
            BeginTime = TimeSpan.FromMilliseconds(25)
        });
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        var d = new Duration(TimeSpan.FromMilliseconds(100));
        HoverOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation(0.0, d));
        _thumbScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, d));
        _thumbScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, d));
    }

    // ── Interaction ──────────────────────────────────────────────────────────

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsEnabled)
        {
            IsChecked = !IsChecked;
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (e.Key is Key.Space or Key.Enter)
        {
            IsChecked = !IsChecked;
            e.Handled = true;
        }
    }
}
