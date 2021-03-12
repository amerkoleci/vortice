// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Vortice.Graphics
{
    public static unsafe class VGPU
    {
        private const string LibName = "vgpu";

        //private static readonly IntPtr s_vgpuLibrary = LoadLibrary();

        //public const int MaxAdapterNameSize = 256;

        //private static IntPtr LoadLibrary()
        //{
        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //    {
        //        return NativeLibrary.Load("vgpu.dll");
        //    }
        //    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //    {
        //        return NativeLibrary.Load("vgpu.so");
        //    }
        //    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //    {
        //        return NativeLibrary.Load("vgpu.dylib");
        //    }
        //    else
        //    {
        //        return NativeLibrary.Load("vgpu");
        //    }
        //}

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LogCallback(IntPtr userData, GPULogLevel level, string msg);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vgpu_set_log_callback(LogCallback callback, IntPtr userData);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool32 vgpu_is_backend_supported(BackendType type);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr vgpuCreateDevice(BackendType preferredBackend, GPUDeviceInfo info);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool32 vgpuBeginFrame(IntPtr device);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void vgpuEndFrame(IntPtr device);
    }

    public enum GPULogLevel
    {
        Error = 0,
        Warn = 1,
        Info = 2,
    }

    [Flags]
    public enum GPUDeviceFlags
    {
        None = 0x00000000,
        Debug = 0x00000001,
        GPUBasedValidation = 0x00000002,
        RenderDoc = 0x00000004,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SwapchainInfo
    {
        public IntPtr WindowHandle;
        public TextureFormat ColorFormat;
        public uint Width;
        public uint Height;
        //public PixelFormat DepthStencilFormat;
        public Bool32 VerticalSync;
        public Bool32 Fullscreen;
        public IntPtr Label;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GPUDeviceInfo
    {
        public GPUDeviceFlags Flags;
        public PowerPreference PowerPreference;
        public SwapchainInfo SwapchainInfo;
    }
}
