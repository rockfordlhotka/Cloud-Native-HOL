# Deploy pre-built software into Kubernetes

In this lab we'll use `kubectl` to deploy the Gateway web site into Kubernetes (k8s).

Lesson goals:

1. Create ACR credentials so Kubernetes can pull images
1. Use `kubectl` to deploy ASP.NET Core website

## Deploy Website to Kubernetes

In this section of the lab you'll build a Docker image, create a definition file to deploy that image to Kuberentes, create a definition file to expose that image as a service, and then execute those definition files against k8s.

### Build Docker image

1. Open a CLI window
1. Change directory to src/Lab02/Start/Gateway
1. Build the image `docker build -t gateway:lab02 -f Gateway/Dockerfile .`
   * > ℹ Remember that the trailing `.` is important for the `docker build` command
1. Type `docker image ls` and you should see your new `gateway:lab02` image

Because this new project is identical to the Gateway project from Lab01, the `docker build` command may actually do no work. Docker is smart enough to realize that this "new image" is identical to an existing image, and so it may just add the `lab02` tag to the existing image.

Now push the image to your Azure repository (replacing 'myrepository' with your repository name)

1. Label the image: `docker tag gateway:lab02 myrepository.azurecr.io/lab02/gateway:lab02`
1. Push the image: `docker push myrepository.azurecr.io/lab02/gateway:lab02`
1. Confirm: `az acr repository list -n myrepository`

The new container image may be identical to the one from Lab01, and in that case you can see how Docker doesn't actually push the image over the Internet again, but instead just mounts the existing Lab01 image with the new `lab02` tag. Docker is smart enough to only upload images, or image layers, when a matching image or layer isn't already in the repository.

The result should be that your new image is visible in the Azure repository, similar to the experience in Lab01.

```text
$ az acr repository list -n rdlrep
[
  "lab01/gateway",
  "lab02/gateway"
]
```

### Create Deployment definition

Create a new `deploy.yaml` file in your src/Lab02/Start/Gateway directory. It is recommended to use VS Code for this purpose, as this editor supports an extension that provides Intellisense for yaml files.

Add the following to this new file:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gateway
spec:
  selector:
    matchLabels:
      app: gateway
  replicas: 1
  strategy:
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 1
  minReadySeconds: 5
  template:
    metadata:
      labels:
        app: gateway
    spec:
      containers:
      - name: gateway
        image: myrepository.azurecr.io/lab02/gateway:lab02
        resources:
          limits:
            memory: "128Mi"
            cpu: "500m"
```

This defines a Kubernetes _deployment_, describing the desired end state for deploying your image to a K8s cluster. Notice that it specifies the container image, how many replicas (instances) should be running, a rolling update strategy, runtime resource limits, and runtime environment variables.

### Create Service definition

Create a new `service.yaml` file in your src/Lab02/Start/Gateway directory.

> ℹ You can optionally append this content to your existing `deploy.yaml` file, but for learning purposes it is best to keep these concepts separate.

```yaml
apiVersion: v1
kind: Service
metadata:
  name: gateway
spec:
  type: LoadBalancer
  ports:
  - port: 80
  selector:
    app: gateway
```

This defines a Kubernetes _service endpoint_ for your deployment. Notice that it specifies the use of a `LoadBalancer` on port `80` for the `gateway` deployment.

Not all deployments need a service definition, but if your deployment needs an IP address that can be used by other containers in your K8s cluster, or from outside the K8s cluster, then you need to define a service.

### Deploy image and service

Now that you have deployment and service definitions, you can use the `kubectl` command to apply those definitions to your Kuberentes cluster.

#### Set up permissions to ACR from K8s

Before you can have any k8s cluster pull images from your Azure repository you need to provide the cluster with the credentials to the repository.

In Lab01 you did something similar by providing ACR credentials to the Azure App Service so it could pull your container image from ACR.

To do this for Kubernetes you use the `kubectl` command to create a secret that contains the credentials, and then provide the name of that secret in the `deploy.yaml` file.

As in Lab01, the admin credentials can be retrieved using the following command line (or via the web portal):

```text
az acr credential show -n myrepository
```

Now use those values to enter the following `kubectl` command.

```bash
kubectl create secret docker-registry acr-auth --docker-server myrepository.azurecr.io --docker-username <username> --docker-password <password> --docker-email <your@email.com>
```

> ⚠ Make sure to replace "myrepository", "\<username\>", "\<password\>", and "\<your\@email.com\>" with your real values.

That'll create a secret in k8s named `acr-auth` that contains read-only credentials for your Azure repository.

Now edit the `src/Lab02/Start/Gateway/deploy.yaml` file and add

```yaml
      imagePullSecrets:
      - name: acr-auth
```

The final file should look like this:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gateway
spec:
  selector:
    matchLabels:
      app: gateway
  replicas: 1
  strategy:
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 1
  minReadySeconds: 5
  template:
    metadata:
      labels:
        app: gateway
    spec:
      containers:
      - name: gateway
        image: myrepository.azurecr.io/lab02/gateway:lab02
        resources:
          limits:
            memory: "128Mi"
            cpu: "500m"
      imagePullSecrets:
      - name: acr-auth
```

The Kubernetes cluster has a secret named `acr-auth`, and the container spec references that secret. This will allow k8s to successfully pull the container image from the Azure repository when you deploy your image.

#### Deploy image

`kubectl` uses a _desired state_ philosophy. This is to say that your yaml files describe the desired state for your configuration, and when you apply those files the tooling will attempt to change the current state of the K8s cluster to match your desired state.

Right now the `gateway` deployment and service don't exist, so on first run `kubectl` will change the current state to match the state described in your yaml files.

In a CLI window, change directory to `src/Lab02/Start/Gateway` and type these commands:

```text
kubectl apply -f deploy.yaml
kubectl apply -f service.yaml
```

You can apply those same files to the cluster multiple times, but subsequent calls won't do anything because the current state will already match the desired state.

You can list the deployment and service with commands such as:

```text
$ kubectl get deployments
NAME      READY   UP-TO-DATE   AVAILABLE   AGE
gateway   0/1     1            0           6m48s

$ kubectl get services
NAME         TYPE           CLUSTER-IP      EXTERNAL-IP   PORT(S)        AGE
gateway      LoadBalancer   10.106.70.116   <pending>     80:31149/TCP   48s
kubernetes   ClusterIP      10.96.0.1       <none>        443/TCP        46d
```

The "CLUSTER-IP" value is the IP address of the service _inside the cluster_. The "EXTERNAL-IP" value is the IP address of the service _outside the cluster_. Docker Desktop k8s won't assign an actual external IP for a service, so that value will always be `localhost`.

> ℹ Creating actual external IP addresses for a service requires configuration of the k8s cluster. Most development and production clusters will have been configured to automatically provision a public IP address. This is an infrastructure IT issue, and normally developers just rely on the k8s cluster being properly configured.

## Kubernetes Dashboard

Most interaction with k8s is through the command line, but there are graphical tools to view a cluster. For example, the [Lens](https://k8slens.dev/) app.

There is also a basic dashboard you can enable within k8s itself. To enable the dashboard run this command.

```
kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.2.0/aio/deploy/recommended.yaml
```

This installs and enables the dashboard, with the requirement that you provide a security token to access the dashboard.

You will need to get the token for accessing the dashboard and that can be generated bu runing the folloing command 

```text
kubectl describe secret kubernetes-dashboard --namespace=kubernetes-dashboard
```

At the bottom of the display you'll find the token value. It is a very long string of characters. Copy this into your clipboard.

To gain access to pods running in the cluster, start the kubectl proxy.

```text
kubectl proxy
```

Now click on the below link and paste the token into the login page

http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/

This will open the browser and display the dashboard login where you can paste the token value.

Feel free to explore the dashboard and examine the nodes, pods, services, and other aspects of the cluster.

### Change number of replicas

At this point you should have the `gateway` deployment and service working in k8s.

You can see that there is one instance (replica) of `gateway` running by typing in the terminal window:

```bash
$ kubectl get pods
NAME                       READY   STATUS    RESTARTS   AGE
gateway-76fb9cd568-6lr94   1/1     Running   0          53m
```

You can see that just one replica of the image is running at this time.

One of the advantages of Kubernetes is that you can easily spin up multiple instances (replicas) of a deployment.

In a production environment your K8s cluster will normally have multiple nodes, and K8s will attempt to balance the load of your replicas across all available nodes. With Docker Desktop Kubernetes there is only one node, but that doesn't stop us from spinning up multiple replicas of a deployment.

Edit the `deploy.yaml` file and edit the `replicas` value to a value of 2.

```yaml
  replicas: 2
```

This indicates that our new _desired state_ is to run 2 replicas instead of 1. Use `kubectl` to apply this desired state to the current state of the cluster:

```bash
kubectl apply -f deploy.yaml
```

At this point you can run `kubectl get pods` to see that two instances of the image are now running in the cluster. If you were fast enough typing `kubectl get pods` after applying the change you might even see the new pod in a non-running state as it downloads the image and spins up the instance.

Go ahead and explore changing the `replicas` value to 3 and then down to 1. Quickly get the list of pods after each change to see how K8s starts and stops the various pod instances.

> ℹ Although you can edit the yaml configuration in tools like Lens or the k8s dashboard, I recommend editing your deploy.yaml files, committing them to source control, and allowing your devops pipeline to update the cluster. This provides better control and traceability compared to random edits of the live cluster state.
>
> Your IT group may have their own policies, and you should work with them to determine the best way to change running deployment, service, and other configurations.

## Cleanup

At the end of Lab02 it is important to do some basic cleanup to avoid conflicts with subsequent labs.

1. `kubectl delete deployment gateway`
1. `kubectl delete service gateway`

## References

* [Azure Container Registry authentication with service principals](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-auth-service-principal)
* [Enable the k8s dashboard in Docker Desktop](https://andrewlock.net/running-kubernetes-and-the-dashboard-with-docker-desktop/)
* [Control WSL2 resource usage](https://docs.microsoft.com/en-us/windows/wsl/wsl-config#configure-global-options-with-wslconfig)
