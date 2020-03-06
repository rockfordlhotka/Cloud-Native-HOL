variable "workspace" {
  description = "The workspace from where to pull output from (dev)"
  default = "dev"
}

variable "webapp_name" {
  description = "The name of the webapp (cloud-native-hol-app)"
  default = "cloud-native-hol-app"
}

variable "container_image" {
  description = "The container image to be deployed (image_name)"
  default = "spring-boot-app-docker"
}

variable "container_image_version" {
  description = "The container image version to be deployed (latest)"
  default = "v0.0.02"
}