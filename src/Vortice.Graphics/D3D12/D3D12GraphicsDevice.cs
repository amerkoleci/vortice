// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Vortice.DXGI;
using Vortice.Direct3D12;
using static Vortice.DXGI.DXGI;
using static Vortice.Direct3D12.D3D12;
using static Vortice.Graphics.D3D12.D3D12Utils;
using Vortice.Direct3D;
using Vortice.Direct3D12.Debug;

namespace Vortice.Graphics.D3D12
{
    /// <summary>
    /// Direct3D12 graphics device implementation.
    /// </summary>
    internal unsafe class D3D12GraphicsDevice : GraphicsDevice
    {
        // Feature levels to try creating devices. Listed in descending order so the highest supported level is used.
        private static readonly FeatureLevel[] s_featureLevels = new[]
        {
            FeatureLevel.Level_12_1,
            FeatureLevel.Level_12_0,
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0
        };

        private static bool? s_supportInitialized;
        private static bool s_validationSupported = false;
        private static IDXGIFactory4? s_dxgiFactory4 = default;
        private static bool s_tearingSupported = default;

        private readonly ID3D12Device2? d3d12Device;
        private GraphicsDeviceCaps _capabilities;

        private readonly ID3D12CommandQueue directQueue;

        //        public bool SupportsRenderPass { get; private set; }

        private static bool CreateFactory()
        {
            if (s_dxgiFactory4 != null)
            {
                return true;
            }

            if (EnableValidation || EnableGPUBasedValidation)
            {
                if (D3D12GetDebugInterface(out ID3D12Debug d3d12Debug).Success)
                {
                    // Enable the D3D12 debug layer.
                    d3d12Debug.EnableDebugLayer();

                    ID3D12Debug1? d3d12Debug1 = d3d12Debug.QueryInterfaceOrNull<ID3D12Debug1>();

                    if (d3d12Debug1 != null)
                    {
                        d3d12Debug1.SetEnableGPUBasedValidation(EnableGPUBasedValidation);
                        //d3d12Debug1.SetEnableSynchronizedCommandQueueValidation(true);
                        d3d12Debug1.Dispose();
                    }
                }

#if DEBUG
                if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Success)
                {
                    s_validationSupported = true;

                    dxgiInfoQueue.SetBreakOnSeverity(All, InfoQueueMessageSeverity.Error, true);
                    dxgiInfoQueue.SetBreakOnSeverity(All, InfoQueueMessageSeverity.Corruption, true);

                    var filters = new DXGI.InfoQueueFilter()
                    {
                        DenyList = new DXGI.InfoQueueFilterDescription
                        {
                            Ids = new int[]
                            {
                                80 /* IDXGISwapChain::GetContainingOutput: The swapchain's adapter does not control the output on which the swapchain's window resides. */
                            }
                        }
                    };

                    dxgiInfoQueue.AddStorageFilterEntries(Dxgi, filters);
                }
#endif
            }

            if (CreateDXGIFactory2(s_validationSupported, out s_dxgiFactory4).Failure)
            {
                return false;
            }

            IDXGIFactory5? dxgiFactory5 = s_dxgiFactory4!.QueryInterfaceOrNull<IDXGIFactory5>();
            if (dxgiFactory5 != null)
            {
                s_tearingSupported = dxgiFactory5.PresentAllowTearing;

                if (!s_tearingSupported)
                {
                    Debug.WriteLine("Direct3D12: Variable refresh rate displays not supported");
                }

                dxgiFactory5.Dispose();
            }

            return true;
        }

        public static bool IsSupported()
        {
            if (s_supportInitialized.HasValue)
                return s_supportInitialized.Value;

            s_supportInitialized = false;

            try
            {
                s_supportInitialized = CreateFactory();
                if (!s_supportInitialized.Value)
                {
                    return s_supportInitialized.Value;
                }

                s_supportInitialized = Vortice.Direct3D12.D3D12.IsSupported(FeatureLevel.Level_11_0);
            }
            catch
            {
            }

            return s_supportInitialized.Value;
        }

        public D3D12GraphicsDevice()
        {
#if TODO
            IDXGIFactory6? dxgiFactory6 = s_dxgiFactory4.QueryInterfaceOrNull<IDXGIFactory6>();
            if (dxgiFactory6 != null)
            {
                uint adapterIndex = 0;
                while (!shouldBreak)
                {
                    HRESULT result = dxgiFactor6.Get()->EnumAdapterByGpuPreference(
                        adapterIndex++,
                        DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE,
                         __uuidof<IDXGIAdapter1>(),
                         (void**)dxgiAdapter1.ReleaseAndGetAddressOf());

                    if (result == DXGI_ERROR_NOT_FOUND)
                    {
                        shouldBreak = true;
                        break;
                    }

                    DXGI_ADAPTER_DESC1 desc;
                    ThrowIfFailed(dxgiAdapter1.Get()->GetDesc1(&desc));

                    if ((desc.Flags & (uint)DXGI_ADAPTER_FLAG_SOFTWARE) != 0)
                    {
                        // Don't select the Basic Render Driver adapter.
                        continue;
                    }

                    for (int i = 0; i < s_featureLevels.Length; i++)
                    {
                        if (SUCCEEDED(D3D12CreateDevice(
                            dxgiAdapter1.AsIUnknown().Get(),
                            s_featureLevels[i],
                            __uuidof<ID3D12Device2>(),
                            d3d12Device.GetVoidAddressOf())))
                        {
                            _capabilities.VendorId = new GPUVendorId(desc.VendorId);
                            _capabilities.AdapterId = desc.DeviceId;
                            _capabilities.AdapterName = new string((char*)desc.Description);

                            if ((desc.Flags & (uint)DXGI_ADAPTER_FLAG_SOFTWARE) != 0)
                            {
                                _capabilities.AdapterType = GraphicsAdapterType.CPU;
                            }

#if DEBUG
                            //auto adapterName = ToUtf8(desc.Description);
                            //LOGD("Create Direct3D12 device {} with adapter ({}): VID:{:#04x}, PID:{:#04x} - {}",
                            //    ToString(kFeatureLevels[i]),
                            //    index,
                            //    desc.VendorId,
                            //    desc.DeviceId,
                            //    adapterName);
#endif

                            shouldBreak = true;
                            break;
                        }
                    }
                }
            }
            else 
#endif
            {
                for (int adapterIndex = 0;
                    d3d12Device == null && s_dxgiFactory4!.EnumAdapters1(adapterIndex, out IDXGIAdapter1 dxgiAdapter1).Success; adapterIndex++)
                {
                    AdapterDescription1 desc = dxgiAdapter1.Description1;

                    if ((desc.Flags & AdapterFlags.Software) != 0)
                    {
                        // Don't select the Basic Render Driver adapter.
                        dxgiAdapter1.Dispose();
                        continue;
                    }

                    for (int i = 0; i < s_featureLevels.Length; i++)
                    {
                        if (D3D12CreateDevice(dxgiAdapter1, s_featureLevels[i], out d3d12Device).Success)
                        {
                            _capabilities.VendorId = new GPUVendorId((uint)desc.VendorId);
                            _capabilities.AdapterId = (uint)desc.DeviceId;
                            _capabilities.AdapterName = desc.Description;

                            if ((desc.Flags & AdapterFlags.Software) != 0)
                            {
                                _capabilities.AdapterType = GraphicsAdapterType.CPU;
                            }
#if DEBUG
                            //auto adapterName = ToUtf8(desc.Description);
                            //LOGD("Create Direct3D12 device {} with adapter ({}): VID:{:#04x}, PID:{:#04x} - {}",
                            //    ToString(kFeatureLevels[i]),
                            //    index,
                            //    desc.VendorId,
                            //    desc.DeviceId,
                            //    adapterName);
#endif

                            dxgiAdapter1.Dispose();
                            break;
                        }
                    }
                }
            }

            // Configure debug device (if active).
            {
                ID3D12InfoQueue? d3d12InfoQueue = d3d12Device!.QueryInterfaceOrNull<ID3D12InfoQueue>();
                if (d3d12InfoQueue != null)
                {
#if DEBUG
                    d3d12InfoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
                    d3d12InfoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
#endif
                    //D3D12_MESSAGE_ID[] hide = new[]
                    //{
                    //    D3D12_MESSAGE_ID_CLEARRENDERTARGETVIEW_MISMATCHINGCLEARVALUE,
                    //    D3D12_MESSAGE_ID_CLEARDEPTHSTENCILVIEW_MISMATCHINGCLEARVALUE,

                    //    D3D12_MESSAGE_ID_MAP_INVALID_NULLRANGE,
                    //    D3D12_MESSAGE_ID_UNMAP_INVALID_NULLRANGE,
                    //    //D3D12_MESSAGE_ID_EXECUTECOMMANDLISTS_WRONGSWAPCHAINBUFFERREFERENCE
                    //};

                    //fixed (D3D12_MESSAGE_ID* pIdList = &hide[0])
                    //{
                    //    D3D12_INFO_QUEUE_FILTER filter = new D3D12_INFO_QUEUE_FILTER
                    //    {
                    //        DenyList = new D3D12_INFO_QUEUE_FILTER_DESC
                    //        {
                    //            NumIDs = (uint)hide.Length,
                    //            pIDList = pIdList
                    //        }
                    //    };

                    //    ThrowIfFailed(d3d12InfoQueue.Get()->AddStorageFilterEntries(&filter));
                    //}

                    // Break on DEVICE_REMOVAL_PROCESS_AT_FAULT
                    d3d12InfoQueue.SetBreakOnID(MessageId.DeviceRemovalProcessAtFault, true);
                    d3d12InfoQueue.Dispose();
                }
            }

            // Init capabilites.
            _capabilities.BackendType = BackendType.Direct3D12;

            FeatureDataArchitecture1 featureDataArchitecture = d3d12Device!.Architecture1;
            _capabilities.AdapterType = featureDataArchitecture.Uma ? GraphicsAdapterType.IntegratedGPU : GraphicsAdapterType.DiscreteGPU;

            FeatureDataD3D12Options1 dataOptions1 = d3d12Device.Options1;
            FeatureDataD3D12Options5 dataOptions5 = d3d12Device.Options5;

            SupportsRenderPass = false;
            if (dataOptions5.RenderPassesTier > RenderPassTier.Tier0
                && _capabilities.VendorId.KnownVendor != KnownVendorId.Intel)
            {
                SupportsRenderPass = true;
            }

            _capabilities.Features = new GraphicsDeviceFeatures
            {
                IndependentBlend = true,
                ComputeShader = true,
                TessellationShader = true,
                MultiViewport = true,
                IndexUInt32 = true,
                MultiDrawIndirect = true,
                FillModeNonSolid = true,
                SamplerAnisotropy = true,
                TextureCompressionETC2 = false,
                TextureCompressionASTC_LDR = false,
                TextureCompressionBC = true,
                TextureCubeArray = true,
                Raytracing = dataOptions5.RaytracingTier >= RaytracingTier.Tier1_0
            };

            _capabilities.Limits = new GraphicsDeviceLimits
            {
                MaxVertexAttributes = 16,
                MaxVertexBindings = 16,
                MaxVertexAttributeOffset = 2047,
                MaxVertexBindingStride = 2048,
                MaxTextureDimension1D = RequestTexture1DUDimension,
                MaxTextureDimension2D = RequestTexture2DUOrVDimension,
                MaxTextureDimension3D = RequestTexture3DUVOrWDimension,
                MaxTextureDimensionCube = RequestTextureCubeDimension,
                MaxTextureArrayLayers = RequestTexture2DArrayAxisDimension,
                MaxColorAttachments = SimultaneousRenderTargetCount,
                MaxUniformBufferRange = RequestConstantBufferElementCount * 16,
                MaxStorageBufferRange = uint.MaxValue,
                MinUniformBufferOffsetAlignment = 256u,
                MinStorageBufferOffsetAlignment = 16u,
                MaxSamplerAnisotropy = MaxMaxAnisotropy,
                MaxViewports = ViewportAndScissorRectObjectCountPerPipeline,
                MaxViewportWidth = ViewportBoundsMax,
                MaxViewportHeight = ViewportBoundsMax,
                MaxTessellationPatchSize = InputAssemblerPatchMaxControlPointCount,
                MaxComputeSharedMemorySize = ComputeShaderThreadLocalTempRegisterPool,
                MaxComputeWorkGroupCountX = ComputeShaderDispatchMaxThreadGroupsPerDimension,
                MaxComputeWorkGroupCountY = ComputeShaderDispatchMaxThreadGroupsPerDimension,
                MaxComputeWorkGroupCountZ = ComputeShaderDispatchMaxThreadGroupsPerDimension,
                MaxComputeWorkGroupInvocations = ComputeShaderThreadGroupMaxThreadsPerGroup,
                MaxComputeWorkGroupSizeX = ComputeShaderThreadGroupMaxX,
                MaxComputeWorkGroupSizeY = ComputeShaderThreadGroupMaxY,
                MaxComputeWorkGroupSizeZ = ComputeShaderThreadGroupMaxZ,
            };

            // Create queue
            directQueue = d3d12Device.CreateCommandQueue<ID3D12CommandQueue>(CommandListType.Direct, CommandQueuePriority.Normal);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="D3D12GraphicsDevice" /> class.
        /// </summary>
        ~D3D12GraphicsDevice() => Dispose(disposing: false);

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                directQueue.Dispose();

#if DEBUG
                uint refCount = d3d12Device!.Release();
                if (refCount > 0)
                {
                    Debug.WriteLine($"Direct3D12: There are {refCount} unreleased references left on the device");

                    ID3D12DebugDevice? d3d12DebugDevice = d3d12Device.QueryInterfaceOrNull<ID3D12DebugDevice>();
                    if (d3d12DebugDevice != null)
                    {
                        d3d12DebugDevice.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail | ReportLiveDeviceObjectFlags.IgnoreInternal);
                        d3d12DebugDevice.Dispose();
                    }
                }
#else
                d3d12Device.Dispose();
#endif
                s_dxgiFactory4!.Dispose();

#if DEBUG
                if (DXGIGetDebugInterface1(out IDXGIDebug1 dxgiDebug1).Success)
                {
                    dxgiDebug1.ReportLiveObjects(All, ReportLiveObjectFlags.Summary | ReportLiveObjectFlags.IgnoreInternal);
                    dxgiDebug1.Dispose();
                }
#endif
            }
        }

        internal static IDXGIFactory4 DxgiFactory => s_dxgiFactory4;
        internal static bool IsTearingSupported => s_tearingSupported;

        internal ID3D12Device2 D3D12Device => d3d12Device;

        internal ID3D12CommandQueue DirectQueue => directQueue;

        internal bool SupportsRenderPass { get; }

        /// <inheritdoc/>
        public override GraphicsDeviceCaps Capabilities => _capabilities;

        protected override SwapChain CreateSwapChainCore(IntPtr windowHandle, in SwapChainDescriptor descriptor) => new D3D12SwapChain(this, windowHandle, descriptor);
    }
}
