# websocket-notifications
Polymer 2 and .NET Core based websocket notifications app.

Real time notifications are critical for many business needs. Getting important information in real time so you can take quick action can make a big impact on your business and be the difference between happy customers and unhappy customers. There are many options for real time notifications if you have internet and/or cellular connectivity: Web Push Notifications, SMS messaging, native app alerts and even email. But what do you do in an on-prem offline scenario? What happens if you lose internet connectivity and cellular connection? Getting real time alerts is still critical... so here is a cross platform solution to the problem using Websockets and Polymer 2.

## Scenario
So lets summarize the requirements for our scenario.
1. Real time notifications, alerts and warnings
2. Runs in a disconnected / offline environment (no internet / cellular dependencies)
3. Server side runs on any platform (Windows, Linux, etc.)
4. Client side runs in any fairly modern browser and on modern devices (iOS, Android, etc.)
5. Ability to view history of notifications

![Notifications Web App](https://i.imgur.com/7zuqCa8.png)

## Solution tech

I wanted to make sure this solution was lightweight and as browser compatible as possible. So I went with the following underlying technologies:
* Polymer 2
* Websockets
* ASP.NET core
* Sqlite

Let's go through each quickly.

### Polymer 2
Anyone who as done any front end web development knows how quickly frameworks come in and out of favor in the JavaScript world. There's Knockout, Angular, React, Preact, VueJs, etc... just to name a few. The problem with all of them? The code you write is proprietary to the framework, which means if you ever want to use another framework or library, it is a lot of re-write. Thankfully new HTML/ECMAScript specs have added all of the functionality we need to build rich, component based web apps using web standard code. *No proprietary lock in!* Polymer 2 supports this wholeheartedly and just adds some unobtrusive sugar on top of the standards which allows us to [#UseThePlatform](https://twitter.com/hashtag/UseThePlatform?src=hash). 
[Read more about it at the Polymer 2 site.](https://www.polymer-project.org/about)

### Websockets
There are a number of options for communicating updates to a web app. 

There is **long polling**, which is a basically a loop in the browser to call a back-end API to check for new notifications on a specified time interval. Long polling has a huge disadvantage of having to guess how often you need to check for notifications. Checking very frequently is going to result in a lot of API calls. Checking less often introduces a lag been a notification (maybe a critical alert) coming in and someone seeing it. Not good.

[**Web Push Notifications**](https://developers.google.com/web/fundamentals/engage-and-retain/push-notifications/) are pretty awesome. You get real time notifications, queuing if the receiving browser is offline and an opt-in experience for the user. The issue here is that you need a messaging server to act as an intermediary to store the notifications and deliver them when the receiving web app is online. This breaks our requirement of creating a disconnected real time notifications platform.

So let's talk **Websockets**. 
* [Websockets](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API) create an open communication session between a browser and a server. This means our web app can receive event-driven notifications from the server in real time with no polling.
* [Browser compatibility](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API#Browser_compatibility) for the standards version (v13, RFC 6455) is great across the board (even IE11).
* Multiple web apps can subscribe to listen to for notifications; each client will establish a separate web socket to our server, ensuring all web apps receive our notifications.
* There are no internet requirements and we can host our web app and web sockets API in a disconnected on-prem environment.
* One drawback is there is no queuing mechanism, so if the web app isn't open when a notification is triggered, it will not receive it.

For our needs Websockets solves all of our problems and we can live with the requirement of having the web app open to receive notifications.

Here is a nice diagram showing the basic overview of Websocket communication (credit to [PubNub](https://www.pubnub.com/blog/2015-01-05-websockets-vs-rest-api-understanding-the-difference/) for the diagram).

![Websocket communication diagram](https://www.pubnub.com/wp-content/uploads/2014/09/WebSockets-Diagram.png)

### ASP.NET Core
You could write the server side API for notifications in anything really. Node would be a great choice, but I wanted to go with ASP.NET Core as it has some nice built in APIs for working with Websockets.

I specifically chose [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) because it runs on any OS and has a number of advantages over the full .NET Framework.
* Ability to build and run on Windows, macOS, and Linux.
* Lightweight, high-performance, and modular HTTP request pipeline.
* Open-source and community-focused.

### SQLite
[SQLite](https://www.sqlite.org/) is a very nice open source, lightweight, portable SQL based database engine. It is incredibly small, can be used in embedded environments and has a lot of nice features. It also plays very nicely with ASP.NET Core as well as Node.

```javascript
/*
Note: this isn't going to scale. 
The example app deploys a new instances of a SQLite DB 
for notifications to each server side API instance. 
In production, I recommend you use a shared persistence store.
No real need for a relational DB here either, so a NoSQL 
solution like an on-prem MongoDB might be a better fit.
*/
```

## Solution architecture
So our solution architecture is pretty straight forward. 

### Web Server
We have a web server, which will deliver our Polymer 2 [SPA](https://en.wikipedia.org/wiki/Single-page_application) web app to the client's browser.

### Web app
 The client's browser will talk to our ASP.NET Core API, opening a web socket for real time notifications and sending RESTful HTTP requests to get notification history.
 
### Server side API
Our ASP.NET Core API will expose three APIs:
1. Websockets endpoints for real time notifications
2. RESTful GET endpoint to retrieve historical notifications
3. RESTful POST endpoint to accept incoming notifications that need to be communicated to listening web app clients

Our server side API will store all incoming notifications in SQLite and then broadcast them out via open web sockets to any client browsers that are listening.

![Solution architecture diagram](https://i.imgur.com/lDu3Egq.png)

## Code
All of the code for this app is in github at [https://github.com/aaron-schnieder/websocket-notifications](https://github.com/aaron-schnieder/websocket-notifications). 

### Server side API - Websockets & RESTful endpoints
I won't go through the basic plumbing code (models, etc.) but lets take a look at the key parts.

#### Program.cs
Program.cs is the entry point into our server side API that kicks off initialization. One of the key things to pay attention to is UseUrls. Make sure you don't use "http://localhost or http://127.0.0.1" or you will run into issues if you deploy this API in a docker container. You want to make sure that the app binds to whatever local network address the API is hosted on.

```csharp
var host = new WebHostBuilder()
    .UseKestrel()
    .UseContentRoot(Directory.GetCurrentDirectory())
    .UseIISIntegration()
    .UseStartup<Startup>()
    .UseUrls("http://*:8081") // make sure this is NOT localhost, 127.0.0.1, etc.
    .UseApplicationInsights()
    .Build();
```

#### Startup.cs
Startup.cs is the class that instantiates Websockets, SQLite and ASP.NET Web Api.

**Websockets init**

All of the Websockets init happens in the Configure method, which configures the HTTP request pipeline. Notice we are instantiating the use of a custom websockets middleware class.

```csharp
/* WebSockets init options */
var webSocketOptions = new WebSocketOptions()
{
    KeepAliveInterval = TimeSpan.FromSeconds(120),
    ReceiveBufferSize = 4096
};

/* Register the use of web sockets in this app */
app.UseWebSockets(webSocketOptions);

/* Use custom aspnet core middleware to instantiate the web sockets */
app.UseMiddleware<WebSocketsMiddleware>();
```

**SQLite instantiation**

We need a couple of calls to init SQLite.

In the constructor...
```csharp
// Create SQLite DB if it doesn't exist
using (var client = new NotificationsContext())
{
    client.Database.EnsureCreated();
}
```

And then in the ConfigureServices method...
```csharp
// Configure the SQLite DB
services.AddEntityFrameworkSqlite().AddDbContext<NotificationsContext>();
```

**CORS**

We want to **bypass CORS during local development** so we can send browser HTTP requests from localhost. Notice I wrapped the allow all CORS policy in a compiler DEBUG statement so this doesn't make it into production.

```csharp
#if DEBUG
    // Create an open CORS policy ** DON'T PUT THIS IN PRODUCTION **
    services.AddCors(o => o.AddPolicy("AllAllPolicy", builder =>
    {
        builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
    }));
#endif
```

#### WebSocketsMiddleware.cs
Our custom middleware class gives us a handler that is invoked every time an HTTP request is processed. This allows us to intercept requests using the Websockets protocol and open a web socket communication channel with client web apps.

```csharp
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
```

#### NotificationsController.cs
The NotificationsController contains our RESTful APIs.

```csharp
// GET api/notifications
[HttpGet]
public ActionResult Get() {....}
```

```csharp
// POST api/notifications
[HttpPost]
public async Task<ActionResult> Post([FromBody]Notification notification)
```

#### NotificationManager.cs
NotificationManager keeps track of notification subscribers and sends notifications when they are received.

```csharp
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
```
```javascript
/*
Note: another scalability item to point out here. 
For production workloads, this function should just 
write the notification out to a queue. A separate 
worker process should be running to process messages
from the queue, persist the notifications and send them 
to subscribers. As implemented in this example, the 
solution is ripe for data loss.
*/
```

### Web app
There is a lot to look at in the web app, especially if you aren't familiar with [ES6](http://es6-features.org) and [Custom Elements](https://developer.mozilla.org/en-US/docs/Web/Web_Components/Custom_Elements). Don't worry though, it is pretty straight forward once you get into it. I suggest you start with some [helpful tutorials on Polymer 2](https://www.polymer-project.org/2.0/start/toolbox/set-up).

So lets just look at the important stuff specific to the Websocket based notifications. I am not going to cover all of the components in the web app, but I encourage you to look through the code to see how the components are implemented if you are interested.

#### index.html
I added a config value for the path to the API URI to the global Polymer variable. You will need to change this to point to your local or deployed Notifications API server URI.

```javascript
window.Polymer = {
    rootPath: '/',
    apiRoot: 'localhost:8081' // change this to your Notifications API server URI
};
```

#### my-notifications.html
The constructor for this custom element spins up the web socket connection to the server side Notifications API.

```javascript
// set the protocol and path for the Notifications API URI
var protocol = location.protocol === "https:" ? "wss:" : "ws:";
var wsUri = protocol + "//" + Polymer.apiRoot + "/notifications/ws";

// create a new websocket using the URI
var socket = new WebSocket(wsUri);

// open a web socket
socket.onopen = e => {
    this.$.connectionStatusToast.text = "Server connection opened successfully.";
    this.$.connectionStatusToast.open();
    console.log("socket opened", e);
};
```

We also register a handler for incoming messages.

```javascript
// handle new messages received over the websocket
socket.onmessage = this._notificationReceived.bind(this);
```

And our handler for notifications...

```javascript
// handler function when notifications are received
_notificationReceived(e) {

    // parse the message received
    let notification = JSON.parse(e.data);

    try {
        // parse the message in the notification to JSON
        let messageJson = JSON.parse(notification.message);
        notification.message = messageJson;
    } catch(exception) {
        console.log("Error parsing notification message: " + exception);
    }

    // add the notification to the notifications array
    this.push('notifications', notification);

    // set the curentNotification to bring up the toast message
    this.set('currentNotification', notification);
}
```

## POSTing websocket notifications on localhost
After you git clone the repo with the code, its pretty easy to fire things up and test them out.

### Notifications API
To run the Notifications API locally, just do the following.
1. Install [.NET Core](https://www.microsoft.com/net/download/core)
2. Open NotificationsApi/NotificationsApi.sln with Visual Studio 2017
3. Build -> Build Solution
4. Debug -> Start Debugging
5. You should see a browser open up to http://localhost:8081/api/notifications

### Web app
To run the web app locally and connect to your local websockets notifications API...
1. Open a command shell or powershell or terminal window
2. cd into the directory you cloned the repo
3. cd webapp
4. bower install
5. polymer serve
6. You should receive a URI after running polymer serve, open that up in Chrome
7. After you open the web app in Chrome, you should see a toast message pop up letting you know a web socket was connected successfully

### Postman to POST notifications
If you don't use [Postman](https://www.getpostman.com/), you should. Open up Postman and create a new tab with the following configuration:

```json
URI: http://localhost:8081/api/notifications
HTTP Method: POST
Body: raw JSON(application/json)
{
  "id": 128,
  "timestamp": "2017-08-01T16:29:34+00:00",
  "type": "error",
  "message": "{\"errorCode\":20,\"description\":\"Device has failed!!!\"}"  
}
```

You can change the type in the JSON body to warning, information or error and the color of the toast notifications will change accordingly.

![Postman example](https://i.imgur.com/ZkQTDqe.png)