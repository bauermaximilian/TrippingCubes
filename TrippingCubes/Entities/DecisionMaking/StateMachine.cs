using System;

namespace TrippingCubes.Entities.DecisionMaking
{
    abstract class StateMachine
    {
        private Action currentStateBehavior;
        private Action beforeTransitionToNextState;

        protected StateMachine()
        {
            TransitionTo(null);
        }

        public void TransitionTo(Action nextStateBehavior, 
            Action beforeTransitionToNextState = null)
        {
            currentStateBehavior = nextStateBehavior ?? PerformInitialBehavior;
            this.beforeTransitionToNextState = beforeTransitionToNextState;
        }

        public virtual void Update()
        {
            beforeTransitionToNextState?.Invoke();
            beforeTransitionToNextState = null;

            currentStateBehavior.Invoke();
        }

        protected abstract void PerformInitialBehavior();
    }
}
