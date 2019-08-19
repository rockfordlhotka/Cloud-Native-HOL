# Design and Build Message-Based Microservices

In this lab we'll build a message-based service-based system that runs in docker-compose and K8s. It will use RabbitMQ as a messaging platform.

Lesson goals:

1. Use Helm to deploy RabbitMQ
1. Use a gateway server to provide user access to a service-based system
   1. Understand how to implement a "synchronous" user experience to external users
   1. Discuss how SignalR _could_ be used to provide an asynchronous experience to external users
1. Implement message-based services that work together to provide business functionality
1. See how docker-compose provides a convenient developer inner-loop experience
1. Understand how docker-compose.yaml is different from K8s deploy/service definition files
1. Understand compensating transactions
1. Updating a running container in Kubernetes
1. Implement retry policies for potential network failures

## Terminology

Terminology matters a lot when talking about or working with services. A service is a standalone, autonomous, unit of functionality. So is an app. So apps and services are basically the same thing.

But people talk about building a "service-oriented app" or "microservice-based app". That's nonsense, because that would be an app composed of other apps. Intelligent conversation becomes impossible.

Throughout this and subsequent labs the following terms are used:

* **Service-based**: a term used to encompass SOA and microservices without getting into debates about whether they are the same or different
* **Service** or **app**: an autonomous unit of functionality that can be independently deployed from any other part of the overall system
* **Edge app**: an app that exposes some sort of interface to external consumers
* **Service-based system** (aka **system**): a logical boundary within which all apps/services interact using a common messaging protocol
* **External consumers**: browsers, devices, users, or anything outside the service-based system

## Overview of Solution

At a high level the system consists of a series of apps and services:

1. Gateway app/service - provides access to the service-based system for external consumers
1. Sandwichmaker service - makes sandwiches
1. Bread service - maintains inventory of bread, provides bread upon request
1. Cheese service - maintains inventory of cheese, provides cheese upon request
1. Meat service - maintains inventory of meat, provides meat upon request
1. Lettuce service - maintains inventory of lettuce, provides lettuce upon request

The gateway server literally sits on the boundary of the service-based system, and is therefore available to external consumers and it participates with the system as a peer service. Its primary role is to act as a bridge between external web or mobile based communication protocols and the messaging protocol used _inside_ the service-based system.

The gateway app provides web page and API interfaces for use by external consumers. In today's lab the focus will be on the web page UI. 

### Requesting a Sandwich

In the following diagram you can see how users can request a sandwich via their browser.

![sandwichrequest](images/sandwichrequest.png)

The browser postback is handled by the gateway server. The gateway sends a message to the sandwichmaker service asking for a sandwich. The sandwichmaker service sends messages (in parallel) to the resource services to get bread, cheese, meat, and lettuce as needed.

There's a bit of cleverness involved, in that the browser postback is "blocked" using an `await` statement until either a sandwich response comes back or a timeout occurs. In other words, the user sees the normal web browser behavior of the web page waiting for a response.

### Advantages of Queued Messaging

Notice how messages flow from a service to a queue, and are then processed from that queue by the receiving service. This is extremely powerful because it is an inexpensive and reliable way for your service-based system to gain:

1. Scalability - if a service becomes overloaded you can just add more instances of the service to process messages from the queue
1. Fault tolerance - if a service instance crashes other running instances will continue to process queued messages as if nothing happened
1. Reliability - if all running service instances crash the pending messages remain in the queue, so when new service instances start up they'll simply resume acting on the pending workload

### Completing Sandwich Request

Each of the resource services typically responds to a request by returning a message with the requested resource. The sandwichmaker service then assembles all the resources into a single response message that it sends to the service that requested a sandwich.

![sandwich response](images/sandwichresponse.png)

The gateway service has its own queue, so when the sandwichmaker finishes its work and sends a response, that response ends up in the gateway service's queue. The gateway service has a listener running as an ASP.NET Core background task, so it picks up that reply and matches it to the specific user request that originally requested the sandwich. 

When the response arrives on the gateway server and is matched to the original browser postback request, the background service provides the response message to that postback request and unblocks the postback request so it can process the response and generate appropriate output for the browser.

### What if I Don't Want to Block the User?

The implementation in this lab provides the end user with a standard browser-style experience. They fill in a web form, click a button, and the browser doesn't render anything new until there's some sort of response (a sandwich or a timeout).

In modern web user experiences it is increasingly the case that such a scenario is implemented so the browser refreshes _immediately_ upon the user's button click, typically showing the user a message saying that their request has been submitted and they'll be notified when it completes.

It is possible to implement such a scenario using standard web-based technologies. Most commonly this would be done using SignalR to provide async messaging between the web server (gateway app) and the user's browser.

This is not part of today's lab work, but we mention it here so you know that such an implementation is possible.

### What if the Request Fails?

The workflow described so far has assumed everything works as expected. The sandwichmaker service asks for bread, cheese, meat, and lettuce, and gets back everything necessary to make a sandwich.

But what if the lettuce service runs out of lettuce?

It is important to recognize that this is not a failure case for the lettuce service! There's nothing exceptional about having or not having lettuce, both are perfectly valid scenarios. As a result, the lettuce service still responds to the inbound request for lettuce, but in this case it responds by indicating that it has no lettuce.

This implies that any service sending a request to the lettuce service needs to handle a response that provides lettuce _and_ a response that indicates no lettuce is available. That's not hard to envision, and isn't terribly complex.

_However_, there is complexity here! Because sandwichmaker requests bread, meat, cheese, and lettuce _in parallel_. So when it finds out there's no lettuce, it may already have other resources that it can no longer use.

You can image this scenario in real life. A restaurant cook is making a sandwich and has pulled out bread, meat, and cheese onto the workspace. And then they discover there's no lettuce. So what do they do? They put the meat, bread, and cheese back in the bins, tell the waiter that they can't make the requested sandwich, and move on to other work.

The same is true in the digital world. This whole process is called a _compensating transaction_. Another important term is _saga_. The process of making a sandwich, from the initial request from the gateway service until everything is complete is called a saga. One outcome of a saga is complete success. Another outcome is that, based on business rules and decisions, steps are taken to deal with failure.

In this lab a compensating transaction will be used so when the lettuce service indicates there's no lettuce the sandwichmaker service will send messages to the bread, meat, and cheese services - returning the unused inventory items.

This implies that each resource service can not only provide a resource upon request, but that it can also handle a message indicating that a resource is being returned.

## Debugging in docker-compose

Developing in a container-based environment can be complex. It can be challenging to get your dev/debugging environment set up such that you can debug one or more services, while those services are all interacting with each other.

On a small scale it is often easiest to work with docker-compose, because Visual Studio understands how to interact with Docker Desktop and docker-compose within the familiar "F5 dev experience".

> ℹ It is a fairly safe bet to think that Visual Studio will become more integrated with Kubernetes over time. Already Azure Dev Spaces exist, and innovation within the K8s space is occurring at a breakneck pace.

For this lab you'll learn how to run a set of services in docker-compose from Visual Studio, and later you'll learn how to deploy those services into a K8s cluster.

### Configure RabbitMQ for docker-compose

Before running your services in docker-compose it is necessary to set up a RabbitMQ server for use by those services.

1. Create a docker network for the demo
    1. `docker network create -d bridge --subnet 172.25.0.0/16 demonet`
1. Install rabbitmq in the docker environment
    1. `docker run -d rabbitmq`
    1. Use `docker ps` to get the id of the container
    1. Add rabbitmq to the demonet network
        1. `docker network connect demonet <container id>`
    1. Find the rabbitmq container's ip address in the network
        1. `docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' <container id>`
        1. You'll see two addresses, choose the 172.25.0.??? address from the demonet network

At this point a RabbitMQ container is running in Docker Desktop on your workstation, and you have made note of the container's IP address _inside Docker_.

## RabbitMQ Helper Code

In the `src/Lab03/Start` directory you'll see a pre-existing solution that implements most of the service-based system described earlier in this document. Open that solution. Look for the `RabbitQueue` project and examine the files in that project.

You will be using the types defined in this project to implement the gateway and bread services.

### AsyncManualResetEvent

The `AsyncManualResetEvent` class is an implementation of the `ManualResetEvent` thread locking primitive that is based on the `async`/`await` functionality in modern .NET. Rather than relying on a low-level operating system lock, this implementation relies on the way tasks work in .NET.

The code comes from Stephen Toub @ Microsoft: [Building Async Coordination Primitives](https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-1-asyncmanualresetevent/).

### Queue

The `Queue` class is a simple abstraction over the RabbitMQ API provided by the `RabbitMQ.Client` NuGet package.

A number of lines of code are necessary to open a queue and connection, to prepare for and send a message, and to set up an async listener to process messages as they arrive in a queue. Rather than repeating these blocks of code throughout every service implementation they are centralized in this class.

## Implement Gateway Service/App

The `Gateway` project in the solution is very similar to the project you created in Lab02, in that it is an ASP.NET Core Razor Pages web project configured to work with Docker.

### Enabling docker-compose

Visual Studio can help you add docker-compose support to a solution. Right-click on the Gateway project and select *Add|Container Orchestrator Support*.

![](images/add-docker-compose.png)

You will be asked which container orchestrator to use. If you have a choice, choose docker-compose.

You will be asked to choose the Target OS for your containers. Choose Linux.

> ℹ Windows containers are a viable alternative, but modern server development is moving rapidly toward Linux containers.

The wizard will next ask if you want to overwrite the existing `Dockerfile` for the Gateway project. It is recommended to allow the overwrite.

Once the wizard completes you should see a new node in Solution Explorer:

![](images/docker-compose-node.png)

The `docker-compose.yml` file contains the configuration for docker-compose, telling docker-compose what services to run and how to run them.

```yaml
version: '3.4'

services:
  gateway:
    image: ${DOCKER_REGISTRY-}gateway
    build:
      context: .
      dockerfile: Gateway/Dockerfile
```

Visual Studio understands this file and adds an option to run your web project in *Docker Compose* as well as the pre-existing options to run in *Docker* or to run the project directly in a console window.

Earlier in this lab you started a RabbitMQ container in Docker, and attached it to a virtual network named `demonet`. It is necessary to tell docker-compose about that virtual network. Add the following to the `docker-compose.yml` file above the `services:` node:

```yaml
networks:
  default:
    external:
      name: demonet
```

One of the more important of the [12 Factors](https://12factor.net) is that configuration should come from the environment, not from a config file or any other asset that might be in source control. In the docker-compose environment this configuration comes from the `docker-compose.yml` file. Edit the `gateway:` node as shown here:

```yaml
  gateway:
    image: ${DOCKER_REGISTRY-}gateway
    build:
      context: .
      dockerfile: Gateway/Dockerfile
    environment: 
      - RABBITMQ__URL=172.25.0.9
      - RABBITMQ__USER
      - RABBITMQ__PASSWORD
```

> ⚠ Make sure to use the IP address of _your_ RabbitMQ instance.

The `environment:` node provides a list of environment variables to be set in each container as it is initialized. These values can be easily retrieved by .NET Core code using the modern configuration subsystem using the default configuration for ASP.NET Core.

In other words, your ASP.NET Core code can easily access the three variables defined here, though in this lab only the URL value is important, because the RabbitMQ instances are using the default user/password values.

### Examine the WorkInProgress Class

Open the `Services/WorkInProgress.cs` file. This simple code exists because it is necessary to keep track of outstanding user requests (browser postback requests) that have requested a sandwich. Remember the overall workflow:

1. User requests a sandwich in their browser
1. Gateway app handles postback, sending a request to the sandwichmaker service
   1. Gateway app "blocks", not sending the browser a response until getting a response or a timeout
1. Sandwichmaker service makes the sandwich and sends a reply message to the gateway service
1. Gateway service receives the reply, and needs to match it to the user request in step 2

This is where the code you are examining becomes important. It is nothing more than a dictionary of work items, indexed by a unique *correlation id* value. This correlation id value is also used to knit together all the work of making a sandwich: the overall saga being implemented.

The `Lock` property maintains a reference to an `AsyncManualResetEvent`. The user postback code will be awaiting this event until the gateway service sets the event because it got a reply from the sandwichmaker service. Before setting the lock though, the gateway service sets the `Response` property so it contains the reply from the sandwichmaker service.

This workflow will become more clear as you implement the code to send and receive messages, as that code will make use of this work in progress implementation.

The `WorkInProgress` instance is made available to other code in the `Gateway` project via dependency injection. In the `Startup` class's `ConfigureServices` method there's a singleton declaration for the type:

```c#
      services.AddSingleton<Services.IWorkInProgress>((e) => new Services.WorkInProgress());
```

Classes requiring access to work in progress information can gain access to this singleton via standard .NET Core dependency injection.

### Implement Service Interfaces

The gateway app accepts inbound postback requests from browsers as each user asks for a sandwich to be created. The gateway service must relay this request from the external consumer into the service-based system. Specifically, it needs to send a message to the sandwichmaker service asking for the sandwich.

Before implementing code in the gateway app/service, it is necessary to define a couple POCO message definition classes and an ASP.NET Core service interfaces that'll be used by the gateway code.

#### Message Definitions

Examine the `SandwichRequest` class in the `Messages` folder of the `Gateway` project.

This is the message that will be sent to the sandwichmaker service to request a new sandwich.

Examine the `SandwichResponse` class in the `Messages` folder.

This is the message that will be sent from the sandwichmaker service to the gateway service to report on whether a sandwich was or was not made as requested.

Remember that most response messages must not only provide _success_ information, but also _failure_ information. It isn't at all exceptional or invalid for the sandwichmaker to be unable to make a sandwich, so success and failure are both perfectly reasonable responses.

#### Service Interface Definitions

Then add a `cs` file defining an `ISandwichRequestor` interface to the `Services` folder in the `Gateway` project:

```c#
using System.Threading.Tasks;

namespace Gateway.Services
{
    public interface ISandwichRequestor
    {
        Task<Messages.SandwichResponse> RequestSandwich(Messages.SandwichRequest request);
    }
}
```

Later you will implement a class that sends a request to make a sandwich into the service-based system. This interface defines the method that you'll implement, but right now the interface makes it possible to implement the code that'll ultimately call that implementation.

### Implement the Index Page

The next step in this process is to implement the `Index` page in the Gateway project.

The markup for the page is already in the project, as it is basic Razor code:

```html
@page
@model Gateway.Pages.IndexModel
@{
  ViewData["Title"] = "Sandwich";
}

<h2>Sandwich</h2>

<div class="row">
  <h3>Select ingredients</h3>
  @using (Html.BeginForm())
  {
    <div>Meat</div>
    <input asp-for="TheMeat" />
    <div>Bread</div>
    <input asp-for="TheBread" />
    <div>Cheese</div>
    <input asp-for="TheCheese" />
    <div>Lettuce?</div>
    <input asp-for="TheLettuce" />
    <br /><br />
    <input type="submit" name="sendMessage" value="Ask cook to make sandwich" />
  }
  <p></p>
  <p>Reply from sandwich maker:</p>
  <div style="font-size:24px">@Model.ReplyText</div>
</div>
```

Some of the code behind in `Index.cshtml.cs` is already in the project as well, establishing the properties that are data bound to the Razor markup.

What isn't yet in the class is a constructor that obtains an `IServiceRequestor` instance via dependency injection. Add this field declaration and constructor to the class:

```c#
    readonly Services.ISandwichRequestor _requestor;

    public IndexModel(Services.ISandwichRequestor requestor)
    {
      _requestor = requestor;
    }
```

Using this `_requestor` field it is possible to implement the `OnPost` method:

```c#
    public async Task OnPost()
    {
      var request = new Messages.SandwichRequest
      {
        Meat = TheMeat,
        Bread = TheBread,
        Cheese = TheCheese,
        Lettuce = TheLettuce
      };
      var result = await _requestor.RequestSandwich(request);

      if (result.Success)
        ReplyText = result.Description;
      else
        ReplyText = result.Error;
    }
```

This method creates a `SandwichRequest` message object, populating it with the values provided from the Razor markup via data binding.

It then invokes the `RequestSandwich` method, awaiting the response from the service-based system.

Once a response is available, the data bound `ReplyText` UI control is updated to reflect the success or failure result from the sandwichmaker service.

### Implement the SandwichRequestor Service

The next step is to implement the `SandwichRequestor` service based on the `ISandwichRequestor` interface.

#### SandwichRequestor Class

Add a `SandwichRequestor` class to the `Services` folder with the following code:

```c#
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using RabbitQueue;

namespace Gateway.Services
{
  public class SandwichRequestor : ISandwichRequestor
  {
    readonly IConfiguration _config;
    readonly IWorkInProgress _wip;

    public SandwichRequestor(IConfiguration config, IWorkInProgress wip)
    {
      _config = config;
      _wip = wip;
    }

    public async Task<Messages.SandwichResponse> RequestSandwich(Messages.SandwichRequest request)
    {
      var result = new Messages.SandwichResponse();
      var requestToCook = new Messages.SandwichRequest
      {
        Meat = request.Meat,
        Bread = request.Bread,
        Cheese = request.Cheese,
        Lettuce = request.Lettuce
      };
      var correlationId = Guid.NewGuid().ToString();
      var lockEvent = new AsyncManualResetEvent();
      _wip.StartWork(correlationId, lockEvent);
      try
      {
        using (var _queue = new Queue(_config["rabbitmq:url"], "customer"))
        {
          _queue.SendMessage("sandwichmaker", correlationId, requestToCook);
        }
        var messageArrived = lockEvent.WaitAsync();
        if (await Task.WhenAny(messageArrived, Task.Delay(10000)) == messageArrived)
        {
          result = _wip.FinalizeWork(correlationId);
        }
        else
        {
          result.Error = "The cook didn't get back to us in time, no sandwich";
          result.Success = false;
        }
      }
      finally
      {
          _wip.FinalizeWork(correlationId);
      }

      return result;
    }
  }
}
```

This class relies on dependency injection to gain access to the `IConfiguration` instance for the app, and the `IWorkInProgress` singleton instance.

You can see how the .NET configuration subsystem is used to retrieve the URL of the queue. Remember that the value is coming from an environment variable with the name `RABBITMQ__URL` (yes, two underscores). The .NET Core configuration subsystem translates that name into a dictionary key value of `rabbitmq:url`.

It then implements the `RequestSandwich` method that sends the request message to the sandwichmaker service. Some key things to note in this method:

1. The `correlationId` is a unique identifier for the "create a sandwich" saga, and is used throughout the system to coordinate all the work necessary to attempt creation of a sandwich and return the result to the gateway service
1. The `lockEvent` object is what's used to ensure that the postback request doesn't return to the browser until success, failure, or timeout
1. This code uses the `Queue` type from the `RabbitQueue` project to easily interact with RabbitMQ to send the message
1. The `try..finally` block ensures that, even if an exception occurs, the work in progress list is cleaned up regardless of how this saga completes

#### Blocking the Postback

Perhaps the two lines of code that are hardest to understand are these:

```c#
        var messageArrived = lockEvent.WaitAsync();
        if (await Task.WhenAny(messageArrived, Task.Delay(10000)) == messageArrived)
```

The way .NET implements async/await is not well understood by most developers. You may fully understand what's going on here, in which case you can skip this bit of discussion.

The `AsyncManualResetEvent` type provides a locking mechanism that doesn't rely on any underlying operating system locks. Instead it is implemented in a way that relies on the very nature of the Task Parallel Library (TPL) and async/await implementation in .NET.

The first line of code gets the `Task` object representing the `lockEvent` lock. It is possible to directly await this task, but that wouldn't allow for a timeout. Clearly it is necessary to have some sort of timeout, otherwise the user's web page would be waiting forever for a result.

The second line of code awaits two tasks: the `lockEvent` task and a `Delay` task that acts as a timeout. The `WhenAny` method returns when _either_ of these tasks complete.

The puzzling part of this code isn't the workflow, that's fairly clear. The puzzling part is that this code is reasonable to run on a web server.

People's first instant when seeing some sort of "lock" in web server code is to worry about scalability and consumption of the ASP.NET thread pool. Very reasonable concerns to be sure!

This code is implemented in a way that mirrors exactly what happens when you await a call to a database from ASP.NET code. In other words, it has the same scalability and thread pool consumption characteristics as if it was awaiting a database query via ADO.NET, Entity Framework, or Dapper.

In all cases the code is waiting for a callback via some sort of network IO, and is released when that callback occurs (from the database, or in this case from the RabbitMQ listener).

#### Adding the Service to IServiceCollection

With the `SandwichRequestor` service implemented, it is possible to make it available for use in the `Index` page via dependency injection. This is done by adding some code to the `Startup` class:

```c#
      services.AddSingleton<Services.ISandwichRequestor>((e) =>
          new Services.SandwichRequestor(
              e.GetService<IConfiguration>(), 
              e.GetService<Services.IWorkInProgress>()));
```

Because `SandwichRequestor` relies on dependency injection as it is created it is necessary to resolve the `IConfiguration` and `IWorkInProgress` instances as shown here.

At this point you have all the code necessary to send a request from the user through the `Index` page and the `ServiceRequestor` type to the sandwichmaker service.

### Implement the SandwichmakerListener Hosted Service

When the sandwichmaker service is complete, having succeeded or failed in making a sandwich, it sends a message to the gateway service to provide the result. This means that the gateway service needs to always be listening for messages that arrive on the gateway service's queue.

ASP.NET Core supports this concept via something called a Hosted Service. This is a type of ASP.NET Core service that starts as the web site is launched, and keeps running as long as the server is online.

There are many ways to implement and use a Hosted Service. In this lab you'll implement one type of service that is always awaiting messages that arrive in a queue.

#### SandwichmakerListener Class

Add a `SandwichmakerListener` class in the `Services` folder with the following code:

```c#
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using RabbitQueue;
using Microsoft.Extensions.Configuration;

namespace Gateway.Services
{
  public class SandwichmakerListener : IHostedService
  {
    readonly IConfiguration _config;
    readonly IWorkInProgress _wip;
    private readonly Queue _queue;

    public SandwichmakerListener(IConfiguration config, IWorkInProgress wip)
    {
      _config = config;
      _wip = wip;
      _queue = new Queue(_config["rabbitmq:url"], "customer");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _queue.StartListening<Messages.SandwichResponse>((ea, response) =>
      {
        _wip.CompleteWork(ea.BasicProperties.CorrelationId, response);
      });

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _queue.Dispose();

      return Task.CompletedTask;
    }
  }
}
```

This class relies on dependency injection to gain access to the configuration and work in progress services. In the constructor it also uses the `Queue` class to gain access to the gateway service's queue.

The `IHostedService` interface from ASP.NET requires that the class implement `StartAsync` and `StopAsync` methods. These are invoked by the runtime as the service starts up and shuts down.

The `StopAsync` method simply disposes the `Queue` instance to properly close down.

The `StartAsync` method is more interesting, as this method creates an event hook to process messages in the gateway service's queue. The `StartListening` method invokes a callback method for each message as it arrives.

Inside the callback code the work in progress item is updated via the `CompleteWork` method. Notice that the correlation id value is used to identify the saga (and thus postback browser request) to which this message relates. Also remember that the `CompleteWork` method calls the `Set` method of the `AsyncManualResetEvent` to release the browser postback code so it can return the result to the user's browser.

#### Adding the Service to IServiceCollection

A Hosted Service is added to ASP.NET Core in the `Startup` class like any other dependency injection service. Add the following line of code to the `ConfigureServices` method:

```c#
      services.AddHostedService<Services.SandwichmakerListener>();
```

As the web server starts up it'll automatically create and start an instance of this type.

At this point your code can send and receive messages, enabling full interaction with the sandwichmaker service.

### Examine the SandwichController Class

In the `Gateway` project's `Controllers` folder you'll find a `SandwichController` class. You can uncomment this code, as all the types it uses how exist in your project. The controller code should look like this:

```c#
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class SandwichController : ControllerBase
  {
    readonly Services.ISandwichRequestor _requestor;

    public SandwichController(Services.ISandwichRequestor requestor)
    {
      _requestor = requestor;
    }

    [HttpGet]
    public string OnGet()
    {
      return "I am running; use PUT to make a sandwich";
    }

    [HttpPut]
    public async Task<Messages.SandwichResponse> OnPut(Messages.SandwichRequest request)
    {
      return await _requestor.RequestSandwich(request);
    }
  }
}
```

This lab will not use this controller, but it is worth examining to see how the same types that support the `Index` page can also be used to create an API controller. You can see how this controller supports unknown callers as long as they pass a valid message via an HTML `PUT` call.

## Examine the Sandwichmaker Service

At this point you have a gateway app/service that interacts with the sandwichmaker service. This is a fairly complex service, as it not only interacts with the gateway service, but also with the resource services (for bread, cheese, meat, and lettuce).

You will implement the bread resource service from scratch, but first you should understand key aspects of the sandwichmaker service code.

### Startup

As you'll see when you implement the bread service later, these services are all implemented as console apps. They just listen for, and send, messages to queues, and so there's no need for all the overhead and complexity of ASP.NET. This is very typical of service-based systems.

A major benefit of running code in containers is the high density of services per physical server. Containers themselves require very little memory or overhead, and it is important that your code also use as little memory or resources as possible. You can keep your memory footprint as small as possible by avoiding the use of runtime frameworks like ASP.NET when possible.

Because the sandwichmaker service is a console app, the first code that is run is the `Main` method:

```c#
    static async Task Main(string[] args)
    {
      var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

      if (_queue == null)
        _queue = new Queue(config["rabbitmq:url"], "sandwichmaker");

      Console.WriteLine("### SandwichMaker starting to listen");
      _queue.StartListening(HandleMessage);

      // wait forever - we run until the container is stopped
      await new AsyncManualResetEvent().WaitAsync();
    }
```

This code is interesting, because it leverages the same .NET Core configuration subsystem as ASP.NET Core, but explicitly:

```c#
      var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();
```

Where ASP.NET Core defaults to loading configuration values from numerous sources (web.config, environment, console parameters, etc.), this code is only loading configuration from the environment.

For debug and testing purposes you might also choose to load config from console parameters, but at runtime config should always flow from the environment.

The code then starts listening for messages to arrive on the sandwichmaker queue. Just like with the `SandwichmakerListener` in the `Gateway` project, each message is handed off to a method for handling.

The final line of code simply waits on a lock that'll never be released. This is because this service should run forever, or until its hosting container is terminated.

### Message Classes

Open the `Messages` folder and notice all the classes. These are all the message types that the sandwichmaker service knows how to send and recieve.

These types are the _exact same types_ used in the other services in the system. Many people's first instinct is to put these types into a central Class Library project, and reference that common assembly from all the services.

> ℹ **THAT WOULD BE A BAD DESIGN CHOICE!!**

Perhaps the most important characteristic of any service-based system is that each service or app can be deployed independently from any other services or apps.

If all services are coupled to a common "master document format" or "master message definition" assembly, the result is that any service that needs to expand or change its message type will force all other services to also adopt/accept those changes.

There are various ways to overcome this issue. You _can_ use a common assembly for message types, as long as you are _extremely_ careful to do so in a way that allows dependent projects (all of your services) to only accept changes when they want the changes. That is complex and requires a lot of human-centric process and coordination.

The _easier_ solution is to avoid having any common assembly across all your services, and have each service maintain its own message definitions.

### Handling Inbound Messages

The sandwichmaker service receives different types of message from numerous other services within the overall system. As a result it needs to examine each message to determine what to do with the message:

```c#
    private static void HandleMessage(BasicDeliverEventArgs ea, string message)
    {
      switch (ea.BasicProperties.Type)
      {
        case "SandwichRequest":
          RequestIngredients(ea, message);
          break;
        case "MeatBinResponse":
          HandleMeatBinResponse(ea, message);
          break;
        case "BreadBinResponse":
          HandleBreadBinResponse(ea, message);
          break;
        case "CheeseBinResponse":
          HandleCheeseBinResponse(ea, message);
          break;
        case "LettuceBinResponse":
          HandleLettuceBinResponse(ea, message);
          break;
        default:
          Console.WriteLine($"### Unknown message type '{ea.BasicProperties.Type}' from {ea.BasicProperties.ReplyTo}");
          break;
      }
    }
```

Fortunately RabbitMQ (using the [AMQP protocol](https://www.amqp.org/)) has provisions for passing metadata such as the message type along with the message payload. This is part of the abstraction implemented in the `RabbitQueue` project's `Queue` class.

Notice that when an unknown message arrives it is handled by writing a message to the console: to stdout for Linux-savvy folks. This is standard practice when building container-based apps. In all major container runtimes stdout and stderr are _expected_ to be used for log output from code running in the container.

You might also use other logging frameworks and techniques, but writing to stdout is generally considered a good practice.

Messages that _are_ recognized are routed to methods that handle the specific message type. For example, when you implement the bread service you'll be sending messages to the sandwichmaker service, and they'll be handled in this `HandleBreadResponse` method:

```c#
    private static void HandleBreadBinResponse(BasicDeliverEventArgs ea, string message)
    {
      Console.WriteLine("### SandwichMaker got bread");
      if (!string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId) && 
        _workInProgress.TryGetValue(ea.BasicProperties.CorrelationId, out SandwichInProgress wip))
      {
        var response = JsonConvert.DeserializeObject<Messages.BreadBinResponse>(message);
        wip.GotBread = response.Success;
        SeeIfSandwichIsComplete(wip);
      }
      else
      {
        // got Bread we apparently don't need, so return it
        Console.WriteLine("### Returning unneeded Bread");
        _queue.SendReply("breadbin", null, new Messages.BreadBinRequest { Returning = true });
      }
    }
```

This code attempts to match the inbound message to existing work in progress (yes, the sandwichmaker service also needs to track work in progress). 

If an existing in progress item is found then response from the bread service is attached to the work in progress item, and then a method is called to determine if the sandwich is complete.

No matching work in progress item might exist. In reality this shouldn't happen, but it is wise to write defensive code. Such a message indicates that some orphan request was handled, and the sandwichmaker service has no outstanding work in progress for the response. 

```c#
  _queue.SendReply("breadbin", null, new Messages.BreadBinRequest { Returning = true });
```

This is part of the compensating transaction implementation. The desired business behavior is that if the bread service provides bread to the sandwichmaker service, and that bread can't be used, it is to be returned to the bread service for future use.

### Work in Progress Tracking

As mentioned earlier, the sandwichmaker service needs to track work in progress. This is because it will be handling many concurrent requests to make sandwiches, and it needs to keep track of which are in progress, which are complete, and which can't be completed.

In this service the work in progress information is maintained in a dictionary:

```c#
    private static readonly ConcurrentDictionary<string, SandwichInProgress> _workInProgress =
      new ConcurrentDictionary<string, SandwichInProgress>();
```

Like in the gateway service, the key here is the correlation id for the "make a sandwich" saga that started when the user requested a sandwich.

The data required to track the work required to make a sandwich is fairly complex. You can look at the `SandwichInProgress` class to see what is tracked.

Remember that the sandwichmaker service will request all necessary resources _in parallel_, and there's no way to predict the order of responses from those other services. As a result, as each response does arrive it is recorded in a `SandwichInProgress` object, indexed by the saga's correlation id value.

The `IsComplete` and `Failed` properties implement business logic based on the responses that have arrived at any point in time.

The `IsComplete` property only returns `true` if all four resource services have responded, regardless of whether the response is one of success or failure.

The `Failed` property returns `true` when all four resource services have responded and one or more of the response messages indicate failure.

### Requesting Ingredients (Resources)

The sandwichmaker service handles the `SandwichRequest` message sent from the gateway service (or any other caller really). When such a message is received the result is that a request is sent to each resource service, in parallel, to ask for the required bread, cheese, meat, and lettuce.

The `RequestIngredients` method in the `SandwichMaker` class implements this behavior.

The method sets up a work in progress item to track the process of making the requested sandwich. Then it sends messages to each resource service.

### Compensating Transaction Implementation

The `SeeIfSandwichIsComplete` method is responsible for determining whether the sandwichmaker service has successfully made the requested sandwich.

If the sandwich is complete a success message is sent to the service that requested the sandwich (the gateway service in this implementation). Similarly, if the sandwich couldn't be made a message is also sent to tell the calling service that they can't have their sandwich.

In the case of failure however, it is necessary to implement a compensating transaction. If one or more ingredients aren't available it is very likely that the sandwichmaker service has possession of _other_ resources it no longer needs.

The following code sends messages to the appropriate resource services, telling them to restore ingredients to their inventory for future use:

```c#
  if (wip.GotMeat.HasValue && wip.GotMeat.Value)
    _queue.SendMessage("meatbin", wip.CorrelationId, new Messages.MeatBinRequest { Meat = wip.Request.Meat, Returning = true });
  if (wip.GotBread.HasValue && wip.GotBread.Value)
    _queue.SendMessage("breadbin", wip.CorrelationId, new Messages.BreadBinRequest { Bread = wip.Request.Bread, Returning = true });
  if (wip.GotCheese.HasValue && wip.GotCheese.Value)
    _queue.SendMessage("cheesebin", wip.CorrelationId, new Messages.CheeseBinRequest { Cheese = wip.Request.Cheese, Returning = true });
  if (wip.GotLettuce.HasValue && wip.GotLettuce.Value)
    _queue.SendMessage("lettucebin", wip.CorrelationId, new Messages.LettuceBinRequest { Returning = true });
```

In service-based systems compensating transactions are the normal way this sort of problem is resolved. Keep in mind that this isn't just a technical implementation detail, it is a key part of the overall business process.

## Implement Bread Service

Now that you've seen how the more complex sandwichmaker service is implemented it is time to implement a service from scratch. The resource services are much simpler, but still demonstrate all the key aspects of service implementation.

### Create the Project

Add a new .NET Core Console App project to the solution named `BreadService`.

It needs the following NuGet references:

* `Newtonsoft.Json`
* `RabbitMQ.Client`
* `Microsoft.Extensions.Configuration.CommandLine`
* `Microsoft.Extensions.Configuration.EnvironmentVariables`

It needs the following project references:

* `RabbitQueue`

At this point you might be asking why it is OK to reference the RabbitQueue project when earlier we so strongly recommended _against_ referencing a common message definition assembly.

The difference is that the `RabbitQueue` project, much like .NET itself, or `Newtonsoft.Json`, contain no business logic. They are "horizontal frameworks" in that they cut across many apps or services, providing common platform-level functionality.

It is important to minimize or avoid reuse of _vertical_ code: code that is part of any given business implementation. That includes UI components, viewmodels, controllers, message or document definitions, and data access layers. Those are all examples of app-specific code, where reuse will almost certainly lead to coupling. And coupling prevents independent deployment of services and apps - this defeating the primary value of being service-based.

### Setting the Compiler Version

> ⚠ You can skip this step if you are using Visual Studio 2019.

In Visual Studio 2017 the default C# compiler version is probably lower than version 7.1. However this lab uses code which relies on version 7.1 features.

Edit the `BreadService.csproj` file and add this line to the `PropertyGroup` section:

```xml
    <LangVersion>7.1</LangVersion>
```

That'll require the use of the version 7.1 compiler.

### Message Definitions

Create a `Messages` folder in the project. As with the other services, this one will follow the "shared nothing" strategy to avoid coupling.

Add a `BreadBinRequest` class:

```c#
namespace Messages
{
  internal class BreadBinRequest
  {
    public string Bread { get; set; }
    public bool Returning { get; set; }
  }
}
```

Add a `BreadBinResponse` class:

```c#
namespace Messages
{
  internal class BreadBinResponse
  {
    public bool Success { get; set; }
  }
}
```

The request comes from some other service that needs bread (or needs to return unused bread). The response message is how the bread service indicates success or failure.

### Service Implementation

Like the sandwichmaker service, this is a console app, and so the entrypoint for the code is the `Main` method. This method needs to load configuration, create the `Queue` object, and start listening for inbound messages.

It will need these `using` statements:

```c#
using Microsoft.Extensions.Configuration;
using RabbitQueue;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
```

The `Program` class needs to contain this code:

```c#
    private static Queue _queue; 

    static async Task Main(string[] args)
    {
      var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

      if (_queue == null)
        _queue = new Queue(config["rabbitmq:url"], "breadbin");

      Console.WriteLine("### Bread bin service starting to listen");
      _queue.StartListening<Messages.BreadBinRequest>(HandleMessage);

      // wait forever - we run until the container is stopped
      await new AsyncManualResetEvent().WaitAsync();
    }
```

In a real app the inventory would be maintained in a database. For this lab the inventory will be maintained in memory. 
Add this class-level field:

```c#
    private volatile static int _inventory = 10;
```

To avoid any potential issues with shared state, the inventory value is maintained as a `volatile` value. Were the value being retrieved from a database on request this wouldn't be an issue because no shared/`static` state would be necessary.

The `HandleMessage` method is invoked for each message received from the queue. Add this method to the `Program` class:

```c#
    private static void HandleMessage(BasicDeliverEventArgs ea, Messages.BreadBinRequest request)
    {
      var response = new Messages.BreadBinResponse();
      lock (_queue)
      {
        if (request.Returning)
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - returned");
          _inventory++;
        }
        else if (_inventory > 0)
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - filled");
          _inventory--;
          response.Success = true;
          _queue.SendReply(ea.BasicProperties.ReplyTo, ea.BasicProperties.CorrelationId, response);
        }
        else
        {
          Console.WriteLine($"### Request for {request.GetType().Name} - no inventory");
          response.Success = false;
          _queue.SendReply(ea.BasicProperties.ReplyTo, ea.BasicProperties.CorrelationId, response);
        }
      }
    }
```

Notice that a `lock` statement is used to prevent multi-threading reentrancy issues with this code. Again, this is only necessary due to the use of shared state (the `_inventory` field). In most service implementations the `HandleMessage` method will be only interacting with fields scoped to the method, so reentrancy is not a concern.

There are two possible workflows to handle. 

One is that the caller is returning bread to inventory, in which case the inventory quantity is incremented. 

The other is that the caller is requesting bread. In this case the inventory is checked. If there's bread in inventory the value is decremented and a success response is sent. Otherwise a failure message is sent so the caller knows no bread is available.

Although this implementation is simple, you should be able to see how the `HandleMessage` method could be much more complex, interacting with databases or other services (as shown in the sandwichmaker service).

### Adding Docker Support

Adding Docker support to a Console App project requires adding a `Dockerfile` and editing the `csproj` file.

#### Visual Studio 2019

Visual Studio 2019 provides support for adding Docker support to a Console App project as it does for a web project.

![add docker](images/add-docker-console.png)

Selecting this option, and choosing Linux, will result in Visual Studio adding a `Dockerfile` to your project.

#### Visual Studio 2017

If you are using Visual Studio 2017 you'll have to do this process manually.

Add a file named `Dockerfile` to the project with the following contents:

```docker
FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY BreadService/BreadService.csproj BreadService/
COPY RabbitQueue/RabbitQueue.csproj RabbitQueue/
RUN dotnet restore BreadService/BreadService.csproj
COPY . .
WORKDIR /src/BreadService
RUN dotnet build BreadService.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish BreadService.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "BreadService.dll"]
```

Then edit the `csproj` file and add the following to the `PropertyGroup` node:

```xml
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
```

At this point your project can be built into a Docker container.

### Adding docker-compose Support

The docker-compose environment provides a useful way to run and debug a set of related services and apps. As discussed earlier today, the `docker-compose.yml` file in the docker-compose node in Solution Explorer defines all the services that are to be hosted by docker-compose.

Make sure this file contains an entry for the new `breadservice`:

```yaml
  breadservice:
    image: ${DOCKER_REGISTRY-}breadservice
    build:
      context: .
      dockerfile: BreadService/Dockerfile
    environment: 
      - RABBITMQ__URL=172.25.0.9
      - RABBITMQ__USER
      - RABBITMQ__PASSWORD
```

At this point your `docker-compose.yml` file contains entries only for the `gateway` and `breadservice` services. In reality it needs entries for all the services necessary to run the system in your local environment.

Making note of the IP address for your RabbitMQ instance, copy the `docker-compose.yml` file from the `End` directory into your `Start` directory, replacing the current file. The result is a `docker-compose.yml` that has entries for all the services in the system.

> ⚠ **IMPORTANT:** Replace the IP addresses for `RABBITMQ__URL` with the IP address for your RabbitMQ instance within Docker. This needs to be done for all the services in the file.

## Running in docker-compose

At this point you should be able to press F5 or ctrl-F5 to run the solution in docker-compose.

If you request a sandwich with lettuce it'll fail right away, because that service has an inventory level of 0 to start. You should be able to request other sandwich combinations until running out of inventory.

## Deploy to Kubernetes

The final step in this lab is to deploy the services to K8s. The docker-compose environment is convenient for the F5 experience and debugging, but ultimately most production systems will run on K8s or something similar.

### Deploy RabbitMQ to Kubernetes

Open a CLI window.

1. Type `helm install --name my-rabbitmq --set rabbitmq.username=guest,rabbitmq.password=guest,rabbitmq.erlangCookie=supersecretkey stable/rabbitmq`
   1. Note that in a real environment you'll want to set the `username`, `password`, and `erlangCookie` values to secret values
1. Helm will display infomration about the deployment
1. Type `helm list` to list installed releases
1. Type `kubectl get pods` to list running instances
1. Type `kubectl get services` to list exposed services

At this point you should have an instance of RabbitMQ running in minikube. The output from `kubectl get services` should be something like this:

```text
$ kubectl get services
NAME                   TYPE        CLUSTER-IP       EXTERNAL-IP   PORT(S)                                 AGE
kubernetes             ClusterIP   10.96.0.1        <none>        443/TCP                                 88d
my-rabbitmq            ClusterIP   10.107.206.219   <none>        4369/TCP,5672/TCP,25672/TCP,15672/TCP   8m
my-rabbitmq-headless   ClusterIP   None             <none>        4369/TCP,5672/TCP,25672/TCP,15672/TCP   8m
```

Make note of the `my-rabbitmq` name, and also notice how it has been provided with a `CLUSTER-IP` address. This address is how the RabbitMQ service is exposed within the K8s cluster itself. This isn't a hard-coded or consistent value however, so later on you'll use the _name_ of the service to allow our other running container images to interact with RabbitMQ.

### Replace myrepository With the Real Name

Most of the files in the `deploy/k8s` directory refer to `myrepository` instead of the real name of your ACR repository. Fortunately it is possible to use bash to quickly fix them all up with the correct name.

1. Open a Git Bash CLI
1. Change directory to `deploy/k8s`
1. Type `grep -rl --include=*.sh --include=*.yaml --include=*.yml 'myrepository' | tee | xargs sed -i 's/myrepository/realname/g'`
   * ⚠ Replace `realname` with your real ACR repository name!

### Deployment and Service Configuration Files

K8s uses a different syntax for its deployment and service definition files. You've already seen an example of these in a previous lab.

There is already a `Start/deploy/k8s` directory in the directory structure. In a VS Code instance, open this `deploy/k8s` directory. Add a `breadservice-deployment.yaml` file with the following contents:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: breadservice
spec:
  selector:
    matchLabels:
      app: breadservice
  replicas: 1
  strategy:
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 1
  minReadySeconds: 5
  template:
    metadata:
      labels:
        app: breadservice
    spec:
      containers:
      - name: breadservice
        image: myrepository.azurecr.io/breadservice:lab03
        resources:
          limits:
            memory: "128Mi"
            cpu: "500m"
        env:
        - name: RABBITMQ__URL
          value: my-rabbitmq
      imagePullSecrets:
      - name: acr-auth
```

> ℹ The `myrepository` name should already be replaced with your ACR repo name.

Notice that the `RABBITMQ__URL` environment variable is being set to the _name_ of the RabbitMQ instance you started in K8s in a previous lab. Rather than using a hard-coded IP address, it is important to use the DNS name so K8s can manage the IP address automatically.

> ℹ The .NET Core configuration subsystem translates an environment variable such as `RABBITMQ__URL` to a setting with the key `rabbitmq:url`. So in the .NET code you'll see something like `_config["rabbitmq:url"]` referring to this environment variable.

You can review the pre-existing yaml files in the directory. There's a deployment file for each service in the system, plus a service definition file for the gateway service.

No K8s service definition files are necessary for most of our services, because they don't require any sort of public IP address, or even any known cluster-level IP address. Because all communication occurs via queued messaging, each service is truly a standalone app that has no direct interaction with any other apps via IP address.

Only the gateway service needs a known IP address, and that's because it exposes a web frontend, including web pages and an API for external consumers.

### Pushing the Container Images to Azure

Before you can apply the deployment and service files to the K8s cluster, the Docker container images need to be available in the repository. You've already seen how to tag and push an image to ACR, and that process now needs to happen for all the images in the system.

> ℹ Pushing all the images at once like in this lab isn't necessarily normal over time. Remember that the primary goal of service-based architectures is to be able to deploy or update individual services without redeploying everything else. But you do need to get the whole system running in the first place before you can go into long-term maintenance mode.

#### Building the Images

In the `deploy` directory there's a `build.sh` bash script that builds all the images for the system.

```bash
#!/bin/bash

docker build -f ../BreadService/Dockerfile -t breadservice:dev ..
docker build -f ../CheeseService/Dockerfile -t cheeseservice:dev ..
docker build -f ../LettuceService/Dockerfile -t lettuceservice:dev ..
docker build -f ../MeatService/Dockerfile -t meatservice:dev ..
docker build -f ../Gateway/Dockerfile -t gateway:dev ..
docker build -f ../SandwichMaker/Dockerfile -t sandwichmaker:dev ..
```

Open a Git Bash CLI window and do the following:

1. Change directory to `deploy`
1. `chmod +x build.sh`
1. `./build.sh`

This will build the Docker image for each service in the system based on the individual `Dockerfile` definitions in each project directory.

> ℹ This build process may take some time depending on the speed of your laptop.

#### Tagging the Images

In the `deploy/k8s` directory there's a `tag.sh` bash script that tags all the images created by `build.sh`.

```bash
#!/bin/bash

docker tag breadservice:dev myrepository.azurecr.io/breadservice:lab03
docker tag cheeseservice:dev myrepository.azurecr.io/cheeseservice:lab03
docker tag meatservice:dev myrepository.azurecr.io/meatservice:lab03
docker tag lettuceservice:dev myrepository.azurecr.io/lettuceservice:lab03
docker tag gateway:dev myrepository.azurecr.io/gateway:lab03
docker tag sandwichmaker:dev myrepository.azurecr.io/sandwichmaker:lab03
```

> ℹ The `myrepository` name should already be replaced with your ACR repo name.

Open a Git Bash CLI and do the following:

1. Change directory to `deploy/k8s`
1. `chmod +x tag.sh`
1. `./tag.sh`

This will tag each container image with the repository name for your ACR instance.

#### Pushing the Images

In the `deploy/k8s` directory there's a `push.sh` bash script that pushes the local images to the remote repository.

```bash
#!/bin/bash

docker push myrepository.azurecr.io/gateway:lab03
docker push myrepository.azurecr.io/cheeseservice:lab03
docker push myrepository.azurecr.io/lettuceservice:lab03
docker push myrepository.azurecr.io/sandwichmaker:lab03
docker push myrepository.azurecr.io/breadservice:lab03
docker push myrepository.azurecr.io/meatservice:lab03
```

> ℹ The `myrepository` name should already be replaced with your ACR repo name.

Open a Git Bash CLI and do the following:

1. Change directory to `deploy/k8s`
1. `chmod +x push.sh`
1. `./push.sh`

The result is that all the local images are pushed to the remote ACR repository.

### Applying the Kubernetes State

At this point you have all the deployment and service definition files that describe the desired state for the K8s cluster. And you have all the Docker container images in the ACR repository so they are available for download to the K8s cluster.

> ⚠ **IMPORTANT:** before applying the desired state for this lab, _make sure_ you have done the cleanup step in the previous lab so no containers are running other than RabbitMQ. You can check this with a `kubectl get pods` command.

The next step is to apply the desired state to the cluster by executing each yaml file via `kubectl apply`. To simplify this process, there's a `run-k8s.sh` file in the `deploy/k8s` directory:

```bash
#!/bin/bash

kubectl apply -f gateway-deployment.yaml
kubectl apply -f gateway-service.yaml
kubectl apply -f breadservice-deployment.yaml
kubectl apply -f cheeseservice-deployment.yaml
kubectl apply -f lettuceservice-deployment.yaml
kubectl apply -f meatservice-deployment.yaml
kubectl apply -f sandwichmaker-deployment.yaml
```

Open a Git Bash CLI window and do the following:

1. Change directory to `deploy/k8s`
1. `chmod +x run-k8s.sh`
1. `./run-k8s.sh`
1. `kubectl get pods`

The result is that the desired state described in your local yaml files is applied to the K8s cluster.

If you immediately execute (and repeat) the `kubectl get pods` command you can watch as the K8s pods download, load, and start executing each container image. This may take a little time, because as each pod comes online it needs to download the container image from ACR.

> ℹ Depending on the number of folks doing the lab, and the Internet speeds in the facility, patience may be required! In a production environment it is likely that you'll have much higher Internet speeds, less competition for bandwidth, and so spinning up a container in a pod will be quite fast.

Make sure (via `kubectl get pods`) that all your services are running before moving on to the next step.

### Interacting with the System

1. Open a CLI window _as administrator_
1. Type `minikube service gateway --url`
   1. This will show the localhost URL provided by minikube to access the service
1. Type `minikube service gateway`
   1. This will open your default browser to the URL for the service - it is a shortcut provided by minikube for testing

> ⚠ An Admin CLI window (e.g. run as administrator) is required because interacting with the `minikube` command always needs elevated permissions.

As with the docker-compose instance, you should be able to request sandwiches from the system. Notice that there's no shared state (such as inventory) between the services running in docker-compose and those running in minikube. In a real scenario any such state would typically be maintained in a database, and the various service implementations would be interacting with the database instead of in-memory data.

## Implementing Retry Policies

First on the list of [Fallacies of Distributed Computing](https://en.wikipedia.org/wiki/Fallacies_of_distributed_computing) is the idea that the network is reliable. Virtually all code folks write tends to assume that the network is there and won't fail. Rarely do people implement retry logic in case opening a database, sending an HTTP request, or writing to a queue might fail.

While we tend to get away with that approach, it becomes _really_ problematic when your code is hosted in a dynamic, self-healing runtime like Kubernetes. There's just no guarantee that a service won't go down, and a replacement spun up in its place by K8s.

Such a thing can happen due to bugs, or an intentional rolling update of a running service.

In this system it is possible that the RabbitMQ instance might become temporarily unavailable.

> ℹ In practice this is unlikely, because a production system will almost certainly deploy RabbitMQ across multiple redundant K8s nodes, leveraging RabbitMQ and K8s to achieve high fault tolerance.

### Using Polly Retry Policies

To overcome potential failures when trying to interact with any direct network interactions you should implement a retry policy. One common solution is to use the `Polly` or `Steeltoe` NuGet packages. In this lab you will use the `Polly` package.

In Visual Studio, right-click on the `Gateway` project and choose *Manage NuGet Packages* to add a reference to the `Polly` package.

Then open the `SandwichRequestor` class in the `Services` folder. This class contains the code that sends messages to RabbitMQ, so it is an ideal location to implement a retry policy.

Add a `using Polly;` statement at the top of the file.

Then in the class define a field for the retry policy:

```c#
    readonly Policy _retryPolicy = Policy.
      Handle<Exception>().
      WaitAndRetry(3, r => TimeSpan.FromSeconds(Math.Pow(2, r)));
```

Polly provides a lot of flexibility around retry policies, and supports both sync and async concepts. Because the call to RabbitMQ is synchronous, this is a synchronous policy.

The policy itself will trigger on any `Exception`, and will retry three times. The `Math.Pow(2, r)` expression implements an exponential backoff. The first retry will happen in 1 second, the second 2 seconds later, the third 4 seconds later. In total it will consume 7 seconds before giving up and letting the exception flow to the rest of the code.

Using the policy is fairly straightforward. The policy object is used to wrap a block of your code, and it "protects" that code. In this case, any `Exception` thrown by the protected code will trigger the retry policy.

Edit the `SandwichRequest` method where the queue is opened and the message sent to wrap it with the policy:

```c#
        _retryPolicy.Execute(() =>
        {
          using (var _queue = new Queue(_config["rabbitmq:url"], "customer"))
          {
            _queue.SendMessage("sandwichmaker", correlationId, request);
          }
        });
```

You can see how the retry policy's `Execute` method is used to encapsulate the block of code that uses the network to connect to RabbitMQ and send a message. The most likely failure scenario here is that the RabbitMQ instance is momentarily unavailable, and as long as it becomes available again within seven seconds this code will ultimately succeed.

Make sure to save the changes to the code file and project.

## Updating a Running Container

At this point you have a newer version of the gateway service to deploy: the updated code with a retry policy.

Fortunately the Kubernetes deployment for the gateway service specified the use of rolling updates. You can review the `End/deploy/k8s/gateway-deployment.yaml` file to refresh you memory.

What this means is that we can tell K8s that there's a newer version of the gateway image and "magic" will happen:

1. Kubernetes will suspend all inbound network requests to the existing pod
   1. Any existing network requests will still be routed to and handled by the existing pod
1. Kubernetes will spin up a new instance of the pod
   1. The new pod will download the new container image
   1. The new pod will start up the new container image
1. Kubernetes will route all inbound network requests to the new pod
1. Kubernetes will terminate the old pod

Perform the following steps in a Git Bash CLI to see this happen:

1. Change directory to `Lab03/Start`
1. Build the new container image: `docker build -f Gateway/Dockerfile -t gateway:dev` .
1. Tag the new image: `docker tag gateway:dev myrepository.azurecr.io/gateway:lab03-1`
1. Push the new image: `docker push danrepo.azurecr.io/gateway:lab03-1`
1. Edit the `deploy/k8s/gateway-deployment.yaml` file to express the new desired state
   * Update the `image` element with the new tag
   * `        image: myrepository.azurecr.io/gateway:lab03-1`
1. Apply the new desired state with `kubectl`
   * `kubectl apply -f gateway-deployment.yaml`
1. Quickly and repeatedly use `kubectl get pods` to watch the rolling update occur

At this point you've updated the gateway service to a newer version, and it is running with a retry policy.

> 🎉 If you are ahead of time, feel free to go through all the services and add retry policies anywhere the code opens and sends messages to RabbitMQ.

## Tearing Down the System

Once you are done interacting with the system you can shut it down. In the `deploy/k8s` directory there's a `teardown.sh` bash script that uses `kubectl` to delete the deployment and service items from the cluster:

```bash
#!/bin/bash

kubectl delete deployment gateway
kubectl delete service gateway
kubectl delete deployment breadservice
kubectl delete deployment cheeseservice
kubectl delete deployment lettuceservice
kubectl delete deployment meatservice
kubectl delete deployment sandwichmaker
```

Open a Git Bash CLI window and do the following:

1. Change directory to `deploy/k8s`
1. `chmod +x teardown.sh`
1. `./teardown.sh`

It is critical that you do this before moving on to subsequent labs.

