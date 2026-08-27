using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ProjectHEQTCSDL.Core
{
    public static class DatabaseHelper
    {
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(App_Config.ConnectionString);
        }

        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using var conn = GetConnection();
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public static DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null, CommandType commandType = CommandType.Text)
        {
            var dt = new DataTable();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(query, conn)
            {
                CommandType = commandType
            };

            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

            using var adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dt);
            return dt;
        }

        public static int ExecuteNonQuery(string query, SqlParameter[]? parameters = null, CommandType commandType = CommandType.Text)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(query, conn)
            {
                CommandType = commandType
            };

            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string query, SqlParameter[]? parameters = null, CommandType commandType = CommandType.Text)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(query, conn)
            {
                CommandType = commandType
            };

            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

            return cmd.ExecuteScalar();
        }

        public static DataTable ExecuteProcedure(string spName, SqlParameter[]? parameters = null)
        {
            return ExecuteQuery(spName, parameters, CommandType.StoredProcedure);
        }

        public static async Task<DataTable> ExecuteQueryAsync(string query, SqlParameter[]? parameters = null, CommandType commandType = CommandType.Text)
        {
            var dt = new DataTable();
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(query, conn)
            {
                CommandType = commandType,
                CommandTimeout = 60
            };

            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            dt.Load(reader);
            return dt;
        }

        public static async Task<int> ExecuteNonQueryAsync(string query, SqlParameter[]? parameters = null, CommandType commandType = CommandType.Text)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(query, conn)
            {
                CommandType = commandType,
                CommandTimeout = 60
            };

            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

            return await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<DataTable> ExecuteProcedureAsync(string spName, SqlParameter[]? parameters = null)
        {
            return await ExecuteQueryAsync(spName, parameters, CommandType.StoredProcedure);
        }

        public static SqlParameter CreateStructuredParameter(string paramName, string typeName, DataTable data)
        {
            return new SqlParameter(paramName, SqlDbType.Structured)
            {
                TypeName = typeName,
                Value = data
            };
        }
    }
}
