﻿using Microsoft.Extensions.DependencyInjection;
using Shared.Core.DependencyInjection;

namespace Editors.Reports
{
    public class DependencyInjectionContainer : DependencyContainer
    {
        public DependencyInjectionContainer()
        {
        }

        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<TouchedFilesRecorder>();
        }
    }
}
