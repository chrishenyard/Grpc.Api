namespace Grpc.Service.DataInitializer;

public interface IDataInitializer
{
    Task InitializeData(CancellationToken token);
}
