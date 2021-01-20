using System.Configuration;
using System.Data;
using DAL;

namespace BaseBusiness
{
    public class BaseBusinessRule
    {
        // 连接字串
        private string _strConnectionString = null;

        #region 属性

        /// <summary> 
        /// 连接设定
        /// </summary>
        public ConnectionStringSettings ConnectionSetting
        {
            set;
            get;
        }

        
        /// <summary>
        /// 参数集合
        /// </summary>
        protected CommandParameterCollection Parameter = new CommandParameterCollection();

        #endregion

        #region 方法

        public BaseBusinessRule(string connectstringName)
        {
            _strConnectionString = ConfigurationManager.ConnectionStrings[connectstringName].ConnectionString;
            ConnectionSetting = ConfigurationManager.ConnectionStrings[connectstringName];
        }

        public BaseBusinessRule()
        {
            _strConnectionString = Config.ConnectionString;
            ConnectionSetting = Config.ConnectionStringSetting;
        }

        /// <summary>
        /// 初始化连接
        /// </summary>
        /// <param name="conn">
        /// conn
        /// </param>
        public BaseBusinessRule(ConnectionStringSettings conn)
        {
            this.ConnectionSetting = conn;
        }

        /// <summary>
        /// 取得连接并开启连接
        /// </summary>
        /// <returns></returns>
        protected IDbConnection OpenConnection()
        {
            // 若有连接设定参数，优先以连接设定的参数为主
            if (this.ConnectionSetting != null)
            {
                return ConnectionUnit.NewDBConnection(this.ConnectionSetting);
            }

            if (!string.IsNullOrEmpty(_strConnectionString))
            {
                return ConnectionUnit.NewDBConnection(_strConnectionString);
            }

            return ConnectionUnit.NewDBConnection();
        }

        /// <summary>
        /// 关闭已经开启的连接
        /// </summary>
        protected void CloseConnection(IDbConnection AConnection)
        {
            if (AConnection != null && AConnection.State == ConnectionState.Open)
            {
                AConnection.Close();
            }
        }

        /// <summary>
        /// 开启连接的事务
        /// </summary>
        /// <param name="AConnection">连接事务</param>
        /// <returns></returns>
        protected IDbTransaction GetTransaction(IDbConnection AConnection)
        {
            return AConnection.BeginTransaction();
        }

        /// <summary>
        /// 执行Sql，返回影响行数
        /// </summary>
        /// <param name="sSql">SQL</param>
        /// <returns></returns>
        protected int ExeNonQuery(string sSql)
        {
            return this.ExeNonQuery(sSql, false);
        }

        /// <summary>
        /// 执行Sql，返回影响行数
        /// </summary>
        /// <param name="sSql">SQL</param>
        /// <returns></returns>
        protected int ExecuteNonQueryByAtt(string Sql)
        {
            using (IDbConnection AConnection = this.OpenConnection())
            {
                IDbCommand NewCommand = CommandUnit.NewCommand(Sql, AConnection);

                NewCommand.AddParameters(this.Parameter);

                return NewCommand.ExeNonQuery();
            }
        }

        /// <summary>
        /// 执行Sql，返回影响行数
        /// </summary>
        /// <param name="Sql">Sql</param>
        /// <param name="bPrepare">是否要预编译</param>
        /// <returns></returns>
        protected int ExeNonQuery(string Sql, bool bPrepare)
        {
            using (IDbConnection AConnection = this.OpenConnection())
            {
                // 实例化命令
                IDbCommand NewCommand = CommandUnit.NewCommand(Sql, AConnection);

                // 加入参数
                NewCommand.AddParameters(this.Parameter);

                // 返回结果
                return NewCommand.ExeNonQuery();
            }
        }

        /// <summary>
        /// 执行Sql，返回影响行数
        /// </summary>
        /// <param name="Sql">Sql</param>
        /// <param name="ATans">事务</param>
        /// <returns></returns>
        protected int ExeNonQuery(string Sql, IDbTransaction ATans)
        {
            // 实例化命令
            IDbCommand NewCommand = CommandUnit.NewCommand(Sql, ATans.Connection, ATans);

            // 加入参数
            NewCommand.AddParameters(this.Parameter);

            // 返回结果
            return NewCommand.ExeNonQuery();
        }

        /// <summary>
        /// 使用StoredProcedure查询结果集
        /// </summary>
        /// <param name="spName">SP名称</param>
        /// <returns></returns>
        protected int ExecuteNonQuerySP(string spName)
        {
            using (IDbConnection AConnection = this.OpenConnection())
            {
                // 实例化命令
                IDbCommand newCommand = CommandUnit.NewCommand(AConnection);

                // 存储过程类型
                newCommand.CommandType = CommandType.StoredProcedure;

                // 存储过程名称
                newCommand.CommandText = spName;

                // 加入参数
                newCommand.AddParameters(this.Parameter);

                // 返回结果
                return newCommand.ExeNonQuery(); ;
            }
        }

        /// <summary>
        /// 执行Sql，返回物件
        /// </summary>
        /// <param name="sSql">SQL</param>
        /// <returns></returns>
        protected object ExecuteScalar(string Sql)
        {
            return this.ExecuteScalar(Sql, false);
        }

        /// <summary>
        /// 执行Sql，返回物件
        /// </summary>
        /// <param name="sSql">SQL</param>
        /// <returns></returns>
        protected object ExecuteScalar(string Sql, IDbTransaction Atran)
        {
            // 实例化命令
            IDbCommand NewCommand = CommandUnit.NewCommand(Sql, Atran.Connection, Atran);

            // 加入参数
            NewCommand.AddParameters(this.Parameter);

            return NewCommand.ExeScalar();
        }

        /// <summary>
        /// 执行Sql，返回物件
        /// </summary>
        /// <param name="Sql">Sql</param>
        /// <param name="bPrepare">是否要预编译</param>
        /// <returns></returns>
        protected object ExecuteScalar(string Sql, bool bPrepare)
        {
            using (IDbConnection AConnection = this.OpenConnection())
            {
                // 實例化命令
                IDbCommand NewCommand = CommandUnit.NewCommand(Sql, AConnection);

                // 加入參數
                NewCommand.AddParameters(this.Parameter);

                // 返回結果物件
                return NewCommand.ExeScalar();
            }
        }

        /// <summary>
        /// 执行Sql，返回Reader物件
        /// </summary>
        /// <param name="Sql">SQL</param>
        /// <returns></returns>
        protected IDataReader ExecuteReader(string Sql)
        {
            return this.ExecuteReader(Sql, false);
        }

        /// <summary>
        /// 执行Sql，返回Reader物件
        /// </summary>
        /// <param name="sSql">Sql</param>
        /// <param name="bPrepare">是否要预编译</param>
        /// <returns></returns>
        protected IDataReader ExecuteReader(string Sql, bool bPrepare)
        {
            // 实例化并开启连接
            using (IDbConnection AConnection = this.OpenConnection())
            {
                // 实例化命令
                IDbCommand NewCommand = CommandUnit.NewCommand(Sql, AConnection);

                // 加入参数
                NewCommand.AddParameters(this.Parameter);

                // 返回Reader
                return NewCommand.ExeReader(CommandBehavior.CloseConnection);
            }
        }

        /// <summary>
        /// 查询结果集
        /// </summary>
        /// <param name="Sql">SQL</param>
        /// <returns></returns>
        protected DataTable Search(string Sql)
        {
            using (IDbConnection AConnection = this.OpenConnection())
            {
                // 实例化命令
                IDbCommand NewCommand = CommandUnit.NewCommand(Sql, AConnection);

                // 加入参数
                NewCommand.AddParameters(this.Parameter);

                // 返回结果集
                return NewCommand.Search();
            }
        }

        /// <summary>
        /// 查询结果集
        /// </summary>
        /// <param name="sSql">SQL</param>
        /// <param name="tran">tran事务</param>
        /// <returns></returns>
        /// <remarks></remarks>
        protected DataTable Search(string Sql, IDbTransaction tran)
        {
            // 实例化命令
            IDbCommand NewCommand = CommandUnit.NewCommand(Sql, tran.Connection, tran);

            // 加入参数
            NewCommand.AddParameters(this.Parameter);

            // 返回结果集
            return NewCommand.Search();
        }

        /// <summary>
        /// 查询结果集
        /// </summary>
        /// <param name="Sql">SQL</param>
        /// <returns></returns>
        protected DataSet SearchDataSet(string Sql)
        {
            using (IDbConnection AConnection = this.OpenConnection())
            {
                // 实例化命令
                IDbCommand NewCommand = CommandUnit.NewCommand(Sql, AConnection);

                // 加入参数
                NewCommand.AddParameters(this.Parameter);

                // 返回结果集
                return NewCommand.SearchDataSet();
            }
        }

        /// <summary>
        /// 查询结果集
        /// </summary>
        /// <param name="sSql">SQL</param>
        /// <param name="tran">tran事务</param>
        /// <returns></returns>
        /// <remarks></remarks>
        protected DataSet SearchDataSet(string Sql, IDbTransaction tran)
        {
            // 实例化命令
            IDbCommand NewCommand = CommandUnit.NewCommand(Sql, tran.Connection, tran);

            // 加入参数
            NewCommand.AddParameters(this.Parameter);

            // 返回结果集
            return NewCommand.SearchDataSet();
        }

        /// <summary>
        /// 使用StoredProcedure查询结果集
        /// </summary>
        /// <param name="spName">SP名称</param>
        /// <returns></returns>
        protected DataTable ExecuteSP(string spName)
        {
            using (IDbConnection AConnection = this.OpenConnection())
            {
                // 实例化命令
                IDbCommand NewCommand = CommandUnit.NewCommand(AConnection);

                // 存储过程命令类型
                NewCommand.CommandType = CommandType.StoredProcedure;

                // 命令文本
                NewCommand.CommandText = spName;

                // 加入参数
                NewCommand.AddParameters(this.Parameter);

                // 返回结果集
                return NewCommand.Search();
            }
        }

        /*/// <summary>
        /// 把DataTable转成List
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="dt">数据源</param>
        /// <returns>泛型集合</returns>
        protected IList<T> DataSetToList<T>(DataTable dt)
        {
            if (dt == null)
            {
                return null;
            }

            if (dt.Rows.Count < 0)
            {
                return null;
            }

            // 创建泛型集合
            IList<T> list = new List<T>();

            // 获取实体类和DataTable对应的属性清单
            Dictionary<PropertyInfo, string> pro = new Dictionary<PropertyInfo, string>();

            Array.ForEach<PropertyInfo>(typeof(T).GetProperties(), property =>
            {
                foreach (DataColumn column in dt.Columns)
                {
                    if (string.Compare(column.ColumnName, property.Name, true) == 0)
                    {
                        pro.Add(property, column.ColumnName);
                    }
                }
            });

            // 对清单中的实体类属性赋值
            foreach (DataRow row in dt.Rows)
            {
                // 创建泛型对象
                T t = Activator.CreateInstance<T>();

                foreach (PropertyInfo property in pro.Keys)
                {
                    object value = row[pro[property]];
                    string fullName = property.PropertyType.ToString();
                    if (fullName.Contains("System.Nullable"))
                    {
                        value = string.IsNullOrEmpty(value.ToString()) ? null : value;

                        if (value == null)
                        {
                            property.SetValue(t, null, null);
                        }
                        else
                        {
                            if (fullName.Contains("System.Int32"))
                            {
                                property.SetValue(t, Convert.ToInt32(value.ToString()), null);
                            }
                            else if (fullName.Contains("System.Int64"))
                            {
                                property.SetValue(t, Convert.ToInt64(value.ToString()), null);
                            }
                            else if (fullName.Contains("System.String"))
                            {
                                property.SetValue(t, value.ToString(), null);
                            }
                            else if (fullName.Contains("System.DateTime"))
                            {
                                property.SetValue(t, Convert.ToDateTime(value.ToString()), null);
                            }
                            else if (fullName.Contains("System.Decimal"))
                            {
                                property.SetValue(t, Convert.ToDecimal(value.ToString()), null);
                            }
                            else if (fullName.Contains("System.Char"))
                            {
                                property.SetValue(t, Convert.ToChar(value.ToString()), null);
                            }
                            else if (fullName.Contains("System.Boolean"))
                            {
                                property.SetValue(t, Convert.ToBoolean(value.ToString()), null);
                            }
                            else if (fullName.Contains("System.Guid"))
                            {
                                property.SetValue(t, new Guid(value.ToString()), null);
                            }
                            else
                            {
                                property.SetValue(t, Convert.ToString(value), null);
                            }
                        }
                    }
                    else
                    {
                        switch (fullName)
                        {
                            case "System.Int32":
                                property.SetValue(t, Convert.ToInt32(string.IsNullOrEmpty(value.ToString()) ? "0" : value), null);
                                break;
                            case "System.Int64":
                                property.SetValue(t, Convert.ToInt64(string.IsNullOrEmpty(value.ToString()) ? "0" : value), null);
                                break;
                            case "System.String":
                                if ((property.PropertyType).FullName.Contains("DateTime"))
                                {
                                    property.SetValue(t, Convert.ToDateTime(value.ToString()), null);
                                }
                                else
                                {
                                    property.SetValue(t, value == null ? "" : value.ToString(), null);
                                }

                                break;
                            case "System.DateTime":
                                property.SetValue(t, string.IsNullOrEmpty(value.ToString()) ? DateTime.MinValue : Convert.ToDateTime(value.ToString()), null);
                                break;
                            case "System.Decimal":
                                property.SetValue(t, string.IsNullOrEmpty(value.ToString()) ? 0 : Convert.ToDecimal(value.ToString()), null);
                                break;
                            case "System.Char":
                                property.SetValue(t, string.IsNullOrEmpty(value.ToString()) ? ' ' : Convert.ToChar(value.ToString()), null);
                                break;
                            case "System.Boolean":
                                property.SetValue(t, Convert.ToBoolean(value.ToString()), null);
                                break;
                            case "System.Guid":
                                property.SetValue(t, new Guid(value.ToString()), null);
                                break;
                            case "System.Byte[]":
                                property.SetValue(t, value, null);
                                break;
                            default:
                                property.SetValue(t, value == null ? "" : value.ToString(), null);
                                break;
                        }
                    }
                }

                list.Add(t);
            }

            return list;
        }*/

        #endregion
    }
}
