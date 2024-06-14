using System;
using System.Collections.Generic;
using GameWorld.Core.Components.Selection;
using Shared.Ui.Common.MenuSystem;

namespace KitbasherEditor.ViewModels.MenuBarViews
{
    public class MenuItemVisibilityRuleEngine
    {
        private readonly Dictionary<ActionEnabledRule, Func<bool>> _actionEnabledRules = new Dictionary<ActionEnabledRule, Func<bool>>();
        private readonly Dictionary<ButtonVisibilityRule, Func<bool>> _buttonVisabilityRules = new Dictionary<ButtonVisibilityRule, Func<bool>>();
        private readonly SelectionManager _selectionManager;

        public MenuItemVisibilityRuleEngine(SelectionManager selectionManager)
        {
            _selectionManager = selectionManager;

            _buttonVisabilityRules[ButtonVisibilityRule.Always] = AllwaysTrueRule;
            _buttonVisabilityRules[ButtonVisibilityRule.ObjectMode] = IsObjectMode;
            _buttonVisabilityRules[ButtonVisibilityRule.FaceMode] = IsFaceMode;
            _buttonVisabilityRules[ButtonVisibilityRule.VertexMode] = IsVertexMode;

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
