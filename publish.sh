#!/bin/bash
set -e

cd Telega
rm -rf bin
./generate-dto.sh
dotnet pack
dotnet nuget push bin/Debug/*.nupkg --source http://api.nuget.org/v3/index.json --api-key $TELEGA_NUGET_API_KEY
