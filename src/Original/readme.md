Docker/Kubernetes Services Demo
-------------------------------
This demo uses a number of services to demonstrate communication between services and an end user via a web site.

* Greeter - a simple hello world implementation - not part of the rest of the demo
* Gateway - the web server providing access to the service-oriented system from outside the docker/k8s cluster
* SandwichMaker - a service that makes sandwiches
* BreadService - a service responsible for managing inventory of bread
* MeatService - a service responsible for managing inventory of meat
* LettuceService - a service responsible for managing inventory of lettuce
* CheeseService - a service responsible for managing inventory of cheese

## Building the containers

I assume you have Docker for Windows (or Mac) already installed. 

1. Clone the repo from GitHub
1. Open a command window (I'm using bash in WSL Ubuntu)
1. Change to the `src/02-services/deploy` directory
1. Run `build.sh`
   1. This will build all the containers for the services
   1. The result will be container images in your local Docker install

## docker-compose setup
These instructions should get the demo running in docker-compose on a local dev workstation.

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
1. Update the docker-compose.yml file in the ServicesDemo solution
    1. Open the ServicesDemo solution in Visual Studio 2017 or higher
    1. Edit the docker-compose.yml file and replace the IP address in all `- RABBITMQ__URL=172.25.0.2` lines with your new rabbitmq IP address
    1. _Optional_: Notice the 'networks:' section and that it uses the `demonet` network - if you change network names make sure to update this as well
    
## kubernetes setup
These instructions assume you have a Kubernetes instance running. In my case I'm using AKS (Azure managed k8s) and also Azure Container Service to store my cloud-based containers.

1. If using Azure: grant your AKS instance permission to use your ACR instance
   1. https://docs.microsoft.com/en-us/azure/container-registry/container-registry-auth-aks
1. Tag the local Docker images with your cloud container service
   1. Edit the `/deploy/k8s/tag.sh` file to use your container repo name (in my case it is `rdlk8s.azurecr.io`)
   1. Run the bash script to tag your local containers
1. Push the local container images to your cloud container service
   1. Edit the `/deploy/k8s/push.sh` file to use your container repo name (in my case it is `rdlk8s.azurecr.io`)
   1. Run the bash script to push your local containers to the cloud repo
1. Install rabbitmq in your k8s instance
   1. I use Helm to do this in my environment; bash scripts to install Helm are in the `/deploy/helm` directory
      1. https://github.com/helm/charts/tree/master/stable/rabbitmq
      1. `helm install --name dinky-wallaby --set rabbitmq.username=guest,rabbitmq.password=guest,rabbitmq.erlangCookie=supersecretkey stable/rabbitmq`
   1. You may need to edit each `/deploy/k8s/*.yaml` file to use the correct service name for your rabbitmq install (in my case it is `dinky-wallaby-rabbitmq`)
1. Create the k8s deployments and services
   1. Edit each `/deploy/k8s/*.yaml` file to use your container repo name (in my case it is `rdlk8s.azurecr.io`)
   1. Make sure your current kubectl context is set to your k8s instance (via `kubectl config use-context`)
   1. Run the `/deploy/k8s/run-k8s.sh` bash script to apply the yaml files against your k8s instance