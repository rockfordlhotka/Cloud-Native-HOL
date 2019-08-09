# HTTP Messaging

In this lab we'll update the code from Lab03 to use a service bus messaging model instead of direct queues.

Lesson goals:

1. Understand the difference between direct queued messaging and HTTP-based messaging
1. Set up cluster-level service definitions for all services so they have IP addresses
1. Use configuration to avoid hard-coding IP addresses

## Background

**#### SUPPLY CONTENT HERE**

## Explore the Http Helper Code

Possible Http abstraction like the RabbitQueue project???

**#### SUPPLY CONTENT HERE**

## Update the System

In the interest of time many of the services have already been updated. The two most interesting services to update are the gateway and sandwichmaker services, and you'll do the updates on those.

### Update the Gateway Service

**#### SUPPLY CONTENT HERE**

### Update the Sandwichmaker Service

**#### SUPPLY CONTENT HERE**

### Update the Bread Service

**#### SUPPLY CONTENT HERE**

## Running the System in docker-compose

**#### SUPPLY CONTENT HERE**

## Running the System in Kubernetes

**#### SUPPLY CONTENT HERE**

### Creating the Service Configuration Files

Each deployment now needs a service file so it gets a cluster-level IP address.

**#### SUPPLY CONTENT HERE**

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

### Tagging the Images

In the `deploy/k8s` directory there's a `tag.sh` bash script that tags all the images created by `build.sh`.

```bash
#!/bin/bash

docker tag breadservice:dev myrepository.azurecr.io/breadservice:lab05
docker tag cheeseservice:dev myrepository.azurecr.io/cheeseservice:lab05
docker tag meatservice:dev myrepository.azurecr.io/meatservice:lab05
docker tag lettuceservice:dev myrepository.azurecr.io/lettuceservice:lab05
docker tag gateway:dev myrepository.azurecr.io/gateway:lab05
docker tag sandwichmaker:dev myrepository.azurecr.io/sandwichmaker:lab05
```

Edit this file and replace `myrepository` with your ACR repository name. Then open a Git Bash CLI and do the following:

1. Change directory to `deploy/k8s`
1. `chmod +x tag.sh`
1. `./tag.sh`

This will tag each container image with the repository name for your ACR instance.

### Pushing the Images

In the `deploy/k8s` directory there's a `push.sh` bash script that pushes the local images to the remote repository.

```bash
#!/bin/bash

docker push myrepository.azurecr.io/gateway:lab05
docker push myrepository.azurecr.io/cheeseservice:lab05
docker push myrepository.azurecr.io/lettuceservice:lab05
docker push myrepository.azurecr.io/sandwichmaker:lab05
docker push myrepository.azurecr.io/breadservice:lab05
docker push myrepository.azurecr.io/meatservice:lab05
```

Edit this file and replace `myrepository` with your ACR repository name. Then open a Git Bash CLI and do the following:

1. Change directory to `deploy/k8s`
1. `chmod +x push.sh`
1. `./push.sh`

The result is that all the local images are pushed to the remote ACR repository.

### Applying the Kubernetes State

At this point you have all the deployment and service definition files that describe the desired state for the K8s cluster. And you have all the Docker container images in the ACR repository so they are available for download to the K8s cluster.

> **IMPORTANT:** before applying the desired state for this lab, _make sure_ you have done the cleanup step in the previous lab so no containers are running other than RabbitMQ. You can check this with a `kubectl get pods` command.

The next step is to apply the desired state to the cluster by executing each yaml file via `kubectl apply`. To simplify this process, there's a `run-k8s.sh` file in the `deploy/k8s` directory:

```bash
#!/bin/bash

kubectl apply -f gateway-deployment.yaml
kubectl apply -f gateway-service.yaml
kubectl apply -f greeter-deployment.yaml
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

> Depending on the number of folks doing the lab, and the Internet speeds in the facility, patience may be required! In a production environment it is likely that you'll have much higher Internet speeds, less competition for bandwidth, and so spinning up a container in a pod will be quite fast.

Make sure (via `kubectl get pods`) that all your services are running before moving on to the next step.

### Test the System

At this point you should be able to open a browser and interact with the system to see it in action.

1. Open a CLI window _as administrator_
1. Type `minikube service gateway`
   1. This will open your default browser to the URL for the service - it is a shortcut provided by minikube for testing

> An Admin CLI window (e.g. run as administrator) is required because interacting with the `minikube` command always needs elevated permissions.

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
