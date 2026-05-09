using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TempControl.ViewModels;

public partial class HardwareGroupViewModel : ObservableObject
{
    private float? _sessionMin;
    private float? _sessionMax;

    public string Name { get; init; } = "";
    public ObservableCollection<SensorViewModel> Sensors { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandIcon))]
    private bool _isExpanded = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryDisplayValue), nameof(SessionMinDisplay),
                              nameof(SessionMaxDisplay), nameof(AlertThresholdValue),
                              nameof(ThresholdUnitLabel), nameof(ThresholdMin), nameof(ThresholdMax))]
    private bool _isCelsius = true;

    [ObservableProperty]
    private bool _isMinMaxVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BellOpacity))]
    private bool _isAlertEnabled = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AlertThresholdValue))]
    private float _alertThresholdCelsius = 85f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardBorderBrush))]
    private bool _isAlerting = false;

    [RelayCommand] private void ToggleExpanded() => IsExpanded = !IsExpanded;

    public string  ExpandIcon  => IsExpanded     ? "▲" : "▼";
    public double  BellOpacity => IsAlertEnabled ? 1.0 : 0.3;

    public IBrush CardBorderBrush => IsAlerting
        ? new SolidColorBrush(Color.Parse("#EF5350"))
        : Brushes.Transparent;

    public decimal ThresholdMin => IsCelsius ?   1m :  34m;
    public decimal ThresholdMax => IsCelsius ? 100m : 212m;

    public decimal? AlertThresholdValue
    {
        get => (decimal)(IsCelsius ? AlertThresholdCelsius : AlertThresholdCelsius * 9f / 5f + 32f);
        set
        {
            if (!value.HasValue) return;
            var clamped = Math.Clamp(value.Value, ThresholdMin, ThresholdMax);
            AlertThresholdCelsius = IsCelsius ? (float)clamped : (float)((clamped - 32) * 5 / 9);
            OnPropertyChanged();
        }
    }

    public string ThresholdUnitLabel => IsCelsius ? "°C" : "°F";

    public string SummaryDisplayValue
    {
        get
        {
            var t = GetSummaryTemp();
            if (!t.HasValue) return "—";
            return IsCelsius ? $"{t:F0}°C" : $"{t * 9f / 5f + 32:F0}°F";
        }
    }

    public IBrush SummaryBadgeBrush => SensorViewModel.GetBadgeBrush(GetSummaryTemp());

    public string SessionMinDisplay => FormatCompact(_sessionMin, "↓");
    public string SessionMaxDisplay => FormatCompact(_sessionMax, "↑");

    private string FormatCompact(float? t, string prefix) =>
        t.HasValue ? $"{prefix}{(IsCelsius ? $"{t:F0}°" : $"{t * 9f / 5f + 32:F0}°")}" : "";

    public void NotifySummaryChanged()
    {
        var t = GetSummaryTemp();
        if (t.HasValue)
        {
            _sessionMin = _sessionMin.HasValue ? MathF.Min(_sessionMin.Value, t.Value) : t;
            _sessionMax = _sessionMax.HasValue ? MathF.Max(_sessionMax.Value, t.Value) : t;
            IsAlerting  = IsAlertEnabled && t.Value > AlertThresholdCelsius;
        }

        OnPropertyChanged(nameof(SummaryDisplayValue));
        OnPropertyChanged(nameof(SummaryBadgeBrush));
        OnPropertyChanged(nameof(SessionMinDisplay));
        OnPropertyChanged(nameof(SessionMaxDisplay));
    }

    private float? GetSummaryTemp()
    {
        var measurable = Sensors.Where(s => !IsThresholdSensor(s.Name)).ToList();

        var priority = measurable
            .Where(s => IsPrioritySensor(s.Name) && s.Value.HasValue)
            .OrderByDescending(s => s.Value)
            .FirstOrDefault();

        if (priority is not null) return priority.Value;

        return measurable
            .Select(s => s.Value)
            .Where(v => v.HasValue)
            .DefaultIfEmpty(null)
            .Max();
    }

    private static bool IsThresholdSensor(string name) =>
        name.Contains("Warning",  StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Critical", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Limit",    StringComparison.OrdinalIgnoreCase);

    private static bool IsPrioritySensor(string name) =>
        name.Contains("Package",   StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Tctl",      StringComparison.OrdinalIgnoreCase) ||
        name.Contains("GPU Core",  StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Composite", StringComparison.OrdinalIgnoreCase);
}
