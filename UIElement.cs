using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GSBPGEMG.UI
{
    // UIElement_DropDownBox // TODO (noted by MT) 
    // UIElement_PopupWindow
    // UIElement_Slider
    // UIElement_Toolbar

    public static class UICommon
    {
        public static SpriteBatch RenderTargetSpriteBatch = new(Game1.GraphicsDeviceRef);

        public static AlphaTestEffect RenderTargetAlphaTestEffect = new(Game1.GraphicsDeviceRef);

        public static DepthStencilState StencilWrite = new()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Always,
            StencilPass = StencilOperation.Replace,
            ReferenceStencil = 1,
            DepthBufferEnable = false
        };

        public static DepthStencilState StencilRead = new()
        {
            StencilEnable = true,
            StencilFunction = CompareFunction.Equal,
            StencilPass = StencilOperation.Keep,
            ReferenceStencil = 1,
            DepthBufferEnable = false
        };
    }
}
