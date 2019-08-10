# Service bus messaging

In this lab we'll update the code from Lab03 to use a service bus messaging model instead of direct queues.

Lesson goals:

1. Understand the difference between direct queued messaging and service bus messaging

## Overview of Service Bus Messaging

In Lab03 each service has its own queue, and when a consumer wants to send a message to a service it delivers the message to that specific queue.

In a service bus model messages are sent to the "service bus" instead of to any specific queue.

![abstract service bus](images/abstract-service-bus.png)

If you have ever dabbled in building your own PC or taken a computer architecture class in college/university you are probably familiar with the concept of a _bus_. The idea is that a message is "put on the bus", and all devices attached to the bus have the opportunity to handle (or ignore) that message.

A software bus follows the same philosophy. Any message sent to the bus is available to all services using that bus. Most services ignore (filter out) most messages, only choosing to receive and handle messages that apply to the specific service.
 
When implementing a very basic service bus with RabbitMQ all messages are sent to an _exchange_, and all services subscribe to that exchange by attaching their own queue to the exchange (usually with a filter). Messages are tagged with what RabbitMQ calls a _binding key_ to allow routing and filtering of messages.
 
![rabbitmq service bus](images/rabbitmq-service-bus.png)
 
Logically this is the same as the first diagram, but this diagram shows what is happening behind the scenes.
 
Anything sending a message is called a _publisher_, and the term _publish_ is used to describe sending a message to the bus (the exchange).
 
Services that want to handle messages from the bus do the following:
 
1. Open a connection to RabbitMQ
1. Create a queue to store messages until they can be processed
1. Create a (usually filtered) subscription that routes messages from the exchange to the queue
1. Listen for messages on the subscription, and process them as they arrive

Notice that this approach requires the same number of queues as the direct queue approach in Lab03. Each service still has its own queue, the only difference here is in how messages are sent/published.
 
The _advantage_ to a service bus approach is that the publishers need to know less than they did in the direct queue model. In this case all publishers _always_ send messages to the bus, not knowing or caring which services might handle those messages.
 
Messages are tagged with a binding key to allow subscribers to filter out messages inappropriate for a given service.
 
The _drawback_ to this approach is that if no subscriptions exist for a specific type of message, that type of message will just disappear. When a message is published to a service bus, it is either picked up by one or more subscriptions or it just disappears because nobody was listening.
 
> ℹ If a publisher publishes a message in the forest and nobody is subscribed to hear the message, was the message really published? ;)
 
One thing to also keep in mind, is that a single message can be delivered to _multiple subscriptions_ at the same time. Sometimes this is desired behavior, but in the case of the sandwichmaker system that is undesirable.
 
Consider the case where two replicas of the bread service are running. If each subscribes to the bus, filtering for "BreadBin" messages, they could _both_ get every message. That's not good, because each message should be processed one time, not _n_ times.

## RabbitMQ Helper Code

Open the `ServicesDemo` solution from the `src/Lab04/Start` directory, then open the `RabbitQueue` project and look at the new `ServiceBus.cs` file.

> ℹ This is a very simplistic implementation of a service bus. In a production environment you should consider using a pre-existing service bus product such as [nServiceBus](https://particular.net/nservicebus) or one of its competitors.

The `Initialize` method opens a connection to RabbitMQ just like the `Queue` class does, and then it declares an exchange:

```c#
        channel.ExchangeDeclare(exchangeName, "direct");
```

RabbitMQ supports different types of exchange, each with their own behaviors. In this case the _direct_ type is desired, because it provides the type of message routing necessary to implement a service bus.

Where the `Queue` class had a `SendMessage` method, this class has a `Publish` method, following the terminology of a service bus model. This method publishes messages to the exchange rather than to any specific queue:

```c#
      channel.BasicPublish(
        exchange: exchangeName,
        routingKey: bindingKey,
        basicProperties: props,
        body: body);
```

The `Subscribe` method is more complex, creating a queue and then setting up one or more subscriptions based on the provided binding key values. This is because a service can listen for any number of binding keys, and each binding key filter requires its own subscription.

In this case the queue is being created with default values, and RabbitMQ generates a dynamic name for the queue. When the `channel` is disposed this queue will automatically be deleted, which makes sense since it only gets messages via a subscription.

```c#
      var queueName = channel.QueueDeclare().QueueName;
```

Using the queue, a subscription is then created for each binding key, linking the exchange to the queue with a filter on the binding key:

```c#
      foreach (var item in bindingKeys)
      {
        channel.QueueBind(
          queue: queueName,
          exchange: exchangeName,
          routingKey: item);
      }
```

Next, an event handler is configured to notify the calling code any time a message arrives from any of the subscriptions:

```c#
      var consumer = new EventingBasicConsumer(channel);
      consumer.Received += (model, ea) =>
      {
        var message = Encoding.UTF8.GetString(ea.Body);
        handleMessage(ea, message);
      };
```

Finally, the code starts listening for inbound messages on the queue:

```c#
      channel.BasicConsume(
        queue: queueName, 
        autoAck: true, 
        consumer: consumer);
```

The next step is to update the various services in the solution to use the service bus instead of direct queue messaging.

## Update the System

In the interest of time many of the services have already been updated. The two most interesting services to update are the gateway and sandwichmaker services, and you'll do the updates on those.

### Update the Gateway Service

Open the `Gateway` project in the solution. You will need to update the code in this project to use a `ServiceBus`, where it used to use a `Queue` model.

#### Inject the Service Bus as a Service

Although it would be possible to directly create `ServiceBus` objects like the `Queue` objects were in the previous lab, an improvement to the overall code is to inject the `ServiceBus` as needed. That means declaring it as a service in the `ConfigureServices` method of the `Startup` class in the `Gateway` project:

```c#
      services.AddTransient<RabbitQueue.IServiceBus>((e) =>
        new RabbitQueue.ServiceBus(e.GetService<IConfiguration>()["rabbitmq:url"], "sandwichBus"));
```

This is a transient service, which means each time this service is needed a new instance will be created. One advantage of this change is that the use of the configuration subsystem to retrieve the RabbitMQ host name is now handled only here, instead of in numerous places throughout the code.

#### Update the SandwichRequestor Class

The `SandwichRequstor` class needs to use the new `IServiceBus` type instead of the previous `Queue` type. Its constructor needs a new parameter so the service bus can be injected into the class:

```c#
    readonly IServiceBus _bus;

    public SandwichRequestor(IConfiguration config, IWorkInProgress wip, IServiceBus bus)
    {
      _config = config;
      _wip = wip;
      _bus = bus;
    }
```

The `RequestSandwich` method then needs to use the `_bus` field to publish messages:

```c#
        _retryPolicy.Execute(() =>
        {
          _bus.Publish("SandwichRequest", correlationId, request);
        });
```

If you think back to the `ServiceBus` implementation, the code that connects to the RabbitMQ service and opens a connection was moved out of the constructor and into an `Initialize` method. That initialize method is invoked by the `Publish` method. 

The reason for that implementation is to ensure that any failure that might occur when connecting to the RabbitMQ service occurs in the `Publish` method, not in the constructor. Remember that the constructor is run by the .NET Core dependency injection framework where exceptions wouldn't be easily wrapped by a retry policy. By deferring that process to the `Publish` method call the code ensures that any such failures occur while inside the retry policy.

This is another general improvement over the Lab03 implementation that's independent of the use of a service bus.

> ℹ In fact, if you look at the `Queue` code in Lab04 you'll see that it also defers opening connections to the RabbitMQ service to enable the use of dependency injection and also the use of Polly policies.

And that's it. _Most_ of the changes were to accommodate the use of dependency injection. The change from direct queue messaging to service bus publishing is quite trivial.

The old code:

```c#
  _queue.SendMessage("sandwichmaker", correlationId, request);
```

The new code:

```c#
  _bus.Publish("SandwichRequest", correlationId, request);
```

The meaning of that first parameter is quite different however! When calling `SendMessage` the first parameter is the name of the queue into which the message should be delivered. When calling `Publish` the publisher has no idea who might receive the message. That first parameter is merely a tag (the binding key) to help _all potential consumers_ recognize this as a message of interest.

#### Update the SandwichmakerListener Class

The `SandwichmakerListener` class also relies on dependency injection to get access to the service bus:

```c#
    private readonly IServiceBus _bus;

    public SandwichmakerListener(IConfiguration config, IWorkInProgress wip, IServiceBus bus)
    {
      _config = config;
      _wip = wip;
      _bus = bus;
    }
```

While you are updating this code, you might as well wrap the listening code in a retry policy as well. Define the policy in the class:

```c#
    readonly Policy _retryPolicy = Policy.
      Handle<Exception>().
      WaitAndRetry(3, r => TimeSpan.FromSeconds(Math.Pow(2, r)));
```

Then you can update the `StartAsync` method to use the policy and to subscribe to messages from the service bus:

```c#
    public Task StartAsync(CancellationToken cancellationToken)
    {
      _retryPolicy.Execute(() =>
      {
        _bus.Subscribe<Messages.SandwichResponse>("SandwichResponse", (ea, response) =>
        {
          _wip.CompleteWork(ea.BasicProperties.CorrelationId, response);
        });
      });

      return Task.CompletedTask;
    }
```

Again, the code changes from direct queue messaging to a service bus aren't substantial, but the _meaning_ of the code is quite different.

The old code:

```c#
    _queue.StartListening<Messages.SandwichResponse>((ea, response) =>
    {
      _wip.CompleteWork(ea.BasicProperties.CorrelationId, response);
    });
```

The new code:

```c#
    _bus.Subscribe<Messages.SandwichResponse>("SandwichResponse", (ea, response) =>
    {
      _wip.CompleteWork(ea.BasicProperties.CorrelationId, response);
    });
```

The `Subscribe` method has a new parameter compared to the old code: the name of the binding key (or binding keys) to be handled. Only messages with a matching binding key will arrive via the subscription, all other messages on the service bus are ignored.

This code requires that the messages be of type `Messages.SandwichResonse`, and each message is handled by the `CompleteWork` method.

At this point the gateway service has been converted to use a service bus instead of direct messaging.

### Update the SandwichMaker Service

The most complex code in the entire system is the sandwichmaker service implementation. This service exchanges messages with all other services in the system, and so it is most affected by the change from direct queue messaging to a service bus model.

In the `SandwichMaker` class change the field referencing a `Queue` to one that references an `IServiceBus`:

```c#
    private static IServiceBus _bus;
```

Then in the constructor change from creating a `Queue` to creating a `ServiceBus` instance:

```c#
      if (_bus == null)
        _bus = new ServiceBus(config["rabbitmq:url"], "sandwichBus");
```

The old code listened for messages to arrive on the queue, knowing that several different message types could arrive on the same queue. To handle that, the old code send all those messages to a single method that could figure out how to handle each message type:

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

This is invoked (in the old code) by a single line of code:

```c#
      _queue.StartListening(HandleMessage);
```

One way to update the code is to replace that with a line that subscribes to numerous binding keys (don't make this change):

```c#
      _bus.Subscribe<Messages.SandwichRequest>(
        new string[] { "SandwichRequest", "MeatBinResponse", "BreadBinResponse", "CheeseBinResponse", "LettuceBinResponse" },
        HandleMessage);
```

Although this would work fine, it really isn't ideal. Object-oriented code should avoid `switch` statements in favor of more strongly typed solutions. Fortunately there's an alternative that entirely eliminates the need for the `HandleMessage` method and its `switch` statement.

Completely delete the `HandleMessage` method, then replace the `StartListening` line of code with individual binding key subscriptions:

```c#
      _bus.Subscribe<Messages.SandwichRequest>(
        "SandwichRequest",
        RequestIngredients);
      _bus.Subscribe<Messages.MeatBinResponse>(
        "MeatBinResponse",
        HandleMeatBinResponse);
      _bus.Subscribe<Messages.BreadBinResponse>(
        "BreadBinResponse",
        HandleBreadBinResponse);
      _bus.Subscribe<Messages.CheeseBinResponse>(
        "CheeseBinResponse",
        HandleCheeseBinResponse);
      _bus.Subscribe<Messages.LettuceBinResponse>(
        "LettuceBinResponse",
        HandleLettuceBinResponse);
```

This is possible because the system only sends one type of message per binding key. That is _not_ a requirement of a service bus, but it is how this particular system has been implemented, allowing this code to be improved quite a lot.

Each subscription listens for messages designated with a specific binding key. Those messages are assumed to be of a known, and singular, type, so they can be routed to a strongly typed handler.

The last step is to update each of those per-message handler methods. For example, the method signature of the `HandleCheesBinResponse` method needs to accept a strongly typed parameter:

```c#
    private static void HandleCheeseBinResponse(BasicDeliverEventArgs ea, Messages.CheeseBinResponse response)
```

And there's a line of code in the body of the message that deserializes the message, and that can be removed - so remove this line:

```c#
        var response = JsonConvert.DeserializeObject<Messages.CheeseBinResponse>(message);
```

Repeat this process for the other four message handling methods.

At this point you should be able to build the solution with no compile errors. 

## Running the System in docker-compose

Before running the system it is necessary to make sure that the correct RabbitMQ IP address is in the `docker-compose.yaml` file. Edit the file in Visual Studio and make sure the IP addresses specified for each service match your RabbitMQ instance's IP address from the previous lab.

Run the system from Visual Studio in docker-compose and make sure you can make a sandwich. It is always easier to do this in docker-compose than to try and troubleshoot coding issues in Kubernetes.

## Running the System in Kubernetes

As in the previous lab, it is necessary to deploy the system to K8s.

1. Build the container images
1. Tag the container images
1. Push the container images to ACR
1. Apply the desired state to the Kubernetes cluster

### Building the Images

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

### Replace myrepository With the Real Name

Most of the files in the `deploy/k8s` directory refer to `myrepository` instead of the real name of your ACR repository. Fortunately it is possible to use bash to quickly fix them all up with the correct name.

1. Open a Git Bash CLI
1. Change directory to `deploy/k8s`
1. Type `grep -rl --include=*.sh --include=*.yaml --include=*.yml 'myrepository' | tee | xargs sed -i 's/myrepository/realname/g'`
   * ⚠ Replace `realname` with your real ACR repository name!

### Tagging the Images

In the `deploy/k8s` directory there's a `tag.sh` bash script that tags all the images created by `build.sh`.

```bash
#!/bin/bash

docker tag breadservice:dev myrepository.azurecr.io/breadservice:lab04
docker tag cheeseservice:dev myrepository.azurecr.io/cheeseservice:lab04
docker tag meatservice:dev myrepository.azurecr.io/meatservice:lab04
docker tag lettuceservice:dev myrepository.azurecr.io/lettuceservice:lab04
docker tag gateway:dev myrepository.azurecr.io/gateway:lab04
docker tag sandwichmaker:dev myrepository.azurecr.io/sandwichmaker:lab04
```

> ℹ The `myrepository` name should already be replaced with your ACR repo name.

Open a Git Bash CLI and do the following:

1. Change directory to `deploy/k8s`
1. `chmod +x tag.sh`
1. `./tag.sh`

This will tag each container image with the repository name for your ACR instance.

### Pushing the Images

In the `deploy/k8s` directory there's a `push.sh` bash script that pushes the local images to the remote repository.

```bash
#!/bin/bash

docker push myrepository.azurecr.io/gateway:lab04
docker push myrepository.azurecr.io/cheeseservice:lab04
docker push myrepository.azurecr.io/lettuceservice:lab04
docker push myrepository.azurecr.io/sandwichmaker:lab04
docker push myrepository.azurecr.io/breadservice:lab04
docker push myrepository.azurecr.io/meatservice:lab04
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

### Test the System

At this point you should be able to open a browser and interact with the system to see it in action.

1. Open a CLI window _as administrator_
1. Type `minikube service gateway`
   1. This will open your default browser to the URL for the service - it is a shortcut provided by minikube for testing

> ⚠ An Admin CLI window (e.g. run as administrator) is required because interacting with the `minikube` command always needs elevated permissions.

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
