#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM microsoft/dotnet:2.2-aspnetcore-runtime-nanoserver-1803 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.2-sdk-nanoserver-1803 AS build
WORKDIR /src
COPY ["CardManagement.Api/CardManagement.Api/CardManagement.Api.fsproj", "CardManagement.Api/CardManagement.Api/"]
RUN dotnet restore "CardManagement.Api/CardManagement.Api/CardManagement.Api.fsproj"
COPY . .
WORKDIR "/src/CardManagement.Api/CardManagement.Api"
RUN dotnet build "CardManagement.Api.fsproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CardManagement.Api.fsproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CardManagement.Api.dll"]