

namespace demo.Infrastructure.Interfaces.Connection_Interfaces
{
    public interface IDataConnection<T>
    {
        T LoadData();
    }
}
