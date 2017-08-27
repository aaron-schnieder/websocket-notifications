using NotificationsApi.Models;
using NotificationsApi.Persistence;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationsApi.Notifications
{
    // interface for NotificationManager for dependency injection
    public interface INotificationManager
    {
        string AddSubscriber(WebSocket subscriber);
        void RemoveSubscriber(string subscriberId);
        Task SendNotificationAsync(Notification notification);
    }

    public class NotificationManager : INotificationManager 
    {
        // static instance of the NotificationManager class
        private static INotificationManager _instance;
        public static INotificationManager Instance { get { return _instance ?? (_instance = new NotificationManager()); } set { _instance = value; } }

        // static dictionary to keep track of all notification subscribers
        private static ConcurrentDictionary<string, WebSocket> _subscribers = new ConcurrentDictionary<string, WebSocket>();

        // adds a subscriber to receive notifications
        public string AddSubscriber(WebSocket subscriber)
        {
            var subscriberId = Guid.NewGuid().ToString();
            _subscribers.TryAdd(subscriberId, subscriber);
            return subscriberId.ToString();
        }

        // removes a notifications subscriber
        public void RemoveSubscriber(string subscriberId)
        {
            WebSocket empty;
            _subscribers.TryRemove(subscriberId, out empty);
        }

        // sends a notification to all subscribers
        public async Task SendNotificationAsync(Notification notification)
        {
            // add the notification to the persistence store
            await PersistenceManager.Instance.AddNotificationAsync(notification);

            // send the notification to all subscribers
            foreach (var s in _subscribers)
            {
                if (s.Value.State == WebSocketState.Open)
                {
                    var jsonNotification = JsonConvert.SerializeObject(notification);
                    await SendStringAsync(s.Value, jsonNotification);
                }
            }
        }

        // sends a string via web socket communication
        private async Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default(CancellationToken))
        {
            var buffer = Encoding.UTF8.GetBytes(data);
            var segment = new ArraySegment<byte>(buffer);
            await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
        }
    }
}
