using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationsApi.Notifications
{
    public class WebSocketsMiddleware
    {
        // private variable to track the next delegate to call in the request chain
        private readonly RequestDelegate _next;

        public WebSocketsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            CancellationToken ct = context.RequestAborted;
            string currentSubscriberId = null;
            WebSocket currentSocket = null;

            // we want to listen on a specific path for websocket communications
            if (context.Request.Path == "/notifications/ws")
            {
                // make sure the request is a websocket request
                if (context.WebSockets.IsWebSocketRequest)
                {
                    currentSocket = await context.WebSockets.AcceptWebSocketAsync();
                    currentSubscriberId = NotificationManager.Instance.AddSubscriber(currentSocket);

                    // keep the socket open until we get a cancellation request
                    while (true)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
                else // return an HTTP bad request status code if anything other a web socket request is made on this URI
                { 
                    context.Response.StatusCode = 400;
                }
            }

            // clean up the socket
            if (!string.IsNullOrWhiteSpace(currentSubscriberId))
            {
                NotificationManager.Instance.RemoveSubscriber(currentSubscriberId);
                if (currentSocket != null)
                {
                    await currentSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    currentSocket.Dispose();
                }
            }

            // call the next delegate in the pipeline
            await _next(context);
            return;
        }
    }
}
