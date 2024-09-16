#!/usr/bin/env bash
set -e

echo "Checking if the replica set is already initiated..."
if [[ -z $(mongosh --host mongo:27017 --quiet --eval "rs.isMaster().setName") ]]; then
  echo "Initializing the replica set"
  mongosh --host mongo:27017 --eval "rs.initiate({_id: 'myReplicaSet', members: [{_id: 0, host: 'mongo'}]})"
else 
  echo "Replica set already initiated."
fi
echo "Done!"