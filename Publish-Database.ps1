$connectionString = dotnet user-secrets list -p .\server\Grpc.Service\Grpc.Service.csproj |
    Where-Object { $_ -like 'ConnectionStrings:LocalDatabaseContext*' } |
    ForEach-Object {
        ($_ -split '=\s*', 2)[1].Trim().Trim('"')
    }

dotnet build .\server\Grpc.Database\Grpc.Database.csproj -c Debug -o .\server\Grpc.Database\bin/Debug/net10.0/ /p:GenerateDacpac=true /p:RunSqlCodeAnanlysis=true

sqlpackage /Action:Publish /SourceFile:".\server\Grpc.Database\bin/Debug/net10.0/Grpc.Database.dacpac" /TargetConnectionString:$connectionString
