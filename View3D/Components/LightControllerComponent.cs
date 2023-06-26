using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using View3D.Components.Input;
using View3D.Components.Rendering;
using View3D.Utility;

namespace View3D.Components.Component
{
    public class LightControllerComponent : BaseComponent, IDisposable
    {
        SpriteBatch _spriteBatch;
        SpriteFont _font;
        string _animationText;
        GameTime _animationStart;
        bool _startAnimation;
  
        private readonly ResourceLibary _resourceLibary;
        private readonly DeviceResolverComponent _deviceResolverComponent;
        private readonly KeyboardComponent _keyboardComponent;
        private readonly RenderEngineComponent _renderEngineComponent;

        public LightControllerComponent(ComponentManagerResolver componentManagerResolver,
            ResourceLibary resourceLibary, DeviceResolverComponent deviceResolverComponent, KeyboardComponent keyboardComponent, RenderEngineComponent renderEngineComponent) 
            : base(componentManagerResolver.ComponentManager)
        {
            _resourceLibary = resourceLibary;
            _deviceResolverComponent = deviceResolverComponent;
            _keyboardComponent = keyboardComponent;
            _renderEngineComponent = renderEngineComponent;
        }

        public override void Initialize()
        {


            _font = _resourceLibary.DefaultFont;
            _spriteBatch = new SpriteBatch(_deviceResolverComponent.Device);
        }


        void CreateAnimation(string text)
        {
            _animationText = text;
            _startAnimation = true;
        }

        public override void Draw(GameTime gameTime)
        {
            if (_animationStart != null)
            {
                var timeDiff = (gameTime.TotalGameTime - _animationStart.TotalGameTime).TotalMilliseconds;
                float lerpValue = (float)timeDiff / 2000.0f;
                var alphaValue = MathHelper.Lerp(1, 0, lerpValue);
                if (lerpValue >= 1)
                    _animationStart = null;

                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                _spriteBatch.DrawString(_font, _animationText, new Vector2(5, 20), new Color(0, 0, 0, alphaValue));
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }


        public override void Update(GameTime gameTime)
        {
            bool lightMoved = false;
            bool DirlightMoved_X = false;
            bool DirlightMoved_Y = false;
            bool lightIntensityChanged = false;
            if (_keyboardComponent.IsKeyDown(Keys.PageUp) && !_keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngineComponent.EnvLightRotationDegrees_Y += 1.0f;
                lightMoved = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.PageDown) && !_keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngineComponent.EnvLightRotationDegrees_Y -= 1.0f;
                lightMoved = true;
            }

            if (_keyboardComponent.IsKeyDown(Keys.PageUp) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngineComponent.LightIntensityMult += 0.05f;
                lightIntensityChanged = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.PageDown) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngineComponent.LightIntensityMult -= 0.05f;
                lightIntensityChanged = true;
            }            
            else if (_keyboardComponent.IsKeyDown(Keys.Right) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngineComponent.DirLightRotationDegrees_Y -= 1.0f;
                DirlightMoved_Y = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Left) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngineComponent.DirLightRotationDegrees_Y += 1.0f;
                DirlightMoved_Y = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Up) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngineComponent.DirLightRotationDegrees_X -= 1.0f;
                DirlightMoved_X = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Down) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngineComponent.DirLightRotationDegrees_X += 1.0f;
                DirlightMoved_X = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Home) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                ResetLilghtingParams();
                lightMoved = true;
            }

            LimitLightValues();

            if (lightMoved)
                CreateAnimation($"Env rotation Y: {_renderEngineComponent.EnvLightRotationDegrees_Y}");

            if (DirlightMoved_X)
                CreateAnimation($"DirLight rotation X: {_renderEngineComponent.DirLightRotationDegrees_X}");

            if (DirlightMoved_Y)
                CreateAnimation($"DirLight rotation Y: {_renderEngineComponent.DirLightRotationDegrees_Y}");

            if (lightIntensityChanged)
                CreateAnimation($"Light intensity: {_renderEngineComponent.LightIntensityMult}");

            if (_startAnimation == true)
            {
                _animationStart = gameTime;
            }
            _startAnimation = false;

            base.Update(gameTime);
        }

        
        public void ResetLilghtingParams()
        {
            _renderEngineComponent.LightIntensityMult = 1.0f;
            _renderEngineComponent.EnvLightRotationDegrees_Y = 20.0f;
            _renderEngineComponent.DirLightRotationDegrees_X = 0.0f;
            _renderEngineComponent.DirLightRotationDegrees_Y = 0.0f;
        }

        private static float LimitAndWrapAroundDregrees(float degrees)
        {

            if (degrees > 360.0f)
                return 0.0f;


            if (degrees < 0.0f)
                return 360.0f;

            return degrees;

        }

        private void LimitLightValues()
        {
            _renderEngineComponent.EnvLightRotationDegrees_Y = LimitAndWrapAroundDregrees(_renderEngineComponent.EnvLightRotationDegrees_Y);

            _renderEngineComponent.DirLightRotationDegrees_X = LimitAndWrapAroundDregrees(_renderEngineComponent.DirLightRotationDegrees_X);
            _renderEngineComponent.DirLightRotationDegrees_Y = LimitAndWrapAroundDregrees(_renderEngineComponent.DirLightRotationDegrees_Y);


            if (_renderEngineComponent.LightIntensityMult < 0)
                _renderEngineComponent.LightIntensityMult = 0;
        }


        public void Dispose()
        {
            _spriteBatch.Dispose();
            _spriteBatch = null;
        }
    }
}
