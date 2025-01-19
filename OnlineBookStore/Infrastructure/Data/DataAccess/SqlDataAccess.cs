using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;

namespace OnlineBookStore.Infrastructure.Data.DataAccess
{
    public class SqlDataAccess : ISqlDataAccess
    {
        private readonly IConfiguration _config;
        private readonly static string connectionId = "conn";
        public SqlDataAccess(IConfiguration _config)
        {
            this._config = _config;
        }

        public async Task<IEnumerable<T>> GetDataIEnumerable<T, P>(string spName, P? parameters = null) where P : class?
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            return await connection.QueryAsync<T>(spName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: 0);
        }
        public async Task<Dictionary<string, List<dynamic>>> GetMultipleData<T, P>(string spName, P? parameters = null) where P : class?
        {
            var resultDictionary = new Dictionary<string, List<dynamic>>();
            var resultSetData = new List<dynamic>();

            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            var multi = await connection.QueryMultipleAsync(spName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: 0);
            int resultSetIndex = 1;
            while (!multi.IsConsumed)
            {
                resultSetData = multi.Read().ToList();
                resultDictionary.Add("DataSet_" + resultSetIndex, resultSetData);
                resultSetIndex++;
            }

            return resultDictionary;
        }
        public async Task<string> GetJsonData<T, P>(string spName, P? parameters = null) where P : class?
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            var result = await connection.QueryAsync<T>(spName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: 0);
            return JsonConvert.SerializeObject(result);
        }
        public async Task SaveData<T>(string spName, T? parameters = null) where T : class?
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            await connection.ExecuteAsync(spName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: 0);
        }
        public async Task<DataSet> GetDataSet<P>(string spName, P? parameters = null) where P : class?
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            var reader = await connection.ExecuteReaderAsync(spName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: 0);
            var dataSet = new DataSet();

            while (!reader.IsClosed)
            {
                var dataTable = new DataTable();
                dataSet.Tables.Add(dataTable);
                dataTable.Load(reader);
            }

            reader.Close();
            return dataSet;
        }

        public async Task<T> GetData<T, P>(string spName, P? parameters = null) where P : class?
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            return await connection.QueryFirstOrDefaultAsync<T>(spName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: 0);
        }

        public async Task<int> ExecuteRawQuery<T>(string sql, T? parameters = null) where T : class?
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            return await connection.ExecuteAsync(sql, parameters, commandTimeout: 0);
        }

        public async Task<int> BulkInsertAsync<T>(string sql, IEnumerable<T> entities) where T : class?
        {
            using IDbConnection connection = new SqlConnection(_config.GetConnectionString(connectionId));
            return await connection.ExecuteAsync(sql, entities, commandTimeout: 0);
        }
    }
}
