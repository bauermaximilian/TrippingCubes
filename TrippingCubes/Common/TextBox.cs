/*
 * TrippingCubes
 * A toolkit for creating games in a voxel-based environment.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using ShamanTK.Graphics;
using System;
using System.Numerics;

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
