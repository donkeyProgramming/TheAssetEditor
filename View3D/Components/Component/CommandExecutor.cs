using CommonControls.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using View3D.Commands;
using View3D.Components;
using View3D.Components.Input;
using View3D.Scene;

namespace View3D.Components.Component
{
    public delegate void CommandStackChangedDelegate();
    public class CommandExecutor : BaseComponent, IDisposable
    {
        ILogger _logger = Logging.Create<CommandExecutor>();
        Stack<ICommand> _commands = new Stack<ICommand>();


        public event CommandStackChangedDelegate CommandStackChanged;

        SpriteBatch _spriteBatch;
        SpriteFont _font;
        string _animationText;
        GameTime _animationStart;
        bool _startAnimation;

        public CommandExecutor(WpfGame game) : base(game)
        {

        }

        public override void Initialize()
        {
            _font = Game.Content.Load<SpriteFont>("Fonts//DefaultFont");
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public void ExecuteCommand(ICommand command, bool isUndoable = true)
        {
            if (command == null)
                throw new ArgumentNullException("Command is null");
            if(isUndoable)
                _commands.Push(command);
            command.Initialize(Game);
            command.Execute();

            CreateAnimation($"Command added: {command.GetHintText()}");
            if(isUndoable)
                CommandStackChanged?.Invoke();
        }

        void CreateAnimation(string text)
        {
            _animationText = text;
            _startAnimation = true;
        }

        public string GetUndoHint()
        {
            if (!CanUndo())
                return "No items to undo";

            var obj = _commands.Peek();
            return obj.GetHintText();
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
                _spriteBatch.DrawString(_font, _animationText, new Vector2(5,20), new Color(0, 0, 0, alphaValue));
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }


        public override void Update(GameTime gameTime)
        {
            if (_startAnimation == true)
            {
                _animationStart = gameTime;
            }
            _startAnimation = false;

            base.Update(gameTime);
        }

        public bool CanUndo()
        {
            return _commands.Count != 0;
        }

        public void Undo()
        {
            if (CanUndo())
            {
                CreateAnimation($"Command Undone: {GetUndoHint()}");

                var command = _commands.Pop();
                command.Undo();
                CommandStackChanged?.Invoke();
            }
        }

        public void Dispose()
        {
            _spriteBatch.Dispose();
            _spriteBatch = null;

            if (CommandStackChanged != null)
                foreach (var d in CommandStackChanged.GetInvocationList())
                    CommandStackChanged -= (d as CommandStackChangedDelegate);
        }
    }
}

