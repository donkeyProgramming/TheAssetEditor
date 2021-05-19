using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace View3D.Animation
{
    public class ExternalAnimationAttachmentResolver
    {
        public AnimationPlayer ExternalPlayer { get; set; }
        public int ExternalBoneIndex { get; set; } = -1;
        public bool HasAnimation { get { return ExternalPlayer != null && ExternalBoneIndex != -1; } }
        public Matrix Transform { get; set; } = Matrix.Identity;

        public void UpdateNode(GameTime time)
        {
            if (!HasAnimation || ExternalPlayer == null)
            {
                Transform = Matrix.Identity;
            }
            else
            {
                if (time != null)
                    ExternalPlayer.Update(time);
                Transform = ExternalPlayer._skeleton.GetAnimatedWorldTranform(ExternalBoneIndex);
            }
        }
    }

    public class AnimationFrame
    {
        public class BoneKeyFrame
        {
            public int BoneIndex { get; set; }
            public int ParentBoneIndex { get; set; }
            public Quaternion Rotation { get; set; }
            public Vector3 Translation { get; set; }
            public Matrix WorldTransform { get; set; }
        }

        public List<BoneKeyFrame> BoneTransforms = new List<BoneKeyFrame>();

        public Matrix GetSkeletonAnimatedWorld(GameSkeleton gameSkeleton, int boneIndex)
        {
            Matrix output = gameSkeleton.GetWorldTransform(boneIndex) * BoneTransforms[boneIndex].WorldTransform;
            return output;
        }

        public Matrix GetSkeletonAnimatedWorldDiff(GameSkeleton gameSkeleton, int boneIndex0, int boneIndex1)
        {
            var bone0Transform = GetSkeletonAnimatedWorld(gameSkeleton, boneIndex0);
            var bone1Transform = GetSkeletonAnimatedWorld(gameSkeleton, boneIndex1);

            return bone1Transform * Matrix.Invert(bone0Transform);
        }
    }

    public class AnimationPlayerSettings
    {
        public bool UseTranslationOffset { get; set; } = false;
        public float TranslationOffsetX { get; set; } = 0;
        public float TranslationOffsetY { get; set; } = 0;
        public float TranslationOffsetZ { get; set; } = 0;

        public bool UseRotationOffset { get; set; } = false;
        public float RotationOffsetX { get; set; } = 0;
        public float RotationOffsetY { get; set; } = 0;
        public float RotationOffsetZ { get; set; } = 0;

        public bool FreezeAnimationRoot { get; set; } = false;
        public bool FreezeAnimationBone { get; set; } = false;
        public int FreezeAnimationBoneIndex { get; set; } = -1;

        public bool UseAnimationSnap { get; set; } = false;
        public bool OnlySnapTranslations { get; set; } = false;
    }

    public delegate void FrameChanged(int currentFrame);
    public class AnimationPlayer
    {
        public event FrameChanged OnFrameChanged;
        public AnimationPlayerSettings Settings { get; set; } = new AnimationPlayerSettings();
        public ExternalAnimationAttachmentResolver ExternalAnimationRef { get; set; } = new ExternalAnimationAttachmentResolver();

        public GameSkeleton _skeleton;
        TimeSpan _timeSinceStart;
        AnimationFrame _currentAnimFrame;
        List<AnimationClip> _animationClips;

        public bool IsPlaying { get; private set; } = true;
        public double SpeedMultiplication { get; set; }
        public bool ApplyStaticFrame { get; set; } = true;
        public bool ApplyDynamicFrames { get; set; } = true;
        public bool IsEnabled { get; set; } = false;
        public bool LoopAnimation { get; set; } = true;

        public int CurrentFrame
        {
            get { return (int)Math.Round(_timeSinceStart.TotalSeconds * 20); }
            set
            {
                if (CurrentFrame == value)
                    return;

                if (_animationClips != null)
                {
                    var frameCount = FrameCount();
                    if (frameCount > 0)
                    {
                        int newFrame = value;
                        _timeSinceStart = TimeSpan.FromMilliseconds(newFrame * (1f / 20f) * 1000);
                    }
                    OnFrameChanged?.Invoke(CurrentFrame);
                }
                else
                {
                    OnFrameChanged?.Invoke(0);
                }

                UpdateAnimationFrame();
            }
        }

        public void SetAnimation(AnimationClip animation, GameSkeleton skeleton)
        {
            if (animation == null)
                SetAnimationArray(null, skeleton);
            else
                SetAnimationArray(new List<AnimationClip>() { animation }, skeleton);
        }

        public void SetAnimationArray(List<AnimationClip> animation, GameSkeleton skeleton)
        {
            if (animation != null)
            {
                // Make sure animation fits
                var numBones = skeleton.BoneCount;
                var maxAnimBones = Math.Max(animation.Select(x => x.RotationMappings.Count).Max(), animation.Select(x => x.TranslationMappings.Count).Max());
                if (maxAnimBones > numBones)
                    throw new Exception("This animation does not work for this skeleton!");
            }

            _skeleton = skeleton;
            _animationClips = animation;
            _timeSinceStart = TimeSpan.FromSeconds(0);
            UpdateAnimationFrame();
        }

        float GetAnimationLengthMs()
        {
            if (_animationClips != null && _animationClips.Any())
                return _animationClips[0].DynamicFrames.Count() * (1f / 20f) * 1000;
            return 0;
        }

        public void Update(GameTime gameTime)
        {
            float animationLengthMs = GetAnimationLengthMs();

            if (animationLengthMs != 0 && IsPlaying)
            {
                _timeSinceStart += gameTime.ElapsedGameTime;
                if (_timeSinceStart.TotalMilliseconds >= animationLengthMs)
                {
                    if (LoopAnimation)
                        _timeSinceStart = TimeSpan.FromSeconds(0);
                    else
                        IsPlaying = false;
                }

                if (ExternalAnimationRef != null)
                    ExternalAnimationRef.UpdateNode(gameTime);

                OnFrameChanged?.Invoke(CurrentFrame);
            }

            UpdateAnimationFrame();
        }

        void UpdateAnimationFrame()
        {
            if (IsEnabled == false)
            {
                _currentAnimFrame = null;
                return;
            }

            float sampleT = 0;
            float animationLengthMs = GetAnimationLengthMs();
            if (animationLengthMs != 0)
                sampleT = (float)(_timeSinceStart.TotalMilliseconds / animationLengthMs);
            _currentAnimFrame = AnimationSampler.Sample(sampleT, _skeleton, _animationClips, ApplyStaticFrame, ApplyDynamicFrames);
            if(_skeleton != null)
                _skeleton.Update();
        }



        public void Play() { IsPlaying = true; IsEnabled = true; }

        public void Pause() { IsPlaying = false; }
        public void Stop() 
        { 
            IsPlaying = false;
            _currentAnimFrame = null;
            IsEnabled = false;

            if (_skeleton != null)
                _skeleton.Update();
        }

        public void Play(bool value) { IsPlaying = value;  }

        public AnimationFrame GetCurrentFrame()
        {
            return _currentAnimFrame;
        }

        public int FrameCount()
        {
            if (_animationClips != null)
                return _animationClips[0].DynamicFrames.Count();
            return 0;
        }





        //-------------Move to somewhere else
        /*
        void OffsetAnimation(AnimationFrame currentFrame)
        {
            var translationMatrix = Matrix.Identity;
            var roationMatrix = Matrix.Identity;

            if (Settings.UseTranslationOffset)
                translationMatrix = Matrix.CreateTranslation(new Vector3(Settings.TranslationOffsetX, Settings.TranslationOffsetY, Settings.TranslationOffsetZ));

            if (Settings.UseRotationOffset)
                roationMatrix = Matrix.CreateRotationX(MathHelper.ToRadians(Settings.RotationOffsetX)) * Matrix.CreateRotationY(MathHelper.ToRadians(Settings.RotationOffsetY)) * Matrix.CreateRotationZ(MathHelper.ToRadians(Settings.RotationOffsetZ));

            var matrix = currentFrame.BoneTransforms[0].WorldTransform;
            matrix = roationMatrix * translationMatrix * matrix;
            currentFrame.BoneTransforms[0].WorldTransform = matrix;
        }

        void HandleSnapToExternalAnimation(AnimationFrame currentFrame)
        {
            if (ExternalAnimationRef.HasAnimation && Settings.UseAnimationSnap)
            {
                var refTransform = ExternalAnimationRef.Transform;
                currentFrame.BoneTransforms[0].WorldTransform = Matrix.CreateTranslation(refTransform.Translation); ;// * currentFrame.BoneTransforms[0].Transform ;
            }
        }

        void HandleFreezeAnimation(AnimationFrame currentFrame)
        {
            if (Settings.FreezeAnimationRoot)
            {
                Vector3 rootOfset = new Vector3(0);
                Vector3 animRootOffset = new Vector3(0);
                foreach (var boneTransform in currentFrame.BoneTransforms)
                {
                    if (boneTransform.BoneIndex == 0)
                    {
                        var matrix = boneTransform.WorldTransform;
                        animRootOffset += boneTransform.WorldTransform.Translation;
                        matrix.Translation = new Vector3(0, 0, 0);
                        boneTransform.WorldTransform = Matrix.Identity;
                    }

                    if (Settings.FreezeAnimationBone)
                    {
                        if (boneTransform.BoneIndex == 7)
                        {
                            var matrix = boneTransform.WorldTransform;
                            rootOfset += boneTransform.WorldTransform.Translation;
                            matrix.Translation = new Vector3(0, 0, 0);
                            boneTransform.WorldTransform = Matrix.Identity;
                        }
                    }
                }

                foreach (var boneTransform in currentFrame.BoneTransforms)
                {
                    bool test = Settings.FreezeAnimationBone && boneTransform.BoneIndex != 7;
                    if (boneTransform.ParentBoneIndex == 0 && test)
                    {
                        var matrix = boneTransform.WorldTransform;
                        matrix.Translation -= rootOfset;
                        boneTransform.WorldTransform = Matrix.Identity;
                    }
                }
            }
        }
        */
        ////
    }
}
