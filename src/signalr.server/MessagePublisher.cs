using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR.Infrastructure;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using signalr.server.Hubs;

namespace signalr.server
{
    public delegate Task PublishMessageAsync(string tenantId, string topic, string message);


    //public class MessagePublisher
    //{
    //    private readonly PublishMessageAsync _publishMessageAsync;
    //    //private readonly IConnectionManager _connectionManager;

    //    public MessagePublisher(PublishMessageAsync publishMessageAsync)
    //    {
    //        _publishMessageAsync = publishMessageAsync;
    //    }

    //    public Task PublishMessageAsync(string tenantId, string topic, string message)
    //    {
    //        //var hub = _connectionManager.GetHubContext<MessageHub>();
    //        //IClientProxy proxy = hub.Clients.Group(tenantId + "-" + topic);
    //        //await proxy.Invoke(topic, message);
    //    }
    //}


}
