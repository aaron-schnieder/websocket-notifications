using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotificationsApi.Models
{
    public class Notification
    {
        public Guid? notificationId { get; set; }
        public int id { get; set; }
        public DateTime timestamp { get; set; }
        public string message { get; set; }
        public string type { get; set; }

        public Notification()
        {
            // add a new guid as a unique identifier for the notification in the db
            notificationId = Guid.NewGuid();
        }
    }
}
