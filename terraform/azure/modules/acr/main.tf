#Azure Provider
provider "azurerm" {
  #pin down version to latest and greatest as of February 14th
  version = "=1.44.0"
}

# Resource Group
# az group create --name MyGroup --location "East US"
resource "azurerm_resource_group" "cloud_native_hol" {
  name = "${var.prefix}-resources"
  location = var.location
}

# create azure container registry
# az acr create --name MyRepository --resource-group MyGroup --sku Basic --admin-enabled true
resource "azurerm_container_registry" "cloud_native_hol_acr" {
  name = "${var.prefixCamel}Repo"
  resource_group_name = azurerm_resource_group.cloud_native_hol.name
  location = azurerm_resource_group.cloud_native_hol.location
  sku = "Basic"
  admin_enabled = true
}

# output acr credentials
# az acr credential show -n MyRepository
data "azurerm_container_registry" "cloud_native_hol_acr" {
  name = "${var.prefixCamel}Repo"
  resource_group_name = azurerm_resource_group.cloud_native_hol.name
}


output "acr" {
  value = data.azurerm_container_registry.cloud_native_hol_acr
}