using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DAL;
using System.Net;
using System.Data;
using Model.InfoModel;
using Utilities.Common;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Model.JsonModel;

namespace LnrBusiness
{
    public class LnrBusiness
    {
        string baseUrl = Config.GetValue("baseUrl");
        string zybsqm = Config.GetValue("zybsqm");
        string zlnlqm = Config.GetValue("zlnlqm");

        #region 参数

        private int MaxtryCount = Convert.ToInt32(Config.GetValue("errorTryCount"));

        private int SleepMilliseconds = Convert.ToInt32(Config.GetValue("sleepMilliseconds"));

        /// <summary>
        /// 默认医生
        /// </summary>
        public string DefultDoctor = "";


        public int outkey=0;

        /// <summary>
        /// 默认医生
        /// </summary>
        public string DefultDoctorName = "";

        /// <summary>
        /// 下载到的笔数
        /// </summary>
        public int currentIndex = 1;

        /// <summary>
        /// 每次下载笔数
        /// </summary>
        int pageSize = 100;

        // 存储总行数
        public int totalRows = 0;

        /// <summary>
        /// 系统cookie
        /// </summary>
        public string SysCookie { get; set; }

        /// <summary>
        /// 系统cookie
        /// </summary>
        public CookieContainer SysCookieContainer { get; set; }

        /// <summary>
        /// 待上传数据
        /// </summary>
        public IList<DataSet> lstUploadData = new List<DataSet>();

        public List<PersonModel> lstPerson = new List<PersonModel>();

        #endregion

        public string loginkey { set; get; }

        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="callback"></param>
        public void SaveLnr(Action<string> callback)
        {
            int currentIndex = 1;

            foreach (DataSet ds in lstUploadData)
            {
                TrySaveLnr(ds, 1, callback);

                callback("上传-老年人信息..." + currentIndex + "/" + lstUploadData.Count);
                currentIndex++;

                if (baseUrl.Contains("sdcsm_new"))
                {
                    System.Threading.Thread.Sleep((3) * 1000);
                }
            }
        }

        #region 上传
        /// <summary>
        /// 尝试三次上传
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        private void TrySaveLnr(DataSet ds, int tryCount, Action<string> callback)
        {
            try
            {
                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                #region  生活能力 - 不传老年人自理能力，在体检中上传

                //DataTable dt = ds.Tables["OLDER_SELFCAREABILITY"];
                
                //if (dt != null && dt.Rows.Count > 0)
                //{
                //    string idNumber = dt.Rows[0]["IDCardNo"].ToString();
               

                //    PersonModel person = cb.GetGrdaByIDCardNo(idNumber, loginkey, SysCookieContainer);

                //    if (person == null || string.IsNullOrEmpty(person.pid))
                //    {
                //        callback("EX-老年人信息:身份证[" + idNumber + "]:平台尚未建档");
                //        return;
                //    }

                //    List<SFClass> lstSF = GetSFxxLst(person.pid);

                //    string padSFDate = Convert.ToDateTime(dt.Rows[0]["FollowUpDate"]).ToString("yyyy-MM-dd");
                //    var sfInfo2 = lstSF.Where(m => m.sfDate == padSFDate).ToList();
                //    string msg = "";

                //    if (sfInfo2.Count > 0)
                //    {
                //        //更新随访
                //        msg = UpdateLnr(ds, person, padSFDate, sfInfo2[0].key);
                //    }
                //    else
                //    {
                //        //新增随访
                //        msg = AddLnr(ds, person, padSFDate);
                //    }
                //    if (!string.IsNullOrEmpty(msg))
                //    {
                //        callback("EX-老年人生活自理信息:身份证[" + idNumber + "]:" + msg);
                //    }
                //}
                #endregion

                #region 中医辨识
                string zybj = Config.GetValue("uploadZybj");

                //配置文件配置上传中医保健时才上传
                if (zybj == "1")
                {
                    DataTable dt2 = ds.Tables["OLD_MEDICINE_CN"];
                    if (dt2 == null || dt2.Rows.Count < 1)
                    {
                        return;
                    }
                    string id2 = dt2.Rows[0]["IDCardNo"].ToString();
                    string dat2 = string.IsNullOrWhiteSpace(dt2.Rows[0]["RecordDate"].ToString()) ? "" : Convert.ToDateTime(dt2.Rows[0]["RecordDate"].ToString()).ToString("yyyy-MM-dd");
                    string name = dt2.Rows[0]["CustomerName"].ToString();
                    PersonModel person2 = cb.GetGrdaByIDCardNo(id2, loginkey, SysCookieContainer);

                    if (person2 == null || string.IsNullOrEmpty(person2.pid))
                    {
                        callback("EX-老年人信息:身份证[" + id2 + "],姓名[" + name + "]:平台尚未建档或者档案状态为非活动!");
                        return;
                    }
                    List<SFClass> sflist = GetxxTZLst(person2.pid);
                    var sfInfo = sflist.Where(m => m.sfDate == dat2).ToList();
                    string msg = "";
                    if (sfInfo.Count > 0)
                    {
                        msg = Updatezlbs(ds, person2, sflist[0]);
                    }
                    else
                    {
                        msg = ADDzybs(ds, person2);
                    }
                    if (!string.IsNullOrEmpty(msg))
                    {
                        callback("EX-老年人中医体质辨识:身份证[" + id2 + "],姓名[" + name + "]:" + msg);
                    }
                }
                #endregion

            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("登录超时") > -1)
                {
                    callback("EX-“登录超时”、“该用户在别处登录”或者“当前用户信息被上级用户修改”导致用户无法操作,请您重新登录!");

                    throw;
                }


                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);

                if (tryCount < MaxtryCount)
                {
                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    TrySaveLnr(ds, tryCount, callback);
                }
                else
                {
                    callback("EX-上传老年人信息失败，请确保网路畅通");
                }
            }
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="ds"></param>
        /// 
        public string AddLnr(DataSet ds, PersonModel pm, string padSFDate)
        {
            WebHelper web = new WebHelper();

            string postData = "dGrdabh=" + pm.pid;
            string returnString = web.PostHttp(baseUrl + "/lnr/toaddtjglnrsf.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            StringBuilder sbPost = new StringBuilder();

            sbPost.Append("dGrdabh=").Append(pm.pid);

            #region 不修改栏位
            var node = doc.DocumentNode.SelectSingleNode("//input[@id='sfcs']");

            string strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&sfcs=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='contains']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&contains=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='lnrsf.info.dXm']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&lnrsf.info.dXm=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXb']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dXb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dSfzh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dSfzh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dCsrq']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dCsrq=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dLxdh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dLxdh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dSheng']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dSheng=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dShi']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dShi=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dQu']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dQu=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJd']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dJd=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJwh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dJwh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXxdz']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dXxdz=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            /* node = doc.DocumentNode.SelectSingleNode("//input[@name='happentime']");
             strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
             sbPost.Append("&happentime=").Append(strTmp);*/

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&createtime=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='updatetime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&updatetime=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&createuser=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='updateuser']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&updateuser=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='creatregion']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&creatregion=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='pRgid']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&pRgid=").Append(strTmp);

            #region 新加
            node = doc.DocumentNode.SelectSingleNode("//input[@name='contains']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&contains=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dGrdabh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dGrdabh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dGrdabh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dGrdabh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='lnrsfold.happentime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&lnrsfold.happentime=").Append(strTmp);

            #endregion
            int pNqdqxz = 0;

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXcsfmb']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXcsfmb=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            #endregion

            #region OLDER_SELFCAREABILITY

            DataTable dt = ds.Tables["OLDER_SELFCAREABILITY"];
            DataRow dr = dt.Rows[0];

            strTmp = dr["NextVisitAim"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gXcsfmb=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            sbPost.Append("&happentime=").Append(padSFDate);
            sbPost.Append("&gPgrq=").Append(padSFDate);

            strTmp = dr["Dine"].ToString();

            strTmp = strTmp == "2" ? "0" : strTmp;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&jincan=on");
            sbPost.Append("&jcpf=").Append(strTmp);

            strTmp = dr["Groming"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&shuxi=on");
            sbPost.Append("&sxpf=").Append(strTmp);

            strTmp = dr["Dressing"].ToString();

            strTmp = strTmp == "2" ? "0" : strTmp;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&chuanyi=on");
            sbPost.Append("&cypf=").Append(strTmp);

            strTmp = dr["Tolet"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&ruce=on");
            sbPost.Append("&rcpf=").Append(strTmp);

            strTmp = dr["Activity"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&huodong=on");
            sbPost.Append("&hdpf=").Append(strTmp);

            sbPost.Append("&zpf=").Append(dr["TotalScore"]);

            strTmp = dr["FollowUpDoctor"].ToString();
            if (string.IsNullOrEmpty(zlnlqm))
            {
                zlnlqm = strTmp;
            }
            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gSfys=").Append(CommonExtensions.GetUrlEncodeVal(zlnlqm));

            strTmp = dr["NextfollowUpDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextfollowUpDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfrq=").Append(strTmp);
            sbPost.Append("&info.lXcsfsj=").Append(strTmp);


            sbPost.Append("&qdqxz=").Append(pNqdqxz);
            sbPost.Append("&wzd=").Append(((9 - pNqdqxz) * 100.0 / 9).ToString("#"));

            #endregion

            returnString = web.PostHttp(baseUrl + "/lnr/addtjglnrsf.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return "新增失败！";
            }

            doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null || doc.DocumentNode.SelectSingleNode("//body/script[last()]") == null)
            {
                return "新增失败！";
            }
            else
            {
                var returnNode = doc.DocumentNode.SelectSingleNode("//body/script[last()]");

                if (returnNode.InnerText.IndexOf("操作成功") == -1)
                {
                    return "新增失败！";
                }
            }

            return "";
        }

        /// <summary>
        /// 修改  
        /// </summary>
        /// <param name="ds"></param>
        public string UpdateLnr(DataSet ds, PersonModel pm, string padSFDate, string key)
        {
            WebHelper web = new WebHelper();

            string postData = "dGrdabh=" + pm.pid + "," + key;
            string returnString = web.PostHttp(baseUrl + "/lnr/toupdatetjglnrsf.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            StringBuilder sbPost = new StringBuilder();

            sbPost.Append("id=").Append(key);
            sbPost.Append("&dGrdabh=").Append(pm.pid);

            #region 不修改栏位
            var node = doc.DocumentNode.SelectSingleNode("//input[@id='sfcs']");

            string strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&sfcs=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='lnrsf.info.dXm']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&lnrsf.info.dXm=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='lnrsf.info.dXm']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&lnrsf.info.dXm=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXb']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dXb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dSfzh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dSfzh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dCsrq']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dCsrq=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dLxdh'][1]");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dLxdh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dSheng']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dSheng=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dShi']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dShi=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dQu']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dQu=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJd']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dJd=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJwh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dJwh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dLxdh'][2]");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dLxdh=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            node = doc.DocumentNode.SelectSingleNode("//input[@name='happentime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&happentime=").Append(strTmp);

            sbPost.Append("&gPgrq=").Append(padSFDate);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&createtime=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='updatetime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&updatetime=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&createuser=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='updateuser']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&updateuser=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='creatregion']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&creatregion=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='pRgid']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&pRgid=").Append(strTmp);

            #region 新加
            node = doc.DocumentNode.SelectSingleNode("//input[@name='contains']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&contains=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dGrdabh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dGrdabh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dGrdabh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&info.dGrdabh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='lnrsfold.happentime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&lnrsfold.happentime=").Append(strTmp);

            #endregion
            int pNqdqxz = 0;

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXcsfmb']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&gXcsfmb=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            #endregion

            #region OLDER_SELFCAREABILITY

            DataTable dt = ds.Tables["OLDER_SELFCAREABILITY"];
            DataRow dr = dt.Rows[0];


            strTmp = dr["NextVisitAim"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gXcsfmb=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["Dine"].ToString();

            strTmp = strTmp == "2" ? "0" : strTmp;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&jincan=on");
            sbPost.Append("&jcpf=").Append(strTmp);

            strTmp = dr["Groming"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&shuxi=on");
            sbPost.Append("&sxpf=").Append(strTmp);

            strTmp = dr["Dressing"].ToString();

            strTmp = strTmp == "2" ? "0" : strTmp;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&chuanyi=on");
            sbPost.Append("&cypf=").Append(strTmp);

            strTmp = dr["Tolet"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&ruce=on");
            sbPost.Append("&rcpf=").Append(strTmp);

            strTmp = dr["Activity"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&huodong=on");
            sbPost.Append("&hdpf=").Append(strTmp);

            sbPost.Append("&zpf=").Append(dr["TotalScore"]);

            strTmp = dr["FollowUpDoctor"].ToString();

            if (string.IsNullOrEmpty(zlnlqm))
            {
                zlnlqm = strTmp;
            }
            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gSfys=").Append(CommonExtensions.GetUrlEncodeVal(zlnlqm));

            strTmp = dr["NextfollowUpDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextfollowUpDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfrq=").Append(strTmp);
            sbPost.Append("&info.lXcsfsj=").Append(strTmp);

            sbPost.Append("&qdqxz=").Append(pNqdqxz);
            sbPost.Append("&wzd=").Append(((9 - pNqdqxz) * 100.0 / 9).ToString("#"));

            #endregion

            //id=56603&dGrdabh=371481020010013201&info.dGrdabh=371481020010013201&qdqxz=1&sfcs=1&wzd=89&lnrsf.info.dXm=cfa4e87702f1e0f70b17855393c4d335&info.dXb=1&info.dSfzh=230101193809096018&info.dCsrq=1938-09-09&info.dLxdh=15053702035&info.dSheng=37&info.dShi=3714&info.dQu=371481&info.dJd=37148102&info.dJwh=37148102001&info.dLxdh=%E5%BE%B7%E5%B7%9E%E5%B8%82%E4%B9%90%E9%99%B5%E5%B8%82%E9%83%AD%E5%AE%B6%E8%A1%97%E9%81%93%E5%8A%9E%E4%BA%8B%E5%A4%84%E5%85%AB%E9%87%8C%E5%BA%84%E5%B1%B1%E4%B8%9C&jincan=on&jcpf=0&shuxi=on&sxpf=3&chuanyi=on&cypf=3&ruce=on&rcpf=5&huodong=on&hdpf=1&zpf=12&gXcsfmb=&happentime=2016-07-18&gSfys=%E5%8F%B2&gXcsfrq=2017-07-18&info.lXcsfsj=2017-07-18&createtime=2016-07-18+10%3A16%3A37&updatetime=2016-07-18+10%3A55%3A01&pRgid=371481B10001&creatregion=371481B10001&createuser=371481B100010015&updateuser=371481B100010015
            returnString = web.PostHttp(baseUrl + "/lnr/updatetjglnrsf.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return "更新失败！";
            }

            doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null || doc.DocumentNode.SelectSingleNode("//body/script[last()]") == null)
            {
                return "更新失败！";
            }
            else
            {
                var returnNode = doc.DocumentNode.SelectSingleNode("//body/script[last()]");

                if (returnNode.InnerText.IndexOf("操作成功") == -1)
                {
                    return "更新失败！";
                }
            }

            return "";
        }

        /// <summary>
        /// 修改中医辨识
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pm"></param>
        /// <param name="sf"></param>
        /// <returns></returns>
        public string Updatezlbs(DataSet ds, PersonModel pm, SFClass sf) 
        {

            WebHelper web = new WebHelper();
            string postData = "stu=0&id=" + sf.key;
            //http://20.1.1.124:9000/sdcsm//lnr/zyytzgl/toUpdate.action?id=B60430931C50CCB1E0530100007FE18E&stu=0
            string returnString = web.PostHttp(baseUrl + "/lnr/zyytzgl/toUpdate.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return "";
            }

            DataTable dtzlnl = ds.Tables["OLDER_SELFCAREABILITY"];
           
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            DataTable dt = ds.Tables["OLD_MEDICINE_CN"];
            DataRow dr = dt.Rows[0];
            StringBuilder str = new StringBuilder();
            #region 页面
            str.Append("id="+sf.key);
            str.Append("&dGrdabh=").Append(pm.pid);
            //个人主键？          
            var node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXm']");
            string sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dXm=").Append(sr);
            
            //性别
             node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXb']");
             sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dXb=").Append(sr);

            //身份证 
            str.Append("&info.dSfzh=").Append(pm.idNumber);

            //出生日期 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dCsrq']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dCsrq=").Append(sr);

            //联系电话
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dLxdh']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dLxdh=").Append(sr);


            //本地户籍常住 info.dJzzk 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJzzk']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dJzzk=").Append(sr);

            //居住地址代码 省
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dSheng']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dSheng=").Append(sr);

            //市 info.dShi 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dShi']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dShi=").Append(sr);

            //县 info.dShi 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dQu']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dQu=").Append(sr);

            //镇 info.dShi 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJd']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dJd=").Append(sr);

            //村
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJwh']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dJwh=").Append(sr);

            //居住地址info.dXxdz
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXxdz']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dXxdz=").Append(CommonExtensions.GetUrlEncodeVal(sr));
            #endregion
            #region  问卷OLD_MEDICINE_CN
            //您精力充沛吗？（指精神头足，乐于做事）  
            str.Append("&lJl=").Append(dr["Energy"].ToString());

            //您容易疲乏吗？
            str.Append("&lTl=").Append(dr["Tired"].ToString());

            //您容易气短，呼吸短促，接不上气吗？
            str.Append("&lHx=").Append(dr["Breath"].ToString());

            //您说话声音低弱无力吗?
            str.Append("&lSh=").Append(dr["Voice"].ToString());

            //您感到闷闷不乐、情绪低沉吗?
            str.Append("&lXq=").Append(dr["Emotion"].ToString());

            //您容易精神紧张、焦虑不安吗?
            str.Append("&lJzjl=").Append(dr["Spirit"].ToString());

            //您因为生活状态改变而感到孤独、失落吗？
            str.Append("&lShztgb=").Append(dr["Alone"].ToString());

            //您容易感到害怕或受到惊吓吗?
            str.Append("&lHpjx=").Append(dr["Fear"].ToString());

            //您感到身体超重不轻松吗?
            str.Append("&lStcz=").Append(dr["Weight"].ToString());

            //您眼睛干涩吗?
            str.Append("&lYjgs=").Append(dr["Eye"].ToString());

            //您手脚发凉吗?
            str.Append("&lSjfl=").Append(dr["FootHand"].ToString());

            //您胃脘部、背部或腰膝部怕冷吗？
            str.Append("&lWwbyx=").Append(dr["Stomach"].ToString());

            //您比一般人耐受不了寒冷吗？
            str.Append("&lSblhl=").Append(dr["Cold"].ToString());

            //您容易患感冒吗?
            str.Append("&lRygm=").Append(dr["Influenza"].ToString());

            //您没有感冒时也会鼻塞、流鼻涕吗?
            str.Append("&lBslbt=").Append(dr["Nasal"].ToString());

            //您有口粘口腻，或睡眠打鼾吗？
            str.Append("&lKnndh=").Append(dr["Snore"].ToString());


            //您容易过敏(对药物、食物、气味、花粉或在季节交替、气候变化时)吗?
            str.Append("&lGm=").Append(dr["Allergy"].ToString());

            //您的皮肤容易起荨麻疹吗?
            str.Append("&lXmz=").Append(dr["Urticaria"].ToString());

            //您的皮肤容易起荨麻疹吗?
            str.Append("&lPfqzcx=").Append(dr["Skin"].ToString());

            //您的皮肤一抓就红，并出现抓痕吗?
            str.Append("&lPfhhfy=").Append(dr["Scratch"].ToString());

            //您皮肤或口唇干吗?
            str.Append("&lPfkcg=").Append(dr["Mouth"].ToString());

            //您有肢体麻木或固定部位疼痛的感觉吗？
            str.Append("&lZtmmtt=").Append(dr["Arms"].ToString());
            //您面部或鼻部有油腻感或者油亮发光吗?
            str.Append("&lYnyl=").Append(dr["Greasy"].ToString());

            //您面色或目眶晦黯，或出现褐色斑块/斑点吗?
            str.Append("&lMsmk=").Append(dr["Spot"].ToString());

            //您有皮肤湿疹、疮疖吗？
            str.Append("&lPfszcj=").Append(dr["Eczema"].ToString());

            //您感到口干咽燥、总想喝水吗？
            str.Append("&lKgyzhs=").Append(dr["Thirsty"].ToString());

            //您感到口苦或嘴里有异味吗?
            str.Append("&lKkkc=").Append(dr["Smell"].ToString());

            //您腹部肥大吗?
            str.Append("&lFbfd=").Append(dr["Abdomen"].ToString());

            //您吃(喝)凉的东西会感到不舒服或者怕吃(喝)凉的东西吗？
            str.Append("&lBxhls=").Append(dr["Coolfood"].ToString());

            //您有大便黏滞不爽、解不尽的感觉吗?
            str.Append("&lDbnz=").Append(dr["Defecate"].ToString());

            //您容易大便干燥吗?
            str.Append("&lDbgz=").Append(dr["Defecatedry"].ToString());

            //您舌苔厚腻或有舌苔厚厚的感觉吗?
            str.Append("&lSthn=").Append(dr["Tongue"].ToString());

            //您舌下静脉瘀紫或增粗吗？
            str.Append("&lSxjmyz=").Append(dr["Vein"].ToString());
            #endregion
            #region 评分
            DataTable dt2 = ds.Tables["OLD_MEDICINE_RESULT"];
            DataRow drLnrResult = dt2.Rows[0];
            //气虚得分 lQxDf   
            str.Append("&lQxDf=").Append(drLnrResult["FaintScore"].ToString());
            //气虚辨识 lQxBs= 
            str.Append("&lQxBs=").Append(drLnrResult["Faint"].ToString()); ;
            
            //阳虚得分 
            str.Append("&lYangDf=").Append(drLnrResult["YangsCore"].ToString());
            //阳虚辨识 lQxBs= 
            str.Append("&lYangBs=").Append(drLnrResult["Yang"].ToString());

            //阴虚得分 lYinDf 
            str.Append("&lYinDf=").Append(drLnrResult["YinScore"].ToString());
            str.Append("&lYinBs=").Append(drLnrResult["Yin"].ToString());

            //痰湿得分 lTsDf 
            str.Append("&lTsDf=").Append(drLnrResult["PhlegmdampScore"].ToString());
            str.Append("&lTsBs=").Append(drLnrResult["PhlegmDamp"].ToString());


            //湿热得分  
            str.Append("&lSrDf=").Append(drLnrResult["MuggyScore"].ToString());
            str.Append("&lSrBs=").Append(drLnrResult["Muggy"].ToString());


            //血淤得分  
            str.Append("&lXyDf=").Append(drLnrResult["BloodStasisScore"].ToString());
            str.Append("&lXyBs=").Append(drLnrResult["BloodStasis"].ToString());

            //气郁得分  
            str.Append("&lQyDf=").Append(drLnrResult["QiConstraintScore"].ToString());
            str.Append("&lQyBs=").Append(drLnrResult["QIconStraint"].ToString());

            //特兼质得分  
            str.Append("&lTbDf=").Append(drLnrResult["CharacteristicScore"].ToString());
            str.Append("&lTbBs=").Append(drLnrResult["Characteristic"].ToString());


            //平和得分  
            str.Append("&lPhDf=").Append(drLnrResult["MildScore"].ToString());
            str.Append("&lPhBs=").Append(drLnrResult["Mild"].ToString());

            //气虚指导及其他
            string qx = drLnrResult["FaintAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lQxZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lQxZd=");
            }
            string sr3 = string.IsNullOrWhiteSpace(drLnrResult["FaintAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["FaintAdvisingEx"].ToString());
            str.Append("&lQxQt=").Append(sr3);


            //阳虚指导及其他
            qx = drLnrResult["YangAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lYangZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lYangZd=");
            }
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["YangadvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["YangadvisingEx"].ToString());
             str.Append("&lYangQt=").Append(sr3);

            //阴虚指导及其他
            qx = drLnrResult["YinAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lYinZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lYinZd=");
            }
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["YinAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["YinAdvisingEx"].ToString());
            str.Append("&lYinQt=").Append(sr3);

            //痰湿
            qx = drLnrResult["PhlegmdampAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lTsZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lTsZd=");
            }
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["PhlegmdampAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["PhlegmdampAdvisingEx"].ToString());
            str.Append("&lTsQt=").Append(sr3);


            //湿热
            qx = drLnrResult["MuggyAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lSrZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lSrZd=");
            }
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["MuggyAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["MuggyAdvisingEx"].ToString());
            str.Append("&lSrQt=").Append(sr3);


            //血淤
            qx = drLnrResult["BloodStasisAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lXyZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lXyZd=");
            }
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["BloodStasisAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["BloodStasisAdvisingEx"].ToString());
            str.Append("&lXyQt=").Append(sr3);


            //气郁
            qx = drLnrResult["QiconstraintAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lQyZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lQyZd=");
            }
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["QiconstraintAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["QiconstraintAdvisingEx"].ToString());
            str.Append("&lQyQt=").Append(sr3);


            //特秉
            qx = drLnrResult["CharacteristicAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lTbZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lTbZd=");
            }
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["CharacteristicAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["CharacteristicAdvisingEx"].ToString());
            str.Append("&lTbQt=").Append(sr3);


            //平和
            qx = drLnrResult["MildAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lPhZd=").Append(item);
                }
            }
            else
            {
                str.Append("&lPhZd=");
            }

            sr3 = string.IsNullOrWhiteSpace(drLnrResult["MildAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["MildAdvisingEx"].ToString());
            str.Append("&lPhQt=").Append(sr3);

            //随访日期 RecordDate
            string sw2 = string.IsNullOrWhiteSpace(dr["RecordDate"].ToString()) ? "" : Convert.ToDateTime(dr["RecordDate"].ToString()).ToString("yyyy-MM-dd");
            str.Append("&happentime=").Append(sw2);

            //医生签名  ysqm 
            sr3 = string.IsNullOrWhiteSpace(dr["FollowupDoctor"].ToString()) ? "" : dr["FollowupDoctor"].ToString();

            if (string.IsNullOrEmpty(zybsqm))
            {
                zybsqm = sr3;
            }
            if (string.IsNullOrEmpty(zybsqm))
            {
                zybsqm = zlnlqm;
            }
            str.Append("&ysqm=").Append(CommonExtensions.GetUrlEncodeVal(zybsqm));

            //创建时间 
            //str.Append("&createtime=&updatetime=");
            node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&createtime=").Append(sr);

            //修改时间
            node = doc.DocumentNode.SelectSingleNode("//input[@name='updatetime']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&updatetime=").Append(sr);
            //创建单位 pRgid 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='pRgid']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&pRgid=").Append(sr);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createregion']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&createregion=").Append(sr);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&createuser=").Append(sr);


            node = doc.DocumentNode.SelectSingleNode("//input[@name='updateuser']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&updateuser=").Append(sr);         
            #endregion 

            //http://20.1.1.124:9000/sdcsm//lnr/zyytzgl/update.action
            string ret3 = web.PostHttp(baseUrl + "/lnr/zyytzgl/update.action", str.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(ret3))
            {
                return "更新失败！";
            }

            doc = HtmlHelper.GetHtmlDocument(ret3);

            if (doc == null || doc.DocumentNode.SelectSingleNode("//body/script[last()]") == null)
            {
                return "更新失败！";
            }
            else
            {
                var returnNode = doc.DocumentNode.SelectSingleNode("//body/script[last()]");

                if (returnNode.InnerText.IndexOf("操作成功") == -1)
                {
                    return "更新失败！";
                }
            }

            return "";
        }

        /// <summary>
        /// 新增中医辨识
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pm"></param>
        public string ADDzybs(DataSet ds, PersonModel pm) 
        {
            //http://124.133.2.189:9080/sdcsm//lnr/zyytzgl/toAdd.action?dah=370112140040001003 
            //http://20.1.1.124:9000/sdcsm//lnr/zyytzgl/toAdd.action?dah=371482110010115101
            //id=99533&lQxDf=13&lYangDf=10&lYinDf=12&lTsDf=10&lSrDf=15&lXyDf=13&lQyDf=11&lTbDf=15&lPhDf=11
            WebHelper web = new WebHelper();
            string pada = "dah="+pm.pid;
            string returnString = web.PostHttp(baseUrl + "/lnr/zyytzgl/toAdd.action", pada, "application/x-www-form-urlencoded", SysCookieContainer);
            
            if (string.IsNullOrEmpty(returnString))
            {
                return "";
            }

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            DataTable dt = ds.Tables["OLD_MEDICINE_CN"];
            DataRow dr = dt.Rows[0];
            StringBuilder str = new StringBuilder();

            #region 页面

            str.Append("dGrdabh=").Append(pm.pid);
            //str.Append("&sfzzztz=1");
            //个人主键？          
            var node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXm']");
            string sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dXm=").Append(sr);

            //是否同步到最近一次体检中
            node = doc.DocumentNode.SelectSingleNode("//input[@name='sfzzztz']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&sfzzztz=").Append(sr);

            //性别
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXb']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dXb=").Append(sr);

            //身份证 
            str.Append("&info.dSfzh=").Append(pm.idNumber);

            //出生日期 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dCsrq']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dCsrq=").Append(sr);

            //联系电话
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dLxdh']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dLxdh=").Append(sr);


            //本地户籍常住 info.dJzzk 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJzzk']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dJzzk=").Append(sr);

            //居住地址代码 省
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dSheng']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dSheng=").Append(sr);

            //市 info.dShi 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dShi']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dShi=").Append(sr);

            //县 info.dShi 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dQu']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dQu=").Append(sr);

            //镇 info.dShi 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJd']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dJd=").Append(sr);

            //村
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dJwh']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dJwh=").Append(sr);

            //居住地址info.dXxdz
            node = doc.DocumentNode.SelectSingleNode("//input[@name='info.dXxdz']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&info.dXxdz=").Append(CommonExtensions.GetUrlEncodeVal(sr));
            #endregion
            #region  问卷OLD_MEDICINE_CN
            //您精力充沛吗？（指精神头足，乐于做事）  
            str.Append("&lJl=").Append(dr["Energy"].ToString());

            //您容易疲乏吗？
            str.Append("&lTl=").Append(dr["Tired"].ToString());


            //您容易气短，呼吸短促，接不上气吗？
            str.Append("&lHx=").Append(dr["Breath"].ToString());

            //您说话声音低弱无力吗?
            str.Append("&lSh=").Append(dr["Voice"].ToString());

            //您感到闷闷不乐、情绪低沉吗?
            str.Append("&lXq=").Append(dr["Emotion"].ToString());

            //您容易精神紧张、焦虑不安吗?
            str.Append("&lJzjl=").Append(dr["Spirit"].ToString());

            //您因为生活状态改变而感到孤独、失落吗？
            str.Append("&lShztgb=").Append(dr["Alone"].ToString());

            //您容易感到害怕或受到惊吓吗?
            str.Append("&lHpjx=").Append(dr["Fear"].ToString());

            //您感到身体超重不轻松吗?
            str.Append("&lStcz=").Append(dr["Weight"].ToString());

            //您眼睛干涩吗?
            str.Append("&lYjgs=").Append(dr["Eye"].ToString());

            //您手脚发凉吗?
            str.Append("&lSjfl=").Append(dr["FootHand"].ToString());

            //您胃脘部、背部或腰膝部怕冷吗？
            str.Append("&lWwbyx=").Append(dr["Stomach"].ToString());

            //您比一般人耐受不了寒冷吗？
            str.Append("&lSblhl=").Append(dr["Cold"].ToString());


            //您容易患感冒吗?
            str.Append("&lRygm=").Append(dr["Influenza"].ToString());

            //您没有感冒时也会鼻塞、流鼻涕吗?
            str.Append("&lBslbt=").Append(dr["Nasal"].ToString());

            //您有口粘口腻，或睡眠打鼾吗？
            str.Append("&lKnndh=").Append(dr["Snore"].ToString());


            //您容易过敏(对药物、食物、气味、花粉或在季节交替、气候变化时)吗?
            str.Append("&lGm=").Append(dr["Allergy"].ToString());

            //您的皮肤容易起荨麻疹吗?
            str.Append("&lXmz=").Append(dr["Urticaria"].ToString());

            //您的皮肤容易起荨麻疹吗?
            str.Append("&lPfqzcx=").Append(dr["Skin"].ToString());

            //您的皮肤一抓就红，并出现抓痕吗?
            str.Append("&lPfhhfy=").Append(dr["Scratch"].ToString());


            //您皮肤或口唇干吗?
            str.Append("&lPfkcg=").Append(dr["Mouth"].ToString());

            //您有肢体麻木或固定部位疼痛的感觉吗？
            str.Append("&lZtmmtt=").Append(dr["Arms"].ToString());
            //您面部或鼻部有油腻感或者油亮发光吗?
            str.Append("&lYnyl=").Append(dr["Greasy"].ToString());

            //您面色或目眶晦黯，或出现褐色斑块/斑点吗?
            str.Append("&lMsmk=").Append(dr["Spot"].ToString());

            //您有皮肤湿疹、疮疖吗？
            str.Append("&lPfszcj=").Append(dr["Eczema"].ToString());

            //您感到口干咽燥、总想喝水吗？
            str.Append("&lKgyzhs=").Append(dr["Thirsty"].ToString());

            //您感到口苦或嘴里有异味吗?
            str.Append("&lKkkc=").Append(dr["Smell"].ToString());


            //您腹部肥大吗?
            str.Append("&lFbfd=").Append(dr["Abdomen"].ToString());

            //您吃(喝)凉的东西会感到不舒服或者怕吃(喝)凉的东西吗？
            str.Append("&lBxhls=").Append(dr["Coolfood"].ToString());


            //您有大便黏滞不爽、解不尽的感觉吗?
            str.Append("&lDbnz=").Append(dr["Defecate"].ToString());

            //您容易大便干燥吗?
            str.Append("&lDbgz=").Append(dr["Defecatedry"].ToString());

            //您舌苔厚腻或有舌苔厚厚的感觉吗?
            str.Append("&lSthn=").Append(dr["Tongue"].ToString());

            //您舌下静脉瘀紫或增粗吗？
            str.Append("&lSxjmyz=").Append(dr["Vein"].ToString());
            #endregion
            #region 评分
            DataTable dt2 = ds.Tables["OLD_MEDICINE_RESULT"];
            DataRow drLnrResult = dt2.Rows[0];
            //气虚得分 lQxDf   
            str.Append("&lQxDf=").Append(drLnrResult["FaintScore"].ToString());
            //气虚辨识 lQxBs= 
            str.Append("&lQxBs=").Append(drLnrResult["Faint"].ToString()); ;
            
            //阳虚得分 
            str.Append("&lYangDf=").Append(drLnrResult["YangsCore"].ToString());
            //阳虚辨识 lQxBs= 
            str.Append("&lYangBs=").Append(drLnrResult["Yang"].ToString());

            //阴虚得分 lYinDf 
            str.Append("&lYinDf=").Append(drLnrResult["YinScore"].ToString());
            str.Append("&lYinBs=").Append(drLnrResult["Yin"].ToString());

            //痰湿得分 lTsDf 
            str.Append("&lTsDf=").Append(drLnrResult["PhlegmdampScore"].ToString());
            str.Append("&lTsBs=").Append(drLnrResult["PhlegmDamp"].ToString());


            //湿热得分  
            str.Append("&lSrDf=").Append(drLnrResult["MuggyScore"].ToString());
            str.Append("&lSrBs=").Append(drLnrResult["Muggy"].ToString());


            //血淤得分  
            str.Append("&lXyDf=").Append(drLnrResult["BloodStasisScore"].ToString());
            str.Append("&lXyBs=").Append(drLnrResult["BloodStasis"].ToString());

            //气郁得分  
            str.Append("&lQyDf=").Append(drLnrResult["QiConstraintScore"].ToString());
            str.Append("&lQyBs=").Append(drLnrResult["QIconStraint"].ToString());

            //特兼质得分  
            str.Append("&lTbDf=").Append(drLnrResult["CharacteristicScore"].ToString());
            str.Append("&lTbBs=").Append(drLnrResult["Characteristic"].ToString());

            //平和得分  
            str.Append("&lPhDf=").Append(drLnrResult["MildScore"].ToString());
            str.Append("&lPhBs=").Append(drLnrResult["Mild"].ToString());

            //气虚指导及其他
            string qx = drLnrResult["FaintAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lQxZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lQxZd=");
            //}
            string sr3 = string.IsNullOrWhiteSpace(drLnrResult["FaintAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["FaintAdvisingEx"].ToString());
            str.Append("&lQxQt=").Append(sr3);


            //阳虚指导及其他
            qx = drLnrResult["YangAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lYangZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lYangZd=");
            //}
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["YangadvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["YangadvisingEx"].ToString());
            str.Append("&lYangQt=").Append(sr3);

            //阴虚指导及其他
            qx = drLnrResult["YinAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lYinZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lYinZd=");
            //}
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["YinAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["YinAdvisingEx"].ToString());
            str.Append("&lYinQt=").Append(sr3);

            //痰湿
            qx = drLnrResult["PhlegmdampAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lTsZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lTsZd=");
            //}
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["PhlegmdampAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["PhlegmdampAdvisingEx"].ToString());
            str.Append("&lTsQt=").Append(sr3);


            //湿热
            qx = drLnrResult["MuggyAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lSrZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lSrZd=");
            //}
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["MuggyAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["MuggyAdvisingEx"].ToString());
            str.Append("&lSrQt=").Append(sr3);


            //血淤
            qx = drLnrResult["BloodStasisAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lXyZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lXyZd=");
            //}
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["BloodStasisAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["BloodStasisAdvisingEx"].ToString());
            str.Append("&lXyQt=").Append(sr3);


            //气郁
            qx = drLnrResult["QiconstraintAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lQyZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lQyZd=");
            //}
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["QiconstraintAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["QiconstraintAdvisingEx"].ToString());
            str.Append("&lQyQt=").Append(sr3);


            //特秉
            qx = drLnrResult["CharacteristicAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lTbZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lTbZd=");
            //}
            sr3 = string.IsNullOrWhiteSpace(drLnrResult["CharacteristicAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["CharacteristicAdvisingEx"].ToString());
            str.Append("&lTbQt=").Append(sr3);


            //平和
            qx = drLnrResult["MildAdvising"].ToString();
            if (!string.IsNullOrWhiteSpace(qx))
            {
                var qxx = qx.Split(',');
                foreach (var item in qxx)
                {
                    str.Append("&lPhZd=").Append(item);
                }
            }
            //else
            //{
            //    str.Append("&lPhZd=");
            //}

            sr3 = string.IsNullOrWhiteSpace(drLnrResult["MildAdvisingEx"].ToString()) ? "" : CommonExtensions.GetUrlEncodeVal(drLnrResult["MildAdvisingEx"].ToString());
            str.Append("&lPhQt=").Append(sr3);

            //随访日期 RecordDate
            string sw2 = string.IsNullOrWhiteSpace(dr["RecordDate"].ToString()) ? "" : Convert.ToDateTime(dr["RecordDate"].ToString()).ToString("yyyy-MM-dd");
            str.Append("&happentime=").Append(sw2);

            //医生签名  ysqm 
            sr3 = string.IsNullOrWhiteSpace(dr["FollowupDoctor"].ToString()) ? "" : dr["FollowupDoctor"].ToString();

            if (string.IsNullOrEmpty(zybsqm))
            {
                zybsqm = sr3;
            }
            if (string.IsNullOrEmpty(zybsqm))
            {
                zybsqm = zlnlqm;
            }
            str.Append("&ysqm=").Append(CommonExtensions.GetUrlEncodeVal(zybsqm));

            //创建时间 
            //    str.Append("&createtime=&updatetime=");
            node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
            string src = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&createtime=").Append(src);

            //修改时间
            node = doc.DocumentNode.SelectSingleNode("//input[@name='updatetime']");
            src = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&updatetime=").Append(src);
            //创建单位 pRgid 
            node = doc.DocumentNode.SelectSingleNode("//input[@name='pRgid']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&pRgid=").Append(sr);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createregion']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&createregion=").Append(sr);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&createuser=").Append(sr);


            node = doc.DocumentNode.SelectSingleNode("//input[@name='updateuser']");
            sr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            str.Append("&updateuser=").Append(sr);
            #endregion

             //http://124.133.2.189:9080/sdcsm//lnr/zyytzgl/add.action
            //http://20.1.1.124:9000/sdcsm//lnr/zyytzgl/add.action
             string ret3 = web.PostHttp(baseUrl + "/lnr/zyytzgl/add.action", str.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

             if (string.IsNullOrEmpty(ret3))
             {
                 return "更新失败！";
             }

             doc = HtmlHelper.GetHtmlDocument(ret3);

             if (doc == null || doc.DocumentNode.SelectSingleNode("//body/script[last()]") == null)
             {
                 return "更新失败！";
             }
             else
             {
                 var returnNode = doc.DocumentNode.SelectSingleNode("//body/script[last()]");

                 if (returnNode.InnerText.IndexOf("操作成功") == -1)
                 {
                     return "更新失败！";
                 }
             }
             return "";
        }

        #endregion

        //下载全部  入口
        public void Download(Action<string> callback)
        {
            TryDownload(1, callback);
            GC.Collect();
        }

        /// <summary>
        /// 通过身份证下载todu
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="callback"></param>
        public void DownLnrByIDs(string ids, Action<string> callback)
        {
            try
            {
                var idsa = ids.Split(',');
                int cIndex = 1;

                foreach (string id in idsa)
                {
                    if (id == "")
                    {
                        callback("下载-老年人信息..." + cIndex + "/" + idsa.Length);
                        cIndex++;
                        continue;
                    }

                    CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();

                    PersonModel pm = cb.GetGrdaByIDCardNo(id, loginkey, SysCookieContainer);

                    // 判断是否已经存在;存在则修改，否则新增
                    if (pm != null && !string.IsNullOrEmpty(pm.pid))
                    {
                        GetInfoByPersonModel(pm);

                        // 体质辨识
                        DownOldHerb(pm);
                    }

                    callback("下载-老年人信息..." + cIndex + "/" + idsa.Length);
                    cIndex++;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("登录超时") > -1)
                {
                    callback("EX-“登录超时”、“该用户在别处登录”或者“当前用户信息被上级用户修改”导致用户无法操作,请您重新登录!");

                    throw;
                }

                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);
            }
        }

        /// <summary>
        /// 根据个人档案下载
        /// </summary>
        /// <param name="info"></param>
        /// <param name="pm"></param>
        /// <param name="callback"></param>
        public void DownInfoByPerson(PersonModel pm, Action<string> callback)
        {
            callback("下载-老年人信息..." + currentIndex + "/" + totalRows);
            if (pm != null && !string.IsNullOrEmpty(pm.pid))
            {
                //自理能力
                GetInfoByPersonModel(pm);

                // 体质辨识
                DownOldHerb(pm);
            }
            currentIndex++;
        }

        /// <summary>
        /// 尝试3次下载
        /// </summary>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        private void TryDownload(int tryCount, Action<string> callback)
        {
            try
            {
                GetPageKeyAndInfo(callback);
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("登录超时") > -1)
                {
                    callback("EX-“登录超时”、“该用户在别处登录”或者“当前用户信息被上级用户修改”导致用户无法操作,请您重新登录!");

                    throw;
                }

                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);

                if (tryCount < MaxtryCount)
                {
                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    TryDownload(tryCount, callback);
                }
                else
                {
                    callback("EX-下载老年人信息失败，请确保网路畅通");
                }
            }
        }

        /// <summary>
        /// 从第二页开始
        /// </summary>
        /// <param name="callback"></param>
        private void GetPageKeyAndInfo(Action<string> callback)
        {
            int PageSum = 0;
            List<PersonModel> personList = GetLnrKeyAndInfo(callback, out PageSum);

            //调方法，便利当前页的表示，获取信息
            this.GetInfoByPersonList(personList, callback);

            for (int i = 2; i <= PageSum; i++)
            {
                personList.Clear();
                //调方法，遍历当前页标示，获取信息

                personList = this.GetPageNumKeyInfo(i, callback);
                //
                this.GetInfoByPersonList(personList, callback);
            }
        }

        /// <summary>
        /// 获取First页key
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="pageSum"></param>
        /// <returns></returns>
        private List<PersonModel> GetLnrKeyAndInfo(Action<string> callback, out int pageSum)
        {
            pageSum = 1;
            WebHelper web = new WebHelper();
            List<PersonModel> personList = new List<PersonModel>();

            string key = "";
            if (loginkey.Length == 16)
            {
                key = loginkey.Substring(0, 12);
            }
            else
            {
                key = loginkey.Substring(0, 15);
            }

            string postData = "null=%E8%87%A7%E5%AE%B6%E7%A4%BE%E5%8C%BA%E5%8D%AB%E7%94%9F%E6%9C%8D%E5%8A%A1%E7%AB%99&dqjg=" + key + "&beginCreatetime=&endCreatetime=&standard=&info.dXm=&beginCsrq=&endCsrq=&info.dDazt=1&dGrdabh=&age1=&age2=&createuser=&info.dXb=&info.dSfzh=&selectChange=1&info.dJd=&info.dJwh=&info.dXxdz=&contains=on";
            string returnString = web.PostHttp(baseUrl + "/lnr/listtjglnrsf.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null)
            {
                return personList;
            }

            var node = doc.DocumentNode.SelectSingleNode("//table[@class='QueryTable']");    //  获取信息所在table
            var nodes = node.SelectNodes("tr");

            if (nodes.Count > 1)
            {
                nodes.Remove(0);
                foreach (var n in nodes)
                {
                    var tds = n.SelectNodes("td");
                    string idNumber = tds[5].InnerText.Replace("\r", "").Replace("\n", "");
                    if (idNumber.Trim() == "")
                    {
                        currentIndex++;

                        continue;
                    }
                    PersonModel person = new PersonModel();
                    person.pid = tds[1].InnerText.Replace("\r", "").Replace("\n", "");
                    person.memberName = tds[2].InnerText.Replace("\r", "").Replace("\n", "");
                    person.idNumber = idNumber.Trim();
                    personList.Add(person);
                }
            }

            var ulNodes = doc.DocumentNode.SelectSingleNode("//body[1]/div[1]/div[1]/div[1]").SelectSingleNode("ul");
            var divNodes = ulNodes.SelectNodes("li");
            var nodePage = ulNodes.SelectSingleNode("input[@id='all']");

            string pages = nodePage == null || !nodePage.Attributes.Contains("value") ? "0" : nodePage.Attributes["value"].Value;

            int.TryParse(pages, out pageSum);

            string rowSum = divNodes[0].InnerText;

            rowSum = rowSum.Substring(rowSum.IndexOf('：') + 1);
            int.TryParse(rowSum, out totalRows);

            return personList;
        }

        /// <summary>
        /// 获取指定页信息
        /// </summary>
        /// <param name="pageNum"></param>
        /// <param name="callbac"></param>
        /// <returns></returns>
        private List<PersonModel> GetPageNumKeyInfo(int pageNum, Action<string> callbac)
        {
            WebHelper web = new WebHelper();
            List<PersonModel> personList = new List<PersonModel>();

            string key = "";
            if (loginkey.Length == 16)
            {
                key = loginkey.Substring(0, 12);
            }
            else
            {
                key = loginkey.Substring(0, 15);
            }

            string postData = "page.currentPage=" + pageNum + "&status=ajax&null=&dqjg=" + key + "&beginCreatetime=&endCreatetime=&standard=&info.dXm=&beginCsrq=&endCsrq=&info.dDazt=1&dGrdabh=&age1=&age2=&createuser=&info.dXb=&info.dSfzh=&selectChange=1&info.dJd=&info.dJwh=&info.dXxdz=&contains=on";
            string returnString = web.PostHttp(baseUrl + "/lnr/listtjglnrsf.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null)
            {
                return personList;
            }
            var node = doc.DocumentNode.SelectSingleNode("//table[@class='QueryTable']");
            var nodes = node.SelectNodes("tr");

            if (nodes.Count > 1)
            {
                nodes.Remove(0);
                foreach (var n in nodes)
                {
                    var tds = n.SelectNodes("td");
                    string idNumber = tds[5].InnerText.Replace("\r", "").Replace("\n", "");
                    if (idNumber.Trim() == "")
                    {
                        currentIndex++;

                        continue;
                    }
                    PersonModel person = new PersonModel();
                    person.pid = tds[1].InnerText.Replace("\r", "").Replace("\n", "");
                    person.memberName = tds[2].InnerText.Replace("\r", "").Replace("\n", "");
                    person.idNumber = idNumber.Trim();
                    personList.Add(person);
                }
            }

            return personList;
        }

        /// <summary>
        /// 根据PersonModel集合获取数据
        /// </summary>
        /// <param name="list"></param>
        /// <param name="callback"></param>
        private void GetInfoByPersonList(List<PersonModel> list, Action<string> callback)
        {
            foreach (var pm in list)
            {
                GetInfoByPersonModel(pm);
                callback("下载-老年人信息..." + currentIndex + "/" + totalRows);
                currentIndex++;
            }
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="person"></param> 
        private void GetInfoByPersonModel(PersonModel person)
        {
            //表结构
            DataSet saveDS = new DataSet();
            DataSet ds = DataSetTmp.LnrDataSet;

            //  历史列表   信息
            List<SFClass> lstSF = GetSFxxLst(person.pid);

            if (lstSF.Count > 0)
            {
                SFClass sf = lstSF[0];

                var id = sf.key;

                WebHelper web = new WebHelper();

                string postData = "dGrdabh=" + person.pid + "," + id + "&_=";
                //http://20.1.1.124:9000/sdcsm/lnr/selecttjglnrsfforId.action?dGrdabh=371482010620000103,371482503567&_=1607320769496
                string returnString = web.GetHttp(baseUrl + "lnr/selecttjglnrsfforId.action?"+ postData, "", SysCookieContainer);
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                //存储信息的表
                DataTable zldt = ds.Tables["OLDER_SELFCAREABILITY"].Clone();
                DataRow zlDR = zldt.NewRow();
                zlDR["IDCardNo"] = person.idNumber;
                var node = doc.DocumentNode.SelectSingleNode("//input[@id='jcpf']");
                zlDR["Dine"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='sxpf']");
                zlDR["Groming"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='cypf']");
                zlDR["Dressing"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='rcpf']");
                zlDR["Tolet"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='hdpf']");
                zlDR["Activity"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='zpf']");
                zlDR["TotalScore"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                zlDR["FollowUpDate"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSfys']");
                zlDR["FollowUpDoctor"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXcsfrq']");
                zlDR["NextfollowUpDate"] = node == null || !node.Attributes.Contains("value") ? "0" : node.Attributes["value"].Value;

                zldt.Rows.Add(zlDR);
                string sfrq = Convert.ToDateTime(zlDR["FollowUpDate"].ToString()).ToString("yyyy-MM-dd");
                saveDS.Tables.Add(zldt);
   
                CommonBusiness.CommonDAOBusiness dao = new CommonBusiness.CommonDAOBusiness();
                outkey = dao.SaveMainTable(zldt, person.idNumber,sfrq);
              //  dao.SaveDataSet(saveDS, person.idNumber);
                saveDS.Tables.Clear();
            }
        }

        public void DownOldHerb(PersonModel pm)
        {
            List<SFClass> lstXX = GetxxTZLst(pm.pid);
            if (lstXX.Count > 0)
            {
                SFClass info = lstXX.OrderBy(o => o.key).LastOrDefault();
                GetInfoTZxx(info, pm);
            }
        }
        /// <summary>
        /// 获取体质辨识列表
        /// </summary>
        /// <param name="strPcode"></param>
        /// <returns></returns>
        private List<SFClass> GetxxTZLst(string strPcode)
        {
            List<SFClass> lstXX = new List<SFClass>();

            WebHelper web = new WebHelper();

            string postData = "lnr/zyytzgl/viewAll.action?dah=" + strPcode;
            //http://20.1.1.124:9000/sdcsm/lnr/zyytzgl/viewAll.action?dah=371482030500007902
            string returnString = web.GetHttp(baseUrl + postData, "", SysCookieContainer);
            if (string.IsNullOrEmpty(returnString))
            {
                return lstXX;
            }

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            var nodes = doc.DocumentNode.SelectNodes("//table[@class='new_tb2']/tr[@id]");
            if (nodes != null)
            {
                List<DateTime> list = new List<DateTime>();

                foreach (var n in nodes)
                {
                    var tds = n.SelectNodes("td");
                    if (tds.Count < 1)
                    {
                        return null;
                    }
                    var spans = tds[0].SelectNodes("//span[@id]");
                    foreach (var i in spans)
                    {
                        SFClass sfxx = new SFClass();
                        if (i.Id != "" && i.Id.IndexOf('_') > -1)
                        {
                            sfxx.key = i.Id.Split('_')[1];
                            sfxx.sfDate = i.InnerText;
                            lstXX.Add(sfxx);
                        }
                    }
                    break;
                }
            }
            return lstXX;
        }
        /// <summary>
        /// 获取下载信息
        /// </summary>
        /// <param name="info"></param>
        /// <param name="pm"></param>
        private void GetInfoTZxx(SFClass info, PersonModel pm)
        {
            WebHelper web = new WebHelper();

            //http://20.1.1.124:9000/sdcsm//lnr/zyytzgl/toUpdate.action?id=371482321151&stu=0
            string returnString = web.GetHttp(baseUrl + "/lnr/zyytzgl/toUpdate.action?id=" + info.key + "&stu=0", "", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return;
            }

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            string strIDCardNo = pm.idNumber.ToString();

            DataSet ds = DataSetTmp.LnrDataSet;

            #region  OLD_MEDICINE_CN

            DataTable dtDataLnrTZ = ds.Tables["OLD_MEDICINE_CN"].Clone();
            DataRow dr = dtDataLnrTZ.NewRow();

            //您精力充沛吗？（指精神头足，乐于做事）
            var node = doc.DocumentNode.SelectSingleNode("//input[@name='lJl'][@checked]");
            string strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Energy"] = strtmp;
            dr["OutKey"] = outkey.ToString();
            //您容易疲乏吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lTl'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Tired"] = strtmp;
            //您容易气短，呼吸短促，接不上气吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lHx'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Breath"] = strtmp;
            //您说话声音低弱无力吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lSh'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Voice"] = strtmp;
            //您感到闷闷不乐、情绪低沉吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lXq'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Emotion"] = strtmp;
            //您容易精神紧张、焦虑不安吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lJzjl'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Spirit"] = strtmp;
            //您因为生活状态改变而感到孤独、失落吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lShztgb'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Alone"] = strtmp;
            //您容易感到害怕或受到惊吓吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lHpjx'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Fear"] = strtmp;
            //您感到身体超重不轻松吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lStcz'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Weight"] = strtmp;
            //您眼睛干涩吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lYjgs'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Eye"] = strtmp;
            //您手脚发凉吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lSjfl'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["FootHand"] = strtmp;

            //您胃脘部、背部或腰膝部怕冷吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lWwbyx'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Stomach"] = strtmp;
            //您比一般人耐受不了寒冷吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lSblhl'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Cold"] = strtmp;
            //您容易患感冒吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lRygm'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Influenza"] = strtmp;
            //您没有感冒时也会鼻塞、流鼻涕吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lBslbt'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Nasal"] = strtmp;

            //您有口粘口腻，或睡眠打鼾吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lKnndh'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Snore"] = strtmp;

            //您容易过敏(对药物、食物、气味、花粉或在季节交替、气候变化时)吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lGm'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Allergy"] = strtmp;
            //您的皮肤容易起荨麻疹吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lXmz'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Urticaria"] = strtmp;
            //您的皮肤在不知不觉中会出现青紫瘀斑、皮下出血吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lPfqzcx'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Skin"] = strtmp;
            //您的皮肤一抓就红，并出现抓痕吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lPfhhfy'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Scratch"] = strtmp;

            //您皮肤或口唇干吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lPfkcg'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Mouth"] = strtmp;

            //您有肢体麻木或固定部位疼痛的感觉吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lZtmmtt'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Arms"] = strtmp;
            //您面部或鼻部有油腻感或者油亮发光吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lYnyl'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Greasy"] = strtmp;
            //您面色或目眶晦黯，或出现褐色斑块/斑点吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lMsmk'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Spot"] = strtmp;
            //您有皮肤湿疹、疮疖吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lPfszcj'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Eczema"] = strtmp;
            //您感到口干咽燥、总想喝水吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lKgyzhs'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Thirsty"] = strtmp;
            //您感到口苦或嘴里有异味吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lKkkc'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Smell"] = strtmp;
            //您腹部肥大吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lFbfd'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Abdomen"] = strtmp;

            //您吃(喝)凉的东西会感到不舒服或者怕吃(喝)凉的东西吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lBxhls'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Coolfood"] = strtmp;
            //您有大便黏滞不爽、解不尽的感觉吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lDbnz'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Defecate"] = strtmp;
            //您容易大便干燥吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lDbgz'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Defecatedry"] = strtmp;
            //您舌苔厚腻或有舌苔厚厚的感觉吗?
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lSthn'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Tongue"] = strtmp;

            //您舌下静脉瘀紫或增粗吗？
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lSxjmyz'][@checked]");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dr["Vein"] = strtmp;

            dr["RecordID"] = pm.pid;
            dr["IDCardNo"] = strIDCardNo;
            //填表日期  
            dr["RecordDate"] = info.sfDate;
            //随访医生
            node = doc.DocumentNode.SelectSingleNode("//input[@name='ysqm']");
            dr["FollowupDoctor"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


            dtDataLnrTZ.Rows.Add(dr);

            #endregion

            #region OLD_MEDICINE_RESULT
            DataTable dtDataLnrResult = ds.Tables["OLD_MEDICINE_RESULT"].Clone();
            DataRow drLnrResult = dtDataLnrResult.NewRow();

            decimal MildScore = 0;
            decimal FaintScore = 0;
            decimal YangsCore = 0;
            decimal YinScore = 0;
            decimal PhlegmdampScore = 0;
            decimal MuggyScore = 0;
            decimal BloodStasisScore = 0;
            decimal QiConstraintScore = 0;
            decimal CharacteristicScore = 0;

            drLnrResult["IDCardNo"] = pm.idNumber;
            drLnrResult["OutKey"] = outkey.ToString();
            //平和质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lPhDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["MildScore"] = strtmp;
            MildScore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            //气虚质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lQxDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["FaintScore"] = strtmp;

            //体质勾选
            FaintScore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            if (FaintScore >= 11M)
            {
                drLnrResult["Faint"] = "1";
            }
            else if (FaintScore >= 9M && FaintScore <= 10M)
            {
                drLnrResult["Faint"] = "2";
            }

            //阳虚质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lYangDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["YangsCore"] = strtmp;

            YangsCore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            if (YangsCore >= 11M)
            {
                drLnrResult["Yang"] = "1";
            }
            else if (YangsCore >= 9M && YangsCore <= 10M)
            {
                drLnrResult["Yang"] = "2";
            }

            //阴虚质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lYinDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["YinScore"] = strtmp;

            YinScore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            if (YinScore >= 11M)
            {
                drLnrResult["Yang"] = "1";
            }
            else if (YinScore >= 9M && YinScore <= 10M)
            {
                drLnrResult["Yin"] = "2";
            }

            //痰湿质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lTsDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["PhlegmdampScore"] = strtmp;

            PhlegmdampScore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            if (PhlegmdampScore >= 11M)
            {
                drLnrResult["PhlegmDamp"] = "1";
            }
            else if (PhlegmdampScore >= 9M && PhlegmdampScore <= 10M)
            {
                drLnrResult["PhlegmDamp"] = "2";
            }

            //湿热质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lSrDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["MuggyScore"] = strtmp;

            MuggyScore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            if (MuggyScore >= 11M)
            {
                drLnrResult["Muggy"] = "1";
            }
            else if (MuggyScore >= 9M && MuggyScore <= 10M)
            {
                drLnrResult["Muggy"] = "2";
            }

            //血瘀质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lXyDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["BloodStasisScore"] = strtmp;

            BloodStasisScore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            if (BloodStasisScore >= 11M)
            {
                drLnrResult["BloodStasis"] = "1";
            }
            else if (BloodStasisScore >= 9M && BloodStasisScore <= 10M)
            {
                drLnrResult["BloodStasis"] = "2";
            }

            //气郁质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lQyDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["QiConstraintScore"] = strtmp;

            QiConstraintScore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            if (QiConstraintScore >= 11M)
            {
                drLnrResult["QIconStraint"] = "1";
            }
            else if (QiConstraintScore >= 9M && QiConstraintScore <= 10M)
            {
                drLnrResult["QIconStraint"] = "2";
            }

            //特兼质得分
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lTbDf']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            drLnrResult["CharacteristicScore"] = strtmp;

            CharacteristicScore = string.IsNullOrEmpty(strtmp) ? 0 : decimal.Parse(strtmp);
            if (CharacteristicScore >= 11M)
            {
                drLnrResult["Characteristic"] = "1";
            }
            else if (CharacteristicScore >= 9M && CharacteristicScore <= 10M)
            {
                drLnrResult["Characteristic"] = "2";
            }

            //平和质得分>17分
            if (MildScore >= 17M)
            {
                //其他体征分数都<8分则平和质为"是"，其他体质都< 10，基本是
                if (FaintScore < 8M && YangsCore < 8M && YinScore < 8M && PhlegmdampScore < 8M && MuggyScore < 8M && BloodStasisScore < 8M && QiConstraintScore < 8M && CharacteristicScore < 8M)
                {
                    drLnrResult["Mild"] = "1";
                }
                else if (FaintScore < 10M && YangsCore < 10M && YinScore < 10M && PhlegmdampScore < 10M && MuggyScore < 10M && BloodStasisScore < 10M && QiConstraintScore < 10M && CharacteristicScore < 10M)
                {
                    drLnrResult["Mild"] = "2";
                }
            }
            #region 平和质指导 平和质指导：1．情志调摄2．饮食调养3．起居调摄4．运动保健5．穴位保健6．其他
            var phzXx = "";
            var nodes = doc.DocumentNode.SelectNodes("//input[@name='lPhZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["MildAdvising"] = phzXx.TrimStart(',');
            }

            #endregion

            #region 气虚质指导

            phzXx = "";
            nodes = doc.DocumentNode.SelectNodes("//input[@name='lQxZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["FaintAdvising"] = phzXx.TrimStart(',');
            }

            #endregion

            #region 阳虚质指导
            phzXx = "";
            nodes = doc.DocumentNode.SelectNodes("//input[@name='lYangZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["YangAdvising"] = phzXx.TrimStart(',');
            }

            #endregion

            #region 阴虚质指导
            phzXx = "";
            nodes = doc.DocumentNode.SelectNodes("//input[@name='lYinZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["YinAdvising"] = phzXx.TrimStart(',');
            }

            #endregion

            #region 痰湿质指导
            phzXx = "";
            nodes = doc.DocumentNode.SelectNodes("//input[@name='lTsZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["PhlegmdampAdvising"] = phzXx.TrimStart(',');
            }

            #endregion

            #region 湿热质指导
            phzXx = "";
            nodes = doc.DocumentNode.SelectNodes("//input[@name='lSrZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["MuggyAdvising"] = phzXx.TrimStart(',');
            }
            #endregion

            #region 血淤质指导
            phzXx = "";
            nodes = doc.DocumentNode.SelectNodes("//input[@name='lXyZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["BloodStasisAdvising"] = phzXx.TrimStart(',');
            }
            #endregion

            #region 气郁质指导
            phzXx = "";
            nodes = doc.DocumentNode.SelectNodes("//input[@name='lQyZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["QiconstraintAdvising"] = phzXx.TrimStart(',');
            }
            #endregion

            #region 特秉质指导
            phzXx = "";
            nodes = doc.DocumentNode.SelectNodes("//input[@name='lTbZd'][@checked='checked']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    if (n.Attributes.Contains("value"))
                    {
                        phzXx += "," + n.Attributes["value"].Value;
                    }
                }
                drLnrResult["CharacteristicAdvising"] = phzXx.TrimStart(',');
            }
            #endregion



            //平和质指导其他
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lPhQt']");
            drLnrResult["MildAdvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //气虚质指导其他

            node = doc.DocumentNode.SelectSingleNode("//input[@name='lQxQt']");
            drLnrResult["FaintAdvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //阳虚质指导其他
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lYangQt']");
            drLnrResult["YangadvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //阴虚质指导其他

            node = doc.DocumentNode.SelectSingleNode("//input[@name='lYinQt']");
            drLnrResult["YinAdvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


            //痰湿质指导其他
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lTsQt']");
            drLnrResult["PhlegmdampAdvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //湿热质指导其他
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lSrQt']");
            drLnrResult["MuggyAdvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


            //血瘀质指导其他
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lXyQt']");
            drLnrResult["BloodStasisAdvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //气郁质指导其他

            node = doc.DocumentNode.SelectSingleNode("//input[@name='lQyQt']");
            drLnrResult["QiconstraintAdvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //特兼质指导其他
            node = doc.DocumentNode.SelectSingleNode("//input[@name='lTbQt']");
            drLnrResult["CharacteristicAdvisingEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            dtDataLnrResult.Rows.Add(drLnrResult);

            #endregion

            DataSet dsSave = new DataSet();

            dsSave.Tables.Add(dtDataLnrTZ);
            dsSave.Tables.Add(dtDataLnrResult);

            CommonBusiness.CommonDAOBusiness cDAO = new CommonBusiness.CommonDAOBusiness();

            cDAO.SaveDataSet(dsSave, strIDCardNo,"",outkey.ToString());

            dsSave.Tables.Clear();
        }
        /// <summary>
        /// 根据人员key获取随访列表
        /// </summary>
        /// <param name="strkey"></param>
        /// <returns></returns>
        public List<SFClass> GetSFxxLst(string key)
        {
            List<SFClass> lstSF = new List<SFClass>();

            //http://20.1.1.124:9000/sdcsm/lnr/toshowtjglnrsf.action?dGrdabh=371482030500007902
            WebHelper web = new WebHelper();

            string returnString = web.GetHttp(baseUrl + "lnr/toshowtjglnrsf.action?dGrdabh=" + key, "", SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            var nodes = doc.DocumentNode.SelectNodes("//div[@id='yearContainer']//a[@id]");

            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    SFClass sf = new SFClass();

                    sf.key = n.Attributes["id"].Value;

                    if (sf.key != "")
                    {
                        sf.key = sf.key.Remove(0, 1);
                        sf.sfDate = n.InnerText.Trim();

                        lstSF.Add(sf);
                    }

                }
            }

            return lstSF;
        }

        /// <summary>
        /// 中医药获取key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private List<SFClass> GetZylst(string key)
        {
            WebHelper web = new WebHelper();
            //http://124.133.2.189:9080/sdcsm/lnr/zyytzgl/listAll.action?contains=on&dqjg=&info.dSfzh=370112192203085619&info.dDazt=1&sfzzztz=1
            string pata = "contains=on&dqjg=&info.dSfzh=" + key + "&info.dDazt=1&sfzzztz=1";
            string retrunstring = web.PostHttp(baseUrl+"/lnr/zyytzgl/listAll.action",pata, "application/x-www-form-urlencoded", SysCookieContainer);
            if (string.IsNullOrWhiteSpace(retrunstring))
            {
                return null;
            }
       
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(retrunstring);
           List<SFClass> sf = new  List<SFClass>();
           SFClass sf2 = new SFClass();
            //获取key
            var node = doc.DocumentNode.SelectSingleNode("//table[@id='cxlist']/tr[2]/td[1]/input[1]");
            if (node == null) {
                return sf;
            }
            sf2.key = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value.ToString();
            
            //根据key获取随访日期 
            //http://124.133.2.189:9080/sdcsm/lnr/zyytzgl/viewById.action?id=99531
            if (string.IsNullOrWhiteSpace(sf2.key))
            {
                return sf;
            }
            string pada = "id=" + sf2.key;
            string ret2 = web.PostHttp(baseUrl + "/lnr/zyytzgl/viewById.action", pada,"application/x-www-form-urlencoded",SysCookieContainer);
            HtmlDocument doc2 = HtmlHelper.GetHtmlDocument(ret2);
            var sr = doc2.DocumentNode.SelectSingleNode("//table[@id='table3']/tr[7]/td[2]");
            sf2.sfDate = sr == null ? "" : sr.InnerText;
            sf.Add(sf2);
            return sf;
        }


        /// <summary>
        /// 中医药获取key 2 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<SFClass> GetZylst2(string key, string pid)
        {
            WebHelper web = new WebHelper();
            //http://20.1.1.124:9000/sdcsm/lnr/zyytzgl/viewAll.action?dah=371482030500007902
            string pata = "dah=" + pid;
            string retrunstring = web.GetHttp(baseUrl + "lnr/zyytzgl/viewAll.action"+ pata, "", SysCookieContainer);
            if (string.IsNullOrWhiteSpace(retrunstring))
            {
                return null;
            }

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(retrunstring);
            List<SFClass> sf = new List<SFClass>();

            //取得存放数据的table
            var node = doc.DocumentNode.SelectNodes("//table[@id='table2']/tr[position()>1]");

            if (node == null)
            {
                return sf;
            }
            foreach (var item in node)
            {
                var subnode = item.SelectNodes("td[2]/div[1]/table[1]/tr[position()>1]");
                if (subnode == null)
                {
                    return sf;
                }
                foreach (var subitem in subnode)
                {
                    var subnode2 = subitem.SelectSingleNode("td[1]/a/span");
                    if (subnode2 == null)
                    {
                        return sf;
                    }
                    string subnode3 = subnode2.InnerText;
                    var subnode4 = subitem.SelectSingleNode("td[1]/a");
                    if (subnode4 == null)
                    {
                        return null;
                    }
                    string subnode5 = subnode4.InnerHtml;
                    subnode5 = HtmlHelper.GetTagValue(subnode5, "time_", "\">");
                    SFClass sf2 = new SFClass();
                    sf2.key = subnode5;
                    sf2.sfDate = subnode3;
                    sf.Add(sf2);

                }

            }

            return sf;

        }
    }
}
