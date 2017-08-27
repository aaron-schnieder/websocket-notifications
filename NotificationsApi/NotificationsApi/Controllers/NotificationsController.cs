using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NotificationsApi.Models;
using NotificationsApi.Notifications;
using NotificationsApi.Persistence;
using System.Net.Http;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NotificationsApi.Controllers
{
    [Route("api/[controller]")]
    public class NotificationsController : Controller
    {
        // GET api/notifications
        [HttpGet]
        public ActionResult Get()
        {
            try
            {
                var notifications = new List<Notification>();
                notifications = PersistenceManager.Instance.GetNotifications();
                return Ok(notifications);
            }
            catch (Exception exception)
            {
                // log exception
                // TODO: implement logging

                // return a 500
                return StatusCode(500);
            }
        }

        // POST api/notifications
        [HttpPost]
        public async Task<ActionResult> Post([FromBody]Notification notification)
        {
            try
            {
                // return a 400 if we didn't get a valid json payload in the body
                if (notification == null)
                    return BadRequest();

                await NotificationManager.Instance.SendNotificationAsync(notification);

                // we aren't returning the object to reference because POSTing a notification is fire and forget
                return Created(string.Empty, null);
            }
            catch (Exception exception)
            {
                // log the error
                // TODO: implement logging

                // return a 500
                return StatusCode(500);
            }
        }
    }
}
