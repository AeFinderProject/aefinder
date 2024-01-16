#!/usr/bin/env pwsh
$WebClient = New-Object System.Net.WebClient
$WebClient.DownloadFile("https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-7.15.1-windows-x86_64.zip","elasticsearch.zip")
Expand-Archive -Path elasticsearch.zip -DestinationPath elasticsearch;
cd elasticsearch/elasticsearch-7.15.1/
.\bin\elasticsearch-service.bat install
.\bin\elasticsearch-service.bat start
sleep 30
curl http://127.0.0.1:9200
