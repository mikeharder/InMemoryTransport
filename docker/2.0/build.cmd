@echo off

docker build -t in-memory-transport:2.0 -f %~dp0\Dockerfile %~dp0/../../
