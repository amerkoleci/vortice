// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using static Vortice.Graphics.VGPU;

namespace Vortice.Graphics
{
    public class GraphicsDevice : IDisposable
    {
        public const string EnableValidationSwitchName = "Vortice.Graphics.EnableValidation";
        public const string EnableGPUBasedValidationSwitchName = "Vortice.Graphics.EnableGPUBasedValidation";

        static GraphicsDevice()
        {
            if (!AppContext.TryGetSwitch(EnableValidationSwitchName, out bool validationValue))
            {
#if DEBUG
                validationValue = true;
                AppContext.SetSwitch(EnableValidationSwitchName, validationValue);
#endif
            }

            EnableValidation = validationValue;

            if (AppContext.TryGetSwitch(EnableGPUBasedValidationSwitchName, out validationValue))
            {
                EnableGPUBasedValidation = validationValue;
                if (validationValue)
                {
                    EnableValidation = true;
                }
            }
        }

        private GraphicsDevice(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }

        public static bool EnableValidation { get; }
        public static bool EnableGPUBasedValidation { get; }
        public static BackendType PreferredBackendType { get; set; } = BackendType.Default;

        protected GraphicsDevice()
        {
        }

        public virtual GraphicsDeviceCaps Capabilities { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc cref="Dispose()" />
        /// <param name="disposing">
        /// <c>true</c> if the method was called from <see cref="Dispose()" />; otherwise, <c>false</c>.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        public bool BeginFrame()
        {
            return vgpuBeginFrame(Handle);
        }

        public void EndFrame()
        {
            vgpuEndFrame(Handle);
        }

        public static GraphicsDevice? CreateSystemDefault(IntPtr windowHandle)
        {
            if (PreferredBackendType == BackendType.Default)
            {
                PreferredBackendType = GetDefaultPlatformBackend();
            }

            // Setup graphics.
            vgpu_set_log_callback(GPULogCallback, IntPtr.Zero);

            GPUDeviceInfo info = new GPUDeviceInfo
            {
                PowerPreference = PowerPreference.HighPerformance,
                Flags = GPUDeviceFlags.None,
                SwapchainInfo = new SwapchainInfo
                {
                    WindowHandle = windowHandle,
                    ColorFormat = TextureFormat.BGRA8Unorm,
                }
            };

            var device = VGPU.vgpuCreateDevice(BackendType.Direct3D11, info);

            return new GraphicsDevice(device);
        }

        public static bool IsBackendSupported(BackendType type)
        {
            if (type == BackendType.Default)
            {
                type = GetDefaultPlatformBackend();
            }

            return vgpu_is_backend_supported(type);
        }

        private static void GPULogCallback(IntPtr userData, GPULogLevel level, string message)
        {
            Console.WriteLine($"{level}: {message}");
            System.Diagnostics.Debug.WriteLine($"{level}: {message}");
        }

        /// <summary>
        /// Gets the best supported <see cref="BackendType"/> on the current platform.
        /// </summary>
        /// <returns></returns>
        public static BackendType GetDefaultPlatformBackend()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (IsBackendSupported(BackendType.Direct3D12))
                {
                    return BackendType.Direct3D12;
                }

                if (IsBackendSupported(BackendType.Vulkan))
                {
                    return BackendType.Vulkan;
                }

                return BackendType.Direct3D11;
            }
            //else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            //{
            //    return BackendType.Metal;
            //}

            if (IsBackendSupported(BackendType.Vulkan))
            {
                return BackendType.Vulkan;
            }

            return BackendType.Vulkan;
        }

        //public SwapChain CreateSwapChain(IntPtr windowHandle, in SwapChainDescriptor descriptor)
        //{
        //    return CreateSwapChainCore(windowHandle, descriptor);
        //}

        //protected abstract SwapChain CreateSwapChainCore(IntPtr windowHandle, in SwapChainDescriptor descriptor);
    }
}
