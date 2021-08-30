#!/bin/sh
set -e
SCRIPT=$(readlink -f "$0")
SCRIPTPATH=$(dirname "$SCRIPT")

DB="$SCRIPTPATH/bookstore.prod.db"
[ -e "$DB" ] && rm "$DB"

PUBLISHDIR="$SCRIPTPATH/bin/Release/net5.0/linux-x64/publish/"

dotnet clean
dotnet publish -r linux-x64 --no-self-contained --configuration Release
ASPNETCORE_ENVIRONMENT=Production dotnet-ef database update -v
cp "$DB" "$PUBLISHDIR"
tar -C "$PUBLISHDIR" -cvf $SCRIPTPATH/bookstore.tar.gz .
