using System.IO;
using HtmlAgilityPack;
using System;
using System.Web;

namespace Utilities.Common
{
    public static class HtmlHelper
    {
        public static HtmlDocument GetHtmlDocument(string strHtml)
        {
            HtmlDocument doc = new HtmlDocument();

            strHtml = strHtml.Replace("\t", "").Replace("/>", " />");

            try
            {
                using (TextReader reader = new StringReader(strHtml))
                {
                    doc.Load(reader);
                }
            }
            catch (Exception ex)
            {
                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);

                return doc;
            }

            return doc;
        }

        public static string GetCreateMenName(string strInfos, string startTag)
        {
            string strTmp = GetLastTagValue(strInfos, startTag, "</tr>");

            strTmp = GetInputValue(strTmp, "text");

            return strTmp;
        }

        public static string GetTagValue(string strInfos, string startTag, string endTag)
        {
            int indexStr = strInfos.IndexOf(startTag);

            if (indexStr < 0)
            {
                return "";
            }

            string tagStr = strInfos.Substring(indexStr + startTag.Length);

            int endIndex = tagStr.IndexOf(endTag);

            if (endIndex > -1)
            {
                tagStr = tagStr.Substring(0, endIndex);
            }
            else
            {
                tagStr = "";
            }

            return tagStr;
        }

        public static string GetLastTagValue(string strInfos, string startTag, string endTag)
        {
            int indexStr = strInfos.LastIndexOf(startTag);

            if (indexStr < 0)
            {
                return "";
            }

            string tagStr = strInfos.Substring(indexStr + startTag.Length);

            int endIndex = tagStr.IndexOf(endTag);

            if (endIndex > -1)
            {
                tagStr = tagStr.Substring(0, endIndex);
            }
            else
            {
                tagStr = "";
            }

            return tagStr;
        }

        /// <summary>
        /// input 标签 只限制输入框
        /// </summary>
        /// <param name="strInfos"></param>
        /// <param name="startTag"></param>
        /// <returns></returns>
        private static string GetInputValue(string strInfos, string startTag)
        {
            startTag = startTag + "\"";

            // 整个input标签
            string strFlagSE = GetTagHtml(strInfos, startTag, "<input", "/>");

            return GetTagValue(strFlagSE, "value=\"", "\"");
        }

        /// <summary>
        /// 下拉单
        /// </summary>
        /// <param name="strInfos"></param>
        /// <param name="htmlIDorName"></param>
        /// <returns></returns>
        private static string GetSelectValue(string strInfos, string htmlIDorName)
        {
            htmlIDorName = htmlIDorName + "\"";

            // 整个select标签
            string strFlagSE = GetTagHtml(strInfos, htmlIDorName, "<select", "</select>");

            string strTmp = GetTagHtml(strFlagSE, "selected", "<option", "</option>");

            strTmp = GetTagValue(strTmp, "value=\"", "\"");

            return strTmp;
        }

        /// <summary>
        /// 下拉单
        /// </summary>
        /// <param name="strInfos"></param>
        /// <param name="htmlIDorName"></param>
        /// <returns></returns>
        private static string GetSelectText(string strInfos, string htmlIDorName)
        {
            htmlIDorName = htmlIDorName + "\"";

            // 整个select标签
            string strFlagSE = GetTagHtml(strInfos, htmlIDorName, "<select", "</select>");

            string strTmp = GetTagHtml(strFlagSE, "selected", "<option", "</option>");

            strTmp = GetTagValue(strTmp, ">", "</option>");

            return strTmp;
        }

        /// <summary>
        /// 获取前后标记信息
        /// </summary>
        /// <param name="strInfos"></param>
        /// <param name="startTag"></param>
        /// <param name="htmlFlag"></param>
        /// <returns></returns>
        private static string GetTagHtml(string strInfos, string startTag, string startHtmlFlag, string endHtmlFlag)
        {
            // 开始标记
            int startF = strInfos.IndexOf(startTag);

            if (startF < 0)
            {
                return "";
            }

            string strFlagStart = strInfos.Substring(0, startF);

            string strFlagEnd = strInfos.Substring(startF);

            startF = strFlagStart.LastIndexOf(startHtmlFlag);

            if (startF < 0)
            {
                return "";
            }

            strFlagStart = strFlagStart.Substring(startF);

            // 结束标记
            startF = strFlagEnd.IndexOf(endHtmlFlag);

            if (startF < 0)
            {
                return "";
            }

            strFlagEnd = strFlagEnd.Substring(0, startF + endHtmlFlag.Length);

            // 整个html标签
            return strFlagStart + strFlagEnd;
        }

        /// <summary>
        /// checkbox 或 radio
        /// </summary>
        /// <param name="strInfos"></param>
        /// <param name="startTag"></param>
        /// <returns></returns>
        private static string GetCheckBoxOrRadioValue(string strInfos, string startTag)
        {
            string returnVal = "";

            startTag = startTag + "\"";

            int indexStr = strInfos.IndexOf(startTag);

            string strTmp = "";

            if (indexStr > -1)
            {
                strTmp = strInfos.Substring(indexStr);

                strTmp = GetTagHtml(strInfos, startTag, "<input", startTag) + strTmp;
            }

            indexStr = strTmp.LastIndexOf(startTag);

            if (indexStr > -1)
            {
                strTmp = strTmp.Substring(0, indexStr);
            }

            int indexStrLast = strInfos.LastIndexOf(startTag);

            string strTmp2 = "";

            // 最后一项
            if (indexStrLast > -1)
            {
                strTmp2 = strInfos.Substring(indexStrLast);
            }

            int indexStrLast2 = strTmp2.IndexOf("/>");

            if (indexStrLast2 > -1)
            {
                strTmp2 = strTmp2.Substring(0, indexStrLast2 + 2);
            }

            strTmp = strTmp + strTmp2;

            if (strTmp.Length > 0)
            {
                returnVal = GetCheckVal(strTmp, returnVal);
            }

            return returnVal.TrimStart(',');
        }

        /// <summary>
        /// 获取选中的box
        /// </summary>
        /// <param name="strInfos"></param>
        /// <param name="returnVal"></param>
        /// <returns></returns>
        private static string GetCheckVal(string strInfos, string returnVal)
        {
            // 第一个
            string strFlagSE = GetTagHtml(strInfos, "checked", "<input", "/>");

            string rt = GetLastTagValue(strFlagSE, "value=\"", "\"");

            if (string.IsNullOrEmpty(rt))
            {
                return returnVal;
            }
            else
            {
                returnVal += "," + rt;
            }

            string nextStr = strInfos.Replace(strFlagSE, "");

            if (nextStr.Length > 0)
            {
                returnVal = GetCheckVal(nextStr, returnVal);
            }

            return returnVal;
        }

        private static string GetBody(string strInfos)
        {
            return GetTagValue(strInfos, "<body", "</body>");
        }

        public static string GetUrlEncodeVal(string encodeVal, string encodeName = "UTF-8")
        {
            return string.IsNullOrEmpty(encodeVal) ? "" : HttpUtility.UrlEncode(encodeVal, System.Text.Encoding.GetEncoding(encodeName));
        }
    }
}
