using ShamanTK.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using TrippingCubes.Common;
using TrippingCubes.Physics;

namespace TrippingCubes.Entities
{
    class AdvancedCharacter : ICharacter
    {
        public const float JumpVelocity = 7;

        public int HealthPoints { get; set; }

        public bool IsInvisible { get; private set; }

        public RigidBody Body { get; }

        public GameWorld World { get; }

        public AdvancedCharacter(GameWorld gameWorld)
        {
            World = gameWorld;

            Body = gameWorld.Physics.AddNewBody(
                new BoundingBox(0, 0, 0, 0.6f, 1.6f, 0.6f));
            Body.EnableAutoJump = true;
            Body.AutoJump += (s, e) =>
                Body.ApplyVelocityChange(new Vector3(0, JumpVelocity, 0));
        }        

        public void ApplyParameters(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            throw new NotImplementedException();
        }

        public void Redraw(IRenderContext context)
        {
            throw new NotImplementedException();
        }

        public void Update(TimeSpan delta)
        {
            throw new NotImplementedException();
        }
    }
}
