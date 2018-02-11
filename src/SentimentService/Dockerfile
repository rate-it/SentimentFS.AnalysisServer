FROM mono:latest
COPY ${source:-obj/Docker/publish} .
WORKDIR .
EXPOSE 5002/tcp
ENTRYPOINT ["mono", "SentimentService.exe"]
