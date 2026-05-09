using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibreHardwareMonitor.Hardware;
using TempControl.Services;

namespace TempControl.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly HardwareMonitorService _monitor;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<string, DateTime?> _alertClearTimes = [];

    public ObservableCollection<HardwareGroupViewModel> Groups { get; } = [];
    public ObservableCollection<string> ActiveAlerts { get; } = [];

    public bool HasActiveAlerts => ActiveAlerts.Count > 0;

    [ObservableProperty] private string _lastUpdated = "—";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnitLabel), nameof(UnitTooltip))]
    private bool _isCelsius = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PinOpacity))]
    private bool _isAlwaysOnTop = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EyeOpacity), nameof(EyeTooltip))]
    private bool _isMinMaxVisible = true;

    public string UnitLabel   => IsCelsius      ? "°C" : "°F";
    public string UnitTooltip => IsCelsius      ? "Cambiar a °F" : "Cambiar a °C";
    public double PinOpacity  => IsAlwaysOnTop  ? 1.0 : 0.3;
    public double EyeOpacity  => IsMinMaxVisible ? 1.0 : 0.3;
    public string EyeTooltip  => IsMinMaxVisible ? "Ocultar mín/máx" : "Mostrar mín/máx";

    partial void OnIsCelsiusChanged(bool value)
    {
        foreach (var group in Groups)
        {
            group.IsCelsius = value;
            foreach (var sensor in group.Sensors)
                sensor.IsCelsius = value;
        }
    }

    partial void OnIsMinMaxVisibleChanged(bool value)
    {
        foreach (var group in Groups)
        {
            group.IsMinMaxVisible = value;
            foreach (var sensor in group.Sensors)
                sensor.IsMinMaxVisible = value;
        }
    }

    [RelayCommand] private void ToggleUnit()        => IsCelsius      = !IsCelsius;
    [RelayCommand] private void ToggleAlwaysOnTop() => IsAlwaysOnTop  = !IsAlwaysOnTop;
    [RelayCommand] private void ToggleMinMax()      => IsMinMaxVisible = !IsMinMaxVisible;

    public MainWindowViewModel()
    {
        ActiveAlerts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasActiveAlerts));

        _monitor = new HardwareMonitorService();
        Refresh();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    private void Refresh()
    {
        _monitor.Update();

        foreach (var hw in _monitor.GetHardware())
            SyncGroup(hw, hw.SubHardware);

        UpdateActiveAlerts();
        LastUpdated = DateTime.Now.ToString("HH:mm:ss");
    }

    private void UpdateActiveAlerts()
    {
        var now = DateTime.Now;

        // Track alert state transitions
        foreach (var group in Groups.Where(g => g.IsAlertEnabled))
        {
            if (group.IsAlerting)
            {
                _alertClearTimes[group.Name] = null; // null = currently alerting
            }
            else if (_alertClearTimes.TryGetValue(group.Name, out var clearedAt) && clearedAt is null)
            {
                _alertClearTimes[group.Name] = now; // just stopped alerting
            }
        }

        // Expire cleared alerts after 10s and clean up disabled ones
        var toRemove = _alertClearTimes
            .Where(kv => (kv.Value.HasValue && now - kv.Value.Value > TimeSpan.FromSeconds(10))
                      || !Groups.Any(g => g.IsAlertEnabled && g.Name == kv.Key))
            .Select(kv => kv.Key).ToList();
        foreach (var key in toRemove) _alertClearTimes.Remove(key);

        // Rebuild messages
        ActiveAlerts.Clear();
        foreach (var group in Groups.Where(g => g.IsAlertEnabled && _alertClearTimes.ContainsKey(g.Name)))
        {
            var isAlerting = _alertClearTimes[group.Name] is null;
            ActiveAlerts.Add(isAlerting
                ? $"🔔  {group.Name}: {group.SummaryDisplayValue} supera el umbral de {group.AlertThresholdValue:F0}{group.ThresholdUnitLabel}"
                : $"✓  {group.Name}: temperatura normalizada");
        }
    }

    private void SyncGroup(IHardware hw, IEnumerable<IHardware> subHardware)
    {
        var tempSensors = hw.Sensors
            .Concat(subHardware.SelectMany(s => s.Sensors))
            .Where(s => s.SensorType == SensorType.Temperature)
            .ToList();

        if (tempSensors.Count == 0) return;

        var group = Groups.FirstOrDefault(g => g.Name == hw.Name);
        if (group is null)
        {
            group = new HardwareGroupViewModel { Name = hw.Name, IsCelsius = IsCelsius, IsMinMaxVisible = IsMinMaxVisible };
            Groups.Add(group);
        }

        foreach (var sensor in tempSensors)
        {
            var vm = group.Sensors.FirstOrDefault(s => s.Name == sensor.Name);
            if (vm is null)
            {
                vm = new SensorViewModel { Name = sensor.Name, IsCelsius = IsCelsius, IsMinMaxVisible = IsMinMaxVisible };
                group.Sensors.Add(vm);
            }
            vm.Value = sensor.Value;
        }

        group.NotifySummaryChanged();
    }

    public void Dispose()
    {
        _timer.Stop();
        _monitor.Dispose();
    }
}
