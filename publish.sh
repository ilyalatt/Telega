#!/bin/bash
set -e

./generate-dto.sh

cd Telega
rm -rf bin

dotnet pack
dotnet nuget push bin/Debug/*.nupkg --source http://api.nuget.org/v3/index.json --api-key $TELEGA_NUGET_API_KEY
