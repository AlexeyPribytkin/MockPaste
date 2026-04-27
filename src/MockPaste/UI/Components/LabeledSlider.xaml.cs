using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MockPaste.UI.Components;

/// <summary>
/// A labeled slider control that shows a header and a formatted current value.
/// Supports click-to-seek, arrow-key stepping, and Shift+Arrow for 5% range jumps.
/// </summary>
public partial class LabeledSlider : UserControl
{
    // ── Dependency Properties ────────────────────────────────────────────────

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(LabeledSlider),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(LabeledSlider),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(LabeledSlider),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(LabeledSlider),
            new PropertyMetadata(100.0));

    public static readonly DependencyProperty TickFrequencyProperty =
        DependencyProperty.Register(nameof(TickFrequency), typeof(double), typeof(LabeledSlider),
            new PropertyMetadata(1.0));

    public static readonly DependencyProperty SmallChangeProperty =
        DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(LabeledSlider),
            new PropertyMetadata(1.0));

    public static readonly DependencyProperty LargeChangeProperty =
        DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(LabeledSlider),
            new PropertyMetadata(10.0));

    /// <summary>Pre-formatted display string for the value label. Bind to a ViewModel property such as <c>PasteDelayDisplay</c>.</summary>
    public static readonly DependencyProperty ValueDisplayProperty =
        DependencyProperty.Register(nameof(ValueDisplay), typeof(string), typeof(LabeledSlider),
            new PropertyMetadata(string.Empty));

    // ── CLR Wrappers ─────────────────────────────────────────────────────────

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double TickFrequency
    {
        get => (double)GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    public double SmallChange
    {
        get => (double)GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    public double LargeChange
    {
        get => (double)GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    /// <summary>Pre-formatted display string for the value label. Bind to a ViewModel property such as <c>PasteDelayDisplay</c>.</summary>
    public string ValueDisplay
    {
        get => (string)GetValue(ValueDisplayProperty);
        set => SetValue(ValueDisplayProperty, value);
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    public LabeledSlider()
    {
        InitializeComponent();
    }

    // ── Mouse handling ───────────────────────────────────────────────────────

    private void InnerSlider_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var slider = (Slider)sender;
        double position = e.GetPosition(slider).X;
        double ratio = position / slider.ActualWidth;
        Value = Math.Clamp(Minimum + ratio * (Maximum - Minimum), Minimum, Maximum);
    }

    // ── Keyboard handling ────────────────────────────────────────────────────

    private void InnerSlider_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Left or Key.Right))
        {
            return;
        }

        bool shiftHeld = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
        double step = shiftHeld
            ? (Maximum - Minimum) * 0.05
            : SmallChange;

        double delta = e.Key == Key.Right ? step : -step;
        Value = Math.Clamp(Value + delta, Minimum, Maximum);

        e.Handled = true;
    }
}
