# Deploy pre-built software into Kubernetes

In this lab we'll install RabbitMQ into k8s using Helm, and then we'll use `kubectl` to deploy the Gateway web site into k8s.

Lesson goals:

1. Use Helm to deploy RabbitMQ
1. Use `kubectl` to deploy ASP.NET Core website

## Deploy RabbitMQ to Kubernetes

1. Type `helm install --name my-rabbitmq --set rabbitmq.username=guest,rabbitmq.password=guest,rabbitmq.erlangCookie=supersecretkey stable/rabbitmq`
   1. Note that in a real environment you'll want to set the `username`, `password`, and `erlangCookie` values to secret values
1. Helm will display infomration about the deployment
1. Type `helm list` to list installed releases
1. Type 'kubectl get pods' to list running instances
1. Type `kubectl get services` to list exposed services

At this point you should have an instance of RabbitMQ running in minikube. The output from `kubectl get services` should be something like this:

```text
$ kubectl get services
NAME                   TYPE        CLUSTER-IP       EXTERNAL-IP   PORT(S)                                 AGE
kubernetes             ClusterIP   10.96.0.1        <none>        443/TCP                                 88d
my-rabbitmq            ClusterIP   10.107.206.219   <none>        4369/TCP,5672/TCP,25672/TCP,15672/TCP   8m
my-rabbitmq-headless   ClusterIP   None             <none>        4369/TCP,5672/TCP,25672/TCP,15672/TCP   8m
```

Make note of the `my-rabbitmq` name, and also notice how it has been provided with a `CLUSTER-IP` address. This address is how the RabbitMQ service is exposed within the k8s cluster itself. This isn't a hard-coded or consistent value however, so later on you'll use the _name_ of the service to allow our other running container images to interact with RabbitMQ.

## Deploy Website to Kubernetes

In this section of the lab you'll build a Docker image, create a definition file to deploy that image to Kuberentes, create a definition file to expose that image as a service, and then execute those definition files against minikube.

### Build Docker iamage

1. Open a CLI window
1. Change directory to src/Lab02/End/Gateway
1. Build the image `docker build -t gateway:v3 -f Gateway/Dockerfile .`
1. Type `docker image ls` and you should see your new `gateway:v3` image

### Create Deployment definition

### Create Service definition

### Build container image

### Deploy image and service
