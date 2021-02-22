using ShamanTK.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace TrippingCubes.Common
{
    class TextBox
    {
        private readonly SpriteTextFormat format;
        private readonly SpriteFont font;

        private SpriteText spriteText;

        public string Text
        {
            get => text;
            set
            {
                if (text != value)
                {
                    text = value;
                    RefreshCachedText();
                }
            }
        }
        private string text;

        public Vector3 Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    RefreshCachedText();
                }
            }
        }
        private Vector3 position;        

        public TextBox(SpriteFont font, SpriteTextFormat format)
        {
            this.font = font ??
                throw new ArgumentNullException(nameof(font));
            this.format = format ??
                throw new ArgumentNullException(nameof(format));
        }

        private void RefreshCachedText()
        {
            spriteText = font.CreateText(position, text ?? "", format);
        }

        public void Draw(IRenderContext renderContext)
        {
            spriteText.Draw(renderContext);
        }
    }
}
