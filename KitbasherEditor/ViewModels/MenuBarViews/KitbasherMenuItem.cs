using CommonControls.Common.MenuSystem;
using CommonControls.Events.UiCommands;
using System;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public interface IKitbasherUiCommand : IUiCommand
    {
        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule { get; }
        public Hotkey HotKey { get; }

        public void Execute();
    }

    public class KitbasherMenuItem<T> : MenuAction
        where T : IKitbasherUiCommand
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly T _instance;

        public KitbasherMenuItem(IUiCommandFactory uiCommandFactory, Action<T> function = null) : base()
        {
            // TODO: remove
            // Console.WriteLine("KitBasherMenuItem");
            _uiCommandFactory = uiCommandFactory;
            _instance = _uiCommandFactory.Create(function);



            Hotkey = _instance.HotKey;
            ToolTip = _instance.ToolTip;
            EnableRule = _instance.EnabledRule;
        }

        public override void TriggerInternal()
        {
            _instance.Execute();
        }
    }
}
