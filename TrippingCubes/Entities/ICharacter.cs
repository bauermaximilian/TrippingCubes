using System;

namespace TrippingCubes.Entities
{
    delegate void ValueChangedEventHandler<T>(T previousValue, T currentValue);

    interface ICharacter : IEntity
    {
        int HealthPoints { get; set; }

        bool IsInvisible { get; }

        string Name { get; }

        string CurrentState { get; }

        event ValueChangedEventHandler<int> HealthPointsChanged;

        event ValueChangedEventHandler<string> StateChanged;
    }
}
