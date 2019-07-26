# Confirming Installation of Prerequisites

In this lab we'll confirm that everyone has the software installed for subsequent labs.

See this [list of prerequisites](https://github.com/rockfordlhotka/Cloud-Native-HOL/blob/master/docs/prerequisites.md).

## Chocolatey

1. Open a console window (CLI) of your choice
1. Type `choco --version`
1. Ensure that the version is at least `0.10.15`

## Git for Windows

1. Open the Git Bash CLI
1. Type `git --version`
1. Ensure that the version is at least `2.19.0`

## Clone/update local repo

If you have not yet cloned the repo

1. Change to your root source directory (such as `cd /c/src`)
1. `git clone https://github.com/rockfordlhotka/Cloud-Native-HOL.git`

If you have already cloned the repo do a pull

1. Change to the repo directory (such as `cd /c/src/Cloud-Native-HOL`)
1. `git pull`

This should ensure that you have a local copy of the latest content from GitHub.

## .NET Core

1. Open the Git Bash CLI
1. Type `dotnet --version`
1. Ensure that the version is at least `2.1.700`

## Docker

1. Open the Git Bash CLI
1. Type `docker --version`
1. Ensure that the version is at least `18.09.2`
1. Type `docker run hello-world`
1. You should get several lines of output, starting with `Hello from Docker!`

## minikube and Helm

1. Open the Git Bash CLI (**as admin**)
1. Type `minikube version`
1. Ensure that the version is at least `1.2.0`
1. Type `minikube status`
1. Output should appear similar to: ![mkstatus](images/mkstatus.png)
   1. If minikube is not running follow the instructions to start minikube
1. Close admin CLI window (type `exit`)

### Starting minikube

1. Change directory to the `Lab00` in this repo
1. Type `chmod +x mkstart.sh`
1. Type `chmod +x mkstop.sh`
1. Type `./mkstart.sh` to start minikube
   1. Type `cat mkstart.sh` to view script contents

### Initialize Helm

1. minikube must be running
1. Type `helm init`

### Stopping minikube

1. Type `./mkstop.sh` to stop minikube
   1. Type `cat mkstop.sh` to view script contents

## Kubernetes CLI

1. Open the Git Bash CLI
1. Type `kubectl version`
1. The result will be version numbers for numerous components
   1. Client versions are for the Kubernetes CLI
   2. Server versions are for minikube

![kubectl version](images/kubectlversion.png)

## Azure CLI

1. Open the Git Bash CLI
1. Type `az --version`
1. Ensure that the version is at least `2.0.68`
1. Type `az login`
   1. You should get see a browser window
   1. Log into your Microsoft Azure account
   1. The console should now list your subscriptions
