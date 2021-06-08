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

using ShamanTK.Common;
using System;
using System.Collections.Generic;

namespace TrippingCubes.Common
{
    public class AnimatedColor
    {
        private class Transition
        {
            public Color Source { get; set; }
            public Color Target { get; set; }
            public TimeSpan Duration { get; set; }
            public Action CompletionAction { get; set; }
        }

        private readonly Queue<Transition> transitions = 
            new Queue<Transition>();
        private Transition currentTransition;
        private TimeSpan currentTransitionProgress;

        public Color Color { get; private set; } = Color.Transparent;

        public int QueuedTransitions => transitions.Count;

        public bool IsBlocked { get; set; } = false;

        public void Update(TimeSpan delta)
        {
            if (currentTransition != null)
            {
                currentTransitionProgress += delta;
                float ratio = (float)(currentTransitionProgress /
                    currentTransition.Duration);
                Color = Color.Lerp(currentTransition.Source,
                    currentTransition.Target, ratio);

                if (ratio >= 1)
                {
                    currentTransition.CompletionAction?.Invoke();
                    currentTransition = null;
                    currentTransitionProgress = TimeSpan.Zero;
                }
            }
            
            if (currentTransition == null && transitions.Count > 0)
            {
                currentTransition = transitions.Dequeue();
            }
        }

        public AnimatedColor Clear(Color color)
        {
            if (!IsBlocked)
            {
                Color = color;
                return Clear();
            }
            else return this;
        }

        public AnimatedColor Clear()
        {
            if (!IsBlocked)
            {
                transitions.Clear();
                currentTransition = null;
                currentTransitionProgress = TimeSpan.Zero;
            }
            return this;
        }

        public AnimatedColor Flash(Color color, double durationSeconds = 1, 
            Action onCompleted = null)
        {
            if (!IsBlocked)
            {
                transitions.Enqueue(new Transition()
                {
                    CompletionAction = onCompleted,
                    Duration = TimeSpan.FromSeconds(durationSeconds),
                    Source = color,
                    Target = Color.Transparent
                });
            }

            return this;
        }

        public AnimatedColor Fade(Color color, double durationSeconds = 1,
            Action onCompleted = null)
        {
            if (!IsBlocked)
            {
                transitions.Enqueue(new Transition()
                {
                    CompletionAction = onCompleted,
                    Duration = TimeSpan.FromSeconds(durationSeconds),
                    Source = Color,
                    Target = color
                });
            }

            return this;
        }
    }
}
