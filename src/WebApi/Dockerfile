FROM microsoft/aspnetcore:latest
LABEL Name=sentimentfs.webapi Version=0.0.1
ARG source
WORKDIR /app
EXPOSE 80
EXPOSE 5001
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["dotnet", "WebApi.dll"]
