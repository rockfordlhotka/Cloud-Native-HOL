#!/bin/bash

docker tag breadservice:dev myrepository.azurecr.io/breadservice:lab03
docker tag cheeseservice:dev myrepository.azurecr.io/cheeseservice:lab03
docker tag meatservice:dev myrepository.azurecr.io/meatservice:lab03
docker tag lettuceservice:dev myrepository.azurecr.io/lettuceservice:lab03
docker tag gateway:dev myrepository.azurecr.io/gateway:lab03
docker tag sandwichmaker:dev myrepository.azurecr.io/sandwichmaker:lab03