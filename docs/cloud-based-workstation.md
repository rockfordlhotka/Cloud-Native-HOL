# Using a Cloud-Based Development Workstation

These instructions may be useful if you do not have access to a laptop with the minimum hardware requirements for cloud-native software development, or if your IT group has restrictions in place that prevent you from installing or using the required cloud-native software tools.

Cloud providers, such as Microsoft Azure, allow you to create virtual machines that meet the minimum hardware requirements, and which aren't restricted by your IT policies.

⚠️ Bypassing your IT policies may be problematic based on your employment. You should confirm that your organization will allow you to use an external workstation for the purpose of taking this hands-on lab.

## Nested Virtualization

In this lab we will make use of Docker Desktop. Docker Desktop requires virtualization so it can run your containers in virtual machines. Because you will be creating your actual _workstation_ as a virtual machine, this means that you will be using something called _nested virtualization_.

Not all virtual machines support nested virtualization. For example, in Azure, the low-end virtual machine option do not support this concept. You need to choose a higher-end hosting option for your virtual machine. If you choose an option with at least 4 virtual CPUs and 16 gigs of RAM it will most likely support nested virtualization (based on my experience).

## Controlling Cost

Because you will be using a large and powerful hosting option for your virtual machine workstation in the cloud, the cost of running that VM may be fairly high. It is important to remember that your hosting provider may only charge you for the hours when the VM is actually running. For example, Azure does not charge you for the hours where your VM is shut down and decallocated, they only charge you for the time that your VM is actually loaded in memory and is running in the cloud.

As you set up your VM, look for options that support auto-shutdown of the VM. Azure supports this concept, and can help you avoid having the VM run when you aren't using it, thus saving you money (or Azure credits).

## Using a Free Azure Trial Subscription

If you do not have access to a cloud provider subscription, you may consider using a free Azure trial subscription. Microsoft provides a substantial amount of free Azure resources for a limited time as a trial. In my experience, this trial provides sufficient cloud time to host a VM and do everything else required for the hands-on lab: assuming you shut down the VM when you are not using it (at night, for example).
