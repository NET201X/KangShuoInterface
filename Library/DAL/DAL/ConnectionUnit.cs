using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using MySql.Data;
using System.Configuration;
using System.Data.Common;

namespace DAL
{
    public static class ConnectionUnit
    {
        /// <summary>
        /// 取得DB连接(默认MySQL)
        /// </summary>
        /// <returns>DB连接</returns>
        public static IDbConnection NewDBConnection(string connectionString)
        {
            IDbConnection sqlConn = new MySqlConnection(connectionString);
            sqlConn.Open();

            return sqlConn;
        }

        /// <summary>
        /// 取得DB连接
        /// </summary>
        /// <returns></returns>
        public static IDbConnection NewDBConnection()
        {
            return NewDBConnection(Config.ConnectionStringSetting);
        }

        /// <summary>
        /// 取得DB连接
        /// </summary>
        /// <returns>DB连接</returns>
        public static IDbConnection NewDBConnection(ConnectionStringSettings connection)
        {
            if (connection == null)
            {
                throw new Exception("沒有连接配置信息！");
            }

            // 创建IDBConnection
            var con = InitDbProviderFactory(connection.ProviderName).CreateConnection();

            // 连接初始化
            con.ConnectionString = connection.ConnectionString;

            // 打开连接
            con.Open();

            return con;
        }

        /// <summary>
        /// 初始化DB 提供器工厂
        /// </summary>
        /// <param name="providerName">DB 客戶端</param>
        /// <returns></returns>
        private static DbProviderFactory InitDbProviderFactory(string providerName)
        {
            return DbProviderFactories.GetFactory(providerName);
        }
    }
}
