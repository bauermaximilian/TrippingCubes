using ShamanTK.Graphics;
using System;
using System.Collections.Generic;
using TrippingCubes.Physics;

namespace TrippingCubes.Entities
{
    delegate IEntity EntityFactory(GameWorld gameWorld);

    interface IEntity
    {
        RigidBody Body { get; }

        GameWorld World { get; }

        void ApplyParameters(
            IEnumerable<KeyValuePair<string, string>> parameters);

        void Update(TimeSpan delta);

        void Redraw(IRenderContext context);
    }
}
