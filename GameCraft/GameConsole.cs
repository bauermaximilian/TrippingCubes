/*
 * GameCraft
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

using Eterra.Common;
using Eterra.Controls;
using Eterra.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace GameCraft
{
    class GameConsole : DisposableBase
    {
        private const char CursorChar = '|';

        private const float Padding = 0.0075f;

        public bool HasFocus { get; set; }

        private readonly ControlMapping toggleFocus, addNewline,
            confirmInput, clearInput, moveCursorLeft, moveCursorRight,
            moveCursorBeginning, moveCursorEnd,
            removeCharacterBeforeCursor, removeCharacterAfterCursor;

        private readonly Vector3 position;

        private readonly SpriteTextFormat spriteTextFormat;

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Is thrown when the assigned value is less than 0.
        /// </exception>
        public int DisplayedLinesCount
        {
            get => displayedLinesCount;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException();
                else displayedLinesCount = value;

                PruneOutputTextLineQueue(true);
            }
        }
        private int displayedLinesCount = 8;

        public Color BackgroundColor { get; set; } = new Color(0, 0, 0, 100);

        public Color ForegroundColor
        {
            get => foregroundColor;
            set
            {
                if (inputSpriteText != null)
                    inputSpriteText.Color = value;
                if (outputSpriteText != null)
                    outputSpriteText.Color = value;
                foregroundColor = value;
            }
        }

        private Color foregroundColor = Color.White;

        private SpriteText inputSpriteText = null;
        private SpriteText outputSpriteText = null;

        private string inputText = "";

        private readonly Queue<string> outputTextLines = new Queue<string>();

        private int cursorIndex = 0;

        private readonly ControlsManager controlsManager;
        private readonly SpriteFont spriteFont;
        private readonly MeshBuffer planeMeshBuffer;

        private DateTime lastRemoval = DateTime.Now;
        private DateTime lastCursorMovement = DateTime.Now;

        private static readonly TimeSpan retriggerControlActionStartTreshold = 
            TimeSpan.FromSeconds(0.5);
        private static readonly TimeSpan retriggerControlActionTreshold =
            TimeSpan.FromSeconds(0.025);

        public event EventHandler<string> CommandIssued;

        public GameConsole(MeshBuffer planeMeshBuffer, SpriteFont spriteFont,
            ControlsManager controlsManager, Vector3 position,
            SpriteTextFormat spriteTextFormat)
            : this(planeMeshBuffer, spriteFont, controlsManager)
        {
            this.position = position;
            this.spriteTextFormat = spriteTextFormat ??
                throw new ArgumentNullException(nameof(spriteTextFormat));
        }

        public GameConsole(MeshBuffer planeMeshBuffer, SpriteFont spriteFont, 
            ControlsManager controlsManager)
        {
            this.planeMeshBuffer = planeMeshBuffer ??
                throw new ArgumentNullException(nameof(planeMeshBuffer));
            this.spriteFont = spriteFont ??
                throw new ArgumentNullException(nameof(spriteFont));
            this.controlsManager = controlsManager ??
                throw new ArgumentNullException(nameof(controlsManager));

            spriteTextFormat = new SpriteTextFormat
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                TypeSize = 0.03f
            };

            position = new Vector3(Padding, Padding, 0);

            toggleFocus = controlsManager.Map(KeyboardKey.F1);
            addNewline = controlsManager.MapCustom(i => i.IsPressed(
                KeyboardKey.Enter) && i.IsPressed(KeyboardKey.Shift));
            confirmInput = controlsManager.MapCustom(i => i.IsPressed(
                KeyboardKey.Enter) && !i.IsPressed(KeyboardKey.Shift));
            clearInput = controlsManager.MapCustom(i => i.IsPressed(
                KeyboardKey.Shift) && (i.IsPressed(KeyboardKey.Backspace) ||
                i.IsPressed(KeyboardKey.Delete)));
            moveCursorLeft = controlsManager.Map(KeyboardKey.Left);
            moveCursorRight = controlsManager.Map(KeyboardKey.Right);
            moveCursorBeginning = controlsManager.Map(KeyboardKey.Home);
            moveCursorEnd = controlsManager.Map(KeyboardKey.End);
            removeCharacterAfterCursor = controlsManager.MapCustom(i => 
                !i.IsPressed(KeyboardKey.Shift) && 
                i.IsPressed(KeyboardKey.Delete));
            removeCharacterBeforeCursor = controlsManager.MapCustom(i =>
                 !i.IsPressed(KeyboardKey.Shift) &&
                 i.IsPressed(KeyboardKey.Backspace));
        }

        private void PruneOutputTextLineQueue(bool invalidateSpriteText)
        {
            while (outputTextLines.Count > displayedLinesCount)
                outputTextLines.Dequeue();
            if (invalidateSpriteText) outputSpriteText = null;
        }

        public void AppendOutputText(string text)
        {
            ThrowIfDisposed();

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            foreach (string line in text.Split('\n'))
                outputTextLines.Enqueue(line.Trim('\r'));

            PruneOutputTextLineQueue(true);
        }

        private void UnregisterInputMappings()
        {
            controlsManager.Unmap(toggleFocus);
            controlsManager.Unmap(addNewline);
            controlsManager.Unmap(confirmInput);
            controlsManager.Unmap(clearInput);
            controlsManager.Unmap(moveCursorLeft);
            controlsManager.Unmap(moveCursorRight);
            controlsManager.Unmap(removeCharacterAfterCursor);
            controlsManager.Unmap(removeCharacterBeforeCursor);
        }

        private int GetClampedCursorIndex(int moveDirection = 0)
        {
            return Math.Max(Math.Min(cursorIndex + moveDirection, 
                inputText.Length), 0);
        }

        private void GetInputTextParts(out string textBeforeCursor, 
            out string textAfterCursor)
        {
            int safeCursorIndex = GetClampedCursorIndex();

            textBeforeCursor = inputText.Substring(0, safeCursorIndex);
            textAfterCursor = inputText.Substring(safeCursorIndex,
                inputText.Length - safeCursorIndex);
        }

        private void RemoveCharacterAtCursor(bool removeAfter)
        {
            GetInputTextParts(out string textBefore, out string textAfter);

            if (removeAfter && textAfter.Length > 0)
                textAfter = textAfter.Substring(1, textAfter.Length - 1);
            else if (!removeAfter && textBefore.Length > 0)
            {
                textBefore = textBefore.Substring(0, textBefore.Length - 1);
                cursorIndex = GetClampedCursorIndex(-1);
            }
            else return;

            inputText = textBefore + textAfter;
            inputSpriteText = null;
        }

        private void InsertTextAtCursor(string insertText)
        {
            if (insertText.Length == 0) return;

            GetInputTextParts(out string before, out string after);

            before += insertText;

            inputText = before + after;
            cursorIndex = GetClampedCursorIndex(insertText.Length);

            inputSpriteText = null;
        }

        private void MoveCursor(int direction)
        {
            cursorIndex = GetClampedCursorIndex(direction);

            inputSpriteText = null;
        }

        private void ResetInputText()
        {
            inputText = "";
            cursorIndex = 0;

            inputSpriteText = null;
        }

        public void InvalidateText()
        {
            inputSpriteText = null;
            outputSpriteText = null;
        }

        private float fadeout = 0;

        public void Update(TimeSpan delta)
        {
            ThrowIfDisposed();

            if (HasFocus)
            {
                fadeout = Math.Min(1, fadeout +
                    (float)(delta.TotalSeconds * 2.5));

                controlsManager.Input.SetMouse(MouseMode.VisibleFree);

                string typedText = controlsManager.Input.GetTypedCharacters();

                if (typedText.Length > 0) InsertTextAtCursor(typedText);
                else if (TriggersControlAction(removeCharacterAfterCursor,
                    ref lastRemoval)) RemoveCharacterAtCursor(true);
                else if (TriggersControlAction(removeCharacterBeforeCursor,
                    ref lastRemoval)) RemoveCharacterAtCursor(false);
                else if (TriggersControlAction(moveCursorLeft,
                    ref lastCursorMovement)) MoveCursor(-1);
                else if (TriggersControlAction(moveCursorRight,
                    ref lastCursorMovement)) MoveCursor(1);
                else if (moveCursorBeginning.IsActivated)
                    MoveCursor(-int.MaxValue);
                else if (moveCursorEnd.IsActivated)
                    MoveCursor(int.MaxValue);
                else if (clearInput.IsActivated) ResetInputText();
                else if (addNewline.IsActivated)
                    InsertTextAtCursor(Environment.NewLine);
                else if (confirmInput.IsActivated)
                {
                    CommandIssued?.Invoke(this, inputText);
                    ResetInputText();
                }

                if (inputSpriteText == null)
                {
                    GetInputTextParts(out string textBefore,
                        out string textAfter);
                    string textWithCursor =
                        textBefore + CursorChar + textAfter;
                    inputSpriteText = spriteFont.CreateText(position,
                        textWithCursor, spriteTextFormat);
                    inputSpriteText.Color = foregroundColor;
                }

                if (outputSpriteText == null)
                {
                    string outputText = string.Join(Environment.NewLine,
                        outputTextLines);

                    outputSpriteText = spriteFont.CreateText(position +
                        new Vector3(0, inputSpriteText.AreaSize.Y, 0),
                        outputText, spriteTextFormat);
                    outputSpriteText.Color = foregroundColor;
                }
            }
            else
            {
                fadeout = Math.Max(0, fadeout - 
                    (float)(delta.TotalSeconds * 2));
            }

            if (toggleFocus != null && toggleFocus.IsActivated)
                HasFocus = !HasFocus;
        }

        private static bool TriggersControlAction(ControlMapping mapping, 
            ref DateTime triggeredLast)
        {
            if (mapping.IsActivated || ((mapping.IsActive &&
                mapping.ValueUnchanged > retriggerControlActionStartTreshold)
                && ((DateTime.Now - triggeredLast) >
                retriggerControlActionTreshold)))
            {
                triggeredLast = DateTime.Now;
                return true;
            }
            else return false;
        }

        public void Draw(IRenderContext renderContext)
        {
            ThrowIfDisposed();

            if (renderContext == null)
                throw new ArgumentNullException(nameof(renderContext));

            if (fadeout <= float.Epsilon || inputSpriteText == null || 
                outputSpriteText == null) return;

            inputSpriteText.Opacity = outputSpriteText.Opacity =
                renderContext.Opacity = fadeout;
            renderContext.Mesh = planeMeshBuffer;
            renderContext.Color = BackgroundColor;
            renderContext.Texture = null;
            renderContext.Transformation = MathHelper.CreateTransformation(0.5f,
                (inputSpriteText.AreaSize.Y + outputSpriteText.AreaSize.Y + 
                Padding * 2) / 2, 0.2f, 1, (inputSpriteText.AreaSize.Y +
                outputSpriteText.AreaSize.Y + Padding * 2), 1);
            renderContext.Draw();

            if (HasFocus) inputSpriteText.Draw(renderContext);
            outputSpriteText.Draw(renderContext);
        }

        protected override void Dispose(bool disposing)
        {
            UnregisterInputMappings();
        }
    }
}
