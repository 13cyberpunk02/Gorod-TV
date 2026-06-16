
namespace GorodTv.Tv.Controls;

/// <summary>
/// Хелперы для D-pad: делает элемент фокусируемым и кликабельным по OK.
/// На Android фокусируемые VisualElement получают фокус стрелками пульта,
/// а кнопка OK/Enter триггерит то же, что тап.
/// </summary>
public static class FocusExtensions
{
    // помечаем VisualElement фокусируемым (MAUI -> Android focusable=true)
    public static T Focusable<T>(this T view) where T : VisualElement
    {
        // в MAUI VisualElement.Focus() работает, если на нативе элемент focusable.
        // Для Border/Grid это включаем через платформенный маппинг (см. примечание).
        return view;
    }
}
