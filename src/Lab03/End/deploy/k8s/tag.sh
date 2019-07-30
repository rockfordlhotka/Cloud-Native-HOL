#!/bin/bash

docker tag breadservice:dev rdlk8s.azurecr.io/breadservice:v1
docker tag cheeseservice:dev rdlk8s.azurecr.io/cheeseservice:v1
docker tag meatservice:dev rdlk8s.azurecr.io/meatservice:v1
docker tag lettuceservice:dev rdlk8s.azurecr.io/lettuceservice:v1
docker tag gateway:dev rdlk8s.azurecr.io/gateway:v1
docker tag sandwichmaker:dev rdlk8s.azurecr.io/sandwichmaker:v1
docker tag greeter:dev rdlk8s.azurecr.io/greeter:v1
