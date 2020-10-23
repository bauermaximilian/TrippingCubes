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

using ShamanTK;
using ShamanTK.Common;
using ShamanTK.Controls;
using ShamanTK.Graphics;
using ShamanTK.IO;
using GameCraft.BlockChunk;
using GameCraft.Common;
using System;
using System.Numerics;
using System.Threading;

namespace GameCraft
{
    public class GameCraftGame : ShamanApplicationBase
    {
        private readonly float availabilityDistanceChangeTreshold = 16;
        private readonly float distanceForAvailabilityDataOnly = 48;
        private readonly float distanceForAvailabilityNone = 64;

        private Chunk<BlockVoxel> rootChunk;

        private readonly RenderParameters gameParameters = 
            new RenderParameters();
        private readonly RenderParameters guiParameters =
            new RenderParameters();

        private MeshBuffer cursor;
        private MeshBuffer plane;
        private MeshBuffer skyboxMesh;
        private TextureBuffer skyboxTextureInner;
        private TextureBuffer skyboxTextureOuter;
        private SpriteFont font;
        private RenderTextureBuffer gameRenderTarget = null;

        private ControlMapping exit;
        private ControlMapping fullscreenToggle;
        private ControlMapping addBlock, removeBlock;

        private FlyCamController cameraController;
        private GameConsole gameConsole;
        private BlockRegistry blockRegistry;                

        private Vector3I cursorPosition;
        private BlockKey currentCursorBlockKey = default;
        private Vector3I selectionStart;
        private bool editTempLock;
        private bool cursorActive;

        public static void Main(string[] args)
        {
            Log.UseEnglishExceptionMessages = true;
            Log.MessageLogged += (s, e) =>
                Console.WriteLine(e.ToString(Console.BufferWidth - 2));
            GameCraftGame game = new GameCraftGame();
            game.Run(new ShamanTK.Platforms.DesktopGL.PlatformProvider());

            if (Log.HighestLogMessageLevel >= Log.MessageLevel.Warning)
                Console.ReadKey(true);
        }

        protected override void Load()
        {
            fullscreenToggle = Controls.Map(KeyboardKey.F11);

            gameParameters.BackfaceCullingEnabled = true;
            //guiParameters.BackfaceCullingEnabled = true;

            Graphics.Size = new Size(800, 600);

            guiParameters.Camera.ProjectionMode = 
                ProjectionMode.OrthgraphicRelativeProportional;

            cameraController = new FlyCamController(gameParameters.Camera)
            {
                MoveForward = Controls.Map(KeyboardKey.W),
                MoveLeft = Controls.Map(KeyboardKey.A),
                MoveBackward = Controls.Map(KeyboardKey.S),
                MoveRight = Controls.Map(KeyboardKey.D),
                MoveUp = Controls.MapCustom(c => c.IsPressed(KeyboardKey.E) 
                    || c.IsPressed(KeyboardKey.Space)),
                MoveDown = Controls.MapCustom(c => c.IsPressed(KeyboardKey.Q)
                    || c.IsPressed(KeyboardKey.Shift)),
                LookUp = Controls.Map(MouseSpeedAxis.Up),
                LookRight = Controls.Map(MouseSpeedAxis.Right),
                LookDown = Controls.Map(MouseSpeedAxis.Down),
                LookLeft = Controls.Map(MouseSpeedAxis.Left)
            };
            exit = Controls.Map(KeyboardKey.Escape);

            addBlock = Controls.Map(MouseButton.Left);
            removeBlock = Controls.Map(MouseButton.Right);

            Graphics.Title = "GameCraft - Development Preview";

            Graphics.Resized += GraphicsResized;

            DateTime startTime = DateTime.Now;
            BlockRegistryBuilder blockRegistryBuilder =
                BlockRegistryBuilder.FromXml("/synthwave/registry.xml",
                Resources.FileSystem, false);
            blockRegistry = blockRegistryBuilder.GenerateRegistry(
                Resources.FileSystem);
            Log.Trace("Block registry with " + blockRegistry.Count +
                " block definitions loaded in " +
                (DateTime.Now - startTime).TotalMilliseconds + "ms.");

            FontRasterizationParameters fontRasterizationParameters =
                new FontRasterizationParameters() {
                    UseAntiAliasing = false,
                    SizePx = 22 //13, 17, 22, 27
                };

            Resources.LoadMesh(MeshData.Plane).AddFinalizer(r => plane = r);
            Resources.LoadMesh(MeshData.Skybox).AddFinalizer(
                r => skyboxMesh = r);
            Resources.LoadTexture("/synthwave/skybox-inner.png", TextureFilter.Linear).AddFinalizer(
                r => skyboxTextureInner = r);
            Resources.LoadTexture("/synthwave/skybox-outer.png", TextureFilter.Linear).AddFinalizer(
                r => skyboxTextureOuter = r);
            Resources.LoadGenericFont(new FileSystemPath("/VT323-Regular.ttf"),
                fontRasterizationParameters, TextureFilter.Nearest)
                .AddFinalizer(r => font = r);

            Resources.LoadingTasksCompleted += LoadingTasksCompleted;

            Resources.LoadMesh(MeshData.Block).AddFinalizer(r => cursor = r);

            rootChunk = new Chunk<BlockVoxel>(
                new BlockChunkManager("/world/", blockRegistry, Resources));
        }

        private void LoadingTasksCompleted(object sender, EventArgs e)
        {
            if (gameConsole == null)
            {
                gameConsole = new GameConsole(plane, font, Controls);
                gameConsole.CommandIssued += GameConsole_CommandIssued;
                gameConsole.AppendOutputText("Hello there! Enter '?' to " +
                    "list all available blocks.\nEnter the name of the " +
                    "block you want to use,\nthen use the left/right mouse " +
                    "button to place blocks.\nHave fun :>");

                Log.Information("HINT: Use F1 to open the console.");
            }
        }

        protected override void Unload()
        {
            if (rootChunk != null)
            {
                Log.Trace("Saving and closing world...");
                foreach (var chunk in rootChunk)
                {
                    chunk.Unlock();
                    chunk.ChangeAvailability(ChunkAvailability.None);
                    chunk.Update(TimeSpan.Zero);
                }

                bool unsavedChunksLeft = false;
                do
                {
                    Thread.Sleep(1000);
                    foreach (var chunk in rootChunk)
                    {
                        chunk.Update(TimeSpan.Zero);
                        unsavedChunksLeft |=
                            chunk.Availability != ChunkAvailability.None;
                    }
                } while (unsavedChunksLeft);

                Log.Trace("All chunks saved. Exiting...");
            }
            gameConsole?.Dispose();

        }

        private void GraphicsResized(object sender, EventArgs e)
        {
            gameRenderTarget?.Dispose();
            gameRenderTarget = Graphics.CreateRenderBuffer(Graphics.Size, 
                TextureFilter.Nearest);
        }

        protected override void Redraw(TimeSpan delta)
        {
            Graphics.Render<IRenderContext>(gameParameters, RenderGame, 
                gameRenderTarget);
            Graphics.Render<IRenderContext>(guiParameters, RenderGui);
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
        }

        private void RenderGame(IRenderContext context)
        {
            context.Mesh = skyboxMesh;

            context.Texture = skyboxTextureOuter;
            context.Transformation = MathHelper.CreateTransformation(
                gameParameters.Camera.Position,
                new Vector3(gameParameters.Camera.ClippingRange.Y - 100),
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, Angle.Deg(45)));
            context.Draw();

            context.Texture = skyboxTextureInner;
            context.Transformation = MathHelper.CreateTransformation(
                gameParameters.Camera.Position, new Vector3((
                gameParameters.Camera.ClippingRange.Y - 100) * 0.65f),
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, Angle.Deg(45)));
            context.Opacity = MathHelper.GetTimeSine(0.5, 0.05) + 0.95f;
            context.Draw();

            context.Opacity = 1;

            context.Fog = new Fog(20, 10, Color.TransparentWhite, false);

            foreach (var chunk in rootChunk) chunk.Redraw(context);

            if (cursor != null && cursorActive)
            {
                if ((addBlock.IsActive || removeBlock.IsActive) 
                    && !editTempLock)
                {
                    context.Opacity = 0.5f;
                    if (addBlock.IsActive) context.Color = Color.Green;
                    else if (removeBlock.IsActive) context.Color = Color.Red;
                }
                else
                {
                    if (editTempLock) context.Opacity = 0.15f;
                    else context.Opacity = 0.2f;
                    context.Color = Color.White;
                }                

                context.Mesh = cursor;
                
                context.Texture = null;

                GetAreaFromPoints(cursorPosition, selectionStart,
                    out Vector3I areaPosition, out Vector3I areaScale);

                context.Transformation = MathHelper.CreateTransformation(
                    areaPosition - new Vector3(0.075f),
                    areaScale + new Vector3(0.15f));

                context.Draw();
            }
        }

        private void GameConsole_CommandIssued(object sender, string e)
        {
            e = e.Trim();

            if (e.Length == 0) gameConsole.HasFocus = false;
            else if (e == "?")
            {
                gameConsole.AppendOutputText(Log.WordWrapText(
                    string.Join(", ", blockRegistry.Identifiers), 80));
            }
            else
            {
                Block block;
                if (BlockKey.TryParse(e, out BlockKey blockKey))
                    block = blockRegistry.GetBlock(blockKey, false);
                else block = blockRegistry.GetBlock(e, false);

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

        protected override void Update(TimeSpan delta)
        {
            if (fullscreenToggle.IsActivated)
                Graphics.Mode = Graphics.Mode == WindowMode.Fullscreen ?
                    WindowMode.NormalScalable : WindowMode.Fullscreen;

            if (exit.IsActivated) Close();

            foreach (var chunk in rootChunk)
            {
                Vector3 chunkCenter = new Vector3(chunk.Offset.X +
                   chunk.SideLength / 2.0f, chunk.Offset.Y +
                   chunk.SideLength / 2.0f, chunk.Offset.Z +
                   chunk.SideLength / 2.0f);

                float distanceFromCamera = 
                    (gameParameters.Camera.Position - chunkCenter).Length();

                if (distanceFromCamera < distanceForAvailabilityDataOnly)
                    chunk.ChangeAvailability(ChunkAvailability.Full);
                else if (distanceFromCamera > (distanceForAvailabilityDataOnly
                    + availabilityDistanceChangeTreshold) &&
                    (distanceFromCamera < distanceForAvailabilityNone))
                    chunk.ChangeAvailability(ChunkAvailability.DataOnly);
                else if (distanceFromCamera > (distanceForAvailabilityNone +
                    availabilityDistanceChangeTreshold))
                    chunk.ChangeAvailability(ChunkAvailability.None);

                chunk.Update(delta);
            }

            if (gameConsole != null)
            {
                gameConsole.Update(delta);
                if (gameConsole.HasFocus) return;
            }

            cursorActive = currentCursorBlockKey != default;

            if (cursorActive)
            {
                cursorPosition = GetCurrentlySelectedVoxel();

                if ((addBlock.IsActive && removeBlock.IsActive))
                    editTempLock = true;
                else if (addBlock.IsDeactivated && !editTempLock)
                    SetArea(selectionStart, cursorPosition, new BlockVoxel(
                        currentCursorBlockKey));
                else if (removeBlock.IsDeactivated && !editTempLock)
                    SetArea(selectionStart, cursorPosition, new BlockVoxel(0));
                else if (!(addBlock.IsActive || removeBlock.IsActive))
                {
                    selectionStart = GetCurrentlySelectedVoxel();
                    editTempLock = false;
                }
            }

            Controls.Input.SetMouse(MouseMode.InvisibleFixed);
            cameraController.Update(
                TimeSpan.FromSeconds(1.0 / TargetUpdatesPerSecond));
        }

        private Vector3I GetCurrentlySelectedVoxel()
        {
            return Vector3I.FromVector3(
                gameParameters.Camera.Position - 
                new Vector3(0.5f, 0.5f, 0.5f) +
                gameParameters.Camera.AlignVector(new Vector3(0, 0, 3.0f),
                false, false));
        }

        private static void GetAreaFromPoints(Vector3I a, Vector3I b,
            out Vector3I areaPosition, out Vector3I areaScale)
        {
            areaPosition = new Vector3I(Math.Min(a.X, b.X),
                Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
            areaScale = new Vector3I(Math.Max(a.X, b.X),
                Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z)) + Vector3I.One -
                areaPosition;
        }

        private void SetArea(Vector3I start, Vector3I end, BlockVoxel voxel)
        {
            GetAreaFromPoints(start, end, out Vector3I areaStart,
                out Vector3I areaScale);

            rootChunk.TraverseToChunk(areaStart, true, out var startChunk);
            rootChunk.TraverseToChunk(areaStart + areaScale, true,
                out var endChunk);
            int chunkSideLength = rootChunk.SideLength;
            Vector3I wholeChunkArea = Vector3I.One +
                ((endChunk.Offset - startChunk.Offset) / chunkSideLength);

            Chunk<BlockVoxel>[,,] chunkCache = new Chunk<BlockVoxel>[
                wholeChunkArea.X, wholeChunkArea.Y, wholeChunkArea.Z];
            chunkCache[0, 0, 0] = startChunk;
            chunkCache[chunkCache.GetLength(0) - 1,
                chunkCache.GetLength(1) - 1,
                chunkCache.GetLength(2) - 1] = endChunk;

            Chunk<BlockVoxel> currentChunk;

            for (int x = 0; x < areaScale.X; x++)
            {
                for (int y = 0; y < areaScale.Y; y++)
                {
                    for (int z = 0; z < areaScale.Z; z++)
                    {
                        Vector3I offset = new Vector3I(x, y, z);
                        Vector3I cacheOffset =
                            ((areaStart - startChunk.Offset) + offset) /
                            chunkSideLength;

                        if (chunkCache[cacheOffset.X, cacheOffset.Y,
                            cacheOffset.Z] == null)
                        {
                            rootChunk.TraverseToChunk(areaStart + offset,
                                true, out chunkCache[cacheOffset.X,
                                cacheOffset.Y, cacheOffset.Z]);
                        }

                        currentChunk = chunkCache[cacheOffset.X,
                                cacheOffset.Y, cacheOffset.Z];

                        if (currentChunk.Availability !=
                            ChunkAvailability.None)
                        {
                            currentChunk.Unlock();
                            currentChunk.SetVoxel(areaStart + offset -
                                currentChunk.Offset, voxel);
                        }
                    }
                }
            }
        }

        /*
        //Previous implementation, with small worlds/selections it performs
        //the same like the new, but it does perform significantly worse on
        //larger selections (and probably larger worlds).
        private void SetArea(Vector3I start, Vector3I end, BlockVoxel voxel)
        {
            GetAreaFromPoints(start, end, out Vector3I areaStart,
                out Vector3I areaScale);
            Vector3I areaEndPosition = areaStart + areaScale;

            for (int x = areaStart.X; x < areaEndPosition.X; x++)
            {
                for (int y = areaStart.Y; y < areaEndPosition.Y; y++)
                {
                    for (int z = areaStart.Z; z < areaEndPosition.Z; z++)
                    {
                        if (rootChunk.TraverseToChunk(new Vector3I(x, y, z),
                            true, out var targetChunk))
                        {
                            if (targetChunk.Availability !=
                                ChunkAvailability.None)
                            {
                                targetChunk.Unlock();
                                targetChunk.SetVoxel(new Vector3I(x, y, z) -
                                    targetChunk.Offset, voxel);
                            }
                        }
                    }
                }
            }
        }
        */
    }
}
