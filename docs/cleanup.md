# Cleanup After Workshop

This document describes some cleanup steps you might consider after the workshop is complete.

## Stopping Services

You will probably want to stop services that are running as part of the workshop.

### Azure

In Azure, you can remove all services and resources by deleting the Resource Group in which you created all the resources.

```text
az group delete --name MyGroup
```

As always, replacing `MyGroup` with your resource group name.

### Docker Desktop Kubernetes

The most agressive way to remove all your Kubernetes resources is to use the Docker Desktop settings panel to either _reset_ Kubernetes, or remove Kubernetes entirely.

Alternately, at the end of each lab there is a _Cleanup_ section that describes how to remove the resources used in Docker and Kubernetes.

For the most part, if you delete each _deployment_ and _service_ using kubectl, that'll remove everything from the cluster. For example, at the end of Lab02 the cleanup section describes this:

1. `kubectl delete deployment gateway`
1. `kubectl delete service gateway`

Other labs have different steps in their cleanup section at the end.

## Uninstalling Software

In the most extreme case, you might choose to uninstall the software installed from the prerequisites document. That will result in your workstation no longer being set up for cloud-native development.

You can use `choco` to uninstall all the packages installed from the prerequisite document, and then you can uninstall Chocolatey itself.
