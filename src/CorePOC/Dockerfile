#1: Build app: $ docker build -f Dockerfile -t adrianclaveri11/corepoc .
#2: Start BD: $ docker run -d -p 9200:9200 --name my-db -v esdata:/usr/share/elasticsearch/data elasticsearch -Des.cluster.name="esdemo" -Des.node.name="node01"
#3: Run app: $ docker run -d -p 8080:5000 adrianclaveri11/corepoc

FROM microsoft/dotnet:1.0.0-preview2-sdk

MAINTAINER Adrian

ENV ASPNETCORE_URLS http://*:5000

COPY . /app

WORKDIR /app

RUN ["dotnet", "restore"]

EXPOSE 5000

ENTRYPOINT ["dotnet","run"]