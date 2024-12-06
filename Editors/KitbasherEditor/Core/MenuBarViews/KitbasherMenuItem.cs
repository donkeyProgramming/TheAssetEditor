using Shared.Core.Events;
using Shared.Ui.Common.MenuSystem;

namespace Editors.KitbasherEditor.Core.MenuBarViews
{
    public interface IKitbasherUiCommand : IUiCommand
    {
        public string ToolTip { get; set; }
        public ActionEnabledRule EnabledRule { get; }
        public Hotkey? HotKey { get; }

        public void Execute();
    }

    public interface ITransientKitbasherUiCommand : IKitbasherUiCommand
    {
    }

    public interface IScopedKitbasherUiCommand : IKitbasherUiCommand
    {
    }

    public class KitbasherMenuItem<T> : MenuAction
        where T : IKitbasherUiCommand
    {
        private readonly IUiCommandFactory _uiCommandFactory;
        private readonly T _instance;

        public KitbasherMenuItem(IUiCommandFactory uiCommandFactory, Action<T> function = null) : base()
        {
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
