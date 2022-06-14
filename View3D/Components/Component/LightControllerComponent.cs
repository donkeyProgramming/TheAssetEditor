using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
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
        KeyboardComponent _keyboard;
        RenderEngineComponent _renderEngine;

        public LightControllerComponent(IComponentManager componentManager) : base(componentManager)
        {

        }

        public override void Initialize()
        {
            var resourceLib = ComponentManager.GetComponent<ResourceLibary>();
            var graphics = ComponentManager.GetComponent<DeviceResolverComponent>();
            _keyboard = ComponentManager.GetComponent<KeyboardComponent>();
            _renderEngine = ComponentManager.GetComponent<RenderEngineComponent>();

            _font = resourceLib.DefaultFont;
            _spriteBatch = new SpriteBatch(graphics.Device);
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
            if (_keyboard.IsKeyDown(Keys.PageUp) && !_keyboard.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngine.EnvLightRotationDegrees_Y += 1.0f;
                lightMoved = true;
            }
            else if (_keyboard.IsKeyDown(Keys.PageDown) && !_keyboard.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngine.DirLightRotationDegrees_Y -= 1.0f;
                lightMoved = true;
            }

            if (_keyboard.IsKeyDown(Keys.PageUp) && _keyboard.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngine.LightIntensityMult += 0.1f;
                lightIntensityChanged = true;
            }
            else if (_keyboard.IsKeyDown(Keys.PageDown) && _keyboard.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngine.LightIntensityMult -= 0.1f;
                lightIntensityChanged = true;
            }

            // TODO: Phazer Code: added this code for the directional light
            else if (_keyboard.IsKeyDown(Keys.Right) && _keyboard.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngine.DirLightRotationDegrees_Y -= 1.0f;
                DirlightMoved_Y = true;
            }
            else if (_keyboard.IsKeyDown(Keys.Left) && _keyboard.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngine.DirLightRotationDegrees_Y += 1.0f;
                DirlightMoved_Y = true;
            }
            else if (_keyboard.IsKeyDown(Keys.Up) && _keyboard.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngine.DirLightRotationDegrees_X -= 1.0f;
                DirlightMoved_X = true;
            }
            else if (_keyboard.IsKeyDown(Keys.Down) && _keyboard.IsKeyDown(Keys.LeftAlt))
            {
                _renderEngine.DirLightRotationDegrees_X += 1.0f;
                DirlightMoved_X = true;
            }
            else if (_keyboard.IsKeyDown(Keys.Home) && _keyboard.IsKeyDown(Keys.LeftAlt))
            {
                ResetLilghtingParams();
                lightMoved = true;
            }

            ClampLightValues();

            if (lightMoved)
                CreateAnimation($"Env rotation Y: {_renderEngine.EnvLightRotationDegrees_Y}");

            if (DirlightMoved_X)
                CreateAnimation($"DirLight rotation X: {_renderEngine.DirLightRotationDegrees_X}");

            if (DirlightMoved_Y)
                CreateAnimation($"DirLight rotation Y: {_renderEngine.DirLightRotationDegrees_Y}");

            if (lightIntensityChanged)
                CreateAnimation($"Light intensity: {_renderEngine.LightIntensityMult}");

            if (_startAnimation == true)
            {
                _animationStart = gameTime;
            }
            _startAnimation = false;

            base.Update(gameTime);
        }


        public void ResetLilghtingParams()
        {
            _renderEngine.LightIntensityMult = 1.0f;
            _renderEngine.EnvLightRotationDegrees_Y = 0.0f;
            _renderEngine.DirLightRotationDegrees_X = 0.0f;
            _renderEngine.DirLightRotationDegrees_Y = 0.0f;
        }


        private void ClampLightValues()
        {
            // (keep rotation = [0; 306])

            // enviroment rotation
            if (_renderEngine.EnvLightRotationDegrees_Y > 360.0f)
                _renderEngine.EnvLightRotationDegrees_Y = 0;

            if (_renderEngine.EnvLightRotationDegrees_Y < 0)
                _renderEngine.EnvLightRotationDegrees_Y = 360.0f;

            // diretional lighting rotation, 
            if (_renderEngine.DirLightRotationDegrees_X > 360.0f)
                _renderEngine.DirLightRotationDegrees_X = 0.0f;

            if (_renderEngine.DirLightRotationDegrees_X < 0)
                _renderEngine.DirLightRotationDegrees_X = 360.0f;


            if (_renderEngine.DirLightRotationDegrees_Y < 0)
                _renderEngine.DirLightRotationDegrees_Y = 360.0f;

            if (_renderEngine.DirLightRotationDegrees_Y > 360.0f)
                _renderEngine.DirLightRotationDegrees_Y = 0.0f;


            if (_renderEngine.LightIntensityMult < 0)
                _renderEngine.LightIntensityMult = 0;
        }


        public void Dispose()
        {
            _spriteBatch.Dispose();
            _spriteBatch = null;
        }
    }
}
