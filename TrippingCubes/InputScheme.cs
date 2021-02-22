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

using ShamanTK.Controls;
using TrippingCubes.Common;

namespace TrippingCubes
{
    class InputScheme : InputSchemeBase
    {
        public ControlMapping MoveForward { get; }

        public ControlMapping MoveRight { get; }

        public ControlMapping MoveBackward { get; }

        public ControlMapping MoveLeft { get; }

        public ControlMapping MoveUp { get; }

        public ControlMapping MoveDown { get; }

        public ControlMapping Jump { get; }

        public ControlMapping LookUp { get; }

        public ControlMapping LookRight { get; }

        public ControlMapping LookDown { get; }

        public ControlMapping LookLeft { get; }

        public ControlMapping Attack { get; }

        public ControlMapping Exit { get; }

        public ControlMapping FullscreenToggle { get; }

        public ControlMapping AddBlock { get; }

        public ControlMapping RemoveBlock { get; }

        public ControlMapping SwitchInputScheme { get; }

        public ControlMapping FilterToggle { get; }

        public InputScheme(ControlsManager controls)
        {
            MoveForward = controls.Map(KeyboardKey.W);
            MoveRight = controls.Map(KeyboardKey.D);
            MoveBackward = controls.Map(KeyboardKey.S);
            MoveLeft = controls.Map(KeyboardKey.A);
            MoveUp = controls.MapCustom(c => c.IsPressed(KeyboardKey.E)
                || c.IsPressed(KeyboardKey.Space));
            MoveDown = controls.MapCustom(c => c.IsPressed(KeyboardKey.Q)
                || c.IsPressed(KeyboardKey.Shift));
            Jump = controls.Map(KeyboardKey.Space);
            LookUp = controls.Map(MouseSpeedAxis.Up);
            LookRight = controls.Map(MouseSpeedAxis.Right);
            LookDown = controls.Map(MouseSpeedAxis.Down);
            LookLeft = controls.Map(MouseSpeedAxis.Left);
            Attack = controls.Map(MouseButton.Left);
            Exit = controls.Map(KeyboardKey.Escape);
            FullscreenToggle = controls.Map(KeyboardKey.F11);
            AddBlock = controls.Map(MouseButton.Left);
            RemoveBlock = controls.Map(MouseButton.Right);
            SwitchInputScheme = controls.Map(KeyboardKey.Tab);
            FilterToggle = controls.Map(KeyboardKey.V);
        }
    }
}
