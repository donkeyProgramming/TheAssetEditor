using GameWorld.Core.Components.Input;
using GameWorld.Core.Components.Rendering;
using GameWorld.Core.Rendering.RenderItems;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GameWorld.Core.Components
{

    public class SceneLightParametersStore
    {
        public float EnvLightRotationDegrees_Y { get; set; } = 20;
        public float DirLightRotationDegrees_X { get; set; } = 0;
        public float DirLightRotationDegrees_Y { get; set; } = 0;
        public float LightIntensityMult { get; set; } = 1;
    }

    public class LightControllerComponent : BaseComponent
    { 
        string _animationText;
        GameTime? _animationStart;
        bool _startAnimation;

        private readonly IKeyboardComponent _keyboardComponent;
        private readonly RenderEngineComponent _renderEngineComponent;
        private readonly ResourceLibrary _resourceLibrary;
        private readonly SceneLightParametersStore _sceneLightParametersStore;

        public LightControllerComponent(IKeyboardComponent keyboardComponent, RenderEngineComponent renderEngineComponent, ResourceLibrary resourceLibrary, SceneLightParametersStore sceneLightParametersStore)
        {
            _keyboardComponent = keyboardComponent;
            _renderEngineComponent = renderEngineComponent;
            _resourceLibrary = resourceLibrary;
            _sceneLightParametersStore = sceneLightParametersStore;
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

                _renderEngineComponent.AddRenderItem(RenderBuckedId.Font, new FontRenderItem(_resourceLibrary, _animationText, new Vector2(5, 20), new Color(0, 0, 0, alphaValue)));
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
                _sceneLightParametersStore.EnvLightRotationDegrees_Y += 1.0f;
                lightMoved = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.PageDown) && !_keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneLightParametersStore.EnvLightRotationDegrees_Y -= 1.0f;
                lightMoved = true;
            }

            if (_keyboardComponent.IsKeyDown(Keys.PageUp) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneLightParametersStore.LightIntensityMult += 0.05f;
                lightIntensityChanged = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.PageDown) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneLightParametersStore.LightIntensityMult -= 0.05f;
                lightIntensityChanged = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Right) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneLightParametersStore.DirLightRotationDegrees_Y -= 1.0f;
                DirlightMoved_Y = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Left) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneLightParametersStore.DirLightRotationDegrees_Y += 1.0f;
                DirlightMoved_Y = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Up) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneLightParametersStore.DirLightRotationDegrees_X -= 1.0f;
                DirlightMoved_X = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Down) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                _sceneLightParametersStore.DirLightRotationDegrees_X += 1.0f;
                DirlightMoved_X = true;
            }
            else if (_keyboardComponent.IsKeyDown(Keys.Home) && _keyboardComponent.IsKeyDown(Keys.LeftAlt))
            {
                ResetLightingParams();
                lightMoved = true;
            }

            LimitLightValues();

            if (lightMoved)
                CreateAnimation($"Env rotation Y: {_sceneLightParametersStore.EnvLightRotationDegrees_Y}");

            if (DirlightMoved_X)
                CreateAnimation($"DirLight rotation X: {_sceneLightParametersStore.DirLightRotationDegrees_X}");

            if (DirlightMoved_Y)
                CreateAnimation($"DirLight rotation Y: {_sceneLightParametersStore.DirLightRotationDegrees_Y}");

            if (lightIntensityChanged)
                CreateAnimation($"Light intensity: {_sceneLightParametersStore.LightIntensityMult}");

            if (_startAnimation == true)
            {
                _animationStart = gameTime;
            }
            _startAnimation = false;

            base.Update(gameTime);
        }


        public void ResetLightingParams()
        {
            _sceneLightParametersStore.LightIntensityMult = 1.0f;
            _sceneLightParametersStore.EnvLightRotationDegrees_Y = 20.0f;
            _sceneLightParametersStore.DirLightRotationDegrees_X = 0.0f;
            _sceneLightParametersStore.DirLightRotationDegrees_Y = 0.0f;
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
            _sceneLightParametersStore.EnvLightRotationDegrees_Y = LimitAndWrapAroundDegrees(_sceneLightParametersStore.EnvLightRotationDegrees_Y);

            _sceneLightParametersStore.DirLightRotationDegrees_X = LimitAndWrapAroundDegrees(_sceneLightParametersStore.DirLightRotationDegrees_X);
            _sceneLightParametersStore.DirLightRotationDegrees_Y = LimitAndWrapAroundDegrees(_sceneLightParametersStore.DirLightRotationDegrees_Y);

            if (_sceneLightParametersStore.LightIntensityMult < 0)
                _sceneLightParametersStore.LightIntensityMult = 0;
        }
    }
}
