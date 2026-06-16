
namespace GorodTv.Tv.Controls;

/// <summary>
/// Делает элемент видимо «сфокусированным» при навигации пультом:
/// масштаб + рамка + поднятие. Вешается на любой VisualElement (Border тайла и т.п.).
/// Работает в паре с тем, что элемент Focusable (см. ниже SetupFocusable).
/// </summary>
public class TvFocusBehavior : Behavior<VisualElement>
{
    public static readonly BindableProperty FocusedScaleProperty =
        BindableProperty.Create(nameof(FocusedScale), typeof(double), typeof(TvFocusBehavior), 1.08);

    public double FocusedScale
    {
        get => (double)GetValue(FocusedScaleProperty);
        set => SetValue(FocusedScaleProperty, value);
    }

    private VisualElement? _target;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        _target = bindable;
        bindable.Focused += OnFocused;
        bindable.Unfocused += OnUnfocused;
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.Focused -= OnFocused;
        bindable.Unfocused -= OnUnfocused;
        _target = null;
    }

    private async void OnFocused(object? sender, FocusEventArgs e)
    {
        if (_target is null) return;
        // визуальная реакция фокуса
        await _target.ScaleToAsync(FocusedScale, 120, Easing.CubicOut);
        if (_target is Border b)
        {
            b.Stroke = Color.FromArgb("#1B66E5");
            b.StrokeThickness = 3;
        }
    }

    private async void OnUnfocused(object? sender, FocusEventArgs e)
    {
        if (_target is null) return;
        await _target.ScaleToAsync(1.0, 120, Easing.CubicOut);
        if (_target is Border b)
        {
            b.StrokeThickness = 0;
        }
    }
}

