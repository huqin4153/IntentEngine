using System;
using System.IO;
using System.Runtime.InteropServices;

namespace IntentEngine.Services
{
    internal static class OnnxRuntimeNative
    {
        [DllImport("onnxruntime.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OrtGetApiBase();

        internal static bool CheckHealth()
        {
            try
            {
                IntPtr basePtr = OrtGetApiBase();
                return basePtr != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsDllAvailable()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dllPath = Path.Combine(baseDir, "onnxruntime.dll");
            if (!File.Exists(dllPath))
                dllPath = Path.Combine(baseDir, "bin", "onnxruntime.dll");
            return File.Exists(dllPath);
        }
    }
}
