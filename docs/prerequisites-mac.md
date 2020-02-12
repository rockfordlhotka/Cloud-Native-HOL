# Tips for Mac Setup
## For the impatient:

(install IDE of your choice, e.g. steps 5 and 6 below)
```
/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"
brew install hyperkit minikube azure-cli kubernetes-cli kubernetes-helm
minikube config set vm-driver hyperkit
minikube start --vm-driver=hyperkit
docker pull mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim
docker pull mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim
docker pull mcr.microsoft.com/dotnet/core/sdk:3.1-buster
```
# Individual steps
1. Installing HomeBrew

	1. `/usr/bin/ruby -e "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)"`
2. Installing minikube on a mac

	1. `brew install docker`
	1. Install virtual software
		1. `brew install hyperkit`
			1. Catalina bug: [https://github.com/kubernetes/minikube/issues/5827]
		2. `brew cask install virtualbox` - backup opinion   
	1. `brew install minikube`
	1. `brew install azure-cli`
   1. `brew install kubernetes-cli`
   1. `brew install kubernetes-helm`
   
3. Start minikube for package download
	1. VirtualBox: 
		1. `minikube config set vm-driver virtualbox`
		2. `minikube start --vm-driver virtualbox --host-only-cidr 172.16.0.1/24`
	1. HyperKit: 
		1. `minikube config set vm-driver hyperkit`
		2. `minikube start --vm-driver=hyperkit`
4. Pull large Docker base images
    1. `docker pull mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim`
    1. `docker pull mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim`
    1. `docker pull mcr.microsoft.com/dotnet/core/sdk:3.1-buster`
5. Install Visual Studio Code
    1. `brew cask install visual-studio-code`
6. Install Visual Studio for Mac
    1. `brew cask install visual-studio`
