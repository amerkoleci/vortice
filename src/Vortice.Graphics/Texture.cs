// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Vortice.Graphics
{
    public abstract class Texture : GraphicsResource
    {
        protected Texture(GraphicsDevice device, in TextureDescriptor descriptor)
            : base(device)
        {
        }
    }
}
