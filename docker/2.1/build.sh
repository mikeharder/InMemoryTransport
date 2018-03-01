#!/usr/bin/env bash

docker build -t in-memory-transport:2.1 -f `dirname $0`/Dockerfile `dirname $0`/../../
