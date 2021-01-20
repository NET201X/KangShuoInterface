using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using FluorineFx.IO;
using System;

namespace Utilities.Common
{
    public class WebHelper
    {
        public string CookieInfo = "";

        public CookieContainer cookiesContainer;

        private string SocketUrl = DAL.Config.GetValue("SocketUrl");
        private string SocketPort = DAL.Config.GetValue("SocketPort");
        private string BaseUrl = DAL.Config.GetValue("baseUrl");
        /// <summary>
        /// 请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="contentType"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public string PostHttp(string url, string body, string contentType, CookieContainer cookie)
        {
            string returnVal = TryPostHttp(url, body, contentType, cookie, 1);

            if (returnVal.IndexOf("登录超时") > -1)
            {
                throw (new Exception("“登录超时”、“该用户在别处登录”或者“当前用户信息被上级用户修改”导致用户无法操作,请您重新登录!"));

                return "";
            }

            return returnVal;
        }

        public string PostHttpNoCookie(string url, string body, string contentType)
        {
            string returnVal = TryPostHttpNoCookie(url, body, contentType, 1);

            if (returnVal.IndexOf("登录超时") > -1)
            {
                throw (new Exception("“登录超时”、“该用户在别处登录”或者“当前用户信息被上级用户修改”导致用户无法操作,请您重新登录!"));

                return "";
            }

            return returnVal;
        }

        private string TryPostHttp(string url, string body, string contentType, CookieContainer cookie, int tryCount, int exception500 = 1)
        {
            //if (url.Contains("sdcsm_new"))
            //{
            //    if (!url.Contains("getJwhByJdRgid.dwr"))
            //    {
            //        System.Threading.Thread.Sleep((2) * 1000);
            //    }
            //}

            HttpWebRequest httpWebRequest;
            try
            {
                httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                httpWebRequest.ContentType = contentType;
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 60000;
                httpWebRequest.ReadWriteTimeout = 3000;
                httpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; Win64; x64; Trident/4.0; .NET CLR 2.0.50727; SLCC2; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";
                httpWebRequest.CookieContainer = CommonExtensions.Cookies;
                httpWebRequest.Referer = BaseUrl + "index.action";
                if (!string.IsNullOrEmpty(SocketUrl) && !string.IsNullOrEmpty(SocketPort))
                {
                    WebProxy wp = new WebProxy(SocketUrl, int.Parse(SocketPort));
                    httpWebRequest.Proxy = wp;
                }
                byte[] btBodys = Encoding.UTF8.GetBytes(body);
                httpWebRequest.ContentLength = btBodys.Length;

                httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);

                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                string responseContent = streamReader.ReadToEnd();

                streamReader.Close();
                httpWebRequest.Abort();
                httpWebResponse.Close();

                if (responseContent.Contains("<title>应用程序异常"))
                {
                    if (exception500 <= 1)
                    {
                    //    System.Threading.Thread.Sleep(1000 * 1);
                        exception500++;

                        if (url.Contains("sdcsm_new"))
                        {
                            CommonExtensions.Cookies = new CookieContainer();
                            // 重新登录
                            string postData = "loginname=" + CommonExtensions.Userid + "&password=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.Password) + "&loginType=1";
                            PostHttp(BaseUrl + "login.action", postData, "application/x-www-form-urlencoded", cookie);
                        }
                        return TryPostHttp(url, body, contentType, cookie, tryCount, exception500);
                    }
                    //else
                    //{
                    //    throw new Exception("登录超时！");
                    //}
                }

                return responseContent;
            }
            catch (Exception ex)
            {
                if (tryCount < 1)
                {
                    System.Threading.Thread.Sleep(1000);

                    tryCount++;
                    return TryPostHttp(url, body, contentType, cookie, tryCount);
                }
                else
                {
                    CommonExtensions.WriteLog(ex.Message);
                    CommonExtensions.WriteLog(ex.StackTrace);

                    return "";
                }
            }

        }

        private string TryPostHttpNoCookie(string url, string body, string contentType, int tryCount, int exception500 = 1)
        {
            HttpWebRequest httpWebRequest;
            try
            {
                httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                httpWebRequest.ContentType = contentType;
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 2000;
                httpWebRequest.ReadWriteTimeout = 3000;
                httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4181.9 Safari/537.36";
                httpWebRequest.Referer = BaseUrl + "index.action";
                
                if (!string.IsNullOrEmpty(SocketUrl) && !string.IsNullOrEmpty(SocketPort))
                {
                    WebProxy wp = new WebProxy(SocketUrl, int.Parse(SocketPort));
                    httpWebRequest.Proxy = wp;
                }
                byte[] btBodys = Encoding.UTF8.GetBytes(body);
                httpWebRequest.ContentLength = btBodys.Length;

                httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);

                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                string responseContent = streamReader.ReadToEnd();

                streamReader.Close();
                httpWebRequest.Abort();
                httpWebResponse.Close();

                if (responseContent.Contains("<title>应用程序异常"))
                {
                    if (exception500 <= 1)
                    {
                        //    System.Threading.Thread.Sleep(1000 * 1);
                        exception500++;
                        return TryPostHttpNoCookie(url, body, contentType, tryCount, exception500);
                    }
                    //else
                    //{
                    //    throw new Exception("登录超时！");
                    //}
                }

                return responseContent;
            }
            catch (Exception ex)
            {
                if (tryCount < 3)
                {
                    System.Threading.Thread.Sleep(50);

                    tryCount++;
                    return TryPostHttpNoCookie(url, body, contentType, tryCount);
                }
                else
                {
                    CommonExtensions.WriteLog(ex.Message);
                    CommonExtensions.WriteLog(ex.StackTrace);

                    return "";
                }
            }

        }

        public string PostHttpSOAP(string url, string body, string contentType, CookieContainer cookie)
        {
            string returnVal = TryPostSOAP(url, body, contentType, cookie, 1);

            if (returnVal.IndexOf("登录超时") > -1)
            {
                throw (new Exception("“登录超时”、“该用户在别处登录”或者“当前用户信息被上级用户修改”导致用户无法操作,请您重新登录!"));

                return "";
            }

            return returnVal;
        }

        private string TryPostSOAP(string url, string body, string contentType, CookieContainer cookie, int tryCount, int exception500 = 1)
        {
            HttpWebRequest httpWebRequest;
            try
            {
                httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

                httpWebRequest.ContentType = contentType;
                httpWebRequest.Method = "POST";
                httpWebRequest.Timeout = 60000;
                httpWebRequest.ReadWriteTimeout = 3000;
                //httpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; Win64; x64; Trident/4.0; .NET CLR 2.0.50727; SLCC2; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";
                httpWebRequest.CookieContainer = CommonExtensions.Cookies;
                httpWebRequest.Headers.Add("SOAPAction", "");
                if (!string.IsNullOrEmpty(SocketUrl) && !string.IsNullOrEmpty(SocketPort))
                {
                    WebProxy wp = new WebProxy(SocketUrl, int.Parse(SocketPort));
                    httpWebRequest.Proxy = wp;
                }
                byte[] btBodys = Encoding.UTF8.GetBytes(body);
                httpWebRequest.ContentLength = btBodys.Length;

                httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);

                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                string responseContent = streamReader.ReadToEnd();

                streamReader.Close();
                httpWebRequest.Abort();
                httpWebResponse.Close();

                if (responseContent.Contains("<title>应用程序异常"))
                {
                    if (exception500 <= 3)
                    {
                        //    System.Threading.Thread.Sleep(1000 * 1);
                        exception500++;

                        if (url.Contains("sdcsm_new"))
                        {
                            CommonExtensions.Cookies = new CookieContainer();
                            // 重新登录
                            string postData = "loginname=" + CommonExtensions.Userid + "&password=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.Password) + "&loginType=1";
                            PostHttp(BaseUrl + "login.action", postData, "application/x-www-form-urlencoded", cookie);
                        }
                        return TryPostSOAP(url, body, contentType, cookie, tryCount, exception500);
                    }
                    //else
                    //{
                    //    throw new Exception("登录超时！");
                    //}
                }

                return responseContent;
            }
            catch (Exception ex)
            {
                if (tryCount < 1)
                {
                    System.Threading.Thread.Sleep(1000);

                    tryCount++;
                    return TryPostSOAP(url, body, contentType, cookie, tryCount);
                }
                else
                {
                    CommonExtensions.WriteLog(ex.Message);
                    CommonExtensions.WriteLog(ex.StackTrace);

                    return "";
                }
            }

        }

        /// <summary>
        /// GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="urlData"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public string GetHttp(string url, string urlData, CookieContainer cookie)
        {
            return TryGetHttp(url, urlData, cookie, 1);
        }

        private string TryGetHttp(string Url, string postDataStr, CookieContainer cookie, int tryCount, int exception500 = 1)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
                request.Method = "GET";
                request.ContentType = "text/html;charset=UTF-8";
                request.Timeout = 20000;
                request.ReadWriteTimeout = 3000;
                request.CookieContainer = CommonExtensions.Cookies;
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; Win64; x64; Trident/4.0; .NET CLR 2.0.50727; SLCC2; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";
                request.Referer = BaseUrl + "index.action";
                if (!string.IsNullOrEmpty(SocketUrl) && !string.IsNullOrEmpty(SocketPort))
                {
                    WebProxy wp = new WebProxy(SocketUrl, int.Parse(SocketPort));
                    request.Proxy = wp;
                }
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                if (retString.Contains("<title>应用程序异常"))
                {
                    if (exception500 <= 3)
                    {
                        //System.Threading.Thread.Sleep(1000 * 1);
                        exception500++;

                        if (Url.Contains("sdcsm_new"))
                        {
                            CommonExtensions.Cookies = new CookieContainer();
                            // 重新登录
                            string postData = "loginname=" + CommonExtensions.Userid + "&password=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.Password) + "&loginType=1";
                            PostHttp(BaseUrl + "login.action", postData, "application/x-www-form-urlencoded", cookie);

                        }

                        return TryGetHttp(Url, postDataStr, cookie, tryCount, exception500);
                    }
                    //else
                    //{
                    //    throw new Exception("登录超时！");
                    //}
                }
                return retString;
            }
            catch (Exception ex)
            {
                if (tryCount < 4)
                {
                    System.Threading.Thread.Sleep(1000);

                    tryCount++;
                    return TryGetHttp(Url, postDataStr, cookie, tryCount);
                }
                else
                {
                    CommonExtensions.WriteLog(ex.Message);
                    CommonExtensions.WriteLog(ex.StackTrace);

                    return "";
                }
            }
        }
    }

    public enum AMFType
    {
        AMF0,
        AMF3
    }
}
