using ShamanTK.Common;
using ShamanTK.Graphics;
using System;
using System.Numerics;

namespace TrippingCubes.Common
{
    public class Model
    {
        public MeshBuffer Mesh { get; set; }

        public TextureBuffer BaseColorMap { get; set; }

        public TextureBuffer SpecularMap { get; set; }

        public TextureBuffer NormalMap { get; set; }

        public TextureBuffer EmissiveMap { get; set; }

        public TextureBuffer MetallicMap { get; set; }

        public TextureBuffer OcclusionMap { get; set; }

        public DeformerAnimation Animation { get; set; }

        public Matrix4x4 Transformation { get; set; }
            = Matrix4x4.Identity;

        private string lastPrimaryAnimation = null;
        private string lastSecondaryAnimation = null;

        public Model() { }

        public virtual void Update(TimeSpan delta)
        {
            Animation?.Update(delta);
        }

        public virtual void Draw(IRenderContext context)
        {
            context.Color = Color.Transparent;
            context.ColorBlending = BlendingMode.Add;
            context.TextureBlending = BlendingMode.Add;
            context.Texture = BaseColorMap;
            context.Mesh = Mesh;
            context.Transformation = Transformation;
            context.Deformation = Animation?.GetCurrentDeformer();
            context.Draw();
        }

        public void SetAnimationsFade(float fade)
        {
            if (Animation != null)
            {
                Animation.OverlayInfluence = fade;
            }
        }

        public void SetAnimations(string primaryAnimation,
            string secondaryAnimation, bool loop = true)
        {
            if (Animation != null)
            {
                if (lastPrimaryAnimation != primaryAnimation)
                {
                    if (primaryAnimation != null)
                    {
                        if (Animation.Animation.SetPlaybackRange(
                            primaryAnimation, true))
                        {
                            Animation.Animation.LoopPlayback = loop;
                            Animation.Animation.Play();
                        }
                        else Animation.Animation.Stop();
                    }
                    else Animation.Animation.Stop();

                    lastPrimaryAnimation = primaryAnimation;
                }

                if (lastSecondaryAnimation != secondaryAnimation)
                {
                    if (secondaryAnimation != null)
                    {
                        if (Animation.OverlayAnimation.SetPlaybackRange(
                            secondaryAnimation, true))
                        {
                            Animation.OverlayAnimation.LoopPlayback = loop;
                            Animation.OverlayAnimation.Play();
                        }
                        else Animation.OverlayAnimation.Stop();
                    }
                    else Animation.OverlayAnimation.Stop();

                    lastSecondaryAnimation = secondaryAnimation;
                }
            }
        }
    }
}
