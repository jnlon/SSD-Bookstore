#!/bin/sh
set -e
SCRIPT=$(readlink -f "$0")
SCRIPTPATH=$(dirname "$SCRIPT")

DB="$SCRIPTPATH/bookstore.prod.db"
[ -e "$DB" ] && rm "$DB"

dotnet publish --configuration Release
ASPNETCORE_ENVIRONMENT=Production ./remigrate.sh
cp "$DB" $SCRIPTPATH/bin/Release/net5.0/publish/
tar -C bin/Release/net5.0/publish/ -cvf $SCRIPTPATH/bookstore.tar.gz .
