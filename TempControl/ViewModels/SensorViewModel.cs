using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TempControl.ViewModels;

public partial class SensorViewModel : ObservableObject
{
    private float? _sessionMin;
    private float? _sessionMax;

    public string Name { get; init; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue), nameof(BadgeBrush),
                              nameof(SessionMinDisplay), nameof(SessionMaxDisplay))]
    private float? _value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue), nameof(SessionMinDisplay), nameof(SessionMaxDisplay))]
    private bool _isCelsius = true;

    [ObservableProperty]
    private bool _isMinMaxVisible = true;

    partial void OnValueChanged(float? value)
    {
        if (!value.HasValue) return;
        _sessionMin = _sessionMin.HasValue ? MathF.Min(_sessionMin.Value, value.Value) : value;
        _sessionMax = _sessionMax.HasValue ? MathF.Max(_sessionMax.Value, value.Value) : value;
    }

    public string DisplayValue => Value.HasValue
        ? (IsCelsius ? $"{Value:F0}°C" : $"{Value * 9f / 5f + 32:F0}°F")
        : "—";

    public string SessionMinDisplay => FormatCompact(_sessionMin, "↓");
    public string SessionMaxDisplay => FormatCompact(_sessionMax, "↑");

    private string FormatCompact(float? t, string prefix) =>
        t.HasValue ? $"{prefix}{(IsCelsius ? $"{t:F0}°" : $"{t * 9f / 5f + 32:F0}°")}" : "";

    public IBrush BadgeBrush => GetBadgeBrush(Value);

    public static IBrush GetBadgeBrush(float? temp) => temp switch
    {
        < 50     => new SolidColorBrush(Color.Parse("#2E7D32")),
        < 70     => new SolidColorBrush(Color.Parse("#1565C0")),
        < 85     => new SolidColorBrush(Color.Parse("#E65100")),
        not null => new SolidColorBrush(Color.Parse("#B71C1C")),
        _        => new SolidColorBrush(Color.Parse("#424242")),
    };
}
