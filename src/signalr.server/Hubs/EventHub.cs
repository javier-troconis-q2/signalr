using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Hubs;

namespace signalr.server.Hubs
{

    public class EventHub : Hub
    {
        public void Subscribe(string topic)
        {
            var tenantId = Context.Request.Query["tenantId"];
            Groups.Add(Context.ConnectionId, tenantId + "-" + topic);
        }

        public void Unsubscribe(string topic)
        {
            var tenantId = Context.Request.Query["tenantId"];
            Groups.Remove(Context.ConnectionId, tenantId + "-" + topic);
        }
    }
}
