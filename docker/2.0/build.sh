#!/usr/bin/env bash

docker build -t in-memory-transport:2.0 -f `dirname $0`/Dockerfile `dirname $0`/../../
