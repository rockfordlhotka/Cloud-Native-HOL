#!/bin/bash

cd k8s
grep -rli --include=*.yml --include=*.yaml --include=*.sh "$1" | xargs sed -i "s/$1/$2/g"
cd ..
