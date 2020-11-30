using System;
using System.Numerics;

namespace TrippingCubes.Physics
{
    /// <summary>
    /// Provides helper methods for various physical operations.
    /// </summary>
    public static class PhysicsHelper
    {
        /// <summary>
        /// Calculates an accerlation.
        /// </summary>
        /// <param name="accerlation">
        /// The accerlation of the body.
        /// </param>
        /// <param name="gravity">
        /// The gravity affecting the body.
        /// </param>
        /// <returns>The combined accerlation.</returns>
        public static float CalculateAccerlation(float accerlation,
            float gravity)
        {
            return accerlation + gravity;
        }

        /// <summary>
        /// Calculates an accerlation.
        /// </summary>
        /// <param name="force">
        /// The force on the body.
        /// </param>
        /// <param name="mass">
        /// The mass of the body. Must not be less then 
        /// <see cref="float.Epsilon"/>.
        /// </param>
        /// <param name="gravity">
        /// The gravity affecting the body.
        /// </param>
        /// <returns>The combined accerlation.</returns>
        public static float CalculateAccerlation(float force, float mass,
            float gravity)
        {
            return force * (1f / mass) + gravity;
        }

        /// <summary>
        /// Calculates an accerlation.
        /// </summary>
        /// <param name="accerlation">
        /// The accerlation of the body.
        /// </param>
        /// <param name="gravity">
        /// The gravity affecting the body.
        /// </param>
        /// <returns>The combined accerlation.</returns>
        public static Vector2 CalculateAccerlation(Vector2 accerlation,
            Vector2 gravity)
        {
            return accerlation + gravity;
        }

        /// <summary>
        /// Calculates an accerlation.
        /// </summary>
        /// <param name="force">
        /// The force on the body.
        /// </param>
        /// <param name="mass">
        /// The mass of the body. Must not be less then 
        /// <see cref="float.Epsilon"/>.
        /// </param>
        /// <param name="gravity">
        /// The gravity affecting the body.
        /// </param>
        /// <returns>The combined accerlation.</returns>
        public static Vector2 CalculateAccerlation(Vector2 force, float mass,
            Vector2 gravity)
        {
            return force * (1f / mass) + gravity;
        }

        /// <summary>
        /// Calculates an accerlation.
        /// </summary>
        /// <param name="accerlation">
        /// The accerlation of the body.
        /// </param>
        /// <param name="gravity">
        /// The gravity affecting the body.
        /// </param>
        /// <returns>The combined accerlation.</returns>
        public static Vector3 CalculateAccerlation(Vector3 accerlation,
            Vector3 gravity)
        {
            return accerlation + gravity;
        }

        /// <summary>
        /// Calculates an accerlation.
        /// </summary>
        /// <param name="force">
        /// The force on the body.
        /// </param>
        /// <param name="mass">
        /// The mass of the body. Must not be less then 
        /// <see cref="float.Epsilon"/>.
        /// </param>
        /// <param name="gravity">
        /// The gravity affecting the body.
        /// </param>
        /// <returns>The combined accerlation.</returns>
        public static Vector3 CalculateAccerlation(Vector3 force, float mass,
            Vector3 gravity)
        {
            return force * (1f / mass) + gravity;
        }

        /// <summary>
        /// Calculates the length a specific velocity needs to have in order
        /// to travel a certain <paramref name="peakDistance"/> (e.g. jump 
        /// height) while being influenced by an opposing accerlation 
        /// (e.g. gravity) with a specific 
        /// <paramref name="opposingAccerlationLength"/>.
        /// </summary>
        /// <param name="peakDistance">
        /// The highest distance a body should travel while being under the
        /// influence by an opposing accerlation.
        /// </param>
        /// <param name="opposingAccerlationLength">
        /// The length of the opposing accerlation influencing the body.
        /// </param>
        /// <returns>
        /// The length a velocity needs to have.
        /// </returns>
        public static float CalculateRequiredVelocityLength(float peakDistance,
            float opposingAccerlationLength)
        {
            return (float)Math.Sqrt(peakDistance *
                opposingAccerlationLength * 2);
        }

        /// <summary>
        /// Applies an accerlation to a velocity.
        /// </summary>
        /// <param name="velocity">
        /// The current velocity, which will be updated.
        /// </param>
        /// <param name="accerlation">
        /// The accerlation, which is used to update the specified
        /// <paramref name="velocity"/>.
        /// </param>
        /// <param name="delta">
        /// The time, for which the <paramref name="velocity"/> should 
        /// be updated.
        /// </param>
        public static void ApplyAccerlationToVelocity(ref float velocity,
            float accerlation, TimeSpan delta)
        {
            velocity += accerlation * (float)delta.TotalSeconds;
        }

        /// <summary>
        /// Applies an accerlation to a velocity.
        /// </summary>
        /// <param name="velocity">
        /// The current velocity, which will be updated.
        /// </param>
        /// <param name="accerlation">
        /// The accerlation, which is used to update the specified
        /// <paramref name="velocity"/>.
        /// </param>
        /// <param name="delta">
        /// The time, for which the <paramref name="velocity"/> should 
        /// be updated.
        /// </param>
        public static void ApplyAccerlationToVelocity(ref Vector2 velocity,
            Vector2 accerlation, TimeSpan delta)
        {
            velocity += accerlation * (float)delta.TotalSeconds;
        }

        /// <summary>
        /// Applies an accerlation to a velocity.
        /// </summary>
        /// <param name="velocity">
        /// The current velocity, which will be updated.
        /// </param>
        /// <param name="accerlation">
        /// The accerlation, which is used to update the specified
        /// <paramref name="velocity"/>.
        /// </param>
        /// <param name="delta">
        /// The time, for which the <paramref name="velocity"/> should 
        /// be updated.
        /// </param>
        public static void ApplyAccerlationToVelocity(ref Vector3 velocity,
            Vector3 accerlation, TimeSpan delta)
        {
            velocity += accerlation * (float)delta.TotalSeconds;
        }

        /// <summary>
        /// Applies a drag to a velocity.
        /// </summary>
        /// <param name="velocity">
        /// The current velocity, which will be updated.
        /// </param>
        /// <param name="drag">
        /// The drag, which will be applied to the specified
        /// <paramref name="velocity"/>.
        /// </param>
        /// <param name="delta">
        /// The time, for which the <paramref name="velocity"/> should 
        /// be updated.
        /// </param>
        /// <remarks>
        /// This method should only be used once per velocity and update.
        /// </remarks>
        public static void ApplyDragToVelocity(ref float velocity,
            float drag, TimeSpan delta)
        {
            velocity *= (1 - (float)delta.TotalSeconds * drag);
        }

        /// <summary>
        /// Applies a drag to a velocity.
        /// </summary>
        /// <param name="velocity">
        /// The current velocity, which will be updated.
        /// </param>
        /// <param name="drag">
        /// The drag, which will be applied to the specified
        /// <paramref name="velocity"/>.
        /// </param>
        /// <param name="delta">
        /// The time, for which the <paramref name="velocity"/> should 
        /// be updated.
        /// </param>
        /// <remarks>
        /// This method should only be used once per velocity and update.
        /// </remarks>
        public static void ApplyDragToVelocity(ref Vector2 velocity,
            float drag, TimeSpan delta)
        {
            velocity *= (1 - (float)delta.TotalSeconds * drag);
        }

        /// <summary>
        /// Applies a drag to a velocity.
        /// </summary>
        /// <param name="velocity">
        /// The current velocity, which will be updated.
        /// </param>
        /// <param name="drag">
        /// The drag, which will be applied to the specified
        /// <paramref name="velocity"/>.
        /// </param>
        /// <param name="delta">
        /// The time, for which the <paramref name="velocity"/> should 
        /// be updated.
        /// </param>
        /// <remarks>
        /// This method should only be used once per velocity and update.
        /// </remarks>
        public static void ApplyDragToVelocity(ref Vector3 velocity,
            float drag, TimeSpan delta)
        {
            velocity *= (1 - (float)delta.TotalSeconds * drag);
        }
    }
}
