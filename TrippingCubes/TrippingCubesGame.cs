﻿/*
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

using ShamanTK;
using ShamanTK.Common;
using ShamanTK.Controls;
using ShamanTK.Graphics;
using ShamanTK.IO;
using TrippingCubes.Common;
using System;
using System.Numerics;
using TrippingCubes.World;

namespace TrippingCubes
{
    public class TrippingCubesGame : ShamanApp
    {
        private readonly RenderParameters gameParameters = 
            new RenderParameters();
        private readonly RenderParameters guiParameters =
            new RenderParameters();

        private MeshBuffer cursor;
        private MeshBuffer plane;        
        private SpriteFont font;        

        private RenderTextureBuffer gameRenderTarget = null;

        private GameConsole gameConsole;

        private Vector3I cursorPosition;
        private BlockKey currentCursorBlockKey = default;
        private Vector3I selectionStart;
        private bool editTempLock;
        private bool cursorActive;        

        internal TextBox OnScreenTextBox { get; private set; }

        internal GameWorld World { get; private set; }

        internal InputScheme InputScheme { get; private set; }

        public static void Main(string[] args)
        {
            Log.UseEnglishExceptionMessages = true;
            Log.MessageLogged += (s, e) =>
                Console.WriteLine(e.ToString(Console.BufferWidth - 2));
            TrippingCubesGame game = new TrippingCubesGame();
            game.Run(new ShamanTK.Platforms.DesktopGL.PlatformProvider());

            if (Log.HighestLogMessageLevel >= Log.MessageLevel.Error)
                Console.ReadKey(true);
        }

        protected override void Load()
        {
            InputScheme = new InputScheme(Controls);

            World = new GameWorld(this, Resources, gameParameters.Camera);
            
            gameParameters.Filters.ColorShades = new Vector3(8, 8, 4);
            gameParameters.Filters.ResolutionScaleFactor = 0.42f;
            gameParameters.Filters.ResolutionScaleFilter = 
                TextureFilter.Nearest;

            guiParameters.BackfaceCullingEnabled = true;
            guiParameters.Camera.ProjectionMode = 
                ProjectionMode.OrthgraphicRelativeProportional;

            Graphics.Size = new Size(800, 600);
            Graphics.Title = "TrippingCubes - Development Preview";
            Graphics.Resized += (s,e) =>
            {
                gameRenderTarget?.Dispose();
                gameRenderTarget = Graphics.CreateRenderBuffer(Graphics.Size,
                    TextureFilter.Nearest);
            };

            FontRasterizationParameters fontRasterizationParameters =
                new FontRasterizationParameters() {
                    UseAntiAliasing = false,
                    SizePx = 48 //13, 17, 22, 27
                };

            Resources.LoadMesh(MeshData.Plane).Subscribe(r => plane = r);
            
            Resources.LoadGenericFont(new FileSystemPath("/VT323-Regular.ttf"),
                fontRasterizationParameters, TextureFilter.Linear)
                .Subscribe(r =>
                {
                    font = r;
                    OnScreenTextBox = new TextBox(font, new SpriteTextFormat()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        TypeSize = 0.035f
                    })
                    {
                        Position = new Vector3(0.5f, 1f, 0f)
                    };
                });

            Resources.LoadMesh(MeshData.Block).Subscribe(r => cursor = r);

            // TODO: Rewrite this as CompoundSyncTask
            Resources.LoadingTasksCompleted += (s,e) =>
            {
                if (gameConsole == null)
                {
                    gameConsole = new GameConsole(plane, font, Controls);
                    gameConsole.CommandIssued += GameConsole_CommandIssued;
                    gameConsole.AppendOutputText("Hello there! Enter '?' to " +
                        "list all available blocks.\nEnter the name of the " +
                        "block you want to use,\nthen use the left/right " +
                        "mouse button to place blocks.\nHave fun :>");
                    Log.Information("HINT: Use F1 to open the console.");
                }
            };
        }

        protected override void Unload()
        {
            gameConsole?.Dispose();
            World.Unload();
        }

        protected override void Redraw(TimeSpan delta)
        {            
            Graphics.Render<IRenderContext>(gameParameters, RenderGame, 
                gameRenderTarget);
            Graphics.Render<IRenderContext>(guiParameters, RenderGui);
        }

        private void RenderGame(IRenderContext context)
        {
            World.Redraw(context);

            if (cursor != null && cursorActive)
            {
                if ((InputScheme.AddBlock.IsActive || 
                    InputScheme.RemoveBlock.IsActive)
                    && !editTempLock)
                {
                    context.Opacity = 0.5f;
                    if (InputScheme.AddBlock.IsActive) 
                        context.Color = Color.Green;
                    else if (InputScheme.RemoveBlock.IsActive) 
                        context.Color = Color.Red;
                }
                else
                {
                    if (editTempLock) 
                        context.Opacity = 0.15f;
                    else 
                        context.Opacity = 0.2f;

                    context.Color = Color.White;
                }

                context.Mesh = cursor;
                context.Texture = null;

                GameWorld.GetAreaFromPoints(cursorPosition, selectionStart,
                    out Vector3I areaPosition, out Vector3I areaScale);
                context.Transformation = MathHelper.CreateTransformation(
                    areaPosition - new Vector3(0.075f),
                    areaScale + new Vector3(0.15f));

                context.Draw();
            }
        }

        private void RenderGui(IRenderContext context)
        {
            context.Mesh = plane;
            context.Transformation = MathHelper.CreateTransformation(
                0.5f, 0.5f, 1,
                Math.Max(1, (float)Graphics.Size.Width / Graphics.Size.Height),
                Math.Max(1, (float)Graphics.Size.Height / Graphics.Size.Width),
                1);

            context.Texture = gameRenderTarget;
            context.Draw();

            gameConsole?.Draw(context);
            OnScreenTextBox?.Draw(context);
        }        

        protected override void Update(TimeSpan delta)
        {
            if (InputScheme.FullscreenToggle.IsActivated)
                Graphics.Mode = Graphics.Mode == WindowMode.Fullscreen ?
                    WindowMode.NormalScalable : WindowMode.Fullscreen;
            
            if (InputScheme.Exit.IsActivated) Close();

            if (InputScheme.FilterToggle.IsActivated)
                gameParameters.Filters.Enabled =
                    !gameParameters.Filters.Enabled;

            Controls.Input.SetMouse(MouseMode.InvisibleFixed);

            if (gameConsole != null)
            {
                gameConsole.Update(delta);
                if (gameConsole.HasFocus) return;
            }

            cursorActive = currentCursorBlockKey != default;

            if (cursorActive)
            {
                cursorPosition = GetCurrentlySelectedVoxel();

                if ((InputScheme.AddBlock.IsActive &&
                    InputScheme.RemoveBlock.IsActive))
                    editTempLock = true;
                else if (InputScheme.AddBlock.IsDeactivated && !editTempLock)
                    World.SetArea(selectionStart, cursorPosition, 
                        new BlockVoxel(currentCursorBlockKey));
                else if (InputScheme.RemoveBlock.IsDeactivated && !editTempLock)
                    World.SetArea(selectionStart, cursorPosition, 
                        new BlockVoxel(0));
                else if (!(InputScheme.AddBlock.IsActive || 
                    InputScheme.RemoveBlock.IsActive))
                {
                    selectionStart = GetCurrentlySelectedVoxel();
                    editTempLock = false;
                }
            }

            World.Update(delta);
        }

        private void GameConsole_CommandIssued(object sender, string e)
        {
            e = e.Trim();

            if (e.Length == 0) gameConsole.HasFocus = false;
            else if (e == "?")
            {
                gameConsole.AppendOutputText(Log.WordWrapText(
                    string.Join(", ", World.Blocks.Identifiers), 80));
            }
            else
            {
                Block block;
                if (BlockKey.TryParse(e, out BlockKey blockKey))
                    block = World.Blocks.GetBlock(blockKey, false);
                else block = World.Blocks.GetBlock(e, false);

                if (block != null)
                {
                    /*
                    gameConsole.AppendOutputText("Cursor block changed to \"" +
                        block.Identifier + "\" (key '" + block.Key + "').");
                    */
                    currentCursorBlockKey = block.Key;
                    gameConsole.HasFocus = false;
                }
                else gameConsole.AppendOutputText("Couldn't find block with " +
                  "that key or identifier.");
            }
        }

        private Vector3I GetCurrentlySelectedVoxel()
        {
            return Vector3I.FromVector3(
                gameParameters.Camera.Position - 
                new Vector3(0.5f, 0.5f, 0.5f) +
                gameParameters.Camera.AlignVector(new Vector3(0, 0, 3.0f),
                false, false), true);
        }
    }
}