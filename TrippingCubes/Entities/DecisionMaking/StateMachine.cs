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
