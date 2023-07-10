// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Common;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Events.Global
{
    public class GlobalEventSender
    {
        private readonly ScopeRepository _scopeRepository;

        public GlobalEventSender(ScopeRepository scopeRepository)
        {
            _scopeRepository = scopeRepository;
        }

        public void TriggerEvent<T>(T e)
        {
            foreach (var scope in _scopeRepository.Scopes.Values)
            {
                var handler = scope.ServiceProvider.GetService<EventHub>();
                if (handler != null)
                    handler.Publish(e);
            }
        }
    }
}
