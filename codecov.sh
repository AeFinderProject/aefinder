#!/usr/bin/env bash

TOKEN=$1
rm -r ./test/*.Tests/TestResults
rm -r CodeCoverage
for name in `ls ./test/*.Tests/*.csproj | awk '{print $NF}'`;
do
    echo ${name}
    dotnet test ${name} --logger trx --settings CodeCoverage.runsettings --collect:"XPlat Code Coverage"
done
reportgenerator /test/*/TestResults/*/coverage.cobertura.xml -reports:./test/*/TestResults/*/coverage.cobertura.xml -targetdir:./CodeCoverage -reporttypes:Cobertura -assemblyfilters:-xunit*
codecov -f ./CodeCoverage/Cobertura.xml -t ${TOKEN}