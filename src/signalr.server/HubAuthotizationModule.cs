using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Hubs;

namespace signalr.server
{
    public class HubAuthotizationModule : HubPipelineModule
    {
        protected override bool OnBeforeAuthorizeConnect(HubDescriptor hubDescriptor, HttpRequest request)
        {
            Console.WriteLine("Authorizing ... " + hubDescriptor.Name);
            return base.OnBeforeAuthorizeConnect(hubDescriptor, request);
        }
    }
}
