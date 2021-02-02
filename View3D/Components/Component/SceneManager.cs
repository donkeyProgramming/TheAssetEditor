using MonoGame.Framework.WpfInterop;
using System.Collections.Generic;
using View3D.Rendering;

namespace View3D.Components.Component
{
    public class SceneManager : BaseComponent
    {
        public SceneManager(WpfGame game) : base(game) { }

        public List<RenderItem> RenderItems = new List<RenderItem>();
    }
}
