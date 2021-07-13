#!/bin/bash
set -e

cd Telega
rm -rf bin

dotnet pack
dotnet nuget push bin/Debug/*.nupkg --source http://api.nuget.org/v3/index.json --api-key $ILYALATT_NUGET_API_KEY
