provider "azurerm" {
  #pin down version to latest and greatest as of February 14th
  version = "=1.44.0"
}

#obtain outputs from terrafrom state of previous execution of acr for an environment
data "terraform_remote_state" "acr" {
  backend = "local"

  config = {
    path = "../acr/terraform.tfstate.d/${var.workspace}/terraform.tfstate"
  }
}


# az webapp create --resource-group MyGroup --plan myAppServicePlan --name MyAppName --deployment-container-image-name myrepository.azurecr.io/lab01/gateway:lab01
# Create a App Service Plan with Linux
resource "azurerm_app_service_plan" "appserviceplan" {
  name = "${var.webapp_name}-plan"
  location = data.terraform_remote_state.acr.outputs.acr.location
  resource_group_name = data.terraform_remote_state.acr.outputs.acr.resource_group_name
  kind = "Linux"
//  sku {
//    tier = "Standard"
//    size = "S1"
//  }
//https://github.com/terraform-providers/terraform-provider-azurerm/issues/1560
  sku {
    tier = "Basic"
    size = "B2"
  }
  reserved = true
}


//# Create a App Service Plan with Linux
//# Create a WebApp and Deploy your container.
resource "azurerm_app_service" "dockerapp" {
  name = var.webapp_name
  location = data.terraform_remote_state.acr.outputs.acr.location
  resource_group_name = data.terraform_remote_state.acr.outputs.acr.resource_group_name
  app_service_plan_id = azurerm_app_service_plan.appserviceplan.id

  # Do not attach Storage by default
  app_settings = {
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = false

    DOCKER_REGISTRY_SERVER_URL = data.terraform_remote_state.acr.outputs.acr.login_server
    DOCKER_REGISTRY_SERVER_USERNAME = data.terraform_remote_state.acr.outputs.acr.admin_username
    DOCKER_REGISTRY_SERVER_PASSWORD = data.terraform_remote_state.acr.outputs.acr.admin_password
  }

  # Configure Docker Image to load on start
  site_config {
    linux_fx_version = "DOCKER|${data.terraform_remote_state.acr.outputs.acr.location}/${var.container_image}:${var.container_image_version}"
  }
}

output "instrumentation_key" {
  value = "${azurerm_app_service.dockerapp.default_site_hostname}"
}