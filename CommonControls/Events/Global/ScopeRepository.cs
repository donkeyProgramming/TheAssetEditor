﻿using CommonControls.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace CommonControls.Events.Global
{
    public class ScopeRepository
    {
        public Dictionary<IEditorViewModel, IServiceScope> Scopes { get; private set; } = new Dictionary<IEditorViewModel, IServiceScope>();

        public void Add(IEditorViewModel owner, IServiceScope scope)
        {
            if (Scopes.ContainsKey(owner))
                throw new ArgumentException("Owner already added!");

            Scopes.Add(owner, scope);
        }

        public void RemoveScope(IEditorViewModel owner)
        {
            Scopes[owner].Dispose();
            Scopes.Remove(owner);
        }
    }
}