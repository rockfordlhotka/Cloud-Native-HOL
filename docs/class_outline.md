# Cloud-Native HOL Outline

1. Introduction to cloud-native computing and tools
   1. Docker
   1. Kubernetes
   1. minikube
   1. Helm
   1. RabbitMQ
   1. Azure App Services
   1. bash command line
1. Hosting ASP.NET Core in Docker containers and Azure
   1. Creating a website that builds/deploys to a container
   1. Debugging container-based software
   1. Pushing a container image to the cloud
   1. Running a container in Azure
1. Deploying pre-built software into Kubernetes
   1. Use Helm to deploy RabbitMQ in minikube
   1. Use `kubectl` to deploy/run an ASP.NET Core website in minikube
   1. Exposing a container as a public service
   1. Understanding how kubernetes scales services
1. Designing and building message-based microservices
   1. Hosting ASP.NET Core console apps in a container
   1. Understanding docker-compose and local debugging
   1. Using queues for message delivery
   1. Externalizing configuration to the environment
   1. Implementing retry policies
1. Service bus messaging
   1. Branches off module 4
   1. Using queues as a service bus
   1. Implementing services over a service bus vs a dedicated queue
   1. Understanding how kubernetes applies container updates
1. Http messaging
   1. Branches off module 4
   1. Creating a private kubernetes service for a container
   1. Understanding the kubernetes load balancer
   1. Using k8s DNS (no fixed IP addresses)
   1. Failover with Steeltoe and/or Polly
