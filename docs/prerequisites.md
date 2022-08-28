# Prerequisites for attendees

⚠ **Arriving with a pre-configured workstation is critical.**

Building cloud-native apps requires a powerful developer device, and quite a lot of software tooling. Although it would be ideal to install all the required tools as part of the in-person lab, hotel wifi is usually not fast enough to support an entire classroom of people downloading gigabytes of installers.

As a result, you should plan on spending some time prior to traveling to the event so you can install the required tools.

Also be aware that restrictive IT policies may interfere with installing or running some of the required tools. Restrictive IT policies are not compatible with cloud-native software development, because many of the required cloud-native tools are too new for some IT groups to recognize their value and utility.

If you find that you can not install the required software, or do not have a sufficiently powerful developer laptop, you might consider creating a [virtual machine in Azure or another cloud provider](cloud-based-workstation.md) where you can run Windows 11 and install the required tooling.

⚠ **Arriving with a pre-configured workstation is critical.**

## What you should know

We assume familiarity with

1. ASP.NET server-side code development
1. Know how to pull down NuGet packages into a project
1. Basic usage of git source control

## Services you should have pre-configured

We will use some cloud-based services

1. You should have an active GitHub account
1. You **must have** an active Azure subscription with credits remaining (your Visual Studio Subscription typically provides some number of free Azure credits)
   * If you do not have an Azure subscription, or if you are using your organization's subscription and do not have the rights to create an Azure Resource Group, please consider creating a free trial subscription for use during the hands-on lab.

## The workstation you should bring

Minimum hardware required:

* CPU with _at least_ 4 virtual cores (2 cores with hyperthreading)
  * i7 with 8 virtual cores (4 cores with hyperthreading) is recommended
* 8gb RAM is the bare minimum required
  * 16 is recommended
* High speed HDD
  * SSD strongly recommended
* Ability to connect to wifi

**Before arriving at the event** you should make sure your laptop workstation has the following:

> ℹ Assuming PC, but if you have a Mac you should have [comparable tooling installed](https://github.com/rockfordlhotka/Cloud-Native-HOL/blob/master/docs/prerequisites-mac.md).

1. Windows 11 (or Windows 10 2004 or later)
1. Visual Studio 2022
1. Install/activate the [HyperV feature](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/quick-start/enable-hyper-v) in Windows
1. [Windows Subsystem for Linux (WSL2)](https://docs.microsoft.com/en-us/windows/wsl/install-win10)
   1. The following assumes Ubuntu or Debian
   1. Inside WSL install `sudo apt install git`
   1. Inside WSL install the Azure CLI command (Use the instructions at: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-apt?view=azure-cli-latest)
   1. Inside WSL install the Kubernetes CLI commands (Use the instructions at: https://kubernetes.io/docs/tasks/tools/install-kubectl/#install-kubectl-on-linux) ⚠️Kubernetes is not installed yet, so `kubectl` commands will not yet work
   1. You may want to follow the instructions in this blog so [WSL can seamlessly interact with Docker Desktop](https://nickjanetakis.com/blog/setting-up-docker-for-windows-and-wsl-to-work-flawlessly)
1. Install [Chocolatey](https://chocolatey.org) on Windows
1. Using Chocolatey from an _admin_ command line (cmd or PowerShell)
   1. `choco install dotnetcore-sdk` (or install the latest [.NET Core SDK](https://dotnet.microsoft.com/download) manually)
   1. `choco install git`
   1. `choco install microsoft-windows-terminal`
   1. `choco install docker-desktop`
   1. `choco install kubernetes-cli`
   1. `choco install kubernetes-helm`
   1. `choco install azure-cli`
   1. `choco install vscode` (or install [Visual Studio Code](https://code.visualstudio.com/) manually)
1. Enable Kubernetes in Docker Desktop
   1. Open the Docker Desktop settings page
   1. Select the Kubernetes tab
   1. Check the option to enable Kubernetes
   1. Restart Docker Desktop
   1. It may take several minutes to download and initialize the Kubernetes cluster images
      * ⚠️ You **MUST** see two green boxes in the lower-left of the Docker Desktop window. The first indicates Docker is working, the second indicates Kubernetes is working. ![](images/docker-k8s.png)
1. Pull large Docker base images
   1. `docker pull mcr.microsoft.com/dotnet/aspnet:6.0`
   1. `docker pull mcr.microsoft.com/dotnet/runtime:6.0`
   1. `docker pull mcr.microsoft.com/dotnet/sdk:6.0`
1. Clone this repo to your workstation: `git clone https://github.com/rockfordlhotka/Cloud-Native-HOL.git`
   1. There may be some last-minute fixes to the code so we recommend waiting to clone the repository until a day or two prior to the HOL

⚠ **Arriving with a pre-configured workstation is critical.**
