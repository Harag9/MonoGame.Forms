﻿#region File Description

//-----------------------------------------------------------------------------
// GraphicsDeviceControl.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#endregion

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Forms.Services;
using System.Collections.Generic;
using MonoGame.Forms.Utils;
using MonoGameKeys = Microsoft.Xna.Framework.Input.Keys;

namespace MonoGame.Forms.Controls
{
    /// <summary>
    /// This class mainly creates the <see cref="GraphicsDevice"/> and the <see cref="SwapChainRenderTarget"/>.
    /// It inherits from <see cref="System.Windows.Forms.Control"/>, which makes its childs available as a tool box control.
    /// </summary>
    public abstract class GraphicsDeviceControl : System.Windows.Forms.Control
    {
        private bool designMode
        {
            get
            {
                System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                bool res = process.ProcessName == "devenv";
                process.Dispose();
                return res;
            }
        }

        /// <summary>
        /// A swap chain used for rendering to a secondary GameWindow.
        /// Note: When working with different <see cref="RenderTarget2D"/>, 
        /// you need to set the current render target back to the <see cref="SwapChainRenderTarget"/> as this is the real 'Back Buffer'. 
        /// 'GraphicsDevice.SetRenderTarget(null)' will NOT work as you are doing usally in MonoGame. Instead use 'GraphicsDevice.SetRenderTarget(SwapChainRenderTarget)'.
        /// Otherwise you will see only a black control window.
        /// <remarks>This is an extension and not part of stock XNA. It is currently implemented for Windows and DirectX only.</remarks>
        /// </summary>
        public SwapChainRenderTarget SwapChainRenderTarget { get { return _chain; } }
        private SwapChainRenderTarget _chain;

        /// <summary>
        /// Get the GraphicsDevice.
        /// </summary>
        public GraphicsDevice GraphicsDevice => _graphicsDeviceService.GraphicsDevice;

        /// <summary>
        /// Get the GraphicsDeviceService.
        /// </summary>
        protected GraphicsDeviceService _graphicsDeviceService;

        /// <summary>
        /// Get the ServiceContainer.
        /// </summary>
        protected ServiceContainer Services { get; } = new ServiceContainer();

        #pragma warning disable 1591
        protected override void OnCreateControl()
        {
            if (!designMode)
            {
                _graphicsDeviceService = GraphicsDeviceService.AddRef(Handle, ClientSize.Width, ClientSize.Height);
                _chain = new SwapChainRenderTarget(_graphicsDeviceService.GraphicsDevice, Handle, ClientSize.Width,
                    ClientSize.Height);
                Services.AddService<IGraphicsDeviceService>(_graphicsDeviceService);
                Initialize();
            }
            base.OnCreateControl();
        }

        protected override void Dispose(bool disposing)
        {
            if (_graphicsDeviceService != null)
            {
                _graphicsDeviceService.Release(disposing);
                _graphicsDeviceService = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (MonoGameKeysList != null) KeyboardService.SetKeys(MonoGameKeysList);

            var beginDrawError = BeginDraw();
            if (string.IsNullOrEmpty(beginDrawError))
            {
                Draw();
                EndDraw();
            }
            else
            {
                PaintUsingSystemDrawing(e.Graphics, beginDrawError);
            }
        }

        private string BeginDraw()
        {
            if (_graphicsDeviceService == null)
            {
                return Text + "\n\n" + GetType();
            }
            var deviceResetError = HandleDeviceReset();
            if (!string.IsNullOrEmpty(deviceResetError))
            {
                return deviceResetError;
            }
            var viewport = new Viewport
            {
                X = 0,
                Y = 0,
                Width = ClientSize.Width,
                Height = ClientSize.Height,
                MinDepth = 0,
                MaxDepth = 1
            };
            GraphicsDevice.Viewport = viewport;
            _graphicsDeviceService.GraphicsDevice.SetRenderTarget(_chain);
            return null;
        }

        private void EndDraw()
        {
            try
            {
                _chain.Present();
            }
            catch
            {
                // ignored
            }
        }

        private string HandleDeviceReset()
        {
            var deviceNeedsReset = false;
            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    return "Graphics device lost";
                case GraphicsDeviceStatus.NotReset:
                    deviceNeedsReset = true;
                    break;
                case GraphicsDeviceStatus.Normal:
                    break;
                default:
                    var pp = GraphicsDevice.PresentationParameters;
                    deviceNeedsReset = (ClientSize.Width > pp.BackBufferWidth) ||
                                       (ClientSize.Height > pp.BackBufferHeight);
                    break;
            }
            if (!deviceNeedsReset) return null;
            try
            {
                _graphicsDeviceService.ResetDevice(ClientSize.Width,
                    ClientSize.Height);
            }
            catch (Exception e)
            {
                return "Graphics device reset failed\n\n" + e;
            }
            return null;
        }

        protected virtual void PaintUsingSystemDrawing(System.Drawing.Graphics graphics, string text)
        {
            graphics.Clear(System.Drawing.Color.DimGray);
            using (Brush brush = new SolidBrush(System.Drawing.Color.CornflowerBlue))
            {
                using (var format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    graphics.DrawString(text, Font, brush, ClientRectangle, format);
                }
            }
        }
        
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        protected abstract void Initialize();
        protected abstract void Draw();

        #region Input

        public Microsoft.Xna.Framework.Input.KeyboardState GetKeyboardState { get; set; }

        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;

        private List<MonoGameKeys> MonoGameKeysList = new List<MonoGameKeys>();

        /// <summary>
        /// Register your controls by calling this method to enable Keyboard functionallity on this control.
        /// </summary>
        /// <param name="m">The sent window message.</param>
        public new void ProcessKeyPreview(ref Message m)
        {
            if (Visible)
            {
                if (m.Msg == WM_KEYDOWN)
                {
                    MonoGameKeys monoGameKeys = KeyboardUtil.FormsKeyToMonoGameKey((Keys)m.WParam);
                    if (!MonoGameKeysList.Contains(monoGameKeys)) MonoGameKeysList.Add(monoGameKeys);
                }
                else if (m.Msg == WM_KEYUP)
                {
                    MonoGameKeys monoGameKeys = KeyboardUtil.FormsKeyToMonoGameKey((Keys)m.WParam);
                    if (MonoGameKeysList.Contains(monoGameKeys)) MonoGameKeysList.Remove(monoGameKeys);
                }
            }
        }

        #endregion
    }
}