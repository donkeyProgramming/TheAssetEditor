using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using Shared.Core.ErrorHandling;

namespace Shared.Core.Misc
{
    public class MemoryOptimiser
    {
        static readonly ILogger s_logger = Logging.Create<MemoryOptimiser>();

        public static void Optimise()
        {

            RunGarbageCollection();

            using var process = Process.GetCurrentProcess();
            try
            {
                EmptyWorkingSet(process.Handle);
            }
            catch { }

        }

        [DllImport("psapi.dll")] private static extern bool EmptyWorkingSet(nint hProcess);

        public static void RunGarbageCollection()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        }

        public static void LogMemory(string label)
        {
            var process = Process.GetCurrentProcess();
            var memoryInfo = GC.GetGCMemoryInfo();
            var managed = GC.GetTotalMemory(forceFullCollection: true);
            s_logger.Here().Information($"{label} | Managed={managed / 1024 / 1024} MB, HeapSize={memoryInfo.HeapSizeBytes / 1024 / 1024} MB, " +
                $"TotalCommitted={memoryInfo.TotalCommittedBytes / 1024 / 1024} MB, WorkingSet={process.WorkingSet64 / 1024 / 1024} MB");
        }
    }
}
