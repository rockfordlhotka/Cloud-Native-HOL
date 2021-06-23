# Run Microservices in Kubernetes

In this lab we'll run the microservices system from Lab03 in Kubernetes, and make use of the RabbitMQ service configured in Lab04.

Lesson goals:

1. Use a gateway server to provide user access to a service-based system
   1. Discuss how to implement a "synchronous" user experience to external users
   1. Use Blazor (and SignalR) to provide an interactive and asynchronous experience to external users
1. Update a running container in Kubernetes
1. Implement retry policies for potential network failures

## Deploy to Kubernetes

The final step in this lab is to deploy the services to K8s. The docker-compose environment is convenient for the F5 experience and debugging, but ultimately most production systems will run on k8s or something similar.

### Replace myrepository With the Real Name

Most of the files in the `Lab03/deploy/k8s` directory refer to `myrepository` instead of the real name of your ACR repository. Fortunately it is possible to use bash to quickly fix them all up with the correct name.

1. Open a Git Bash CLI
1. Change directory to `Lab03/deploy/k8s`
1. Type `grep -rl --include=*.sh --include=*.yaml --include=*.yml 'myrepository' | xargs sed -i 's/myrepository/realname/g'`
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

Notice that the `RABBITMQ__URL` environment variable is being set to the _name_ of the RabbitMQ instance you started in K8s in the previous lab. Rather than using a hard-coded IP address, it is important to use the DNS name so K8s can manage the IP address automatically.

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

To gain access to pods running in the cluster, use kubectl to set up a port forwarding tunnel.

```text
kubectl port-forward svc/gateway 31919:80
```

The `31919` port is arbitrary, so you can use any high number you'd like. This makes `localhost:31919` on your computer forward to port 80 of the `gateway` service.

Open a browser to `localhost:31919` using the port of the Gateway service.

You should be able to request sandwiches from the system. Notice that there's no shared state (such as inventory) between the services running in docker-compose and those running in k8s. In a real scenario any such state would typically be maintained in a database, and the various service implementations would be interacting with the database instead of in-memory data.

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
1. Build the new container image: `docker build -f Gateway/Dockerfile -t gateway:dev .`
1. Tag the new image: `docker tag gateway:dev myrepository.azurecr.io/gateway:lab03-1`
1. Push the new image: `docker push myrepository.azurecr.io/gateway:lab03-1`
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
