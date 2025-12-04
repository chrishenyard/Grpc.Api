$connectionString = dotnet user-secrets list -p .\Grpc.Service\Grpc.Service.csproj |
    Where-Object { $_ -like 'ConnectionStrings:LocalDatabaseContext*' } |
    ForEach-Object {
        ($_ -split '=\s*', 2)[1].Trim().Trim('"')
    }

dotnet build ./Grpc.Database/Grpc.Database.csproj -c Debug -o ./Grpc.Database/bin/Debug/net10.0/ /p:GenerateDacpac=true /p:RunSqlCodeAnanlysis=true

sqlpackage /Action:Publish /SourceFile:"./Grpc.Database/bin/Debug/net10.0/Grpc.Database.dacpac" /TargetConnectionString:$connectionString
