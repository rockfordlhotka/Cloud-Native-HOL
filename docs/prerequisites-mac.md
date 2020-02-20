# Tips for Mac Setup

## For the impatient

(install IDE of your choice, e.g. steps 5 and 6 below)

```text
/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"
brew install hyperkit minikube azure-cli kubernetes-cli kubernetes-helm
minikube config set vm-driver hyperkit
minikube start --vm-driver=hyperkit
docker pull mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim
docker pull mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim
docker pull mcr.microsoft.com/dotnet/core/sdk:3.1-buster
```

## Individual steps

1. Installing HomeBrew
   1. `/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"`
1. Installing minikube on a mac
   1. `brew install docker`
   1. Install virtual software
      1. `brew install hyperkit`
      1. Catalina bug: [https://github.com/kubernetes/minikube/issues/5827]
      1. `brew cask install virtualbox` - backup opinion
   1. `brew install minikube`
   1. `brew install azure-cli`
   1. `brew install kubernetes-cli`
   1. `brew install kubernetes-helm`
1. Start minikube for package download
   1. If you installed Hyperkit from the previous section: 
      1. `minikube config set vm-driver hyperkit`
      1. `minikube start --vm-driver=hyperkit`
      1. Proceed to Pull large Docker base images section
   1. If you installed VirtualBox from the previous section: 
      1. `minikube config set vm-driver virtualbox`
      1. `minikube start --vm-driver virtualbox --host-only-cidr 172.16.0.1/24`
1. Pull large Docker base images
    1. `docker pull mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim`
    1. `docker pull mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim`
    1. `docker pull mcr.microsoft.com/dotnet/core/sdk:3.1-buster`
1. Install Visual Studio Code
    1. `brew cask install visual-studio-code`
1. Install Visual Studio for Mac
    1. `brew cask install visual-studio`
1. Install dotNet Core
   1. https://dotnet.microsoft.com/download/dotnet-core/thank-you/sdk-3.1.101-macos-x64-installer
   1. If you receive a popup indicating that you the software is untrusted do the following: (Source https://github.com/dotnet/core/issues/3685#issuecomment-547411254)
      1. Show the file in the Finder and open context menu by right click on the downloaded package. Then select Open and a similar dialog will appear however there will be Open button available. When you open it in this way it will execute the package.

It is caused by Apple security and it forces you to explicitly open the downloaded content. Additional details are on Apple web pages: https://support.apple.com/en-us/HT202491.

## Troubleshooting 

### Docker Desktop for Mac 
   
If you have Docker Desktop previously installed, you may already have a version of kubectl (kubernetes-cli) already installed.
The client version of kubectl provided by the latest Docker Desktop install may be incompatible with the latest version of Kubernetes.
When starting minikube, you may receive the following error:
   
```
⚠️  /usr/local/bin/kubectl is version 1.15.5, and is incompatible with Kubernetes 1.17.2. You will need to update /usr/local/bin/kubectl or use 'minikube kubectl' to connect with this cluster
```
   
To update kubectl to the latest version, execute the following:

1. `brew install kubernetes-cli` 
1. `brew link --overwrite kubernetes-cli` - 'To force the link and overwrite all conflicting files'.  The command does NOT affect the previous install, instead performs a reorg of symlinks in /usr/local/bin.
1. `kubectl version` - verify your new and updated install of kubernetes.cli
1. `kubectl.docker version` - verify your previous install of kubernetes.cli
1. Run `minikube start` and verify successful startup.  
