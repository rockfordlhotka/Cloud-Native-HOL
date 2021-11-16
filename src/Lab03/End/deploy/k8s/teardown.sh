#!/bin/bash

helm uninstall my-rabbitmq
kubectl delete deployment gateway
kubectl delete service gateway
kubectl delete deployment breadservice
kubectl delete deployment cheeseservice
kubectl delete deployment lettuceservice
kubectl delete deployment meatservice
kubectl delete deployment sandwichmaker