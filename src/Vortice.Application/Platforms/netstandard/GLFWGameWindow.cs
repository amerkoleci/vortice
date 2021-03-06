// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using static GLFW;

namespace Vortice
{
    internal class GLFWGameWindow : GameWindow
    {
        private readonly IntPtr _window;

        public override IntPtr Handle
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return glfwGetWin32Window(_window);
                }

                return IntPtr.Zero;
            }
        }

        public override RectangleF ClientBounds => throw new NotImplementedException();

        public override string Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public GLFWGameWindow()
        {
            _window = glfwCreateWindow(800, 600, "Alimer", IntPtr.Zero, IntPtr.Zero);
        }

        public bool ShouldClose() => glfwWindowShouldClose(_window);
        public void SwapBuffers() => glfwSwapBuffers(_window);
    }
}
