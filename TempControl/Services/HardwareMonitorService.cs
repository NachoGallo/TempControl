using LibreHardwareMonitor.Hardware;

namespace TempControl.Services;

public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
        };
        _computer.Open();
    }

    public IEnumerable<IHardware> GetHardware() => _computer.Hardware;

    public void Update()
    {
        foreach (var hw in _computer.Hardware)
        {
            hw.Update();
            foreach (var sub in hw.SubHardware)
                sub.Update();
        }
    }

    public void Dispose() => _computer.Close();
}
