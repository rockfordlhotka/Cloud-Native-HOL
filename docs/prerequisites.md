# Prerequisites for attendees

## What you should know

We assume familiarity with

1. ASP.NET server-side code development
1. ADO.NET and/or EF data access concepts
1. Know how to pull down NuGet packages into a project
1. Basic usage of git source control

## Services you should have pre-configured

We will use some cloud-based services

1. You should have an active GitHub or visualstudio.com subscription
1. (optional) You should have an active Azure subscription with credits remaining (if you have MSDN you have access to some amount of Azure credits every month - and you must have MSDN if you have Visual Studio)

## The workstation you should bring

**Before arriving at the event** you should make sure your laptop workstation has the following (assuming PC, but if you have a Mac you should have comparable tooling installed)

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
   1. `choco install minikube`
   1. `choco install azure-cli`
1. Clone this repo to your workstation: `git clone https://github.com/rockfordlhotka/Cloud-Native-HOL.git`
   1. There may be some last-minute fixes to the code so we recommend waiting to clone the repository until a day or two prior to the HOL

Arriving with a pre-configured workstation is imperative.
