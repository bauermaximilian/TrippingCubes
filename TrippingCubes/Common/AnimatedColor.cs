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
