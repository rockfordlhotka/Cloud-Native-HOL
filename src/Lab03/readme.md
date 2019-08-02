# Design and Build Message-Based Microservices

In this lab we'll build a message-based service-based system that runs in docker-compose and K8s. It will use RabbitMQ as a messaging platform.

Lesson goals:

1. Use a gateway server to provide user access to a service-based system
   1. Understand how to implement a "synchronous" user experience to external users
   1. Discuss how SignalR _could_ be used to provide an asynchronous experience to external users
1. Implement message-based services that work together to provide business functionality
1. See how docker-compose provides a convenient developer inner-loop experience
1. Understand how docker-compose.yaml is different from K8s deploy/service definition files
1. Understand compensating transactions

## Terminology

Terminology matters a lot when talking about or working with services. A service is a standalone, autonomous, unit of functionality. So is an app. So apps and services are basically the same thing.

But people talk about building a "service-oriented app" or "microservice-based app". That's nonsense, because that would be an app composed of other apps. Intelligent conversation becomes impossible.

Throughout this and subsequent labs the following terms are used:

* **Service-based**: a term used to encompass SOA and microservices without getting into debates about whether they are the same or different
* **Service** or **app**: an autonomous unit of functionality that can be independently deployed from any other part of the overall system
* **Edge app**: an app that exposes some sort of interface to external consumers
* **Service-based system** (aka **system**): a logical boundary within which all apps/services interact using a common messaging protocol and run on a common service fabric or runtime
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

> It is a fairly safe bet to think that Visual Studio will become more integrated with Kubernetes over time. Already Azure Dev Spaces exist, and innovation within the K8s space is occurring at a breakneck pace.

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
        1. You'll see two addresses, choose the second - NOT the 172.17.0.??? address

At this point a RabbitMQ container is running in Docker Desktop on your workstation, and you have made note of the container's IP address _inside Docker_.

## RabbitMQ Helper Code

In the src/Lab03/Start directory you'll see a pre-existing solution that implements most of the service-based system described earlier in this document. Open that solution. Look for the RabbitQueue project and examine the files in that project.

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

You'll be asked to choose between Windows and Linux containers. Choose Linux.

> Windows containers are a viable alternative, but modern server development is moving rapidly toward Linux containers.

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

### Implement the SandwichRequestor Service

### Implement the SandwichmakerListener Hosted Service

### Implement the Index Page

### Examine the SandwichController Class

## Implement Bread Service

## Examine the Lettuce Service

## Deploy to Kubernetes
