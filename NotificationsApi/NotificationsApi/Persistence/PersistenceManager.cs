using NotificationsApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotificationsApi.Persistence
{
    // interface for dependency injection
    public interface IPersistenceManager
    {
        Task AddNotificationAsync(Notification notification);
        List<Notification> GetNotifications();
    }

    public class PersistenceManager : IPersistenceManager
    {
        private static IPersistenceManager _instance;
        public static IPersistenceManager Instance { get { return _instance ?? (_instance = new PersistenceManager()); } set { _instance = value; } }

        // persists a notification
        public async Task AddNotificationAsync(Notification notification)
        {
            try
            {
                using (var context = new NotificationsContext())
                {
                    context.Notifications.Add(notification);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception exception)
            {
                // LOG: exception
                Console.WriteLine("Error adding notfication to SQLite");
                throw;
            }
        }

        // gets persisted notifications
        public List<Notification> GetNotifications()
        {
            var notifications = new List<Notification>();

            try
            {
                using (var context = new NotificationsContext())
                {
                    notifications = (from n in context.Notifications
                                     where n.timestamp >= DateTime.UtcNow.AddDays(-30)
                                     select n).ToList<Notification>();
                }
            }
            catch (Exception exception)
            {
                // LOG: exception
                Console.WriteLine("Error getting notfications from SQLite");
                throw;
            }

            return notifications;
        }

    }
}
