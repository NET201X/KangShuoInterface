using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace DAL
{
    public class Config
    {
        ///<summary>
        /// 连线参数 
        ///</summary>
        public static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DBConnectionString"].ConnectionString;
            }
        }

        /// <summary>
        /// 取得DBConnectionString
        /// </summary>
        public static ConnectionStringSettings ConnectionStringSetting
        {
            get { return ConfigurationManager.ConnectionStrings["DBConnectionString"]; }
        }

        /// <summary>
        /// 取得AppSetting资料
        /// </summary>
        /// <returns></returns>
        public static string GetValue(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }

        /// <summary>
        /// 取得连接字串信息
        /// </summary>
        /// <returns></returns>
        public static string GetConnectionString(string name)
        {
            return ConfigurationManager.ConnectionStrings[name].ConnectionString;
        }
    }
}
