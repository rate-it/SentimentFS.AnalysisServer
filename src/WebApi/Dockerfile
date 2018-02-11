FROM microsoft/aspnetcore:latest
COPY ${source:-obj/Docker/publish} .
WORKDIR .
ENV ASPNETCORE_URLS http://+:80
EXPOSE 80/tcp
EXPOSE 5001/tcp
ENTRYPOINT ["dotnet", "WebApi.dll"]
