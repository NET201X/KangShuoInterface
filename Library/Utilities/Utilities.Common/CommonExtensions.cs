using System;
using System.IO;
using System.Data;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Collections.Generic;
using System.Reflection;
using DAL;
using System.Text;
using System.Web;
using System.Net;
using System.Drawing;

namespace Utilities.Common
{
    public static class CommonExtensions
    {
        static string isCutString = Config.GetValue("isCutString");

        /// <summary>
        /// 表头
        /// </summary>
        /// <param name="cookieinfo"></param>
        /// <param name="contentLength"></param>
        /// <returns></returns>
        public static StringBuilder GetSendHeader(string cookieinfo, int contentLength)
        {
            StringBuilder strBuilder = new StringBuilder("");
            strBuilder.Append("POST /chss/messagebroker/amf HTTP/1.1\r\n");
            strBuilder.Append("Accept: */*\r\n");
            strBuilder.Append("Accept-Language: zh-CN\r\n");
            strBuilder.Append("Content-Type: application/x-amf\r\n");

            strBuilder.Append("Accept-Encoding: gzip, deflate\r\n");

            strBuilder.Append("Content-Length:").Append(contentLength).Append("\r\n");
            strBuilder.Append("Host: 192.168.200.3:1080\r\n");
            //strBuilder.Append("DNT: 1\r\n");
            strBuilder.Append("Pragma: no-cache\r\n");
            strBuilder.Append("Cookie:").Append(cookieinfo).Append("\r\n\r\n");

            return strBuilder;
        }

        /// <summary>
        /// 获取总页数
        /// </summary>
        /// <param name="strInfos"></param>
        /// <param name="startTag"></param>
        /// <param name="endTag"></param>
        /// <returns></returns>
        private static string GetTotalPage(string strInfos, string startTag, string endTag)
        {
            int indexStr = strInfos.LastIndexOf(startTag);

            string totalStr = strInfos.Substring(indexStr + startTag.Length);

            totalStr = totalStr.Substring(0, totalStr.IndexOf(endTag));

            return totalStr;
        }

        static object locker = new object();

        /// <summary>
        /// 记录log
        /// </summary>
        /// <param name="messageInfo">log</param>
        public static void WriteLog(string messageInfo)
        {
            string p_logFilePath = System.AppDomain.CurrentDomain.BaseDirectory + "\\Log";

            string pLogName = "\\" + DateTime.Now.ToString("yyyy-MM-dd") + "-ScheduleLog.txt";

            if (p_logFilePath != null)
            {
                if (!Directory.Exists(p_logFilePath))
                {
                    Directory.CreateDirectory(p_logFilePath);
                }

                if (!File.Exists(p_logFilePath + pLogName))
                {
                    FileStream cFile;
                    cFile = File.Create(p_logFilePath + pLogName);
                    cFile.Dispose();
                    cFile.Close();
                }

                lock (locker)
                {
                    // Log文件存在
                    FileStream p_FS = new FileStream(p_logFilePath + pLogName, FileMode.Append);

                    StreamWriter p_SW = new StreamWriter(p_FS, System.Text.Encoding.GetEncoding("UTF-8"));

                    // 第一行:打印當前時間
                    p_SW.WriteLine(DateTime.Now.ToString());

                    // 第二行:打印重要標題和詳細信息
                    p_SW.WriteLine(messageInfo);

                    // 第三行:打印"*"
                    p_SW.WriteLine("***********************************************************************");

                    // 第四行:打印空行,分隔下一筆Log資料
                    p_SW.WriteLine("");

                    // 關閉資料流
                    p_SW.Close();
                    p_FS.Dispose();
                    p_FS.Close();
                }
            }
        }

        /// <summary>
        /// 把List类型转为DataTable
        /// </summary>
        /// <typeparam name="T">目的类型</typeparam>
        /// <param name="lst">List数据源</param>
        /// <returns>DataTable</returns>        
        public static DataTable ToTable<T>(this List<T> lst) where T : class
        {
            DataTable dt = new DataTable();
            PropertyInfo[] propertys = typeof(T).GetProperties();
            Array.ForEach(propertys, x =>
            {
                dt.Columns.Add(x.Name);
            });

            lst.ForEach(x =>
            {
                DataRow dr = dt.NewRow();
                Array.ForEach(propertys, y =>
                {
                    dr[y.Name] = y.GetValue(x, null);
                });
                dt.Rows.Add(dr);
            });

            return dt;
        }

        /// <summary>
        /// 汇出
        /// </summary>
        /// <param name="strTmpname">模板列名</param>
        /// <param name="dt">源</param>
        /// <param name="strSheetName">sheet</param>
        /// <param name="strFileName">文件名</param>
        public static void ExpToExcel(string strTmpname, DataTable dt, string strSheetName, string strFileName)
        {
            try
            {
                var colomnsTmp = ExcelTmp.GetTmpColumn(strTmpname);

                if (colomnsTmp.Count <= 0)
                {
                    return;
                }

                // 创建建Excel文件的对象
                HSSFWorkbook workbook = new HSSFWorkbook();

                // 添加一个sheet
                ISheet sheetEmp = workbook.CreateSheet(strSheetName);

                IRow rowHeader = sheetEmp.CreateRow(0);

                int columnIndex = 0;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow drInfo = dt.Rows[i];

                    columnIndex = 0;

                    IRow rowBody = sheetEmp.CreateRow(i + 1);

                    foreach (var key in colomnsTmp.Keys)
                    {
                        // 创建表头
                        if (i == 0)
                        {
                            rowHeader.CreateCell(columnIndex).SetCellValue(colomnsTmp[key]);
                        }

                        rowBody.CreateCell(columnIndex).SetCellValue(drInfo[key].ToString());

                        columnIndex++;
                    }
                }

                // 写入到客戶端 
                MemoryStream ms = new MemoryStream();
                workbook.Write(ms);

                FileStream fs = new FileStream(Config.GetValue("downPath") + strFileName + ".xls", FileMode.OpenOrCreate);
                BinaryWriter w = new BinaryWriter(fs);

                w.Write(ms.ToArray());
                fs.Close();
                workbook = null;
                ms.Close();
                ms.Dispose();
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
                WriteLog(ex.StackTrace);
            }
        }

        /// <summary>
        /// 汇出多sheet
        /// </summary>
        /// <param name="tmpNames">模板栏位对应</param>
        /// <param name="ds">数据源</param>
        /// <param name="strFileName">保存文件名</param>
        public static void MultiSheetToExcel(IList<string> tmpNames, DataSet ds, string strFileName)
        {
            try
            {
                // 创建建Excel文件的对象
                HSSFWorkbook workbook = new HSSFWorkbook();

                int tableIndex = 0;
                foreach (string strTmpname in tmpNames)
                {
                    var colomnsTmp = ExcelTmp.GetTmpColumn(strTmpname);

                    if (colomnsTmp.Count <= 0)
                    {
                        continue;
                    }

                    DataTable dt = ds.Tables[tableIndex++];

                    // 添加一个sheet
                    ISheet sheetEmp = workbook.CreateSheet(dt.TableName);

                    IRow rowHeader = sheetEmp.CreateRow(0);

                    int columnIndex = 0;

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DataRow drInfo = dt.Rows[i];

                        columnIndex = 0;

                        IRow rowBody = sheetEmp.CreateRow(i + 1);

                        foreach (var key in colomnsTmp.Keys)
                        {
                            if (i == 0)
                            {
                                rowHeader.CreateCell(columnIndex).SetCellValue(colomnsTmp[key]);
                            }

                            rowBody.CreateCell(columnIndex).SetCellValue(drInfo[key].ToString());

                            columnIndex++;
                        }
                    }
                }

                // 写入到客戶端 
                MemoryStream ms = new MemoryStream();
                workbook.Write(ms);
                FileStream fs = new FileStream(Config.GetValue("downPath") + strFileName + ".xls", FileMode.OpenOrCreate);
                BinaryWriter w = new BinaryWriter(fs);

                w.Write(ms.ToArray());
                fs.Close();
                workbook = null;
                ms.Close();
                ms.Dispose();

            }
            catch (Exception ex)
            {
                WriteLog(ex.Message);
                WriteLog(ex.StackTrace);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strDate"></param>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(string strDate)
        {
            try
            {
                return DateTime.ParseExact(strDate, "yyyyMMdd", System.Globalization.CultureInfo.CurrentCulture);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public static string GetConvertDate(string str, string type = "")
        {
            try
            {
                if (string.IsNullOrEmpty(type))
                {
                    return DateTime.Parse(str).ToString("yyyy-MM-dd");
                }
                else
                {
                    return DateTime.Parse(str).ToString("yyyy年MM月dd日");
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static string GetUrlEncodeVal(string encodeVal, string encodeName = "utf-8")
        {
            return string.IsNullOrEmpty(encodeVal) || encodeVal.Trim() == "" ? "" : HttpUtility.UrlEncode(encodeVal, System.Text.Encoding.GetEncoding(encodeName));
        }

        /// <summary>
        /// 按照字节截取字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string cutSubstring(string str, int length)
        {
            if (str == null || str.Length == 0 || length < 0)
            {
                return "";
            }

            if (isCutString == "1")
            {
                byte[] bytes = System.Text.Encoding.Unicode.GetBytes(str);
                int n = 0;  //  表示当前的字节数
                int i = 0;  //  要截取的字节数
                for (; i < bytes.GetLength(0) && n < length; i++)
                {
                    //  偶数位置，如0、2、4等，为UCS2编码中两个字节的第一个字节
                    if (i % 2 == 0)
                    {
                        n++;      //  在UCS2第一个字节时n加1
                    }
                    else
                    {
                        //  当UCS2编码的第二个字节大于0时，该UCS2字符为汉字，一个汉字算两个字节
                        if (bytes[i] > 0)
                        {
                            n++;
                        }
                    }
                }
                //  如果i为奇数时，处理成偶数
                if (i % 2 == 1)
                {
                    //  该UCS2字符是汉字时，去掉这个截一半的汉字
                    if (bytes[i] > 0)
                        i = i - 1;
                    //  该UCS2字符是字母或数字，则保留该字符
                    else
                        i = i + 1;
                }
                return System.Text.Encoding.Unicode.GetString(bytes, 0, i);
            }

            return str;
        }

        public static int Getlenght(string str)
        {

            //使用Unicode编码的方式将字符串转换为字节数组,它将所有字符串(包括英文中文)全部以2个字节存储
            byte[] bytestr = System.Text.Encoding.Unicode.GetBytes(str);
            int j = 0;
            for (int i = 0; i < bytestr.GetLength(0); i++)
            {
                //取余2是因为字节数组中所有的双数下标的元素都是unicode字符的第一个字节
                if (i % 2 == 0)
                {
                    j++;
                }
                else
                {
                    //单数下标都是字符的第2个字节,如果一个字符第2个字节为0,则代表该Unicode字符是英文字符,否则为中文字符
                    if (bytestr[i] > 0)
                    {
                        j++;
                    }
                }
            }
            return j;
        }

        /// <summary>
        /// base64 转 Image
        /// </summary>
        /// <param name="base64"></param>
        public static void Base64ToImage(string base64)
        {
            base64 = base64.Replace("data:image/png;base64,", "").Replace("data:image/jgp;base64,", "").Replace("data:image/jpg;base64,", "").Replace("data:image/jpeg;base64,", "");//将base64头部信息替换
            byte[] bytes = Convert.FromBase64String(base64);
            MemoryStream memStream = new MemoryStream(bytes);
            Image mImage = Image.FromStream(memStream);
            Bitmap bp = new Bitmap(mImage);
            bp.Save("C:/Users/Administrator/Desktop/" + DateTime.Now.ToString("yyyyMMddHHss") + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);//注意保存路径
        }

        /// <summary>
        /// Image 转成 base64
        /// </summary>
        /// <param name="fileFullName"></param>
        public static string ImageToBase64(string fileFullName, string picType = "png")
        {
            try
            {
                Bitmap bmp = new Bitmap(fileFullName);
                MemoryStream ms = new MemoryStream();
                if (picType == "png")
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
               
                byte[] arr = new byte[ms.Length]; ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length); ms.Close();
                return Convert.ToBase64String(arr);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        private static CookieContainer _cookies;

        public static CookieContainer Cookies
        {
            get { return CommonExtensions._cookies; }
            set { CommonExtensions._cookies = value; }
        }

        private static string _userid;

        public static string Userid
        {
            get { return CommonExtensions._userid; }
            set { CommonExtensions._userid = value; }
        }
        private static string _password;

        public static string Password
        {
            get { return CommonExtensions._password; }
            set { CommonExtensions._password = value; }
        }
    }
}

