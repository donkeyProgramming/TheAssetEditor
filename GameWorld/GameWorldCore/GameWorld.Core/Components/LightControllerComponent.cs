using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GameWorld.Core.Components
{

    public class LightControllerComponent : BaseComponent
    { 
        string _animationText;
        GameTime? _animationStart;
        bool _startAnimation;

        private readonly IKeyboardComponent _keyboardComponent;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly SceneRenderParametersStore _sceneRenderParametersStore;

        public LightControllerComponent(IKeyboardComponent keyboardComponent, RenderEngineComponent renderEngineComponent, SceneRenderParametersStore sceneRenderParametersStore)
        {
            _keyboardComponent = keyboardComponent;
            _renderEngineComponent = renderEngineComponent;
            _sceneRenderParametersStore = sceneRenderParametersStore;
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
                var lerpValue = (float)timeDiff / 2000.0f;
                var alphaValue = MathHelper.Lerp(1, 0, lerpValue);
                if (lerpValue >= 1)
                    _animationStart = null;

                _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, new FontRenderItem(_renderEngineComponent, _animationText, new Vector2(5, 20), new Color(0, 0, 0, alphaValue)));
            }

            base.Draw(gameTime);
        }


        public override void Update(GameTime gameTime)
        {
            var lightMoved = false;
            var DirlightMoved_X = false;
            var DirlightMoved_Y = false;
            var lightIntensityChanged = false;
            if (_keyboardComponent.IsKeyDown(Keys.PageUp) && !_keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneRenderParametersStore.EnvLightRotationDegrees_Y += 1.0f;
                lightMoved = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.PageDown) && !_keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneRenderParametersStore.EnvLightRotationDegrees_Y -= 1.0f;
                lightMoved = true;
            }

            if (_keyboardComponent.IsKeyDown(Keys.PageUp) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneRenderParametersStore.LightIntensityMult += 0.05f;
                lightIntensityChanged = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.PageDown) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneRenderParametersStore.LightIntensityMult -= 0.05f;
                lightIntensityChanged = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Right) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneRenderParametersStore.DirLightRotationDegrees_Y -= 1.0f;
                DirlightMoved_Y = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Left) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneRenderParametersStore.DirLightRotationDegrees_Y += 1.0f;
                DirlightMoved_Y = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Up) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneRenderParametersStore.DirLightRotationDegrees_X -= 1.0f;
                DirlightMoved_X = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Down) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneRenderParametersStore.DirLightRotationDegrees_X += 1.0f;
                DirlightMoved_X = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Home) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                ResetLightingParams();
                lightMoved = true;
            }

            LimitLightValues();

            if (lightMoved)
                CreateAnimation($"Env rotation Y: {_sceneRenderParametersStore.EnvLightRotationDegrees_Y}");

            if (DirlightMoved_X)
                CreateAnimation($"DirLight rotation X: {_sceneRenderParametersStore.DirLightRotationDegrees_X}");

            if (DirlightMoved_Y)
                CreateAnimation($"DirLight rotation Y: {_sceneRenderParametersStore.DirLightRotationDegrees_Y}");

            if (lightIntensityChanged)
                CreateAnimation($"Light intensity: {_sceneRenderParametersStore.LightIntensityMult}");

            if (_startAnimation == true)
            {
                _animationStart = gameTime;
            }
            _startAnimation = false;

            base.Update(gameTime);
        }


        public void ResetLightingParams()
        {
            _sceneRenderParametersStore.LightIntensityMult = 1.0f;
            _sceneRenderParametersStore.EnvLightRotationDegrees_Y = 20.0f;
            _sceneRenderParametersStore.DirLightRotationDegrees_X = 0.0f;
            _sceneRenderParametersStore.DirLightRotationDegrees_Y = 0.0f;
        }

        private static float LimitAndWrapAroundDegrees(float degrees)
        {
            if (degrees > 360.0f)
                return 0.0f;

            if (degrees < 0.0f)
                return 360.0f;

            return degrees;
        }

        private void LimitLightValues()
        {
            _sceneRenderParametersStore.EnvLightRotationDegrees_Y = LimitAndWrapAroundDegrees(_sceneRenderParametersStore.EnvLightRotationDegrees_Y);

            _sceneRenderParametersStore.DirLightRotationDegrees_X = LimitAndWrapAroundDegrees(_sceneRenderParametersStore.DirLightRotationDegrees_X);
            _sceneRenderParametersStore.DirLightRotationDegrees_Y = LimitAndWrapAroundDegrees(_sceneRenderParametersStore.DirLightRotationDegrees_Y);

            if (_sceneRenderParametersStore.LightIntensityMult < 0)
                _sceneRenderParametersStore.LightIntensityMult = 0;
        }
    }
}
