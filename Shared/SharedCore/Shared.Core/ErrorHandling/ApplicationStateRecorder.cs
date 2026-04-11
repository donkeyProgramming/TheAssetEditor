using System.Diagnostics;

namespace Shared.Core.ErrorHandling
{
    public static class ApplicationStateRecorder
    {
        static Stopwatch s_applicationTimer;
        static uint s_numberOfEditorsOpened = 0;

        public static void Initialize()
        {
            s_applicationTimer = Stopwatch.StartNew();
        }


        public static void EditorOpened()
        {
            s_numberOfEditorsOpened++;
        }

        public static double GetApplicationRunTimeInSec() => s_applicationTimer.Elapsed.TotalSeconds;
        public static uint GetNumberOfOpenedEditors() => s_numberOfEditorsOpened;
    }
}
