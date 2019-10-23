# Confirming Installation of Prerequisites

In this lab we'll confirm that everyone has the software installed for subsequent labs.

See this [list of prerequisites](https://github.com/rockfordlhotka/Cloud-Native-HOL/blob/master/docs/prerequisites.md). ⚠ These must be installed _before arriving at the event_!

## Bash Aliases

1. Open Git Bash
1. Type 'cd ~' to navigate to your user home directory
1. Type `code .bashrc` to edit your profile file
1. Edit the file to match this, then save and exit:

```bash
alias az='winpty az.cmd'
alias minikube='winpty minikube.exe'
alias docker='winpty docker'
```

Close the CLI window when done.

## Chocolatey

1. Open Git Bash
1. Type `choco --version`
1. Ensure that the version is at least `0.10.15`

## Git for Windows

1. Open Git Bash
1. Type `git --version`
1. Ensure that the version is at least `2.23.0`

## Clone/update local repo

If you have not yet cloned the repo

1. Change to your root source directory (such as `cd /c/src`)
1. `git clone https://github.com/rockfordlhotka/Cloud-Native-HOL.git`

If you have already cloned the repo do a pull

1. Change to the repo directory (such as `cd /c/src/Cloud-Native-HOL`)
1. `git pull`

This should ensure that you have a local copy of the latest content from GitHub.

## .NET Core

1. Open Git Bash
1. Type `dotnet --version`
1. Ensure that the version is at least `3.0.100`

## Docker

1. Open the Git Bash CLI
1. Type `docker --version`
1. Ensure that the version is at least `10.03.2`
1. Type `docker run hello-world`
   1. You should get several lines of output, starting with `Hello from Docker!`
1. Share your local drives with Docker
   1. Right-click on the Docker icon in system tray
   1. Select Settings
   1. Choose the Shared Drives tab
   1. Select your working drive(s)

![shared drives](images/shared-drives.png)

## minikube and Helm

1. Open the Git Bash CLI (**as admin**)
1. Type `minikube version`
1. Ensure that the version is at least `1.4.0`
1. Type `minikube status`
1. Output should appear similar to: ![mkstatus](images/mkstatus.png)
   1. If minikube is not running follow the instructions to start minikube

### Starting minikube

1. Type `winpty minikube start --vm-driver hyperv --hyperv-virtual-switch "Default Switch" --cpus 6 --memory 4096 --kubernetes-version=1.16.0`

Alternately you can use pre-built bash shell scripts to start/stop the cluster:

1. Change directory to the `Lab00` in this repo
1. Type `chmod +x mkstart.sh`
1. Type `chmod +x mkstop.sh`
1. Type `./mkstart.sh` to start minikube
   1. Type `cat mkstart.sh` to view script contents

### Initialize Helm

1. minikube must be running
1. Type `helm init`
1. If this fails it may be due to this [SO issue](https://github.com/helm/helm/issues/6374)

### Stopping minikube

**🛑 IMPORTANT:** Don't actually stop minikube, as you'll be using it throughout the day. However, when you do want to stop minikube this is how you do it.

1. Type `winpty minikube ssh "sudo poweroff"`

Alternately you can use the pre-built bash command script:

1. Type `./mkstop.sh` to stop minikube
   1. Type `cat mkstop.sh` to view script contents


Finally: Close the admin CLI window (type `exit`)

## Kubernetes CLI

1. Open Git Bash
1. Type `kubectl version`
1. The result will be version numbers for numerous components
   1. Client versions are for the Kubernetes CLI
   2. Server versions are for minikube

![kubectl version](images/kubectlversion.png)

**⚠ NOTE:** It is very likely that your path (Windows and Mac) will have you running `kubectl` from Docker rather than the one directly installed for Kubernetes. This is a problem, because the Docker install is outdated. In the image above you can see that the client is version `v1.14.6`, but the minimum version we need is at least `v1.15.2`.

To fix this on Windows:

1. Open a Git Bash CLI window _as administrator_
1. Change directory to `/c/Program\ Files/Docker/Docker/resources/bin`
1. Type `curl -LO https://storage.googleapis.com/kubernetes-release/release/v1.16.0/bin/windows/amd64/kubectl.exe`

This will download version `v1.16.0` of the tool, overwriting the older version in the Docker directory.

For Mac users you can do something similar, or change a symlink as described in this [Stackoverflow thread](https://stackoverflow.com/questions/55417410/kubernetes-create-deployment-unexpected-schemaerror).

## Azure CLI

1. Open Git Bash
1. Type `az --version`
1. Ensure that the version is at least `2.0.74`
1. Type `az login`
   1. You should get see a browser window
   1. Log into your Microsoft Azure account
   1. The console should now list your subscriptions
