variable "prefix" {
  description = "The prefix for MOST (others use prefixCamel) resources in this example (cloud-native-hol)"
  default = "cloud-native-hol"
}

variable "prefixCamel" {
  description = "The camelCase prefix for resources in this example (CloudNativeHolRepository)"
  default = "CloudNativeHolRepository"
}

variable "location" {
  description = "The Azure Region where all resources  should be created. (East US)"
  default = "East US"
}

variable "azure_container_registry_name" {
  description = "The Azure Container Registry Name (CloudNativeHolRepository)"
  default = "CloudNativeHolRepository"
}
