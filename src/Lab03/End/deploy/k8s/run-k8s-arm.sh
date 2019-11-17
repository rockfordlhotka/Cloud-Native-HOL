#!/bin/bash

kubectl apply -f gateway-deployment-arm.yaml
kubectl apply -f gateway-service.yaml
kubectl apply -f breadservice-deployment-arm.yaml
kubectl apply -f cheeseservice-deployment-arm.yaml
kubectl apply -f lettuceservice-deployment-arm.yaml
kubectl apply -f meatservice-deployment-arm.yaml
kubectl apply -f sandwichmaker-deployment-arm.yaml