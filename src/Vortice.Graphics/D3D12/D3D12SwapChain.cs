// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using Vortice.DXGI;
using static Vortice.Graphics.D3D12.D3D12Utils;

namespace Vortice.Graphics.D3D12
{
    internal unsafe class D3D12SwapChain : SwapChain
    {
        private readonly IDXGISwapChain3 _handle;

        public D3D12SwapChain(D3D12GraphicsDevice device, IntPtr windowHandle, in SwapChainDescriptor descriptor)
            : base(device, descriptor)
        {
            var swapChainDesc = new SwapChainDescription1
            {
                Width = descriptor.Width,
                Height = descriptor.Height,
                Format = ToDXGISwapChainFormat(descriptor.ColorFormat),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 2,
                SwapEffect = SwapEffect.FlipDiscard,
                SampleDescription = new SampleDescription(1, 0),
                Flags = D3D12GraphicsDevice.IsTearingSupported ? SwapChainFlags.AllowTearing : SwapChainFlags.None
            };

            var fullscreenDesc = new SwapChainFullscreenDescription
            {
                Windowed = !descriptor.IsFullscreen
            };

            using (IDXGISwapChain1 tempSwapChain = D3D12GraphicsDevice.DxgiFactory.CreateSwapChainForHwnd(
                device.DirectQueue, windowHandle, swapChainDesc, fullscreenDesc))
            {
                D3D12GraphicsDevice.DxgiFactory.MakeWindowAssociation(windowHandle, WindowAssociationFlags.IgnoreAltEnter);

                _handle = tempSwapChain.QueryInterface<IDXGISwapChain3>();
            }

            AfterReset();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _handle.Dispose();
            }
        }

        public override void Present()
        {
            _handle.Present(0, PresentFlags.None).CheckError();
        }

        private void AfterReset()
        {
            SwapChainDescription1 desc = _handle.Description1;

            Width = desc.Width;
            Height = desc.Height;
        }
    }
}
