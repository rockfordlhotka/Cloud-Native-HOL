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
