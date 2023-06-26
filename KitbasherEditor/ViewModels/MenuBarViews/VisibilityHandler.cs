using CommonControls.Common.MenuSystem;
using System;
using System.Collections.Generic;
using View3D.Components.Component.Selection;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class VisibilityHandler
    {
        Dictionary<ActionEnabledRule, Func<bool>> _actionEnabledRules = new Dictionary<ActionEnabledRule, Func<bool>>();
        Dictionary<ButtonVisabilityRule, Func<bool>> _buttonVisabilityRules = new Dictionary<ButtonVisabilityRule, Func<bool>>();

        SelectionManager _selectionManager;
        public VisibilityHandler(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;

            _buttonVisabilityRules[ButtonVisabilityRule.Always] = AllwaysTrueRule;
            _buttonVisabilityRules[ButtonVisabilityRule.ObjectMode] = IsObjectMode;
            _buttonVisabilityRules[ButtonVisabilityRule.FaceMode] = IsFaceMode;
            _buttonVisabilityRules[ButtonVisabilityRule.VertexMode] = IsVertexMode;

            _actionEnabledRules[ActionEnabledRule.Always] = AllwaysTrueRule;
            _actionEnabledRules[ActionEnabledRule.OneObjectSelected] = OneObjectSelectedRule;
            _actionEnabledRules[ActionEnabledRule.AtleastOneObjectSelected] = AtleastOneObjectSelectedRule;
            _actionEnabledRules[ActionEnabledRule.TwoOrMoreObjectsSelected] = TwoOrMoreObjectsSelectedRule;
            _actionEnabledRules[ActionEnabledRule.TwoObjectesSelected] = TwoObjectSelectedRule;

            _actionEnabledRules[ActionEnabledRule.FaceSelected] = FaceSelectedRule;
            _actionEnabledRules[ActionEnabledRule.VertexSelected] = VertexSelectedRule;
            _actionEnabledRules[ActionEnabledRule.AnythingSelected] = AnythingSelectedRule;
            _actionEnabledRules[ActionEnabledRule.ObjectOrVertexSelected] = ObjectOrVertexSelectedRule;
            _actionEnabledRules[ActionEnabledRule.ObjectOrFaceSelected] = ObjectOrFaceSelectedReule;
        }

        bool IsObjectMode() => _selectionManager.GetState().Mode == GeometrySelectionMode.Object;
        bool IsFaceMode() => _selectionManager.GetState().Mode == GeometrySelectionMode.Face;
        bool IsVertexMode() => _selectionManager.GetState().Mode == GeometrySelectionMode.Vertex;
        bool AllwaysTrueRule() => true;

        bool OneObjectSelectedRule() => IsObjectMode() && _selectionManager.GetState().SelectionCount() == 1;
        bool TwoObjectSelectedRule() => IsObjectMode() && _selectionManager.GetState().SelectionCount() == 2;
        bool AtleastOneObjectSelectedRule() => IsObjectMode() && _selectionManager.GetState().SelectionCount() >= 1;
        bool TwoOrMoreObjectsSelectedRule() => IsObjectMode() && _selectionManager.GetState().SelectionCount() >= 2;
        bool AnythingSelectedRule() => _selectionManager.GetState().SelectionCount() >= 1;
        bool ObjectOrFaceSelectedReule() => (IsObjectMode() || IsFaceMode()) && _selectionManager.GetState().SelectionCount() >= 1;
        bool ObjectOrVertexSelectedRule() => (IsObjectMode() || IsVertexMode()) && _selectionManager.GetState().SelectionCount() >= 1;

        bool FaceSelectedRule() => IsFaceMode() && _selectionManager.GetState().SelectionCount() >= 1;
        bool VertexSelectedRule() => IsVertexMode() && _selectionManager.GetState().SelectionCount() >= 1;

        public void Validate(MenuBarButton button)
        {
            button.IsVisible.Value = _buttonVisabilityRules[button.ShowRule].Invoke();
        }

        public void Validate(MenuAction action)
        {
            var rule = action.EnableRule;
            if (rule == ActionEnabledRule.Custom)
                return;
            action.IsActionEnabled.Value = _actionEnabledRules[rule].Invoke();
        }
    }
}
