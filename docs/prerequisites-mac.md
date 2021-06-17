# Mac Workstation Setup

1. Install HomeBrew
   1. `/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"`
1. Installing tools
   1. `brew install docker`
   1. `brew install azure-cli`
   1. `brew install kubernetes-cli`
   1. `brew install kubernetes-helm`
1. Enable Kubernetes in Docker Desktop
   1. Open the Docker Desktop settings page
   1. Select the Kubernetes tab
   1. Check the option to enable Kubernetes
   1. Restart Docker Desktop
   1. It may take several minutes to download and initialize the Kubernetes cluster images
1. Pull large Docker base images
   1. `docker pull mcr.microsoft.com/dotnet/aspnet:5.0`
   1. `docker pull mcr.microsoft.com/dotnet/runtime:5.0`
   1. `docker pull mcr.microsoft.com/dotnet/sdk:5.0`
1. Install Visual Studio Code
    1. `brew cask install visual-studio-code`
1. Install Visual Studio for Mac
    1. `brew cask install visual-studio`
1. Install .NET 5 SDK
   1. https://docs.microsoft.com/en-us/dotnet/core/install/macos
