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

using System;

namespace TrippingCubes.Entities.DecisionMaking
{
    abstract class StateMachine
    {        
        private Action beforeTransitionToNextState;
        private TimeSpan timeSinceLastBehaviorExecution = TimeSpan.MaxValue;

        protected Action CurrentStateBehavior { get; private set; }

        protected TimeSpan BehaviorExecutionFrequency { get; set; }
            = TimeSpan.FromSeconds(0.1);

        protected StateMachine()
        {
            TransitionTo(null);
        }

        public void TransitionTo(Action nextStateBehavior, 
            Action beforeTransitionToNextState = null)
        {
            CurrentStateBehavior = nextStateBehavior ?? PerformInitialBehavior;
            this.beforeTransitionToNextState = beforeTransitionToNextState;
        }

        public virtual void Update(TimeSpan delta)
        {
            if (timeSinceLastBehaviorExecution >=
                BehaviorExecutionFrequency)
            {
                beforeTransitionToNextState?.Invoke();
                beforeTransitionToNextState = null;

                CurrentStateBehavior.Invoke();

                timeSinceLastBehaviorExecution = TimeSpan.Zero;
            }
            else timeSinceLastBehaviorExecution += delta;
        }

        protected abstract void PerformInitialBehavior();
    }
}
