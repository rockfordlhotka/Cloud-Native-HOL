#!/bin/bash

docker tag breadservice:dev myrepository.azurecr.io/breadservice:lab05
docker tag cheeseservice:dev myrepository.azurecr.io/cheeseservice:lab05
docker tag meatservice:dev myrepository.azurecr.io/meatservice:lab05
docker tag lettuceservice:dev myrepository.azurecr.io/lettuceservice:lab05
docker tag gateway:dev myrepository.azurecr.io/gateway:lab05
docker tag sandwichmaker:dev myrepository.azurecr.io/sandwichmaker:lab05