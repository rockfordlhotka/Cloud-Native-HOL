#Infrastructure As Code 

Terraform provides an opportunity to define/deploy the Azure resources for the labs and then destroy them.

##Motivation
To be able to deploy/destroy the resources for the labs. (Lab01 is the only lab supported).

##Pre-requisites
1. Azure CLI - the templates use the Azure CLI as it's provider.
1. Azure Subscription configured for use with the AWS CLI.

##Installation (Mac)
Install terraform using your package manager.
`homebrew install terraform`

Validate that terraform is installed and on your path. 
`terraform -v` 


##Modules
Each module is intended to be run independently.  Below is a description of the modules available:
1. acr - sets up the azure resource group and creates a container repository.  Credentials are outputted to the console.
1. webapp - after publishing your container image to acr, run this module to publish your webapp to azure.  The url is outputted to the console.

##How To Run
    
1. Initialize, Create Workspace (dev) and Execute the template
```
# Navigate to the appropriate directory (from this directory)
cd acr
# Initialize provider plugins.
terraform init
# Create new workspace
terraform workspace new dev
# Run terraform plan to see the resources that will be created.
terraform plan
# Apply the template - create the resources on Azure 
terraform apply
```
1. Go back to the lab (for instructions below):
1. Build your docker image for your web app 
1. Push your docker image to acr.  (You will need the credentials outputted to the console.)  
1. Modify /terraform/azure/webapp/variables.tf - specifically look to update container_image and container_image_version.

```
# Navigate to the appropriate directory (from current directory)
cd ../webapp
# Initialize provider plugins.
terraform init
# Create new workspace
terraform workspace new dev
# Run terraform plan to see the resources that will be created.
terraform plan
# Apply the template - create the resources on Azure 
terraform apply
```
1. You can view your deployed app at the url outputted to the console.  (prefix with https://)
1. Afterwards you can destroy your resources.
```
terraform destroy
```
1. Your application is no longer available.
1. Now you can destroy your other resources...if you choose (For subsequennt labs it might be ok to leave up the acr as it is a low cost resource).  
```
cd ../acr
# Confirm your workspace
terraform workspace list
# Destroy your resources
terraform destroy
``` 