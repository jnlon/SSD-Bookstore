#!/bin/sh
\rm -rv *.db Migrations/
dotnet-ef migrations add InitialCreate -v && \
    dotnet-ef database update -v
