# Creating an Azure VM with lab prerequisites

## Before you begin

You will need:

* Auzure subscription, for example included with a Visual Studio subscription (formerly MSDN subscription).
* Visual Studio 2019 Pro or Enterprise subscription
* Docker ID username and password
  * If you don't already have one, you can create one along the way, while waiting on a long-running process:
browse to **https://hub.docker.com/?overlay=onboarding**, select **Create Account**, and follow the directions.

## Create an Azure VM

This section will:

* create an Azure VM
* from a Visual Studio 2019 image
* using a new resource group
* on a Standard D4_v3 VM size or higher, and
* enable RDP access.

### Steps

1. Log on the **[Azure portal](https://portal.azure.com)**.
1. Select **Virtual Machines | Add**.
1. On **Create a virtual machine**, select the desired **Subscription** if you have more than one.
1. For **Resource group**, select **Create new**.
   * Recommendation: create a new resource group for this VM so that the VM does not accidentally 
get deleted when cleaning up from individual lab activities.
1. For **Name**, enter **`CloudNativeVMRG`** and select **OK**.
1. For **Virtual machine name**, enter **`CloudNativeVM0`**.
1. For **Region**, select a region near you.
1. For **Image**, select **Browse all public and private images**.
1. On **Select an image**, on the **Marketplace** tab, in the left navigation select **Compute**.
1. In the search box, enter **`Visual Studio 2019`**.
1. Select **Visual Studio 2019 Enterprise on Windows 10 Enterprise (x64)** from Microsoft.
1. For **Size**, select **Change size**.
    * N.B.: only v3 VM sizes support nested Hyper-V, 
which you will need in order to enable Hyper-V on the VM, which is required for these labs.
1. On **Select a VM size**, in the searh box, enter **`v3`**.
1. Select **D4_v3** or higher, and then select **Select**.
    * N.B.: I believe that a VM with at least 4 VCPUs is required: 
I tried with a D2_v3 which ran at 100% CPU utilization when installing the prerequisites, and produced errors (see Event Log) presumably due to timeouts.
1. For **Username**, enter a user name for the VM admin account.
1. For **Password** and **Confirm password**, enter a password for the VM admin account.
1. For **Public inbound ports**, select **Allow selected ports**.
1. For **Select inbound ports**, select **`RDP (3389)`**.
1. At the bottom of the page, select **Review + create**.
1. On **Create a virtual machine**, at the bottome of the page, select **Create**.
1. The **Your deployment is underway** page is displayed. This page will update, no need to refresh.
    * This will take several minutes. This is a good time to get a Docker ID if you don't already have one.
1. When complete, the page title will change to **Your deployment is complete**.

## Remote into Azure VM

1. On the **Your deployment is complete** page, select **Go to resource**.
1. On the **CloudNativeVM0** page, in the top toolbar, select **Connect**.
1. On the **Connect to virtual machine** pane on the right, on the **RDP** tab, select **Download RDP File**.
1. Use your browser, save the RDP file to a convenient folder on your workstation so that it will be handy for restarting RDP sessions.
1. In Windows File Explorer, navigate to the downloaded RDP file.
1. Double-click on the RDP file.
1. In the **Remote Desktop Connection | The publisher of this remote connection...** warning dialog, 
select **Don't ask me again for connections to this computer**, and then select **Connect**.
1. In the **Windows Security | Enter your credentials** dialog, select **More choices**, then select **Use a different account**.
1. For **User name** and **Password**, enter the credentials for the VM admin account you specified when creating the VM.
1. Select **Remember me** so that the user name, but not the password, is saved.
1. Select **OK**.
1. In the **Remote Desktop Connection | The identity of the remote computer...** warning dialog, 
select **Don't ask me again for connections to this computer**, then select **Yes**.
1. The Remote Desktop session is displayed. This will take a few minutes for first-time execution.
1. In the Remote Desktop session, the **Networks** pane is shown at the right, 
select **No** to disallow the VM from being discovered by other devices.

### Download Chrome and set as default

I had problems within an Azure VM using the **`az login`** (which launches the default browser to select credentials) 
with Edge as the default browser, but it worked with Chrome as the default browser.

1. In the Remote Desktop session, launch Microsoft Edge.
1. In Edge, navigate to **`https://www.google.com/chrome/`**.
1. Select **Download Chrome**.
1. In the **Download Chrome for Windows** dialog, uncheck **Help make Google Chrome better...**, and then select **Accept and Install**.
1. In the browser download window, select **Run**.
1. When the installation is completed, a Chrome browser is launched with the **Welcome to Chrome** page displayed.
1. In the **Welcome to Chrome** page, select **Open Windows Settings**.
1. In the **Choose an app** dialog, select **Google Chrome**.
1. In the **Before you switch** dialog, *[Sigh. --ed.]*, select **Switch anyway**.
1. Close all windows. 

### Initialize Visual Studio 2019

1. In the Remote Desktop session, launch Visual Studio 2019.
1. In the **Visual Studio | Welcome** dialog:
   1. If you have a Visual Studio ***Enterprise subscription***, select **Sign in**.
      1. Sign in using your Visual Studio account credentials.
      1. In the **Visual Studio | Enterprise 2019** window, displaying the "License required" warning, select **Check for an updated license**.
   1. Otherwise, select **Not now, maybe later**.
      1. I believe this will give you a trial license, but I have not tried it.

### Enable Hyper-V

1. In the Remote Desktop session, launch Windows PowerShell ***as administrator***.
1. In Windows PowerShell, enable Hyper-V with this command:
   * **`Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All`**
1. When complete, it will prompt to restart the computer to complete the operation, enter **`Y`** for Yes.
   * Restarting the VM will end the Remote Desktop session. 
You can monitor the VM restart state in the Azure portal, if you want.

### Install Docker for Windows

1. In your host computer, in Windows File Explorer, double-click on the RDP file previously downloaded.
   * If you launch RDP too soon, opening the connection will timeout and you'll get an error dialog. Try again in a little bit.
1. In the **Windows Security | Enter your credentials** dialog, enter your VM admin account password, then select **OK**.
1. In the Remote Desktop session, launch a browser and navigate to **https://hub.docker.com/?overlay=onboarding**.
1. In the **Sign in with your Docker ID**, enter your Docker ID and password if you already have them; 
otherwise select **Create Account** and follow the instructions.
1. On the Docker **Quick Start | Download** page, select **Download Docker Desktop for Windows**.
1. In your browser download window, select **Run** or **Open** depending on the browser.
1. In the confirmation window, select **OK**.
1. When the installation is complete, the Installing Docker Desktop window displays "Installation succeeded".
1. Select **Close and log out**.
   * The automated Windows log out will end the Remote Desktop session. 
This does not stop the VM, so you can restart RDP immediately.
1. In your host computer, in Windows File Explorer, double-click on the RDP file previously downloaded.
1. In the **Windows Security | Enter your credentials** dialog, enter your VM admin account password, then select **OK**.
1. In the Remote Desktop session, Windows will start Docker Desktop, and after a short time a Docker Welcome window will display.
1. In the **Docker Desktop | Welcome** window, enter your Docker ID and password, then select **Log In**.

### Install Chocolatey

1. Continuing in the Remote Desktop session, launch Windows PowerShell ***as administrator***.
1. In Windows PowerShell, install Chocolatey with this command:
   * **`Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))`**
   * *Source: https://chocolatey.org/install*
1. Close the Windows PowerShell windows in order for the choco command to be available.

### Clone Cloud-Native-HOL repository

1. Continuing in the Remote Desktop session, launch Windows PowerShell ***as administrator***.
1. In Windows PowerShell, enter the following two commands to clone this lab's repository to your repos folder:
   1. **`cd c:\Users\vm-admin-username\source\repos\`**
      * *where **vm-admin-username** is replaced with your VM admin account username.*
   1. **`git clone https://github.com/rockfordlhotka/Cloud-Native-HOL.git`** 

### Install remaining prerequisites

1. Continuing in Windows PowerShell, enter this command to enable running unsigned scripts:
   * **`Set-ExecutionPolicy Unrestricted -Force`**
1. Enter this command to change to the scripts folder:
   * **`cd Cloud-Native-HOL\docs\scripts`**
1. Enter this command to install the remaining prerequisites:
   * **`.\Install-CloudNativePrerequisites.ps1`**
   * This installs the following software:
     * Git for Windows (update)
     * .NET Core SDK (update, this will first install several packages for relevent updates)
     * Kubernetes CLI
     * Kubernetes Helm
     * Minikube
     * Azure CLI 

### Start minikube, initialize helm

1. Continuing in Windows PowerShell, enter this command to start minikube:
   * **`minikube start --vm-driver hyperv --hyperv-virtual-switch "Default Switch"`**
     * This will take several minutes. (Data point: 6 minutes. YMMV.)
1. Once minikube is started, enter this command in initialize helm:
   * **`helm init`**
1. Leave minikube running.

### Fixing kubectl.exe to use the correct version

Lab00 includes instructions (in the **Kubernetes CLI** section, under "To fix this on Windows") 
to get the correct version of kubectl.exe. 
This is simple to address here.

1. Launch a browser, and in the address bar enter:
   * **`https://storage.googleapis.com/kubernetes-release/release/v1.15.2/bin/windows/amd64/kubectl.exe`**
1. In the browser download window, save the **kubectl.exe** file to a convenient folder, e.g., the Downloads folder.
1. In Windows Explorer, cut the downloaded **kubectl.exe** from the convenient folder and paste it in:
   * **`C:\Program Files\Docker\Docker\resources\bin`** 
1. In the **Replace or Skip Files** dialog, select **Replace the file in the destination**.

### Next step

Proceed to Lab00 to validate the installed software.
