using System;

namespace TrippingCubes.Entities.DecisionMaking
{
    abstract class StateMachine
    {
        private Action currentStateBehavior;
        private Action beforeTransitionToNextState;
        private DateTime lastBehaviorExecution;

        protected TimeSpan BehaviorExecutionFrequency { get; set; }
            = TimeSpan.FromSeconds(0.5);

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
            if ((DateTime.Now - lastBehaviorExecution) >=
                BehaviorExecutionFrequency)
            {
                beforeTransitionToNextState?.Invoke();
                beforeTransitionToNextState = null;

                currentStateBehavior.Invoke();

                lastBehaviorExecution = DateTime.Now;
            }
        }

        protected abstract void PerformInitialBehavior();
    }
}
