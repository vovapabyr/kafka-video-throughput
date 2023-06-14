ARG OPENCV_SHARP_BUILD_TAG=2
ARG SDK_VERSION=6.0.202-bullseye-slim-amd64
ARG RUNTIME_VERSION=6.0.4-bullseye-slim

FROM syamaner/opencvsharp-build:$OPENCV_SHARP_BUILD_TAG AS opencv

# Given we are building a .Net application, the build does not have to be in the target architecture.
# Reference: https://github.com/dotnet/dotnet-docker/issues/1537#issuecomment-755351628

# ARG TARGETPLATFORM
# WORKDIR /src
# COPY . .

# Select the correct RID for the target architecture.
# run dotnet publish as usual and pass the RID.
# RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
#         RID=linux-x64 ; \
#     elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
#         RID=linux-arm64 ; \
#     elif [ "$TARGETPLATFORM" = "linux/arm/v7" ]; then \
#         RID=linux-arm ; \
#     fi

# Copy the application as well as native dependencies to the final stage and build the final image without any unnecessary files.
FROM mcr.microsoft.com/dotnet/runtime:$RUNTIME_VERSION as base
WORKDIR /app
EXPOSE 5115

ENV ASPNETCORE_URLS=http://+:5115

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
# RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app && chown -R appuser /usr/lib && chmod u+rwx -R /etc
# USER appuser

FROM mcr.microsoft.com/dotnet/sdk:$SDK_VERSION as build
WORKDIR /src
COPY ["video-frames-producer/video-frames-producer.csproj", "video-frames-producer/"]
RUN dotnet restore "video-frames-producer/video-frames-producer.csproj"
COPY . .
WORKDIR "/src/video-frames-producer"
RUN dotnet build "video-frames-producer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "video-frames-producer.csproj" -c Release -o /app/publish -r linux-x64

FROM base AS final
WORKDIR /app
COPY --from=opencv /artifacts/ /usr/lib/ 
RUN ldconfig
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "video-frames-producer.dll"]
