#!/bin/bash

docker tag breadservice:dev myrepository.azurecr.io/breadservice:lab04
docker tag cheeseservice:dev myrepository.azurecr.io/cheeseservice:lab04
docker tag meatservice:dev myrepository.azurecr.io/meatservice:lab04
docker tag lettuceservice:dev myrepository.azurecr.io/lettuceservice:lab04
docker tag gateway:dev myrepository.azurecr.io/gateway:lab04
docker tag sandwichmaker:dev myrepository.azurecr.io/sandwichmaker:lab04