using System.Runtime.InteropServices;
using System.Timers;
using FileFlows.Shared;

namespace FileFlows.FlowRunner;

public class SystemHelper
{
    System.Timers.Timer StayAwakeTimer;
    readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private readonly RunInstance runInstance;
    
    public SystemHelper(RunInstance runInstance)
    {
        this.runInstance = runInstance;
        this.StayAwakeTimer = new  System.Timers.Timer();
        this.StayAwakeTimer.AutoReset = true;
        this.StayAwakeTimer.Interval = 30_000;
        this.StayAwakeTimer.Elapsed += StayAwakeTimerOnElapsed;
        
    }

    private void StayAwakeTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        runInstance.Properties.Logger?.DLog("Telling Windows to stay awake");
        NativeMethods.SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED);
    }


    public void Start()
    {
        if (IsWindows == false)
            return;
        
        this.StayAwakeTimer.Start();
        
    }

    public void Stop()
    {
        this.StayAwakeTimer.Stop();
    }
    
    enum EXECUTION_STATE : uint
    {
        ES_SYSTEM_REQUIRED = 0x00000001
    }

    class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);
    }
}
