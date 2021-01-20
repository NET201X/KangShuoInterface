using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.OleDb;
using MySql.Data.MySqlClient;
//using System.Data.OracleClient;
using System.Data.SqlClient;
using System.Data.Odbc;

namespace DAL
{
    /// <summary> 
    /// Command单元
    /// </summary>
    public static class CommandUnit
    {
        /// <summary>
        /// 建立Command
        /// </summary>
        /// <param name="connection">DB连接实体</param>
        /// <returns></returns>
        public static IDbCommand NewCommand(string sql, IDbConnection connection)
        {
            IDbCommand command = connection.CreateCommand(); //SwitchNewCommand(connection);

            // 设定Command连接
            command.Connection = connection;
            command.CommandTimeout = 600;

            // sql
            command.CommandText = SqlVaildate(sql);

            return command;
        }

        /// <summary>
        /// 建立事务Command
        /// </summary>
        /// <param name="connection">DB连接实体</param>
        /// <param name="tansaction">事务</param>
        /// <returns></returns>
        public static IDbCommand NewCommand(string sql, IDbConnection connection, IDbTransaction tansaction)
        {
            IDbCommand command = connection.CreateCommand(); //SwitchNewCommand(connection);

            // 设定Command连接
            command.Connection = connection;

            // 设定Command事务
            command.Transaction = tansaction;

            // sql
            command.CommandText = SqlVaildate(sql);

            return command;
        }

        /// <summary>
        /// 处理sql特殊字符
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>sql语句</returns>
        public static string SqlVaildate(string sql)
        {
            string strValue;
            strValue = sql;

            strValue = strValue.Replace("--", "");
            strValue = strValue.Replace("[", "[[]");
            strValue = strValue.Replace("DROP", "");

            return strValue;
        }

        /// <summary>
        /// 建立事务Command
        /// </summary>
        /// <param name="connection">DB连接实体</param>
        /// <param name="tansaction">事务</param>
        /// <returns></returns>
        public static IDbCommand NewCommand(IDbConnection connection, IDbTransaction tansaction)
        {
            IDbCommand command = connection.CreateCommand(); //SwitchNewCommand(connection);

            // 设定Command连接
            command.Connection = connection;

            // 设定Command事务
            command.Transaction = tansaction;

            return command;
        }

        /// <summary>
        /// 建立事务Command
        /// </summary>
        /// <param name="connection">DB连接实体</param>
        /// <returns></returns>
        public static IDbCommand NewCommand(IDbConnection connection)
        {
            IDbCommand command = connection.CreateCommand(); //SwitchNewCommand(connection);

            // 设定Command连接
            command.Connection = connection;

            return command;
        }

        ///// <summary>
        ///// 初始化Command
        ///// </summary>
        ///// <param name="connection"></param>
        ///// <returns></returns>
        //private static IDbCommand SwitchNewCommand(IDbConnection connection)
        //{
        //    IDbCommand command = null;

        //    // SqlCommand
        //    if (connection is SqlConnection)
        //    {
        //        command = new SqlCommand();
        //    }
        //    else if (connection is OracleConnection)
        //    {
        //        // OracleCommand
        //        command = new OracleCommand();
        //    }
        //    else if (connection is OleDbConnection)
        //    {
        //        // OleDbCommand
        //        command = new OleDbCommand();
        //    }
        //    else if (connection is OdbcConnection)
        //    {
        //        // OdbcCommand
        //        command = new OdbcCommand();
        //    }

        //    // 返回Command
        //    return command;
        //}

        ///// <summary>
        ///// 转化Command
        ///// </summary>
        ///// <param name="connection"></param>
        ///// <returns></returns>
        //private static IDbCommand ConvertCommand(IDbCommand cmd)
        //{
        //    // SqlCommand
        //    if (cmd is SqlCommand)
        //    {
        //        return (SqlCommand)cmd;
        //    }
        //    else if (cmd is OracleCommand)
        //    {
        //        // OracleCommand
        //        return (OracleCommand)cmd;
        //    }
        //    else if (cmd is OleDbCommand)
        //    {
        //        // OleDbCommand
        //        return (OleDbCommand)cmd;
        //    }
        //    else if (cmd is OdbcCommand)
        //    {
        //        // OdbcCommand
        //        return (OdbcCommand)cmd;
        //    }

        //    // 返回Command
        //    throw new Exception("未知类型Command！");
        //}

        /// <summary>
        /// 创建DataAdapter
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <returns></returns>
        private static IDataAdapter CreateDataAdapter(this IDbCommand cmd)
        {
            // SqlCommand
            if (cmd is SqlCommand)
            {
                return new SqlDataAdapter((SqlCommand)cmd);
            }
            else if (cmd is MySqlCommand)
            {
                // MySqlCommand
                return new MySqlDataAdapter((MySqlCommand)cmd);
            }
            else if (cmd is OleDbCommand)
            {
                // OleDbCommand
                return new OleDbDataAdapter((OleDbCommand)cmd);
            }
            else if (cmd is OdbcCommand)
            {
                // OdbcCommand
                return new OdbcDataAdapter((OdbcCommand)cmd);
            }

            // 返回Command
            throw new Exception("未知类型Command！");
        }

        /// <summary>
        /// 加入参数
        /// </summary>
        /// <param name="cmd">命令</param>
        /// <param name="parameters">参数集合</param>
        /// <returns></returns>
        public static IDbCommand AddParameters(this IDbCommand cmd, CommandParameterCollection parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return cmd;
            }

            // SqlCommand
            if (cmd is SqlCommand)
            {
                return AddParameters((SqlCommand)cmd, parameters);
            }
            else if (cmd is MySqlCommand)
            {
                // MySqlCommand
                return AddParameters((MySqlCommand)cmd, parameters);
            }
            else if (cmd is OleDbCommand)
            {
                // OleDbCommand
                return AddParameters((OleDbCommand)cmd, parameters);
            }
            else if (cmd is OdbcCommand)
            {
                // OdbcCommand
                return AddParameters((OdbcCommand)cmd, parameters);
            }
            else
            {
                // 返回Command
                throw new Exception("未知类型Command！");
            }
        }

        /// <summary>
        /// 加入参数（SQLSERVER）
        /// </summary>
        /// <param name="cmd">Sql命令</param>
        /// <param name="parameters">参数集合</param>
        /// <returns></returns>
        private static IDbCommand AddParameters(this SqlCommand cmd, CommandParameterCollection parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return cmd;
            }

            // 加入参数
            foreach (CommandParameter item in parameters)
            {
                cmd.Parameters.Add(item.ColumnName, item.DBType.ToSqlDbType(), item.Length).Value = item.Value;
            }

            return cmd;
        }

        /// <summary>
        /// 加入参数（MYSQL）
        /// </summary>
        /// <param name="cmd">MySql命令</param>
        /// <param name="parameters">参数集合</param>
        /// <returns></returns>
        private static IDbCommand AddParameters(this MySqlCommand cmd, CommandParameterCollection parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return cmd;
            }

            // 加入参数
            foreach (CommandParameter item in parameters)
            {
                cmd.Parameters.Add(item.ColumnName, item.DBType.ToOracleType(), item.Length).Value = item.Value;
            }

            return cmd;
        }

        private static IDbCommand AddParameters(this OdbcCommand cmd, CommandParameterCollection parameters)
        {
            throw new NotImplementedException("沒有实现 ODBC AddParameters！");
        }

        private static IDbCommand AddParameters(this OleDbCommand cmd, CommandParameterCollection parameters)
        {
            throw new NotImplementedException("沒有实现 OleDb AddParameters！");
        }

        /// <summary>
        /// 转到SqlDbType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static SqlDbType ToSqlDbType(this FieldType type)
        {
            switch (type)
            {
                case FieldType.DateTime:
                    return SqlDbType.DateTime;
                case FieldType.Decimal:
                    return SqlDbType.Decimal;
                case FieldType.Double:
                    return SqlDbType.Decimal;
                case FieldType.Int:
                    return SqlDbType.Int;
                case FieldType.Object:
                    return SqlDbType.NVarChar;
                case FieldType.String:
                    return SqlDbType.NVarChar;
                default:
                    return SqlDbType.NVarChar;
            }
        }

        /// <summary>
        /// 转到MySqlDbType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static  MySqlDbType ToOracleType(this FieldType type)
        {
            switch (type)
            {
                case FieldType.DateTime:
                    return MySqlDbType.DateTime;
                case FieldType.Decimal:
                    return MySqlDbType.Float;
                case FieldType.Double:
                    return MySqlDbType.Double;
                case FieldType.Int:
                    return MySqlDbType.Int32;
                case FieldType.Object:
                    return MySqlDbType.VarChar;
                case FieldType.String:
                    return MySqlDbType.VarChar;
                default:
                    return MySqlDbType.VarChar;
            }
        }

        /// <summary>
        /// 设定命令参数
        /// </summary>
        /// <param name="command">Command实体</param>
        /// <param name="columnName">列名</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public static IDbDataParameter SetParamValue(IDbCommand command, string columnName, string[] value)
        {
            if (value == null || value.Length == 0)
            {
                return null;
            }

            Array.ForEach<string>(value, x =>
            {
                //SetParamValue((SqlCommand)command, string.Format("@{0}_{1}", columnName, Guid.NewGuid().ToString().Replace("-", "")), x);
            });

            return null;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command">Command</param>
        /// <returns></returns>
        public static int ExeNonQuery(this IDbCommand command)
        {
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="command">Command</param>
        /// <returns></returns>
        public static IDataReader ExeReader(this IDbCommand command, CommandBehavior behavior)
        {
            return command.ExecuteReader(behavior);
        }

        /// <summary>
        /// 执行单行查询
        /// </summary>
        /// <param name="command">Command</param>
        /// <returns></returns>
        public static object ExeScalar(this IDbCommand command)
        {
            return command.ExecuteScalar();
        }

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <param name="command">Command</param>
        /// <returns></returns>
        public static DataTable Search(this IDbCommand command)
        {
            var da = command.CreateDataAdapter();

            DataSet dsResult = new DataSet();

            da.Fill(dsResult);

            return dsResult.Tables.Count == 0 ? new DataTable() : dsResult.Tables[0];
        }

        /// <summary>
        /// 执行查询
        /// </summary>
        /// <param name="command">Command</param>
        /// <returns></returns>
        public static DataSet SearchDataSet(this IDbCommand command)
        {
            var da = command.CreateDataAdapter();

            DataSet dsResult = new DataSet();

            da.Fill(dsResult);

            return dsResult;
        }
    }
}
