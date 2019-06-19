#!/bin/bash

docker build -f ../BreadService/Dockerfile -t breadservice:dev ..
docker build -f ../CheeseService/Dockerfile -t cheeseservice:dev ..
docker build -f ../LettuceService/Dockerfile -t lettuceservice:dev ..
docker build -f ../MeatService/Dockerfile -t meatservice:dev ..
docker build -f ../Gateway/Dockerfile -t gateway:dev ..
docker build -f ../Greeter/Dockerfile -t greeter:dev ..
docker build -f ../SandwichMaker/Dockerfile -t sandwichmaker:dev ..