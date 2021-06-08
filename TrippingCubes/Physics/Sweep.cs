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
 * 
 * The following code is based on "voxel-aabb-sweep" 
 * by Andy Hall (https://github.com/andyhall), used under the
 * conditions of the MIT license. See ABOUT.txt for the complete license info.
 */

using System;
using System.Numerics;

namespace TrippingCubes.Physics
{
    delegate bool SweepCallback(float distance, int axisIndex,
        float dir, ref Vector3 leftToGo);

    class Sweep
    {
        private readonly Func<Vector3, bool> testBlock;

        private float cumulative_t;
        private float t;
        private float max_t;
        private int axis;
        private readonly float[] tr = new float[3];
        private readonly float[] ldi = new float[3];
        private readonly float[] tri = new float[3];
        private readonly float[] step = new float[3];
        private readonly float[] tDelta = new float[3];
        private readonly float[] tNext = new float[3];
        private readonly float[] normed = new float[3];
        private readonly float[] vec = new float[3];
        private readonly float[] bas = new float[3];
        private readonly float[] bbas = new float[3];
        private readonly float[] max = new float[3];
        private readonly float[] bmax = new float[3];
        private readonly float[] sdir = new float[3];
        private readonly float[] result = new float[3];
        private readonly float[] left = new float[3];

        public Sweep(Func<Vector3, bool> testBlock)
        {
            this.testBlock = testBlock ??
                throw new ArgumentNullException(nameof(testBlock));
        }

        // Return total distance moved 
        // (not necessarily magnitude of [end]-[start])
        public float Execute(ref BoundingBox boundingBox, 
            Vector3 direction, SweepCallback callback, bool noTranslate)
        {
            Vector3 maximum = boundingBox.Maximum();

            direction.CopyTo(sdir);
            direction.CopyTo(vec);
            maximum.CopyTo(max);
            maximum.CopyTo(bmax);
            boundingBox.Position.CopyTo(bas);
            boundingBox.Position.CopyTo(bbas);

            cumulative_t = 0;
            axis = 0;

            // init for the current sweep vector and take first step
            Initialize();
            if (max_t == 0) return 0;
            axis = StepForward();

            float dist = 0;

            bool done = false;

            // loop along raycast vector
            while (t <= max_t)
            {
                // sweeps over leading face of AABB
                if (CheckCollision(axis))
                {
                    // calls the callback and decides whether to continue
                    done = HandleCollision(callback);
                    if (done)
                    {
                        dist = cumulative_t;
                        break;
                    }
                }

                axis = StepForward();
            }

            if (!done)
            {
                cumulative_t += max_t;
                for (int i = 0; i < 3; i++)
                {
                    bas[i] += vec[i];
                    max[i] += vec[i];
                }

                dist = cumulative_t;
            }            

            if (!noTranslate)
            {
                for (int i = 0; i < 3; i++)
                {
                    result[i] = (sdir[i] > 0) ? 
                        (max[i] - bmax[i]) : (bas[i] - bbas[i]);
                }

                boundingBox = boundingBox.Translated(new Vector3(
                    result[0], result[1], result[2]));
            }

            return dist;
        }

        private void Initialize()
        {
            t = 0f;
            max_t = (float)Math.Sqrt(vec[0] * vec[0] + vec[1] * vec[1] +
                vec[2] * vec[2]);
            if (max_t == 0f) return;

            for (int i = 0; i < 3; i++)
            {
                bool dir = vec[i] >= 0;
                step[i] = dir ? 1 : -1;
                // trailing / trailing edge coords
                float lead = dir ? max[i] : bas[i];
                tr[i] = dir ? bas[i] : max[i];
                // int values of lead/trail edges
                ldi[i] = LeadEdgeToInt(lead, step[i]);
                tri[i] = TrailEdgeToInt(tr[i], step[i]);
                // normed vector
                normed[i] = vec[i] / max_t;
                // distance along t required to move one voxel in each axis
                tDelta[i] = Math.Abs(1 / normed[i]);
                // location of nearest voxel boundary, in units of t 
                float dist = dir ? (ldi[i] + 1 - lead) : (lead - ldi[i]);
                tNext[i] = (tDelta[i] < float.PositiveInfinity) ?
                    tDelta[i] * dist : float.PositiveInfinity;
            }
        }

        private bool CheckCollision(int i_axis)
        {
            float stepx = step[0];
            float x0 = (i_axis == 0) ? ldi[0] : tri[0];
            float x1 = ldi[0] + stepx;

            float stepy = step[1];
            float y0 = (i_axis == 1) ? ldi[1] : tri[1];
            float y1 = ldi[1] + stepy;

            float stepz = step[2];
            float z0 = (i_axis == 2) ? ldi[2] : tri[2];
            float z1 = ldi[2] + stepz;

            for (float x = x0; x != x1; x += stepx)
            {
                for (float y = y0; y != y1; y += stepy)
                {
                    for (float z = z0; z != z1; z += stepz)
                    {
                        if (testBlock(new Vector3(x, y, z))) return true;
                    }
                }
            }
            return false;
        }

        private bool HandleCollision(SweepCallback callback)
        {
            // set up for callback
            cumulative_t += t;
            float dir = step[axis];

            // vector moved so far, and left to move
            float done = t / max_t;
            for (int i = 0; i < 3; i++)
            {
                float dv = vec[i] * done;
                bas[i] += dv;
                max[i] += dv;
                left[i] = vec[i] - dv;
            }

            // set leading edge of stepped axis exactly to voxel boundary
            // else we'll sometimes rounding error beyond it
            if (dir > 0)
            {
                max[axis] = (float)Math.Round(max[axis]);
            }
            else
            {
                bas[axis] = (float)Math.Round(bas[axis]);
            }

            // call back to let client update the "left to go" vector
            Vector3 leftVec = new Vector3(left[0], left[1], left[2]);
            bool res = callback(cumulative_t, axis, dir, ref leftVec);
            leftVec.CopyTo(left);

            // bail out out on truthy response
            if (res) return true;

            // init for new sweep along vec
            for (int i = 0; i < 3; i++) vec[i] = left[i];
            Initialize();
            if (max_t == 0) return true; // no vector left

            return false;
        }
        // advance to next voxel boundary, and return which axis was stepped
        private int StepForward()
        {
            int axis = (tNext[0] < tNext[1]) ?
                ((tNext[0] < tNext[2]) ? 0 : 2) :
                ((tNext[1] < tNext[2]) ? 1 : 2);
            float dt = tNext[axis] - t;
            t = tNext[axis];
            ldi[axis] += step[axis];
            tNext[axis] += tDelta[axis];
            for (int i = 0; i < 3; i++)
            {
                tr[i] += dt * normed[i];
                tri[i] = TrailEdgeToInt(tr[i], step[i]);
            }
            return axis;
        }

        private float LeadEdgeToInt(float coord, float step)
        {
            return (float)Math.Floor(coord - step * PhysicsSystem.Epsilon);
        }

        private float TrailEdgeToInt(float coord, float step)
        {
            return (float)Math.Floor(coord + step * PhysicsSystem.Epsilon);
        }
    }
}
