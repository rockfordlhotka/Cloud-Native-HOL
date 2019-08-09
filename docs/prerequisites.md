# Prerequisites for attendees

## What you should know

We assume familiarity with

1. ASP.NET server-side code development
1. Know how to pull down NuGet packages into a project
1. Basic usage of git source control

## Services you should have pre-configured

We will use some cloud-based services

1. You should have an active GitHub account
1. You must have an active Azure subscription with credits remaining (your Visual Studio Subscription provides some number of free Azure credits)

## The workstation you should bring

**Before arriving at the event** you should make sure your laptop workstation has the following:

> Assuming PC, but if you have a Mac you should have comparable tooling installed.

1. Windows 10 1809 or later
1. Visual Studio 2017 or later (preferably 2019)
1. [Visual Studio Code](https://code.visualstudio.com/)
1. Install the latest [.NET Core SDK](https://dotnet.microsoft.com/download)
1. Install [git for Windows](https://git-scm.com/download/win)
   1. (optional) Install git with Chocolatey via `choco install git` as per step 9
1. Install/activate the [HyperV feature](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/quick-start/enable-hyper-v) in Windows
1. Install [Docker for Windows](https://docs.docker.com/docker-for-windows/)
1. Install [Chocolatey](https://chocolatey.org)
1. Using Chocolatey from an _admin_ command line
   1. `choco install kubernetes-cli`
   1. `choco install kubernetes-helm`
   1. `choco install minikube`
   1. `choco install azure-cli`
1. Start minikube one time to download/initialize all the container images from an _admin_ command line
   1. This may take several minutes as it downloads numerous images
   1. From Git Bash
      1. `winpty minikube start --vm-driver hyperv --hyperv-virtual-switch "Default Switch"`
      1. `winpty minikube ssh "sudo poweroff"`
      1. `winpty` is included in Git Bash - it is not a separate download/install
   1. From cmd or PowerShell
      1. `minikube start --vm-driver hyperv --hyperv-virtual-switch "Default Switch"`
      1. `minikube ssh "sudo poweroff"`
   1. **If running on Windows 10 before 1809** [this blog post](https://www.c-sharpcorner.com/article/getting-started-with-kubernetes-on-windows-10-using-hyperv-and-minikube/) might help get minikube installed
1. Clone this repo to your workstation: `git clone https://github.com/rockfordlhotka/Cloud-Native-HOL.git`
   1. There may be some last-minute fixes to the code so we recommend waiting to clone the repository until a day or two prior to the HOL
1. (optional) [Windows Subsystem for Linux (WSL)](https://docs.microsoft.com/en-us/windows/wsl/install-win10)
   1. The following assumes Ubuntu or Debian
   1. Inside WSL install `sudo apt-get install git`
   1. Inside WSL install `sudo apt-get install azure-cli` (Use the instructions at: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-apt?view=azure-cli-latest)
   1. Inside WSL install `sudo apt-get install kubernetes-cli` (Use the instructions at: https://kubernetes.io/docs/tasks/tools/install-kubectl/#install-kubectl-on-linux)
   
⚠ Arriving with a pre-configured workstation is imperative.
