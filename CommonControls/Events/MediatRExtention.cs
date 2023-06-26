using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Events
{
    public static class MediatRExtention
    {
        public static void PublishSync<IRequest>(this IMediator mediator, IRequest request) => mediator.Publish(request).GetAwaiter().GetResult();
        public static TResponse SendAsync<TResponse>(this IMediator mediator, IRequest<TResponse> request) => mediator.Send(request).GetAwaiter().GetResult();
    }
}
