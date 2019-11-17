#!/bin/bash

docker tag breadservice:arm myrepository.azurecr.io/breadservice:arm
docker tag cheeseservice:arm myrepository.azurecr.io/cheeseservice:arm
docker tag meatservice:arm myrepository.azurecr.io/meatservice:arm
docker tag lettuceservice:arm myrepository.azurecr.io/lettuceservice:arm
docker tag gateway:arm myrepository.azurecr.io/gateway:arm
docker tag sandwichmaker:arm myrepository.azurecr.io/sandwichmaker:arm