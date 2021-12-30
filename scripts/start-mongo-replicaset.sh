#!/bin/bash

docker compose up -d mongo1 mongo2 mongo3

sleep 5

docker exec mongo1 ./scripts/rs-init.sh