using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GSBPGEMG.UI
{
    public class UIElement_SpriteLayer : SpriteBatch
    {
        public static SpriteBatch GenerateTextureSpriteBatch;
        public static List<Action> GenerateTextureActions;
        public List<Action> postEndActions;

        public UIElement_SpriteLayer(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            GenerateTextureSpriteBatch = new SpriteBatch(graphicsDevice);
            GenerateTextureActions ??= [];
            postEndActions = [];
        }

        public static void PreDrawActions()
        {
            for (int i = 0; i < GenerateTextureActions.Count; i++)
                GenerateTextureActions[i]();
            GenerateTextureActions.Clear();
        }

        public new void End()
        {
            base.End();

            for (int i = 0; i < postEndActions.Count; i++)
                postEndActions[i]();
            postEndActions.Clear();
        }
    }
}
