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

using ShamanTK.Common;
using System;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class ErraticOutburstBehavior<ParamT> : WanderBehavior<ParamT>
    {
        private const int ProtocolSize = 20;

        private readonly TimeSpan slotUpdateFrequency = new TimeSpan(0, 0, 1);
        private readonly Vector3[] protocol = new Vector3[ProtocolSize];

        private int slot = 0;
        private TimeSpan slotAge = TimeSpan.Zero;

        private DateTime lastUpdate = DateTime.MinValue;
        private TimeSpan stuckTime = TimeSpan.Zero;
        private TimeSpan activeTime = TimeSpan.Zero;

        private bool isActive = false;

        public TimeSpan BehaviorDuration { get; set; }
            = TimeSpan.FromSeconds(4);

        public TimeSpan StuckTimeTreshold { get; set; }
            = TimeSpan.FromSeconds(4);

        public TimeSpan ConsiderPastTimeDuration { get; set; }
            = TimeSpan.FromSeconds(4);

        public float TravelledDistanceTreshold { get; set; } = 0.69f;

        public ErraticOutburstBehavior(IEntity self) : base(self)
        {
            WanderOffset = 4.2f;
            WanderRadius = 1f;
            WanderRate = Angle.Deg(6);
            MaximumAccelerationLinear = DefaultMaximumAccelerationLinear;

            for (int i = 0; i < protocol.Length; i++)
                protocol[i] = Self.Body.Position;
        }

        public override void Update()
        {
            TimeSpan delta = DateTime.Now - lastUpdate;
            lastUpdate = DateTime.Now;

            slotAge += delta;            

            if (slotAge > slotUpdateFrequency)
            {
                slotAge = TimeSpan.Zero;
                slot = (slot + 1) % protocol.Length;
                protocol[slot] = Self.Body.Position;
            }

            isActive = false;

            float travelledDistance =
                GetAverageTravelledDistance(ConsiderPastTimeDuration);

            if (travelledDistance < TravelledDistanceTreshold)
            {
                if (stuckTime > StuckTimeTreshold)
                {
                    if (activeTime < BehaviorDuration)
                    {
                        isActive = true;
                        activeTime += delta;
                    }
                    else
                    {
                        stuckTime = activeTime = TimeSpan.Zero;
                    }
                }
                else stuckTime += delta;
            }
            else stuckTime = TimeSpan.Zero;

            base.Update();
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            if (isActive) return base.CalculateAccelerationLinear();
            else return Vector3.Zero;
        }

        protected override Angle CalculateAccelerationAngular()
        {
            if (isActive) return base.CalculateAccelerationAngular();
            else return 0;
        }

        private Vector3 GetAverageTravelledDirection(TimeSpan duration)
        {
            int slotsCount = Math.Min((int)Math.Floor(duration.TotalSeconds /
                slotUpdateFrequency.TotalSeconds), protocol.Length - 1);

            Vector3 distancesSum = Vector3.Zero;
            for (int i=0; i < slotsCount; i++)
            {
                Vector3 previousPosition = protocol[MathHelper.BringToRange(
                    slot - i - 1, protocol.Length)];
                Vector3 currentPosition = protocol[MathHelper.BringToRange(
                    slot - i, protocol.Length)];

                distancesSum += currentPosition - previousPosition;
            }
            return distancesSum / slotsCount;
        }

        private float GetAverageTravelledDistance(TimeSpan duration)
        {
            float distance = GetAverageTravelledDirection(duration).Length();
            return distance;
        }
    }
}
