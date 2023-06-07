// Copyright (c) Microsoft Corporation. 
// Licensed under the MIT license. 

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Simplygon
{
    public class Loader
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetDllDirectory(string lpPathName);

        public static ISimplygon InitSimplygon(out EErrorCodes errorCode, out string errorMessage)
        {
            return InitSimplygonInternal(null, null, out errorCode, out errorMessage);
        }

        public static ISimplygon InitSimplygon(string sdkPath, out EErrorCodes errorCode, out string errorMessage)
        {
            return InitSimplygonInternal(sdkPath, null, out errorCode, out errorMessage);
        }

        public static ISimplygon InitSimplygon(string sdkPath, string licenseDataText, out EErrorCodes errorCode, out string errorMessage)
        {
            return InitSimplygonInternal(sdkPath, licenseDataText, out errorCode, out errorMessage);
        }

        private static ISimplygon InitSimplygonInternal(string sdkPath, string licenseDataText, out EErrorCodes errorCode, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(sdkPath))
                sdkPath = GetSDKPath();

            errorCode = 0;
            errorMessage = string.Empty;

            if (!File.Exists(sdkPath))
            {
                errorCode = EErrorCodes.DLLOrDependenciesNotFound;
                errorMessage = "Simplygon.dll not found";
                return null;
            }

            try
            {
                ISimplygon simplygon = null;
                if (!string.IsNullOrWhiteSpace(licenseDataText))
                {
                    simplygon = Simplygon.InitializeSimplygon(sdkPath, licenseDataText);
                }
                else
                {
                    simplygon = Simplygon.InitializeSimplygon(sdkPath);
                }
                errorCode = Simplygon.GetLastInitializationError();
                if (errorCode != EErrorCodes.NoError)
                {
                    throw new Exception(string.Format("Failed to load Simplygon from {0}\nErrorCode: {1}", sdkPath, errorCode));
                }

                simplygon.SendTelemetry("Loader", "C#", "", "{}");

                return simplygon;
            }
            catch (NotSupportedException ex)
            {
                errorCode = EErrorCodes.AlreadyInitialized;

                string exceptionMessage = string.Format($"Failed to load Simplygon from {sdkPath}\nErrorCode: {errorCode}\nMessage: {ex.Message}");
                Console.Error.WriteLine(exceptionMessage);
                
                errorMessage = exceptionMessage;
            }
            catch (SEHException ex)
            {
                string exceptionMessage = string.Format($"Failed to load Simplygon from {sdkPath}\nErrorCode: {errorCode}\nMessage: {ex.Message}");
                Console.Error.WriteLine(exceptionMessage);

                errorCode = EErrorCodes.DLLOrDependenciesNotFound;
                errorMessage = exceptionMessage;
            }
            catch (Exception ex)
            {
                errorCode = EErrorCodes.DLLOrDependenciesNotFound;
                errorMessage = ex.Message;
                Console.Error.WriteLine(errorMessage);
            }
            finally
            {
                if (errorCode != 0)
                {
                    string errorMessageEx = string.Format("Failed to load Simplygon from {0}\nErrorCode: {1}", sdkPath, errorCode);
                    Console.Error.WriteLine(errorMessageEx);
                }
            }
            return null;
        }

        private static string GetSDKPath()
        {
            string simplygon10Path = Environment.GetEnvironmentVariable("SIMPLYGON_10_PATH");

            if (string.IsNullOrWhiteSpace(simplygon10Path))
            {
                simplygon10Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Simplygon", "10");
            }

            simplygon10Path = Environment.ExpandEnvironmentVariables(simplygon10Path);

            SetDllDirectory(simplygon10Path);

            string simplygonDLLPath = Path.Combine(simplygon10Path, "Simplygon.dll");

            return simplygonDLLPath;
        }
    }
}
