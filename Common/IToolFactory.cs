using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace Common
{
    public interface IToolFactory
    {
        public void RegisterTool<ViewModel, View>(params string[] extentions)
               where ViewModel : IEditorViewModel
               where View : Control;

        public void RegisterToolAsDefault<ViewModel, View>()
           where ViewModel : IEditorViewModel
           where View : Control;

        public void RegisterTool<ViewModel, View>()
            where ViewModel : IEditorViewModel
            where View : Control;


        public ViewModel CreateEditorViewModel<ViewModel>() 
            where ViewModel : IEditorViewModel;
    }
}
