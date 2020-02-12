# Tips for Mac Setup
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
	1. HyperKit: 
		1. `minikube config set vm-driver hyperkit`
		2. `minikube start --vm-driver=hyperkit`
	1. VirtualBox: 
		1. `minikube config set vm-driver virtualbox`
		2. `minikube start --vm-driver virtualbox --host-only-cidr 172.16.0.1/24`
4. Pull large Docker base images
    1. `docker pull mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim`
    1. `docker pull mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim`
    1. `docker pull mcr.microsoft.com/dotnet/core/sdk:3.1-buster`
5. Install Visual Studio Code
    1. `brew cask install visual-studio-code`
6. Install Visual Studio for Mac
    1. `brew cask install visual-studio`
7. Install dotNet Core
	1. https://dotnet.microsoft.com/download/dotnet-core/thank-you/sdk-3.1.101-macos-x64-installer
	1. If you receive a popup indicating that you the software is untrusted do the following: (Source https://github.com/dotnet/core/issues/3685#issuecomment-547411254)
	Show the file in the Finder and open context menu by right click on the downloaded package. Then select Open and a similar dialog will appear however there will be Open button available. When you open it in this way it will execute the package.

It is caused by Apple security and it forces you to explicitly open the downloaded content. Additional details are on Apple web pages: https://support.apple.com/en-us/HT202491.
