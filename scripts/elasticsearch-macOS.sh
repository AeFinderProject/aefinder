#!/bin/bash
wget https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-7.15.1-darwin-x86_64.tar.gz
tar xzvf elasticsearch-7.15.1-darwin-x86_64.tar.gz
cd elasticsearch-7.15.1/
bin/elasticsearch -d
sleep 30
