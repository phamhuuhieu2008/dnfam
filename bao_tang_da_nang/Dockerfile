# Sử dụng image SDK của .NET 9.0 để build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy file .csproj và restore các dependencies
COPY ["Bảo Tàng Đà Nẵng.csproj", "./"]
RUN dotnet restore "Bảo Tàng Đà Nẵng.csproj"

# Copy toàn bộ mã nguồn và tiến hành build
COPY . .
RUN dotnet build "Bảo Tàng Đà Nẵng.csproj" -c Release -o /app/build

# Publish ứng dụng
FROM build AS publish
RUN dotnet publish "Bảo Tàng Đà Nẵng.csproj" -c Release -o /app/publish

# Sử dụng image Runtime để chạy ứng dụng (nhẹ hơn)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render thường sử dụng port do biến môi trường PORT cung cấp (mặc định 80 hoặc 10000)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Bảo Tàng Đà Nẵng.dll"]
