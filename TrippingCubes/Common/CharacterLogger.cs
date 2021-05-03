using System;
using System.Collections.Generic;
using TrippingAnalytics.Core.Common;
using TrippingCubes.Entities;

namespace TrippingCubes.Common
{
    class CharacterLogger
    {
        public ICharacter Character { get; }

        public CharacterProtocol Protocol { get; }

        public CharacterLogger(ICharacter character, 
            CharacterProtocol protocol)
        {
            Character = character;
            Protocol = protocol;

            Character.HealthPointsChanged += Character_HealthPointsChanged;
            Character.StateChanged += Character_StateChanged;

            Character_HealthPointsChanged(Character.HealthPoints,
                Character.HealthPoints);
            Character_StateChanged(Character.CurrentState,
                Character.CurrentState);
        }

        private void Character_StateChanged(string previousValue, 
            string currentValue)
        {
            Protocol.Status.Add(new StateProtocolItem
            {
                Time = DateTime.Now,
                State = currentValue
            });
        }

        private void Character_HealthPointsChanged(int previousValue, 
            int currentValue)
        {
            Protocol.Health.Add(new HealthProtocolItem
            {
                Time = DateTime.Now,
                HealthPoints = currentValue
            });
        }

        public static CharacterLogger Create(ICharacter character)
        {
            CharacterProtocol characterProtocol = new CharacterProtocol
            {
                CharacterName = character.Name,
                TypeName = character.GetType().Name,
                Health = new List<HealthProtocolItem>(),
                Status = new List<StateProtocolItem>()
            };
            return new CharacterLogger(character, characterProtocol);
        }
    }
}
