FROM mono:latest
LABEL Name=sentimentfs.sentimentservice Version=0.0.1
ARG source
WORKDIR /app
EXPOSE 5002
COPY ${source:-obj/Docker/publish} .
ENTRYPOINT ["mono", "SentimentService.exe"]
