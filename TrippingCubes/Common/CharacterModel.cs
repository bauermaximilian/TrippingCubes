using ShamanTK.Common;
using ShamanTK.Graphics;
using ShamanTK.IO;
using System;
using System.Numerics;
using TrippingCubes.Physics;

namespace TrippingCubes.Common
{
    class CharacterModel
    {
        public Vector3 Scale { get; set; } = Vector3.One;

        public Model Character { get; private set; }

        public Model Weapon { get; private set; }

        public CharacterModel()
        {
            Character = new Model();
            Weapon = new Model();
        }

        public CharacterModel CreateClone()
        {
            return new CharacterModel()
            {
                Scale = Scale,
                Character = Character.CreateClone(),
                Weapon = Weapon.CreateClone()
            };
        }

        public void Draw(IRenderContext renderContext)
        {
            Character.Draw(renderContext);
            Weapon.Draw(renderContext);
        }

        public void SetTransformation(RigidBody body)
        {
            var transformation = MathHelper.CreateTransformation(body.Position,
                Scale, Quaternion.CreateFromYawPitchRoll(
                    body.Orientation, 0, 0));

            Character.Transformation = transformation;
            Weapon.Transformation = transformation;
        }

        public void SetCurrentAnimationFade(float fade)
        {
            if (Character.Animation != null)
            {
                Character.Animation.OverlayInfluence = fade;
            }

            if (Weapon.Animation != null)
            {
                Weapon.Animation.OverlayInfluence = fade;
            }
        }

        public void SetCurrentAnimations(string primaryAnimation,
            string secondaryAnimation, bool loop = true)
        {
            Character.SetAnimations(primaryAnimation, secondaryAnimation, 
                loop);
            Weapon.SetAnimations(primaryAnimation, secondaryAnimation,
                loop);
        }

        public virtual void Update(TimeSpan delta)
        {
            Character.Update(delta);
            Weapon.Update(delta);
        }

        public void ImportSceneNodeData(
            ResourceManager resourceManager,
            Node<ParameterCollection> node,
            string characterModelName, string weaponModelName)
        {
            ImportSceneNodeDataRecursively(resourceManager, node, 
                ref characterModelName, ref weaponModelName);
        }

        private void ImportSceneNodeDataRecursively(
            ResourceManager resourceManager,
            Node<ParameterCollection> node,
            ref string characterModelName, ref string weaponModelName)
        {
            if (characterModelName != null &&
                node.Value.Name.ToLowerInvariant() ==
                characterModelName.ToLowerInvariant())
            {
                if (Character.TryImportSceneNodeData(resourceManager, node))
                    characterModelName = null;
            }

            if (weaponModelName != null &&
                node.Value.Name.ToLowerInvariant() ==
                weaponModelName.ToLowerInvariant())
            {
                if (Weapon.TryImportSceneNodeData(resourceManager, node))
                    weaponModelName = null;
            }

            if (characterModelName != null || weaponModelName != null)
            {
                foreach (Node<ParameterCollection> childNode in node.Children)
                {
                    ImportSceneNodeDataRecursively(resourceManager, childNode,
                        ref characterModelName, ref weaponModelName);
                }
            }
        }
    }
}
