using System.Data;

namespace OnlineBookStore.Infrastructure.Data.DataAccess
{
    public interface ISqlDataAccess
    {
        Task<IEnumerable<T>> GetDataIEnumerable<T, P>(string spName, P? parameters = null) where P : class?;
        Task<T> GetData<T, P>(string spName, P? parameters = null) where P : class?;
        Task<string> GetJsonData<T, P>(string spName, P? parameters = null) where P : class?;
        Task SaveData<T>(string spName, T? parameters = null) where T : class?;
        Task<DataSet> GetDataSet<P>(string spName, P? parameters = null) where P : class?;
        Task<Dictionary<string, List<dynamic>>> GetMultipleData<T, P>(string spName, P? parameters = null) where P : class?;
        Task<int> ExecuteRawQuery<T>(string sql, T? parameters = null) where T : class?;
        Task<int> BulkInsertAsync<T>(string sql, IEnumerable<T> entities) where T : class?;
    }
}