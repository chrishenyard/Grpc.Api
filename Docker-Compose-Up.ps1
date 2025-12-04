$buildDate = (Get-Date).ToUniversalTime().ToString("o")

docker compose build --build-arg BUILD_DATE=$buildDate
docker compose create grpc.service
docker compose start grpc.service
