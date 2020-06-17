/*
 * GameCraft
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

using System;
using System.Collections.Generic;

namespace GameCraft.Common
{
    public class AdjacentList<T>
    {
        public T West { get; set; }

        public T East { get; set; }

        public T Above { get; set; }

        public T Below { get; set; }

        public T North { get; set; }

        public T South { get; set; }

        public T Base { get; set; }

        public T this[Direction direction]
        {
            get
            {
                switch (direction)
                {
                    case Direction.West: return West;
                    case Direction.East: return East;
                    case Direction.Above: return Above;
                    case Direction.Below: return Below;
                    case Direction.North: return North;
                    case Direction.South: return South;
                    default: throw new ArgumentException("The specified " +
                        "direction is invalid.");
                }
            }
            set
            {
                switch (direction)
                {
                    case Direction.West: West = value; break;
                    case Direction.East: East = value; break;
                    case Direction.Above: Above = value; break;
                    case Direction.Below: Below = value; break; 
                    case Direction.North: North = value; break;
                    case Direction.South: South = value; break;
                    default:
                        throw new ArgumentException("The specified " +
                            "direction is invalid.");
                }
            }
        }

        public AdjacentList() { }

        public void ForEachSlot(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            action(Base);
            action(East);
            action(West);
            action(Above);
            action(Below);
            action(North);
            action(South);
        }

        public void Clear()
        {
            Base = East = West = Above = Below = North = South = default;
        }

        public void SetAll(T value)
        {
            Base = East = West = Above = Below = North = South = value;
        }

        public void SetAllUndefined(T value)
        {
            if (EqualityComparer<T>.Default.Equals(Base, default))
                Base = value;

            if (EqualityComparer<T>.Default.Equals(West, default))
                West = value;
            if (EqualityComparer<T>.Default.Equals(East, default))
                East = value;
            if (EqualityComparer<T>.Default.Equals(Above, default))
                Above = value;
            if (EqualityComparer<T>.Default.Equals(Below, default))
                Below = value;
            if (EqualityComparer<T>.Default.Equals(North, default))
                North = value;
            if (EqualityComparer<T>.Default.Equals(South, default))
                South = value;
        }

        public bool ContainsUndefinedElements()
        {
            return EqualityComparer<T>.Default.Equals(Base, default) ||
                EqualityComparer<T>.Default.Equals(West, default) ||
                EqualityComparer<T>.Default.Equals(East, default) ||
                EqualityComparer<T>.Default.Equals(Above, default) ||
                EqualityComparer<T>.Default.Equals(Below, default) ||
                EqualityComparer<T>.Default.Equals(North, default) ||
                EqualityComparer<T>.Default.Equals(South, default);
        }

        public bool ContainsOnlyUndefinedElements()
        {
            return EqualityComparer<T>.Default.Equals(Base, default) &&
                EqualityComparer<T>.Default.Equals(West, default) &&
                EqualityComparer<T>.Default.Equals(East, default) &&
                EqualityComparer<T>.Default.Equals(Above, default) &&
                EqualityComparer<T>.Default.Equals(Below, default) &&
                EqualityComparer<T>.Default.Equals(North, default) &&
                EqualityComparer<T>.Default.Equals(South, default);
        }

        public AdjacentList<TargetT> Convert<TargetT>(
            Func<T, TargetT> converterFunction)
        {
            if (converterFunction == null)
                throw new ArgumentNullException(nameof(converterFunction));

            var convertedCollection = new AdjacentList<TargetT>();
            Convert(ref convertedCollection, converterFunction);

            return convertedCollection;
        }

        public void Convert<TargetT>(ref AdjacentList<TargetT> 
            targetCollection, Func<T, TargetT> converterFunction)
        {
            if (targetCollection == null)
                throw new ArgumentNullException(nameof(targetCollection));
            if (converterFunction == null)
                throw new ArgumentNullException(nameof(converterFunction));

            targetCollection.Base = converterFunction(Base);
            targetCollection.East = converterFunction(East);
            targetCollection.West = converterFunction(West);
            targetCollection.Above = converterFunction(Above);
            targetCollection.Below = converterFunction(Below);
            targetCollection.North = converterFunction(North);
            targetCollection.South = converterFunction(South);
        }
    }
}
