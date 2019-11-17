#!/bin/bash

docker build -f ../BreadService/Dockerfile-arm -t breadservice:arm ..
docker build -f ../CheeseService/Dockerfile-arm -t cheeseservice:arm ..
docker build -f ../LettuceService/Dockerfile-arm -t lettuceservice:arm ..
docker build -f ../MeatService/Dockerfile-arm -t meatservice:arm ..
docker build -f ../Gateway/Dockerfile-arm -t gateway:arm ..
docker build -f ../SandwichMaker/Dockerfile-arm -t sandwichmaker:arm ..