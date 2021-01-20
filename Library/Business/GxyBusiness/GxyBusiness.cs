using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Net;
using DAL;
using Model.InfoModel;
using Model.JsonModel;
using Utilities.Common;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace GxyBusiness
{
    public class GxyBusiness
    {
        #region 参数

        string baseUrl = Config.GetValue("baseUrl");

        private int MaxtryCount = Convert.ToInt32(Config.GetValue("errorTryCount"));

        private int SleepMilliseconds = Convert.ToInt32(Config.GetValue("sleepMilliseconds"));

        /// <summary>
        /// 默认医生
        /// </summary>
        public string DefultDoctor = "";

        /// <summary>
        /// 默认医生
        /// </summary>
        public string DefultDoctorName = "";

        public int outkey = 0;
        /// <summary>
        /// 下载到的笔数
        /// </summary>
        public int currentIndex = 1;

        /// <summary>
        /// 每次下载笔数
        /// </summary>
        int pageSize = 100;

        public List<DataSet> lstDs = new List<DataSet>();

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

        public JsonData LoginData { get; set; }

        #endregion

        public string loginkey { set; get; }

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="callback"></param>
        public void DownGxy(Action<string> callback)
        {
            TryDownGxy(1, callback);
            GC.Collect();
        }

        /// <summary>
        /// 通过身份证下载todu
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="callback"></param>
        public void DownGxyByIDs(string ids, Action<string> callback)
        {
            try
            {
                var idsa = ids.Split(',');
                int cIndex = 1;

                foreach (string id in idsa)
                {
                    if (id == "")
                    {
                        callback("下载-高血压信息..." + cIndex + "/" + idsa.Length);
                        cIndex++;
                        continue;
                    }

                    CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                    PersonModel pm = cb.GetGrdaByIDCardNo(id, loginkey, SysCookieContainer);

                    // 判断是否已经存在;存在则下载
                    if (pm != null && !string.IsNullOrEmpty(pm.pid))
                    {
                        TryDownGxyByIDs(pm, 1, callback);
                    }

                    callback("下载-高血压信息..." + cIndex + "/" + idsa.Length);
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

        public void SaveGxy(Action<string> callback)
        {
            int PcurrentIndex = 1;

            foreach (DataSet ds in lstUploadData)
            {
                TrySaveGxy(ds, 1, callback);

                callback("上传-高血压信息..." + PcurrentIndex + "/" + lstUploadData.Count);
                PcurrentIndex++;
                if (baseUrl.Contains("sdcsm_new"))
                {
                    System.Threading.Thread.Sleep((3) * 1000);
                }
            }
        }

        #region 上传

        /// <summary>
        /// 尝试3次
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        private void TrySaveGxy(DataSet ds, int tryCount, Action<string> callback)
        {
            string idcard = "";
            string name = "";
            try
            {
                // 管理卡
                //DataTable dt = ds.Tables["CD_HYPERTENSION_BASEINFO"];

                //if (dt == null || dt.Rows.Count <= 0)
                //{
                //    return;
                //}

                DataTable dtVisit = ds.Tables["CD_HYPERTENSIONFOLLOWUP"];

                if (dtVisit == null || dtVisit.Rows.Count <= 0)
                {
                    return;
                }

                idcard = dtVisit.Rows[0]["IDCardNo"].ToString();
                name = dtVisit.Rows[0]["CustomerName"].ToString();

                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                PersonModel pm = cb.GetGrdaByIDCardNo(idcard, loginkey, SysCookieContainer);

                if (pm == null || string.IsNullOrEmpty(pm.pid))
                {
                    callback("EX-高血压信息:身份证[" + idcard + "],姓名[" + name + "]:平台尚未建档或者档案状态为非活动!");
                    return;
                }

                //管理卡
                //EditGxyGLK(ds, pm);

                List<SFClass> lstSF = GetSFxxLst(pm.pid);

                string padSFDate = Convert.ToDateTime(dtVisit.Rows[0]["FollowUpDate"]).ToString("yyyy-MM-dd");

                var sfInfo = lstSF.Where(m => m.sfDate == padSFDate).ToList();

                string msg = "";

                // 修改
                if (sfInfo.Count > 0)
                {
                    msg = EditGxy(ds, pm, padSFDate, sfInfo[0].key);
                }
                else
                {
                    msg = AddGxy(ds, pm, padSFDate);
                }

                if (!string.IsNullOrEmpty(msg))
                {
                    callback("EX-高血压信息:身份证[" + idcard + "],姓名[" + name + "]:" + msg);
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

                if (tryCount < MaxtryCount)
                {
                    callback("EX-高血压信息:身份证[" + idcard + "],姓名[" + name + "]:上传高血压信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    TrySaveGxy(ds, tryCount, callback);
                }
                else
                {
                    callback("EX-高血压信息:身份证[" + idcard + "],姓名[" + name + "]:上传高血压信息失败。请确保网路畅通。");
                }
            }
        }

        private void EditGxyGLK(DataSet ds, PersonModel pm)
        {
            //http://222.133.17.194:9080/sdcsm/hypertension/toUpdate.action?dGrdabh=371481020010012301
            WebHelper web = new WebHelper();

            string url = baseUrl + "hypertension/toUpdate.action?dGrdabh=" + pm.pid;
            string returnString = web.GetHttp(url, "", SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            #region 不修改栏位
            var node = doc.DocumentNode.SelectSingleNode("//input[@name='gGlkbh']");
            string strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            StringBuilder sbPost = new StringBuilder();

            sbPost.Append("dGrdabh=").Append(pm.pid);
            sbPost.Append("&gGlkbh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='ci_sign']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            sbPost.Append("&ci_sign=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&happentime=").Append(strTmp);

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

            int pNqdqxz = 0;
            //生活习惯
            node = doc.DocumentNode.SelectSingleNode("//input[@name='mXyqk'][@checked]");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&mXyqk=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjqk'][@checked]");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&mYjqk=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl'][@checked]");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&mYdpl=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='mShzlnl'][@checked]");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&mShzlnl=").Append(strTmp);

            //体检结果
            node = doc.DocumentNode.SelectSingleNode("//input[@id='dSg']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dSg=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dTz']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTz=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dBmi']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dBmi=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dYw']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dYw=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dSsy']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (strTmp != "")
            {
                strTmp = Math.Floor(double.Parse(strTmp)).ToString();
            }

            sbPost.Append("&dSsy=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dSzy']");
            string strTmp2 = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp) || string.IsNullOrEmpty(strTmp2))
            {
                pNqdqxz++;
            }

            if (strTmp2 != "")
            {
                strTmp2 = Math.Floor(double.Parse(strTmp2)).ToString();
            }

            sbPost.Append("&dSzy=").Append(strTmp2);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dKfxt']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dKfxt=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dGmddb']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dGmddb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dDmddb']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dDmddb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dGysz']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dGysz=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dZdgc']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dZdgc=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='mEkg']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&mEkg=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dTjrq']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTjrq=").Append(strTmp);

            #endregion

            #region CD_HYPERTENSION_BASEINFO

            DataTable dt = ds.Tables["CD_HYPERTENSION_BASEINFO"];

            DataRow dr = dt.Rows[0];
            strTmp = dr["ManagementGroup"].ToString();
            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gGlzb=").Append(strTmp);
            strTmp = GetCaseOurceForWeb(dr["CaseOurce"].ToString());

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gBlly=").Append(strTmp);

            var strTmpA = dr["Symptom"].ToString().Split(',');

            strTmp = "";
            foreach (var t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t.Trim()))
                {
                    if (t.Trim() == "10")
                    {
                        sbPost.Append("&gMqzz=99");
                    }
                    else
                    {
                        sbPost.Append("&gMqzz=").Append(GetSymptomForWeb(t.Trim()));
                    }
                    strTmp = "1";
                }
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            strTmpA = dr["FatherHistory"].ToString().Split(',');

            strTmp = "";
            foreach (var t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t.Trim()))
                {
                    sbPost.Append("&dJzs=").Append(GetFatherHistoryForWeb(t.Trim()));
                    strTmp = "1";
                }
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            // 并发
            strTmpA = dr["HypertensionComplication"].ToString().Split(',');
            strTmp = "";
            foreach (var t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t.Trim()))
                {
                    sbPost.Append("&gBfzqk=").Append(GetHCForWeb(t.Trim()));
                    strTmp = "1";
                }
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gZzly=").Append(dr["TerminateExcuse"].ToString());
            sbPost.Append("&zzgl=").Append(dr["TerminateManagemen"].ToString());

            strTmp = dr["TerminateTime"].ToString() != "" ? Convert.ToDateTime(dr["TerminateTime"]).ToString("yyyy-MM-dd") : "";
            sbPost.Append("&gZzglrq=").Append(strTmp);

            sbPost.Append("&qdqxz=").Append(pNqdqxz);
            sbPost.Append("&wzd=").Append(((20 - pNqdqxz) * 100.0 / 20).ToString("#"));
            sbPost.Append("&gJyy=").Append(dr["Hypotensor"].ToString());

            var nodes = doc.DocumentNode.SelectNodes("//tbody[@id='dyTbody']/tr[position()>1]");

            if (nodes != null)
            {
                foreach (var t in nodes)
                {
                    var tmpNode = t.SelectSingleNode("td/input[@name='yYwmc']");
                    strTmp = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;
                    sbPost.Append("&yYwmc=").Append(strTmp);
                    tmpNode = t.SelectSingleNode("td/input[@name='yYwyf']");
                    strTmp = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;
                    sbPost.Append("&yYwyf=").Append(strTmp);
                };
            }

            #endregion

            returnString = web.PostHttp(baseUrl + "/hypertension/update.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
        }

        /// <summary>
        /// 新增随访
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pm"></param>
        /// <param name="padSFDate"></param>
        private string AddGxy(DataSet ds, PersonModel pm, string padSFDate)
        {
            //http://20.1.1.124:9000/sdcsm/hypertensionVisit/toAdd.action?dGrdabh=371482110010115101
            WebHelper web = new WebHelper();
            string url = baseUrl + "hypertensionVisit/toAdd.action?dGrdabh=" + pm.pid;
            string returnString = web.GetHttp(url, "", SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            StringBuilder sbPost = new StringBuilder();

            sbPost.Append("dGrdabh=").Append(pm.pid);

            #region 不修改栏位

            var node = doc.DocumentNode.SelectSingleNode("//input[@name='dXm']");
            string strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dXm=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dXb']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dXb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dCsrq']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dCsrq=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dSfzh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dSfzh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dZy']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dZy=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dLxdh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dLxdh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dSheng']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dSheng=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dShi']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dShi=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dQu']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dQu=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dJd']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dJd=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dJwh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dJwh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dXxdz']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dXxdz=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

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
            //医生建议
            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gSfysjy']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //sbPost.Append("&gSfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            #endregion

            int pNqdqxz = 0;

            double ddTmp = 0;

            #region CD_HYPERTENSIONFOLLOWUP

            DataTable dt = ds.Tables["CD_HYPERTENSIONFOLLOWUP"];

            DataRow dr = dt.Rows[0];
            //随访日期
            sbPost.Append("&happentime=").Append(padSFDate);
            //下次随访日期
            strTmp = dr["NextfollowUpDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextfollowUpDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfsj=").Append(strTmp);

            //症状
            var strTmpA = dr["Symptom"].ToString().Split(',');
            strTmp = "";
            foreach (string t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t) && t != "10")
                {
                    sbPost.Append("&gMqzz=").Append(GetSymptomForWeb(t));
                    strTmp = "1";
                }
            }
            //症状其他
            sbPost.Append("&gMqzzqt=").Append(CommonExtensions.GetUrlEncodeVal(dr["SympToMother"].ToString()));

            if (!string.IsNullOrEmpty(dr["SympToMother"].ToString()))
            {
                sbPost.Append("&gMqzz=99");
                strTmp = "1";
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            //血压
            strTmp = dr["Hypertension"].ToString();
            if (strTmp != "")
            {
                strTmp = Math.Floor(double.Parse(strTmp)).ToString();
            }
            sbPost.Append("&dSsy=").Append(strTmp);
            string strTmp2 = dr["Hypotension"].ToString();

            if (string.IsNullOrEmpty(strTmp) || string.IsNullOrEmpty(strTmp2))
            {
                pNqdqxz++;
            }
            if (strTmp2 != "")
            {
                strTmp2 = Math.Floor(double.Parse(strTmp2)).ToString();
            }
            sbPost.Append("&dSzy=").Append(strTmp2);
            //体重
            strTmp = dr["Weight"].ToString();
            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dTz=").Append(strTmp);

            //随访医生
            strTmp = dr["FollowUpDoctor"].ToString();
            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&gSfys=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            //bmi
            sbPost.Append("&dBmi=").Append(dr["BMI"]);
            //心率
            strTmp = dr["Heartrate"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dXl=").Append(strTmp);

            //体征其他
            strTmp = dr["PhysicalSympToMother"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dTzqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            //吸烟
            strTmp = dr["DailySmokeNum"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                sbPost.Append("&mXysl=");
                pNqdqxz++;
            }
            else
            {
                ddTmp = 0;
                double.TryParse(strTmp, out ddTmp);

                sbPost.Append("&mXysl=").Append(ddTmp.ToString("0"));
            }
            //饮酒
            strTmp = dr["DailyDrinkNum"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYjsl=").Append(strTmp);
            //运动
            strTmp = dr["SportTimePerWeek"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                sbPost.Append("&mYdpl=");
                pNqdqxz++;
            }
            else
            {
                ddTmp = 0;
                double.TryParse(strTmp, out ddTmp);

                sbPost.Append("&mYdpl=").Append(ddTmp.ToString("0"));
            }
            //运动时间
            strTmp = dr["SportPerMinuteTime"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYdcxsj=").Append(strTmp);

            strTmp = dr["EatSaltType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dSyqk=").Append(strTmp);

            sbPost.Append("&dSyqk2=").Append(dr["EatSaltTarget"]);

            strTmp = dr["PsyChoadJustMent"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dXltz=").Append(strTmp);

            strTmp = dr["ObeyDoctorBehavior"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dZyxw=").Append(strTmp);
            //辅助检查
            strTmp = dr["AssistantExam"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
           
            sbPost.Append("&gFzjc=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["MedicationCompliance"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gFyycx=").Append(strTmp);

            strTmp = dr["Adr"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gYwfzy=").Append(strTmp);
            sbPost.Append("&gFzyxs=").Append(CommonExtensions.GetUrlEncodeVal(dr["AdrEx"].ToString()));
            //随访分类
            strTmp = dr["FollowUpType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gCcsffl=").Append(strTmp);
            //身高
            strTmp = dr["Hight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dSg=").Append(strTmp);
            //转诊原因及机构
            strTmp = dr["ReferralReason"].ToString();

            sbPost.Append("&gZzyuanyin=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            sbPost.Append("&gZzkb=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralOrg"].ToString()));
            //随访方式
            strTmp = dr["FollowUpWay"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gSffs=").Append(strTmp);

            sbPost.Append("&dTz2=").Append(dr["WeightTarGet"]);
            sbPost.Append("&dBmi2=").Append(dr["BMITarGet"]);

            ddTmp = 0;
            if (dr["DailySmokeNumTarget"].ToString() != "")
            {
                double.TryParse(dr["DailySmokeNumTarget"].ToString(), out ddTmp);

                sbPost.Append("&mXysl2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mXysl2=");
            }
            ddTmp = 0;
            if (dr["DailyDrinkNumTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["DailyDrinkNumTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYjsl2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYjsl2=");
            }

            ddTmp = 0;
            if (dr["SportTimeSperWeekTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["SportTimeSperWeekTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYdpl2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYdpl2=");
            }

            ddTmp = 0;
            if (dr["SportPerMinutesTimeTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["SportPerMinutesTimeTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYdcxsj2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYdcxsj2=");
            }
            //居民签名
            sbPost.Append("&sdjmqm=").Append(CommonExtensions.GetUrlEncodeVal(pm.memberName));
            //下一步管理措施
            sbPost.Append("&xybglcs=").Append(dr["NextMeasures"].ToString());

            //是否转诊
            sbPost.Append("&gZzjl=").Append(dr["IsReferral"]);
            //联系人及电话
            sbPost.Append("&zzlxrjdh=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralContacts"].ToString()));
            //结果
            sbPost.Append("&zzjieguo=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralResult"].ToString()));
            //备注
            sbPost.Append("&remark=").Append(CommonExtensions.GetUrlEncodeVal(dr["Remarks"].ToString()));
   
            #endregion

            #region CD_DRUGCONDITION

            dt = ds.Tables["CD_DRUGCONDITION"];
            string tzyy = "2";
            if (dt != null && dt.Rows.Count > 0)
            {
                //用药
                var drs = dt.Select("Type=1");
                if (drs.Length > 0)
                {
                    foreach (var row in drs)
                    {
                        if (string.IsNullOrWhiteSpace(row["Name"].ToString()))
                        {
                            continue;
                        }
                        tzyy = "1";
                        sbPost.Append("&yYwmc=").Append(CommonExtensions.GetUrlEncodeVal(row["Name"].ToString()));
                        //string strYF = row["EveryTimeMg"].ToString() + row["drugtype"].ToString() + " po " + GetYaopinYongfa(row["DailyTime"].ToString());
                        string strYF = row["DosAge"].ToString();

                        sbPost.Append("&yYwyf=").Append(CommonExtensions.GetUrlEncodeVal(strYF));
                    }
                    sbPost.Append("&gJyy=").Append(tzyy);
                    if (tzyy == "2" || tzyy == "")
                    {
                        sbPost.Append("&yYwmc=");
                        sbPost.Append("&yYwyf=");
                    }

                }
                else
                {
                    sbPost.Append("&yYwmc=");
                    sbPost.Append("&yYwyf=");
                    sbPost.Append("&gJyy=2");
                }

                //用药调整
                var drs1 = dt.Select("Type=7");
                tzyy = "";
                if (drs1.Length > 0)
                {
                    foreach (var row in drs1)
                    {
                        if (string.IsNullOrWhiteSpace(row["Name"].ToString()))
                        {
                            continue;
                        }
                        tzyy = "1";
                        sbPost.Append("&yYwmctz=").Append(CommonExtensions.GetUrlEncodeVal(row["Name"].ToString()));
                        //string strYF = "每日" + dt.Rows[i]["DailyTime"].ToString() + "次,每次" + dt.Rows[i]["EveryTimeMg"].ToString() + dt.Rows[i]["drugtype"].ToString();
                        string strYF = row["DosAge"].ToString();

                        sbPost.Append("&yYwyftz=").Append(CommonExtensions.GetUrlEncodeVal(strYF));
                    }
                    sbPost.Append("&gJyytz=").Append(tzyy);
                    if (tzyy == "2" || tzyy == "")
                    {
                        sbPost.Append("&yYwmctz=");
                        sbPost.Append("&yYwyftz=");
                    }
                }
                else
                {
                    sbPost.Append("&yYwmctz=");
                    sbPost.Append("&yYwyftz=");
                    sbPost.Append("&gJyytz=");
                }
            }
            else
            {
                sbPost.Append("&yYwmc=");
                sbPost.Append("&yYwyf=");
                sbPost.Append("&gJyy=2");
                sbPost.Append("&yYwmctz=");
                sbPost.Append("&yYwyftz=");
                sbPost.Append("&gJyytz=");
            }

            #endregion

            sbPost.Append("&qdqxz=").Append(pNqdqxz);
            sbPost.Append("&wzd=").Append(((22 - pNqdqxz) * 100.0 / 22).ToString("#"));

            // 新增
            //http://20.1.1.124:9000/sdcsm/hypertensionVisit/add.action
            returnString = web.PostHttp(baseUrl + "hypertensionVisit/add.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

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

                if (returnNode.InnerText.IndexOf("'add' == \"add\"") == -1)
                {
                    CommonExtensions.WriteLog(returnString);
                    return "新增失败！";
                }
            }

            return "";
        }

        /// <summary>
        /// 修改随访
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pm"></param>
        /// <param name="zxKey"></param>
        /// <param name="padSFDate"></param>
        private string EditGxy(DataSet ds, PersonModel pm, string padSFDate, string key)
        {
            //http://20.1.1.124:9000/sdcsm/hypertensionVisit/toUpdate.action?id=B613338EECF937E3E0530100007F622A
            WebHelper web = new WebHelper();
            string url = baseUrl + "hypertensionVisit/toUpdate.action?id=" + key;
            string returnString = web.GetHttp(url, "",  SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            StringBuilder sbPost = new StringBuilder();

            sbPost.Append("id=").Append(key);

            #region 不修改栏位
            var node = doc.DocumentNode.SelectSingleNode("//input[@id='sfcs']");

            string strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&sfcs=").Append(strTmp);
            sbPost.Append("&dGrdabh=").Append(pm.pid);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dXm']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dXm=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dXb']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dXb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dCsrq']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dCsrq=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dSfzh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dSfzh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dZy']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dZy=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dLxdh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dLxdh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dSheng']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dSheng=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dShi']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dShi=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dQu']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dQu=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dJd']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dJd=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dJwh']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dJwh=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dXxdz']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dXxdz=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            node = doc.DocumentNode.SelectSingleNode("//input[@name='happentime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&happentime=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&createtime=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            node = doc.DocumentNode.SelectSingleNode("//input[@name='updatetime']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&updatetime=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

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
            //医生建议
            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gSfysjy']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //sbPost.Append("&gSfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='ccsfflBfzQt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //sbPost.Append("&ccsfflBfzQt=").Append(strTmp);
            #endregion

            int pNqdqxz = 0;
            double ddTmp = 0;

            #region CD_HYPERTENSIONFOLLOWUP

            DataTable dt = ds.Tables["CD_HYPERTENSIONFOLLOWUP"];

            DataRow dr = dt.Rows[0];
            //下次随访日期
            strTmp = dr["NextfollowUpDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextfollowUpDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfsj=").Append(strTmp);

            //症状
            var strTmpA = dr["Symptom"].ToString().Split(',');
            strTmp = "";
            foreach (string t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t) && t != "10")
                {
                    sbPost.Append("&gMqzz=").Append(GetSymptomForWeb(t));
                    strTmp = "1";
                }
            }
            //症状其他
            sbPost.Append("&gMqzzqt=").Append(CommonExtensions.GetUrlEncodeVal(dr["SympToMother"].ToString()));

            if (!string.IsNullOrEmpty(dr["SympToMother"].ToString()))
            {
                sbPost.Append("&gMqzz=99");
                strTmp = "1";
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            //血压
            strTmp = dr["Hypertension"].ToString();

            if (strTmp != "")
            {
                strTmp = Math.Floor(double.Parse(strTmp)).ToString();
            }

            sbPost.Append("&dSsy=").Append(strTmp);

            string strTmp2 = dr["Hypotension"].ToString();

            if (string.IsNullOrEmpty(strTmp) || string.IsNullOrEmpty(strTmp2))
            {
                pNqdqxz++;
            }

            if (strTmp2 != "")
            {
                strTmp2 = Math.Floor(double.Parse(strTmp2)).ToString();
            }

            sbPost.Append("&dSzy=").Append(strTmp2);
            //体重
            strTmp = dr["Weight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTz=").Append(strTmp);
            //随访医生
            strTmp = dr["FollowUpDoctor"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gSfys=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            sbPost.Append("&dBmi=").Append(dr["BMI"]);

            strTmp = dr["Heartrate"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dXl=").Append(strTmp);
            //体征其他
            strTmp = dr["PhysicalSympToMother"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dTzqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //吸烟
            strTmp = dr["DailySmokeNum"].ToString();
            ddTmp = 0;
            if (string.IsNullOrEmpty(strTmp))
            {
                sbPost.Append("&mXysl=");
                pNqdqxz++;
            }
            else
            {
                double.TryParse(strTmp, out ddTmp);
                sbPost.Append("&mXysl=").Append(ddTmp.ToString("0"));
            }
            //饮酒
            strTmp = dr["DailyDrinkNum"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYjsl=").Append(strTmp);
            //运动
            strTmp = dr["SportTimePerWeek"].ToString();

            ddTmp = 0;
            if (string.IsNullOrEmpty(strTmp))
            {
                sbPost.Append("&mYdpl=");
                pNqdqxz++;
            }
            else
            {
                double.TryParse(strTmp, out ddTmp);

                sbPost.Append("&mYdpl=").Append(ddTmp.ToString("0"));
            }
            //运动时间
            strTmp = dr["SportPerMinuteTime"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYdcxsj=").Append(strTmp);
            //食盐
            strTmp = dr["EatSaltType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dSyqk=").Append(strTmp);

            sbPost.Append("&dSyqk2=").Append(dr["EatSaltTarget"]);

            strTmp = dr["PsyChoadJustMent"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dXltz=").Append(strTmp);

            strTmp = dr["ObeyDoctorBehavior"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dZyxw=").Append(strTmp);
            //辅助检查
            strTmp = dr["AssistantExam"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gFzjc=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["MedicationCompliance"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gFyycx=").Append(strTmp);

            strTmp = dr["Adr"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gYwfzy=").Append(strTmp);
            sbPost.Append("&gFzyxs=").Append(CommonExtensions.GetUrlEncodeVal(dr["AdrEx"].ToString()));
            //随访分类
            strTmp = dr["FollowUpType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gCcsffl=").Append(strTmp);
            //身高
            strTmp = dr["Hight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dSg=").Append(strTmp);
            //转诊原因,机构
            strTmp = dr["ReferralReason"].ToString();
            sbPost.Append("&gZzyuanyin=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            sbPost.Append("&gZzkb=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralOrg"].ToString()));
            //随访方式
            strTmp = dr["FollowUpWay"].ToString();
            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&gSffs=").Append(strTmp);

            sbPost.Append("&dTz2=").Append(dr["WeightTarGet"]);
            sbPost.Append("&dBmi2=").Append(dr["BMITarGet"]);

            ddTmp = 0;
            if (dr["DailySmokeNumTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["DailySmokeNumTarget"].ToString(), out ddTmp);

                sbPost.Append("&mXysl2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mXysl2=");
            }
            ddTmp = 0;
            if (dr["DailyDrinkNumTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["DailyDrinkNumTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYjsl2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYjsl2=");
            }

            ddTmp = 0;
            if (dr["SportTimeSperWeekTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["SportTimeSperWeekTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYdpl2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYdpl2=");
            }

            ddTmp = 0;
            if (dr["SportPerMinutesTimeTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["SportPerMinutesTimeTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYdcxsj2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYdcxsj2=");
            }
            //居民签名
            sbPost.Append("&sdjmqm=").Append(CommonExtensions.GetUrlEncodeVal(pm.memberName));

            //下一步管理措施
            sbPost.Append("&xybglcs=").Append(dr["NextMeasures"].ToString());

            //是否转诊
            sbPost.Append("&gZzjl=").Append(dr["IsReferral"]);
            //联系人及电话
            sbPost.Append("&zzlxrjdh=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralContacts"].ToString()));
            //结果
            sbPost.Append("&zzjieguo=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralResult"].ToString()));
            //备注
            sbPost.Append("&remark=").Append(CommonExtensions.GetUrlEncodeVal(dr["Remarks"].ToString()));
 
            #endregion

            #region CD_DRUGCONDITION

            dt = ds.Tables["CD_DRUGCONDITION"];
            string tzyy = "2";
            if (dt != null && dt.Rows.Count > 0)
            {
                //用药
                var drs = dt.Select("Type=1");
                if (drs.Length > 0)
                {
                    foreach (var row in drs)
                    {
                        if (string.IsNullOrWhiteSpace(row["Name"].ToString()))
                        {
                            continue;
                        }
                        tzyy = "1";
                        sbPost.Append("&yYwmc=").Append(CommonExtensions.GetUrlEncodeVal(row["Name"].ToString()));
                        //string strYF = "每日" + dt.Rows[i]["DailyTime"].ToString() + "次,每次" + dt.Rows[i]["EveryTimeMg"].ToString() + dt.Rows[i]["drugtype"].ToString();
                        //string strYF = row["EveryTimeMg"].ToString() + row["drugtype"].ToString() + " po " + GetYaopinYongfa(row["DailyTime"].ToString());
                        string strYF = row["DosAge"].ToString();
                        sbPost.Append("&yYwyf=").Append(CommonExtensions.GetUrlEncodeVal(strYF));
                    }
                    sbPost.Append("&gJyy=").Append(tzyy);
                    if (tzyy == "2" || tzyy == "")
                    {
                        sbPost.Append("&yYwmc=");
                        sbPost.Append("&yYwyf=");
                    }
                }
                else
                {
                    sbPost.Append("&yYwmc=");
                    sbPost.Append("&yYwyf=");
                    sbPost.Append("&gJyy=2");
                }

                //用药调整
                var drs1 = dt.Select("Type=7");
                tzyy = "";
                if (drs1.Length > 0)
                {
                    foreach (var row in drs1)
                    {
                        if (string.IsNullOrWhiteSpace(row["Name"].ToString()))
                        {
                            continue;
                        }
                        tzyy = "1";
                        sbPost.Append("&yYwmctz=").Append(CommonExtensions.GetUrlEncodeVal(row["Name"].ToString()));
                        //string strYF = "每日" + dt.Rows[i]["DailyTime"].ToString() + "次,每次" + dt.Rows[i]["EveryTimeMg"].ToString() + dt.Rows[i]["drugtype"].ToString();
                        //string strYF = row["EveryTimeMg"].ToString() + row["drugtype"].ToString() + " po " + GetYaopinYongfa(row["DailyTime"].ToString());
                        string strYF = row["DosAge"].ToString();
                        sbPost.Append("&yYwyftz=").Append(CommonExtensions.GetUrlEncodeVal(strYF));
                    }
                    sbPost.Append("&gJyytz=").Append(tzyy);
                    if (tzyy == "2" || tzyy == "")
                    {
                        sbPost.Append("&yYwmctz=");
                        sbPost.Append("&yYwyftz=");
                    }
                }
                else
                {
                    sbPost.Append("&yYwmctz=");
                    sbPost.Append("&yYwyftz=");
                    sbPost.Append("&gJyytz=");
                }
            }
            else
            {
                sbPost.Append("&yYwmc=");
                sbPost.Append("&yYwyf=");
                sbPost.Append("&gJyy=2");
                sbPost.Append("&yYwmctz=");
                sbPost.Append("&yYwyftz=");
                sbPost.Append("&gJyytz=");
            }

            #endregion

            sbPost.Append("&qdqxz=").Append(pNqdqxz);
            sbPost.Append("&wzd=").Append(((22 - pNqdqxz) * 100.0 / 22).ToString("0"));

            // 修改
            //http://20.1.1.124:9000/sdcsm/hypertensionVisit/update.action
            returnString = web.PostHttp(baseUrl + "hypertensionVisit/update.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

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

                if (returnNode.InnerText.IndexOf("'update' == \"update\"") == -1)
                {
                    CommonExtensions.WriteLog(returnString);

                    return "更新失败！";
                }
            }

            return "";
        }

        #endregion

        #region 下载
        /// <summary>
        /// 根据个人档案下载
        /// </summary>
        /// <param name="pm"></param>
        /// <param name="callback"></param>
        public void DownInfoByPerson(PersonModel pm, Action<string> callback)
        {
            callback("下载-高血压信息..." + currentIndex + "/" + totalRows);

            if (pm != null && !string.IsNullOrEmpty(pm.pid))
            {
                TryDownGxyByIDs(pm, 1, callback);
            }

            currentIndex++;
        }

        /// <summary>
        /// 尝试3次下载
        /// </summary>
        /// <param name="pm"></param>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        private void TryDownGxyByIDs(PersonModel pm, int tryCount, Action<string> callback)
        {
            string idcard = pm.idNumber.ToString();

            try
            {
                GetGxyInfo(pm);
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
                    callback("EX-高血压信息:身份证[" + idcard + "],姓名[" + pm.memberName + "]:下载信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;

                    TryDownGxyByIDs(pm, tryCount, callback);
                }
                else
                {
                    callback("EX-高血压信息:身份证[" + idcard + "],姓名[" + pm.memberName + "]:下载信息失败。请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 尝试3次下载
        /// </summary>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        private void TryDownGxy(int tryCount, Action<string> callback)
        {
            try
            {
                GetGxyKeyAndInfo(callback);
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
                    TryDownGxy(tryCount, callback);
                }
                else
                {
                    callback("EX-下载高血压信息失败，请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 获取key和页面信息
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        private void GetGxyKeyAndInfo(Action<string> callback)
        {
            int PageSum = 0;
            List<PersonModel> personList = GetGxyKeyAndInfo(callback, out PageSum);

            //调方法，便利当前页的表示，获取信息
            this.GetInfoByPersonList(personList, callback);

            for (int i = 2; i <= PageSum; i++)
            {
                personList.Clear();

                //调方法，遍历当前页标示，获取信息
                personList = this.GetPageNumKeyInfo(i, callback);

                this.GetInfoByPersonList(personList, callback);
            }
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

            string postData = "page.currentPage=" + pageNum + "&search=3&siteid=" + key + "&dqjg=" + key + "&dXm=&dXb=&grdabhShow=&dSfzh=&dSspq=&dZy=&dYlbxh=&glzb=&dCsrq1=&dCsrq2=&dDazt=1&createuser=&createtime1=&createtime2=&dJd=&dJwh=&dXxdz=&zzgl=&branch=on";
            string returnString = web.PostHttp(baseUrl + "/hypertension/list.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
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
                    string idNumber = tds[9].InnerText.Replace("\r", "").Replace("\n", "");
                    if (idNumber.Trim() == "")
                    {
                        currentIndex++;

                        continue;
                    }
                    PersonModel person = new PersonModel();
                    person.pid = tds[3].InnerText.Replace("\r", "").Replace("\n", "");
                    person.memberName = tds[4].InnerText.Replace("\r", "").Replace("\n", "");
                    person.idNumber = idNumber.Trim();
                    personList.Add(person);
                }
            }

            return personList;
        }

        /// <summary>
        /// 获取key和页面信息
        /// </summary>
        /// <param name="m"></param>
        /// <param name="callback"></param>
        private List<PersonModel> GetGxyKeyAndInfo(Action<string> callback, out int pageSum)
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

            string postData = "search=3&siteid=" + key + "&dqjg=" + key + "&dXm=&dXb=&grdabhShow=&dSfzh=&dSspq=&dZy=&dYlbxh=&glzb=&dCsrq1=&dCsrq2=&dDazt=1&createuser=&createtime1=&createtime2=&dJd=&dJwh=&dXxdz=&zzgl=&branch=on";
            string returnString = web.PostHttp(baseUrl + "/hypertension/list.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
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
                    string idNumber = tds[9].InnerText.Replace("\r", "").Replace("\n", "");
                    if (idNumber.Trim() == "")
                    {
                        currentIndex++;

                        continue;
                    }
                    PersonModel person = new PersonModel();
                    person.pid = tds[3].InnerText.Replace("\r", "").Replace("\n", "");
                    person.memberName = tds[4].InnerText.Replace("\r", "").Replace("\n", "");
                    person.idNumber = idNumber.Trim();
                    personList.Add(person);
                }
            }

            var divNodes = doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[1]").SelectSingleNode("ul").SelectNodes("li");
            string pages = divNodes[9].InnerText;
            pages = HtmlHelper.GetLastTagValue(pages, "共", "页");

            int.TryParse(pages, out pageSum);

            string rowSum = divNodes[0].InnerText;

            rowSum = rowSum.Substring(rowSum.IndexOf('：') + 1);
            int.TryParse(rowSum, out totalRows);

            return personList;
        }

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="lstKey"></param>
        private void GetInfoByPersonList(List<PersonModel> lstAllPm, Action<string> callback)
        {
            foreach (var pm in lstAllPm)
            {
                TryDownGxyByIDs(pm, 1, callback);

                callback("下载-高血压信息..." + currentIndex + "/" + totalRows);

                currentIndex++;
            }
        }

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="key"></param>
        private void GetGxyInfo(PersonModel pm)
        {
            WebHelper web = new WebHelper();

            List<SFClass> lstSF = GetSFxxLst(pm.pid);

            if (lstSF.Count > 0)
            {
                
                CommonBusiness.CommonDAOBusiness cDAO = new CommonBusiness.CommonDAOBusiness();
               
                DataSet ds = DataSetTmp.GxyDataSet;
                DataSet dsSave = new DataSet();

                #region  CD_HYPERTENSIONFOLLOWUP

                SFClass sf = lstSF[0];
                //http://20.1.1.124:9000/sdcsm/hypertensionVisit/toUpdate.action?id=B613338EECF937E3E0530100007F622A
                string url = baseUrl + "hypertensionVisit/toUpdate.action?id=" + sf.key;
                string returnString = web.GetHttp(url, "", SysCookieContainer);

                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                DataTable dtDataGxy = ds.Tables["CD_HYPERTENSIONFOLLOWUP"].Clone();
                DataRow dr = dtDataGxy.NewRow();

                dr["IDCardNo"] = pm.idNumber;

                var node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                dr["FollowUpDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSfys']");
                dr["FollowUpDoctor"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXcsfsj']");
                dr["NextFollowUpDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                var nodes = doc.DocumentNode.SelectNodes("//input[@name='gMqzz'][@checked]");

                string strtmp = "";

                if (nodes != null)
                {
                    foreach (var no in nodes)
                    {
                        if (no.Attributes.Contains("value"))
                        {
                            string noV = no.Attributes["value"].Value;

                            strtmp += "," + GetSymptom(noV);

                        }
                    }
                }

                dr["Symptom"] = strtmp.TrimStart(',').Replace("100", "10");
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gMqzzqt']");
                dr["SympToMother"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dSsy']");
                dr["Hypertension"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSzy']");
                dr["Hypotension"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dTz']");
                dr["Weight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSg']");
                dr["Hight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dBmi']");
                dr["BMI"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@ name='dXl']");
                dr["Heartrate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dTzqt']");
                dr["PhysicalSympToMother"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl']");
                dr["DailySmokeNum"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl']");
                dr["DailyDrinkNum"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl']");
                dr["SportTimePerWeek"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj']");
                dr["SportPerMinuteTime"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//select[@name='dSyqk']/option[@selected]");
                //摄盐情况           
                string zy = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["EatSaltType"] = zy;

                //下次摄盐情况   
                node = doc.DocumentNode.SelectSingleNode("//select[@name='dSyqk2']/option[@selected]");
                zy = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["EatSaltTarget"] = zy;

                //心理调整
                node = doc.DocumentNode.SelectSingleNode("//select[@name='dXltz']/option[@selected]");
                zy = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["PsyChoadJustMent"] = zy;

                //遵医行为 
                node = doc.DocumentNode.SelectSingleNode("//select[@name='dZyxw']/option[@selected]");
                zy = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["ObeyDoctorBehavior"] = zy;

                //辅助检查
                node = doc.DocumentNode.SelectSingleNode("//input[@name ='gFzjc']");
                dr["AssistantExam"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //服药依从性
                node = doc.DocumentNode.SelectSingleNode("//select[@id='gFyycx']/option[@selected]");
                dr["MedicationCompliance"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //不良反应
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYwfzy'][@checked]");
                dr["Adr"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gFzyxs']");
                dr["AdrEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //随访分类
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gCcsffl'][@checked][1]");
                dr["FollowUpType"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //转诊原因
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZzyuanyin']");
                dr["ReferralReason"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //转诊结构
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZzkb']");
                dr["ReferralOrg"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //随访方式
                node = doc.DocumentNode.SelectSingleNode("//input[@name='tSffs'][@checked]");
                string strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["FollowUpWay"] = strTmp == "99" ? "" : strTmp;

                node = doc.DocumentNode.SelectSingleNode("//input[@id='dTz2']");
                dr["WeightTarGet"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dBmi2']");
                dr["BMITarGet"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl2']");
                dr["DailySmokeNumTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl2']");
                dr["DailyDrinkNumTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl2']");
                dr["SportTimeSperWeekTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj2']");
                dr["SportPerMinutesTimeTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //医生建议 DoctorView 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSfysjy']");
                dr["DoctorView"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //是否转诊 IsReferral 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gZzjl'][@checked]");
                dr["IsReferral"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //下一步管理措施
                node = doc.DocumentNode.SelectSingleNode("//input[@name='xybglcs'][@checked]");
                dr["NextMeasures"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='zzlxrjdh']");
                dr["ReferralContacts"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//select[@name='zzjieguo']/option[@selected]");
                dr["ReferralResult"] = node == null || !node.Attributes.Contains("value") ? "" : GetReferralResult(node.Attributes["value"].Value);

                node = doc.DocumentNode.SelectSingleNode("//input[@name='remark']");
                dr["Remarks"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
       
                nodes = doc.DocumentNode.SelectNodes("//tbody[@id='dyTbody']/tr[position()>1]");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gJyy'][@checked]");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                string sfrq = Convert.ToDateTime(dr["FollowUpDate"].ToString()).ToString("yyyy-MM-dd");
                dtDataGxy.Rows.Add(dr);
                outkey = cDAO.SaveMainTable(dtDataGxy, pm.idNumber, sfrq);

                dtDataGxy = ds.Tables["CD_DRUGCONDITION"].Clone();
                //用药
                if (nodes != null && strtmp == "1")
                {
                    #region  CD_DRUGCONDITION

                    foreach (var t in nodes)
                    {
                        var tmpNode = t.SelectSingleNode("td/input[@name='yYwmc']");

                        dr = dtDataGxy.NewRow();

                        dr["IDCardNo"] = pm.idNumber;
                        dr["Type"] = "1";
                        dr["OutKey"] = outkey.ToString();
                        dr["Name"] = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;


                        tmpNode = t.SelectSingleNode("td/input[@name='yYwyf']");
                        int pN = 0;

                        /*int.TryParse(t.b04_011_04, out pN);
                        dr["DailyTime"] = pN;*/
                        pN = 0;

                        string tmpET = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;
                        dr["DosAge"] = tmpET;
                        dtDataGxy.Rows.Add(dr);
                    }

                    #endregion
                }

                //用药调整
                nodes = doc.DocumentNode.SelectNodes("//tbody[@id='dyTbodytz']/tr[position()>1]");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gJyytz'][@checked]");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                if (nodes != null && strtmp == "1")
                {
                    #region  CD_DRUGCONDITION

                    //http://222.133.17.194:9080/sdcsm/hypertensionVisit/toUpdate.action?id=186241
                    foreach (var t in nodes)
                    {
                        var tmpNode = t.SelectSingleNode("td/input[@name='yYwmctz']");

                        dr = dtDataGxy.NewRow();

                        dr["IDCardNo"] = pm.idNumber;
                        dr["Type"] = "7";
                        dr["OutKey"] = outkey.ToString();
                        dr["Name"] = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;


                        tmpNode = t.SelectSingleNode("td/input[@name='yYwyftz']");
                        int pN = 0;

                        /*int.TryParse(t.b04_011_04, out pN);
                        dr["DailyTime"] = pN;*/
                        pN = 0;

                        string tmpET = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;
                        //int.TryParse(tmpET, out pN);
                        dr["DosAge"] = tmpET;
                        dtDataGxy.Rows.Add(dr);
                    }

                    #endregion
                }

                if (dtDataGxy != null && dtDataGxy.Rows.Count > 0)
                {
                    dsSave.Tables.Add(dtDataGxy);
                }
                #endregion

                #region  CD_HYPERTENSION_BASEINFO
                //http://20.1.1.124:9000/sdcsm/hypertension/toUpdate.action?dGrdabh=371482110010115101
                url = baseUrl + "hypertension/toUpdate.action?dGrdabh=" + pm.pid;
                returnString = web.GetHttp(url, "", SysCookieContainer);
                doc = HtmlHelper.GetHtmlDocument(returnString);

                DataTable dtDataGxy2 = ds.Tables["CD_HYPERTENSION_BASEINFO"].Clone();
                DataRow dr2 = dtDataGxy2.NewRow();

                dr2["IDCardNo"] = pm.idNumber;
                //dr2["OutKey"] = outkey.ToString();
                dr2["RecordID"] = pm.pid.Substring(0, 17);

                //管理组
                var node2 = doc.DocumentNode.SelectSingleNode("//input[@name='gGlzb'][@checked]");
                dr2["ManagementGroup"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //病历来源
                node2 = doc.DocumentNode.SelectSingleNode("//input[@name='gBlly'][@checked]");
                dr2["CaseOurce"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;
                dr2["CaseOurce"] = GetCaseOurce(dr2["CaseOurce"].ToString());

                //家族史
                var nodes2 = doc.DocumentNode.SelectNodes("//input[@name='dJzs'][@checked]");
                string fatherHistory = "";

                if (nodes2 != null)
                {
                    foreach (var no in nodes2)
                    {
                        if (no.Attributes.Contains("value"))
                        {
                            fatherHistory += "," + GetFatherHistory(no.Attributes["value"].Value);
                        }
                    }

                    fatherHistory = fatherHistory.TrimStart(',');
                }

                dr2["FatherHistory"] = fatherHistory;

                //目前症状
                string symptom = "";
                nodes2 = doc.DocumentNode.SelectNodes("//input[@name='gMqzz'][@checked]");

                if (nodes2 != null)
                {
                    foreach (var no in nodes2)
                    {
                        if (no.Attributes.Contains("value"))
                        {
                            string temStr = no.Attributes["value"].Value;

                            if (temStr != "99")
                            {
                                symptom += "," + GetSymptom(temStr);
                            }
                            else
                            {
                                symptom += ",10";
                            }
                        }
                    }

                    symptom = symptom.TrimStart(',');
                }

                dr2["Symptom"] = symptom;

                //高血压并发症
                string hyperComp = "";

                nodes2 = doc.DocumentNode.SelectNodes("//input[@name='gBfzqk'][@checked]");
                if (nodes2 != null)
                {
                    foreach (var no in nodes2)
                    {
                        if (no.Attributes.Contains("value"))
                        {
                            string temStr = no.Attributes["value"].Value;

                            if (temStr == "99")
                            {
                                temStr = "12";
                            }

                            hyperComp += "," + temStr;
                        }
                    }

                    hyperComp = hyperComp.TrimStart(',');
                }

                dr2["HypertensionComplication"] = hyperComp;


                //是否使用降血压药
                node2 = doc.DocumentNode.SelectSingleNode("//input[@name='gJyy'][@checked]");
                dr2["Hypotensor"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //是否终止管理
                node2 = doc.DocumentNode.SelectSingleNode("//input[@name='zzgl'][@checked]");
                dr2["TerminateManagemen"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //终止日期
                node2 = doc.DocumentNode.SelectSingleNode("//input[@id='gZzglrq']");
                dr2["TerminateTime"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //终止理由
                node2 = doc.DocumentNode.SelectSingleNode("//input[@name='gZzly'][@checked]");
                string terExcuse = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                if (terExcuse == "99")
                {
                    terExcuse = "4";
                }
                dr2["TerminateExcuse"] = terExcuse;

                dtDataGxy2.Rows.Add(dr2);
                #endregion

                dsSave.Tables.Add(dtDataGxy2);

                cDAO.SaveDataSet(dsSave, pm.idNumber, "1,7", outkey.ToString());

                dsSave.Tables.Clear();
            }
        }

        #endregion

        #region 栏位对应

        //药品用法转换 上传
        private string GetYaopinYongfa(string str)
        {
            string returnVal = "";

            switch (str)
            {
                case "1":
                    returnVal = "qd";
                    break;
                case "2":
                    returnVal = "bid";
                    break;
                case "3":
                    returnVal = "tid";
                    break;
                case "4":
                    returnVal = "qid";
                    break;
                default:
                    break;
            }

            return returnVal;
        }

        //药品用法转换 上传
        private string GetReferralResult(string str)
        {
            string returnVal = "";

            switch (str)
            {
                case "到位":
                    returnVal = "1";
                    break;
                case "不到位":
                    returnVal = "2";
                    break;
            }

            return returnVal;
        }

        //药品用法转换 下载
        private string GetYpyfForWeb(string str)
        {
            string s = "";
            switch (str)
            {
                case "qd":
                    s = "1";
                    break;
                case "bid":
                    s = "2";
                    break;
                case "tid":
                    s = "3";
                    break;
                case "qid":
                    s = "4";
                    break;
                default:
                    break;
            }
            return s;
        }

        private string GetSymptom(string code)
        {
            code = code.Trim();
            string returnVal = "";

            int intCode = 0;

            if (int.TryParse(code, out intCode))
            {
                returnVal = (intCode + 1).ToString();
            }

            return returnVal;
        }

        private string GetSymptomForWeb(string code)
        {
            code = code.Trim();
            string returnVal = "";

            int intCode = 0;

            if (int.TryParse(code, out intCode))
            {
                returnVal = (intCode - 1).ToString();
            }

            return returnVal;
        }

        private string GetCaseOurce(string code)
        {
            code = code.Trim();
            string returnVal = "";

            switch (code)
            {
                case "1":
                    returnVal = "1";
                    break;
                case "2":
                    returnVal = "2";
                    break;
                case "4":
                    returnVal = "3";
                    break;

            }
            return returnVal;
        }

        private string GetCaseOurceForWeb(string code)
        {
            code = code.Trim();
            string returnVal = "";

            switch (code)
            {
                case "1":
                    returnVal = "1";
                    break;
                case "2":
                    returnVal = "2";
                    break;
                case "3":
                    returnVal = "4";
                    break;

            }
            return returnVal;
        }

        private string GetFatherHistory(string code)
        {
            code = code.Trim();
            string returnVal = "";

            switch (code)
            {
                case "98":
                    returnVal = "5";
                    break;
                case "99":
                    returnVal = "6";
                    break;
                case "100":
                    returnVal = "7";
                    break;
                default:
                    returnVal = code;
                    break;

            }
            return returnVal;
        }

        private string GetFatherHistoryForWeb(string code)
        {
            code = code.Trim();
            string returnVal = "";

            switch (code)
            {
                case "5":
                    returnVal = "98";
                    break;
                case "6":
                    returnVal = "99";
                    break;
                case "7":
                    returnVal = "100";
                    break;
                default:
                    returnVal = code;
                    break;

            }
            return returnVal;
        }

        private string GetHCForWeb(string code)
        {
            code = code.Trim();
            string returnVal = "";

            switch (code)
            {
                case "12":
                    returnVal = "99";
                    break;
                default:
                    returnVal = code;
                    break;

            }
            return returnVal;
        }

        #endregion

        /// <summary>
        /// 根据人员key获取随访列表
        /// </summary>
        /// <param name="strkey"></param>
        /// <returns></returns>
        private List<SFClass> GetSFxxLst(string key)
        {
            List<SFClass> lstSF = new List<SFClass>();

            string postData = "dGrdabh=" + key;
            //http://20.1.1.124:9000/sdcsm/hypertensionVisit/toAdd.action?dGrdabh=371482110010115101
            WebHelper web = new WebHelper();

            string returnString = web.GetHttp(baseUrl + "hypertensionVisit/toAdd.action?" + postData, "", SysCookieContainer);


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
    }
}
