using CommonControls.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using View3D.Animation.AnimationChange;
using View3D.Utility;

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
            public Vector3 Scale { get; set; }
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
        TimeSpanExtension _timeSinceStart;
        AnimationFrame _currentAnimFrame;
        AnimationClip _animationClip;

        public bool IsPlaying { get; private set; } = true;
        public double SpeedMultiplication { get; set; }
        public bool IsEnabled { get; set; } = false;
        public bool LoopAnimation { get; set; } = true;
        public bool MarkedForRemoval { get; set; } = false;

        public List<AnimationChangeRule> AnimationRules { get; set; } = new List<AnimationChangeRule>();

        public int TimeUsToFrame(long timeUs)
        {
            int frame = (int)(timeUs / _animationClip.MicrosecondsPerFrame);
            return MathUtil.EnsureRange(frame, 0, FrameCount() - 1);
        }
        
        public long FrameToStartTimeUs(int frame)
        {
            if (_animationClip.MicrosecondsPerFrame <= 0)
                return 0;
            return MathUtil.EnsureRange((long) _animationClip.MicrosecondsPerFrame * frame, 0L, GetAnimationLengthUs() - _animationClip.MicrosecondsPerFrame);
        }

        public int CurrentFrame
        {
            get 
            {
                if (_animationClip == null)
                    return 0;
                return TimeUsToFrame(_timeSinceStart.TotalMicrosecondsAsLong);
            }
            set
            {
                if (CurrentFrame == value)
                    return;

                if (_animationClip != null)
                {
                    int frameIndex = MathUtil.EnsureRange(value, 0, FrameCount() -1);
                    long timeInUs = FrameToStartTimeUs(frameIndex);
                    _timeSinceStart = TimeSpanExtension.FromMicroseconds(timeInUs);
                    OnFrameChanged?.Invoke(CurrentFrame);
                }
                else
                {
                    OnFrameChanged?.Invoke(0);
                }
                Refresh();
            }
        }

        public void SetAnimation(AnimationClip animation, GameSkeleton skeleton)
        {
            SetAnimationArray(animation , skeleton);
        }

        public void SetAnimationArray(AnimationClip animation, GameSkeleton skeleton)
        {
            if (animation != null)
            {
                // Make sure animation fits
                if (animation.AnimationBoneCount != skeleton.BoneCount)
                    throw new Exception("This animation does not work for this skeleton!");
            }

            AnimationRules.Clear();
            _skeleton = skeleton;
            _animationClip = animation;
            _timeSinceStart = new TimeSpanExtension();
            Refresh();
        }

        // public long GetAnimationLengthMs()
        // {
        //     if (_animationClip != null)
        //         return _animationClip.PlayTimeUs / 1000;
        //     return 0;
        // }
        
        public long GetAnimationLengthUs()
        {
            if (_animationClip != null)
                return _animationClip.PlayTimeUs;
            return 0;
        }

        public long GetTimeUs()
        {
            return _timeSinceStart.TotalMicrosecondsAsLong;
        }

        public int GetFPS()
        {
            if (_animationClip == null)
                return 0;
            var fps = (_animationClip.DynamicFrames.Count / _animationClip.PlayTimeInSec);
            return (int)fps;
        }

        public void Update(GameTime gameTime)
        {
            long animationLengthUs = GetAnimationLengthUs();
            if (animationLengthUs != 0 && IsPlaying && IsEnabled)
            {
                _timeSinceStart.TimeSpan += gameTime.ElapsedGameTime;
                if (_timeSinceStart.TotalMicrosecondsAsLong >= animationLengthUs)
                {
                    if (LoopAnimation)
                    {
                        _timeSinceStart = new TimeSpanExtension();
                    }
                    else
                    {
                        _timeSinceStart = TimeSpanExtension.FromMicroseconds(animationLengthUs);
                        IsPlaying = false;
                    }
                }

                if (ExternalAnimationRef != null)
                    ExternalAnimationRef.UpdateNode(gameTime);

                OnFrameChanged?.Invoke(CurrentFrame);
            }

            Refresh();
        }

        public void Refresh()
        {
            try
            {
                if (IsEnabled == false)
                {
                    _currentAnimFrame = null;
                    return;
                }
                //Fix this so if crash no break
                float sampleT = 0;
                long animationLengthUs = GetAnimationLengthUs();
                if (animationLengthUs != 0)
                    sampleT = (float) _timeSinceStart.TotalMicrosecondsAsLong / animationLengthUs;
                _currentAnimFrame = AnimationSampler.Sample(sampleT, _skeleton, _animationClip, AnimationRules);
                if (_skeleton != null)
                    _skeleton.Update();
            }
            catch
            {
                MessageBox.Show("Error playing animation");
                SetAnimation(null, _skeleton);
            }
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

        public AnimationFrame GetCurrentAnimationFrame()
        {
            return _currentAnimFrame;
        }

        public int FrameCount()
         {
            if (_animationClip != null)
                return _animationClip.DynamicFrames.Count();
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
