using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace GameCraft.Physics
{
    class WorldTest
    {
        private readonly World world;
        private readonly RigidBody body;

        public WorldTest()
        {
            world = new World(IsSolid, IsLiquid);
            body = world.AddNewBody(new BoundingBox(new Vector3(2, 1, 2),
                new Vector3(0.5f)));
        }

        private bool IsSolid(Vector3 position)
        {
            if (position.Y < 1) return true;
            else if (position.X < 1) return true;
            else if (position.X >= 4) return true;
            else return false;
        }

        private bool IsLiquid(Vector3 position) => false;

        public void ResetPosition()
        {
            body.SetPosition(new Vector3(2, 1, 2));
        }

        public void TestMoveOnXAxis(bool positive)
        {
            for (int i = 0; i < 6; i++)//Fehler bei i==3&&positive
            {
                body.ApplyForce(new Vector3(10 * (positive ? 1 : -1), 0, 0));
                world.Update(TimeSpan.FromSeconds(1 / 5f));
            }
        }
    }
}
