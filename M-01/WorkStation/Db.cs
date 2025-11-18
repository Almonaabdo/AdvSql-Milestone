using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WorkStation
{
    internal static class Db
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
            if (obj == null || obj == System.DBNull.Value) return 5; // safe default
            return (int)obj;
        }
    }
}
