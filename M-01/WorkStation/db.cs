/*
 * File : db.cs
 * Developers: Yafet Tekleab, Abdurrahman Almouna
 */


using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Configuration;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using Microsoft.Data.SqlClient;
namespace WorkStation;

public static class Db
{

    public static async Task<int> GetTimeScale(string connectionString)
    {
        const string sql = @"
                SELECT CAST(value AS int)
                FROM dbo.APP_CONFIG
                WHERE configDescription = 'TimeScale'";

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        using var cmd = new SqlCommand(sql, conn);
        var obj = await cmd.ExecuteScalarAsync();
        if (obj == null || obj == System.DBNull.Value) return 10; // safe default
        return (int)obj;
    }
}