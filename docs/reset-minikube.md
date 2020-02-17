# If minikube gets "confused"

It is possible to get minikube confused such that it won't function properly. If this happens, here's how to totally uninstall and reset minikube.

1. Start Hyper-V Manager
   1. Stop the minikube VM if it is running
   1. Delete the minikube VM
1. In Windows, uninstall minikube
   1. `choco uninstall minikube`
1. Delete your `~/.minikube` directory
   1. `cd ~`
   1. `rm -rf .minikube`
1. Reinstall minikube
   1. `choco install minikube`

# minikube notes and errata

1. See details about minikube as it starts
   * `--alsologtostderr -v=8  //shows log and errors for minikube start`
1. The --cpus and --memory switches appear to be ingored on Windows, you must directly change the VM settings via Hyper-V Manager instead

# Docker Desktop for Mac 

If you have Docker Desktop previously installed, you may already have a version of kubectl (kubernetes-cli) already installed.
The client version of kubectl provided by the latest Docker Desktop install may be incompatible with the latest version of Kubernetes.
When starting minikube, you may receive the following error:

```
⚠️  /usr/local/bin/kubectl is version 1.15.5, and is incompatible with Kubernetes 1.17.2. You will need to update /usr/local/bin/kubectl or use 'minikube kubectl' to connect with this cluster
```

To update kubectl to the latest version, execute the following:

1. `brew install kubernetes-cli` 
1. `brew link --overwrite kubernetes-cli` - 'To force the link and overwrite all conflicting files'.  The command does NOT affect the previous install, instead performs a reorg of symlinks in /usr/local/bin.
1  `kubectl version` - verify your new and updated install of kubernetes.cli
1. `kubectl.docker version` - verify your previous install of kubernetes.cli
1. Run `minikube start` and verify successful startup.  


 
  
