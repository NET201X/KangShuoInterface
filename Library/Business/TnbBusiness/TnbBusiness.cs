using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Net;
using Model.InfoModel;
using Model.JsonModel;
using DAL;
using Utilities.Common;
using Newtonsoft.Json;
using HtmlAgilityPack;
using System.Threading.Tasks;

namespace TnbBusiness
{
    public class TnbBusiness
    {
        #region 参数

        string baseUrl = Config.GetValue("baseUrl");

        private int MaxtryCount = Convert.ToInt32(Config.GetValue("errorTryCount"));

        private int SleepMilliseconds = Convert.ToInt32(Config.GetValue("sleepMilliseconds"));

        public string loginkey { set; get; }

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

        /// <summary>
        /// 糖尿病全部下载入口
        /// </summary>
        /// <param name="callback"></param>
        public void DownTnb(Action<string> callback)
        {
            TryDownTnb(1, callback);
            GC.Collect();
        }

        /// <summary>
        /// 根据身份证号下载数据入口
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="callback"></param>
        public void DownTnbByIDs(string ids, Action<string> callback)
        {
            try
            {
                var idsa = ids.Split(',');
                int cIndex = 1;
                foreach (string id in idsa)
                {
                    if (id == "")
                    {
                        callback("下载-糖尿病信息..." + cIndex + "/" + idsa.Length);
                        cIndex++;
                        continue;
                    }
                    CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                    PersonModel person = cb.GetGrdaByIDCardNo(id, loginkey, SysCookieContainer);

                    if (person != null && !string.IsNullOrEmpty(person.pid))
                    {
                        TryDownTnbByIDs(person, 1, callback);
                    }

                    callback("下载-糖尿病信息..." + cIndex + "/" + idsa.Length);
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
        /// 数据上传入口
        /// </summary>
        /// <param name="callback"></param>
        public void SaveTnb(Action<string> callback)
        {
            int currentIndex = 1;
            foreach (DataSet ds in lstUploadData)
            {
                TrySaveTnb(ds, 1, callback);
                callback("上传-糖尿病信息..." + currentIndex + "/" + lstUploadData.Count);
                currentIndex++;
                if (baseUrl.Contains("sdcsm_new"))
                {
                    System.Threading.Thread.Sleep((3) * 1000);
                }
            }

        }

        #region 上传

        private void TrySaveTnb(DataSet ds, int tryCount, Action<string> callback)
        {
            //DataTable dt = ds.Tables["CD_DIABETES_BASEINFO"];

            //if (dt == null || dt.Rows.Count <= 0)
            //{
            //    return;
            //}
            string idcard = "";
            string name = "";

            try
            {
                DataTable dtSF = ds.Tables["CD_DIABETESFOLLOWUP"];
                DataTable dtYao = ds.Tables["CD_DRUGCONDITION"];
                if (dtSF == null || dtSF.Rows.Count <= 0)
                {
                    return;
                }

                idcard = dtSF.Rows[0]["IDCardNo"].ToString();
                name = dtSF.Rows[0]["CustomerName"].ToString();

                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                PersonModel person = cb.GetGrdaByIDCardNo(idcard, loginkey, SysCookieContainer);

                if (person == null || string.IsNullOrEmpty(person.pid))
                {
                    callback("EX-糖尿病信息:身份证[" + idcard + "],姓名[" + name + "]:平台尚未建档或者档案状态为非活动!");
                    return;
                }

                //糖尿病管理卡
                //EditTnbGLK(ds, person);

                List<SFClass> lstSF = GetSFxxLst(person.pid);

                string padSFDate = Convert.ToDateTime(dtSF.Rows[0]["VisitDate"]).ToString("yyyy-MM-dd");
                var sfInfo = lstSF.Where(m => m.sfDate == padSFDate).ToList();
                string msg = "";

                if (sfInfo.Count > 0)
                {
                    //更新随访
                    msg = UpdateTnb(ds, person, padSFDate, sfInfo[0].key);
                }
                else
                {
                    //新增随访
                    msg = AddTnb(ds, person, padSFDate);
                }
                if (!string.IsNullOrEmpty(msg))
                {
                    callback("EX-糖尿病信息:身份证[" + idcard + "],姓名[" + name + "]:" + msg);
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
                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    TrySaveTnb(ds, tryCount, callback);
                }
                else
                {
                    callback("EX-上传脑卒中信息失败，请确保网路畅通");
                }
            }
        }

        private void EditTnbGLK(DataSet ds, PersonModel pm)
        {
            //http://222.133.17.194:9080/sdcsm/diabetes/toUpdate.action?dGrdabh=371481020010012301
            WebHelper web = new WebHelper();

            string url = baseUrl + "diabetes/toUpdate.action?dGrdabh=" + pm.pid;
            string returnString = web.GetHttp(url, "", SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            StringBuilder sbPost = new StringBuilder();

            sbPost.Append("dGrdabh=").Append(pm.pid);

            #region 不修改栏位

            var node = doc.DocumentNode.SelectSingleNode("//input[@name='gGlkbh']");
            string strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

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

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dYw']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dYw=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dSsy']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
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

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dChxt']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dChxt=").Append(strTmp);

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

            node = doc.DocumentNode.SelectSingleNode("//input[@name='mNwldb'][@checked]");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&mNwldb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='tThxhdb']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tThxhdb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@id='dBmi']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            sbPost.Append("&dBmi=").Append(strTmp);

            #endregion

            #region CD_DIABETES_BASEINFO

            DataTable dt = ds.Tables["CD_DIABETES_BASEINFO"];

            DataRow dr = dt.Rows[0];
            strTmp = dr["ManagementGroup"].ToString();
            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tGlzb=").Append(strTmp);

            strTmp = GetCaseSourceForWeb(dr["CaseSource"].ToString());

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tBlly=").Append(strTmp);

            // 家族史
            var strTmpA = dr["FamilyHistory"].ToString().Split(',');

            strTmp = "";
            foreach (var t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t.Trim()))
                {
                    sbPost.Append("&dJzs=").Append(GetFamilyHistoryForWeb(t.Trim()));
                    strTmp = "1";
                }
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            strTmpA = dr["Symptom"].ToString().Split(',');

            strTmp = "";
            foreach (var t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t.Trim()))
                {
                    if (t.Trim() == "11")
                    {
                        sbPost.Append("&tMqzz=99");
                    }
                    else
                    {
                        sbPost.Append("&tMqzz=").Append(GetSymptomForWeb(t.Trim()));
                    }
                    strTmp = "1";
                }
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            strTmp = GetDiabetesTypeForWeb(dr["DiabetesType"].ToString());

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tTnblx=").Append(strTmp);

            strTmp = dr["DiabetesTime"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            else
            {
                strTmp = Convert.ToDateTime(strTmp).ToString("yyyy-MM-dd");
            }

            sbPost.Append("&tQzsj=").Append(strTmp);

            strTmp = dr["DiabetesWork"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tQzdw=").Append(strTmp);

            strTmp = dr["Insulin"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tYds=").Append(strTmp);

            strTmp = dr["InsulinWeight"].ToString();

            sbPost.Append("&tYdsyl=").Append(strTmp);

            strTmp = dr["EnalaprilMelete"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tKfjty=").Append(strTmp);
            strTmp = dr["EndManage"].ToString();

            strTmp = strTmp == "" ? "2" : strTmp;
            sbPost.Append("&zzgl=").Append(strTmp);

            strTmp = dr["EndTime"].ToString() != "" ? Convert.ToDateTime(dr["EndTime"]).ToString("yyyy-MM-dd") : "";
            sbPost.Append("&tZzglrq=").Append(strTmp);
            sbPost.Append("&tZzly=").Append(dr["EndWhy"].ToString());

            sbPost.Append("&tnbbfz=").Append(dr["Lesions"].ToString() == "1" ? "on" : "");

            sbPost.Append("&tSzbbn=").Append(dr["RenalLesionsTime"].ToString());
            sbPost.Append("&tSjbbn=").Append(dr["NeuropathyTime"].ToString());
            sbPost.Append("&tXzbbn=").Append(dr["HeartDiseaseTime"].ToString());
            sbPost.Append("&tSwmbbn=").Append(dr["RetinopathyTime"].ToString());
            sbPost.Append("&tZbbbn=").Append(dr["FootLesionsTime"].ToString());
            sbPost.Append("&tNxgbbn=").Append(dr["CerebrovascularTime"].ToString());

            sbPost.Append("&qdqxz=").Append(pNqdqxz);
            sbPost.Append("&wzd=").Append(((25 - pNqdqxz) * 100.0 / 25).ToString("0"));

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

            //http://222.133.17.194:9080/sdcsm/diabetes/update.action
            //dGrdabh=371481020010012301&gGlkbh=4158&qdqxz=24&wzd=4&ci_sign=&tGlzb=1&tMqzzqt=&tTnblx=&tQzsj=&tQzdw=&tnbbfz=on&tSzbbn=&tSjbbn=&tXzbbn=&tSwmbbn=&tZbbbn=&tNxgbbn=&yYwmc=&yYwyf=&dSg=&dTz=&dYw=&dSsy=&dSzy=&dKfxt=&dChxt=&dGmddb=&dDmddb=&dGysz=&dZdgc=&tThxhdb=&dBmi=&tZzglrq=&happentime=2016-07-14&createtime=2016-07-14+17%3A38%3A35&updatetime=2016-07-14+17%3A41%3A42&createuser=371481B100010015&updateuser=371481B100010015&creatregion=371481B10001&pRgid=371481B10001
            returnString = web.PostHttp(baseUrl + "/diabetes/update.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
        }

        /// <summary>
        /// 新增随访
        /// </summary>
        /// <param name="ds">数据源</param>
        /// <param name="person">当前用户</param>
        /// <param name="padSFData">随访时间</param>
        private string AddTnb(DataSet ds, PersonModel pm, string padSFDate)
        {
            //view-source:http://222.133.17.194:9080/sdcsm/diabetesVisit/toAddWithDirect.action?dGrdabh=371481020010012301&isgxy=0&sfrq=
            //http://222.133.17.194:9080/sdcsm/diabetesVisit/toUpdate.action?id=71666
            WebHelper web = new WebHelper();

            string url = baseUrl + "diabetesVisit/toAddWithDirect.action?isgxy=0&sfrq=&dGrdabh=" + pm.pid;
            string returnString = web.PostHttp(url, "", "application/x-www-form-urlencoded", SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            StringBuilder sbPost = new StringBuilder();

            sbPost.Append("dGrdabh=").Append(pm.pid);

            #region 不修改栏位
            var node = doc.DocumentNode.SelectSingleNode("//input[@id='sfcs']");

            string strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&sfcs=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dXm']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dXm=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dXb']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dXb=").Append(strTmp);

            node = doc.DocumentNode.SelectSingleNode("//input[@name='dCsrq']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            sbPost.Append("&dCsrq=").Append(strTmp);

            sbPost.Append("&dSfzh=").Append(pm.idNumber);

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

            int pNqdqxz = 0;

            node = doc.DocumentNode.SelectSingleNode("//input[@name='tSfysjy']");
            strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tSfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            #endregion

            #region CD_DIABETESFOLLOWUP

            DataTable dt = ds.Tables["CD_DIABETESFOLLOWUP"];
            DataRow dr = dt.Rows[0];

            sbPost.Append("&happentime=").Append(padSFDate);

            strTmp = dr["VisitDoctor"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tSfys=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["NextVisitDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextVisitDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfsj=").Append(strTmp);

            var strTmpA = dr["visitSysptom"].ToString().Split(',');
            strTmp = "";
            foreach (string t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t) && t != "10")
                {
                    sbPost.Append("&tMqzz=").Append(GetSymptomForWeb(t));
                    strTmp = "1";
                }
            }

            sbPost.Append("&tMqzzqt=").Append(CommonExtensions.GetUrlEncodeVal(dr["SymptomOther"].ToString()));

            if (!string.IsNullOrEmpty(dr["SymptomOther"].ToString()))
            {
                sbPost.Append("&tMqzz=99");
                strTmp = "1";
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

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

            strTmp = dr["Weight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTz=").Append(strTmp);

            sbPost.Append("&dBmi=").Append(dr["BMI"]);

            // 足背动脉搏动
            strTmp = dr["DorsalisPedispulse"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            else
            {
                double dDorsalisPedispulse = 0;

                double.TryParse(dr["DorsalisPedispulse"].ToString(), out dDorsalisPedispulse);

                strTmp = dDorsalisPedispulse.ToString("0");
            }
            sbPost.Append("&dZbdmbd=").Append(strTmp);

            // 足背动脉搏动，位置
            if (strTmp == "2" || strTmp == "3")
            {
                sbPost.Append("&dZbdmbdzyc=").Append(GetDorsalisPedispulse(dr["DorsalisPedispulseType"].ToString()));
            }
            else
            {
                sbPost.Append("&dZbdmbdzyc=");
            }

            strTmp = dr["PhysicalSymptomMother"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTzqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["DailySmokeNum"].ToString();

            if (strTmp != "")
            {
                strTmp = Math.Floor(double.Parse(strTmp)).ToString();
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mXysl=").Append(strTmp);

            strTmp = dr["DailyDrinkNum"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYjsl=").Append(strTmp);

            strTmp = dr["SportTimePerWeek"].ToString();

            if (strTmp != "")
            {
                strTmp = Math.Floor(double.Parse(strTmp)).ToString();
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYdpl=").Append(strTmp);

            strTmp = dr["SportPerMinuteTime"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYdcxsj=").Append(strTmp);

            strTmp = dr["StapleFooddailyg"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dShzs=").Append(strTmp);

            strTmp = dr["PsychoAdjustment"].ToString();

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

            strTmp = dr["FPG"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dKfxt=").Append(strTmp);

            strTmp = dr["HbAlc"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tThxhdb=").Append(strTmp);

            strTmp = dr["ExamDate"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            else
            {
                strTmp = Convert.ToDateTime(strTmp).ToString("yyyy-MM-dd");
            }

            sbPost.Append("&dFzjcrq=").Append(strTmp);
            //AssistantExam
            strTmp = dr["MedicationCompliance"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tFyycx=").Append(strTmp);

            strTmp = dr["Adr"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tYwfzy=").Append(strTmp);

            strTmp = dr["AdrEx"].ToString();
            sbPost.Append("&tFzyxs=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["HypoglyceMiarreAction"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dDxtfy=").Append(strTmp);

            strTmp = dr["VisitType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tBcsffl=").Append(strTmp);

            strTmp = dr["Hight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dSg=").Append(strTmp);

            // 随访方式
            strTmp = dr["VisitWay"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tSffs=").Append(string.IsNullOrEmpty(strTmp) ? "" : strTmp.Replace("4", "99"));

            strTmp = dr["ReferralReason"].ToString();
            sbPost.Append("&tZzyuanyin=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["ReferralOrg"].ToString();
            sbPost.Append("&tZzkb=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            sbPost.Append("&dTz2=").Append(dr["TargetWeight"].ToString());
            sbPost.Append("&dBmi2=").Append(dr["BMITarget"]);

            //随机血糖 RBG  
            sbPost.Append("&dSjxt=").Append(dr["RBG"]);


            //餐后俩小时血糖  PBG   
            sbPost.Append("&dCh2xsxt=").Append(dr["PBG"]);

            double ddTmp = 0;
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
            if (dr["SportTimePerWeekTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["SportTimePerWeekTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYdpl2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYdpl2=");
            }

            ddTmp = 0;
            if (dr["SportPerMinuteTimeTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["SportPerMinuteTimeTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYdcxsj2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYdcxsj2=");
            }

            sbPost.Append("&dShzs2=").Append(dr["StapleFooddailygTarget"].ToString());
            //随访医生建议
            string str2 = dr["DoctorView"].ToString();
            if (!string.IsNullOrEmpty(str2))
            {
                sbPost.Append("&tSfysjy=").Append(CommonExtensions.GetUrlEncodeVal(str2));
            }
            else
            {
                sbPost.Append("&tSfysjy=");
            }

            #region 2017-10-20添加
            sbPost.Append("&xybglcs=").Append(dr["NextMeasures"].ToString());
            sbPost.Append("&tYdszl=").Append(CommonExtensions.GetUrlEncodeVal(dr["InsulinType"].ToString()));
            sbPost.Append("&tYdsyl=").Append(CommonExtensions.GetUrlEncodeVal(dr["InsulinUsage"].ToString()));
            //胰岛素调整
            sbPost.Append("&tYdszl1=").Append(CommonExtensions.GetUrlEncodeVal(dr["InsulinAdjustType"].ToString()));
            sbPost.Append("&tYdsyl1=").Append(CommonExtensions.GetUrlEncodeVal(dr["InsulinAdjustUsage"].ToString()));
            sbPost.Append("&gZzjl=").Append(dr["IsReferral"]);
            sbPost.Append("&zzlxrjdh=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralContacts"].ToString()));
            sbPost.Append("&zzjieguo=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralResult"].ToString()));
            sbPost.Append("&remark=").Append(CommonExtensions.GetUrlEncodeVal(dr["Remarks"].ToString()));
            #endregion

            #endregion

            #region CD_DRUGCONDITION

            string tzyy = "2";
            dt = ds.Tables["CD_DRUGCONDITION"];
            if (dt != null && dt.Rows.Count > 0)
            {
                var drs = dt.Select("Type=2");
                //用药
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
                drs = dt.Select("Type=8");
                tzyy = "";
                if (drs.Length > 0)
                {
                    foreach (var row in drs)
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
            sbPost.Append("&wzd=").Append(((26 - pNqdqxz) * 100.0 / 26).ToString("#"));

            // （手动）居民签名
            sbPost.Append("&sdjmqm=").Append(CommonExtensions.GetUrlEncodeVal(pm.memberName));

            //新增
            returnString = web.PostHttp(baseUrl + "diabetesVisit/add.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return "新增失败！";
            }

            doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null || doc.DocumentNode.SelectSingleNode("//body/script[last()]") == null)
            {
                //return "新增失败！";
            }
            else
            {
                var returnNode = doc.DocumentNode.SelectSingleNode("//body/script[last()]");

                if (returnNode.InnerText.IndexOf("'add' == \"add\"") == -1)
                {
                    CommonExtensions.WriteLog(returnString);
                    //return "新增失败！";
                }
            }

            return "";
        }

        /// <summary>
        /// 更新随访
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pm"></param>
        /// <param name="padSFDate"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private string UpdateTnb(DataSet ds, PersonModel pm, string padSFDate, string key)
        {
            //http://222.133.17.194:9080/sdcsm/diabetesVisit/toUpdate.action?id=71666
            WebHelper web = new WebHelper();

            string url = baseUrl + "diabetesVisit/toUpdate.action?id=" + key;
            string returnString = web.PostHttp(url, "", "application/x-www-form-urlencoded", SysCookieContainer);

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

            sbPost.Append("&dSfzh=").Append(pm.idNumber);

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

            #endregion

            #region CD_DIABETESFOLLOWUP

            DataTable dt = ds.Tables["CD_DIABETESFOLLOWUP"];
            DataRow dr = dt.Rows[0];

            strTmp = dr["VisitDoctor"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&tSfys=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["NextVisitDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextVisitDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfsj=").Append(strTmp);

            var strTmpA = dr["visitSysptom"].ToString().Split(',');
            strTmp = "";
            foreach (string t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t) && t != "10")
                {
                    sbPost.Append("&tMqzz=").Append(GetSymptomForWeb(t));
                    strTmp = "1";
                }
            }

            sbPost.Append("&tMqzzqt=").Append(CommonExtensions.GetUrlEncodeVal(dr["SymptomOther"].ToString()));
            //随访医生建议
            string str2 = dr["DoctorView"].ToString();
            if (!string.IsNullOrEmpty(str2))
            {
                sbPost.Append("&tSfysjy=").Append(CommonExtensions.GetUrlEncodeVal(str2));
            }
            else
            {
                sbPost.Append("&tSfysjy=");
            }

            if (!string.IsNullOrEmpty(dr["SymptomOther"].ToString()))
            {
                sbPost.Append("&tMqzz=99");
                strTmp = "1";
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

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

            strTmp = dr["Weight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTz=").Append(strTmp);

            sbPost.Append("&dBmi=").Append(dr["BMI"]);

            // 足背动脉搏动
            strTmp = dr["DorsalisPedispulse"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            else
            {
                double dDorsalisPedispulse = 0;

                double.TryParse(dr["DorsalisPedispulse"].ToString(), out dDorsalisPedispulse);

                strTmp = dDorsalisPedispulse.ToString("0");
            }
            sbPost.Append("&dZbdmbd=").Append(strTmp);

            // 足背动脉搏动，位置
            if (strTmp == "2" || strTmp == "3")
            {
                sbPost.Append("&dZbdmbdzyc=").Append(GetDorsalisPedispulse(dr["DorsalisPedispulseType"].ToString()));
            }
            else
            {
                sbPost.Append("&dZbdmbdzyc=");
            }

            strTmp = dr["PhysicalSymptomMother"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTzqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["DailySmokeNum"].ToString();

            if (strTmp != "")
            {
                strTmp = Math.Floor(double.Parse(strTmp)).ToString();
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mXysl=").Append(strTmp);

            strTmp = dr["DailyDrinkNum"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYjsl=").Append(strTmp);

            strTmp = dr["SportTimePerWeek"].ToString();

            if (strTmp != "")
            {
                strTmp = Math.Floor(double.Parse(strTmp)).ToString();
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYdpl=").Append(strTmp);

            strTmp = dr["SportPerMinuteTime"].ToString();

            if (strTmp != "")
            {
                strTmp = Math.Floor(double.Parse(strTmp)).ToString();
            }

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&mYdcxsj=").Append(strTmp);

            strTmp = dr["StapleFooddailyg"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dShzs=").Append(strTmp);

            strTmp = dr["PsychoAdjustment"].ToString();

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

            strTmp = dr["FPG"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dKfxt=").Append(strTmp);

            strTmp = dr["HbAlc"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tThxhdb=").Append(strTmp);

            strTmp = dr["ExamDate"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            else
            {
                strTmp = Convert.ToDateTime(strTmp).ToString("yyyy-MM-dd");
            }
            sbPost.Append("&dFzjcrq=").Append(strTmp);
            //AssistantExam
            strTmp = dr["MedicationCompliance"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tFyycx=").Append(strTmp);

            strTmp = dr["Adr"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tYwfzy=").Append(strTmp);

            strTmp = dr["AdrEx"].ToString();
            sbPost.Append("&tFzyxs=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            strTmp = dr["HypoglyceMiarreAction"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dDxtfy=").Append(strTmp);

            strTmp = dr["VisitType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tBcsffl=").Append(strTmp);

            strTmp = dr["Hight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dSg=").Append(strTmp);

            // 随访方式
            strTmp = dr["VisitWay"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&tSffs=").Append(string.IsNullOrEmpty(strTmp) ? "" : strTmp.Replace("4", "99"));

            strTmp = dr["ReferralReason"].ToString();
            //if (!string.IsNullOrEmpty(strTmp))
            //{
            //    sbPost.Append("&gZzjl=1");
            //}
            //else
            //{
            //    sbPost.Append("&gZzjl=2");
            //}
            sbPost.Append("&tZzyuanyin=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            strTmp = dr["ReferralOrg"].ToString();
            sbPost.Append("&tZzkb=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            sbPost.Append("&dTz2=").Append(dr["TargetWeight"].ToString());
            sbPost.Append("&dBmi2=").Append(dr["BMITarget"]);

            //随机血糖 RBG  
            sbPost.Append("&dSjxt=").Append(dr["RBG"]);


            //餐后俩小时血糖  PBG   
            sbPost.Append("&dCh2xsxt=").Append(dr["PBG"]);


            double ddTmp = 0;

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
            if (dr["SportTimePerWeekTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["SportTimePerWeekTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYdpl2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYdpl2=");
            }

            ddTmp = 0;
            if (dr["SportPerMinuteTimeTarget"].ToString().Trim() != "")
            {
                double.TryParse(dr["SportPerMinuteTimeTarget"].ToString(), out ddTmp);
                sbPost.Append("&mYdcxsj2=").Append(ddTmp.ToString("0"));
            }
            else
            {
                sbPost.Append("&mYdcxsj2=");
            }

            sbPost.Append("&dShzs2=").Append(dr["StapleFooddailygTarget"].ToString());

            // 随访医生建议
            strTmp = dr["DoctorView"].ToString();
            sbPost.Append("&tSfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            #region 2017-10-20添加

            sbPost.Append("&xybglcs=").Append(dr["NextMeasures"].ToString());
            sbPost.Append("&tYdszl=").Append(CommonExtensions.GetUrlEncodeVal(dr["InsulinType"].ToString()));
            sbPost.Append("&tYdsyl=").Append(CommonExtensions.GetUrlEncodeVal(dr["InsulinUsage"].ToString()));
            //胰岛素调整
            sbPost.Append("&tYdszl1=").Append(CommonExtensions.GetUrlEncodeVal(dr["InsulinAdjustType"].ToString()));
            sbPost.Append("&tYdsyl1=").Append(CommonExtensions.GetUrlEncodeVal(dr["InsulinAdjustUsage"].ToString()));
            sbPost.Append("&gZzjl=").Append(dr["IsReferral"]);
            sbPost.Append("&zzlxrjdh=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralContacts"].ToString()));
            sbPost.Append("&zzjieguo=").Append(CommonExtensions.GetUrlEncodeVal(dr["ReferralResult"].ToString()));
            sbPost.Append("&remark=").Append(CommonExtensions.GetUrlEncodeVal(dr["Remarks"].ToString()));

            #endregion
            #endregion

            #region CD_DRUGCONDITION

            dt = ds.Tables["CD_DRUGCONDITION"];
            string tzyy = "2";
            if (dt != null && dt.Rows.Count > 0)
            {
                var drs = dt.Select("Type=2");
                //用药
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
                drs = dt.Select("Type=8");
                tzyy = "";
                if (drs.Length > 0)
                {
                    foreach (var row in drs)
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
            sbPost.Append("&wzd=").Append(((26 - pNqdqxz) * 100.0 / 26).ToString("0"));

            // （手动）居民签名
            sbPost.Append("&sdjmqm=").Append(CommonExtensions.GetUrlEncodeVal(pm.memberName));

            //修改
            returnString = web.PostHttp(baseUrl + "diabetesVisit/update.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return "更新失败！";
            }

            doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null || doc.DocumentNode.SelectSingleNode("//body/script[last()]") == null)
            {
                //return "更新失败！";
            }
            else
            {
                var returnNode = doc.DocumentNode.SelectSingleNode("//body/script[last()]");

                if (returnNode.InnerText.IndexOf("'update' == \"update\"") == -1)
                {
                    CommonExtensions.WriteLog(returnString);
                    //return "更新失败！";
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
            callback("下载-糖尿病信息..." + currentIndex + "/" + totalRows);

            if (pm != null && !string.IsNullOrEmpty(pm.pid))
            {
                TryDownTnbByIDs(pm, 1, callback);
            }

            currentIndex++;
        }

        /// <summary>
        ///  尝试3次下载
        /// </summary>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param> 
        private void TryDownTnb(int tryCount, Action<string> callback)
        {
            try
            {
                GetTnbKeyAndInfo(callback);
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
                    TryDownTnb(tryCount, callback);
                }
                else
                {
                    callback("EX-下载糖尿病信息失败，请确保网路畅通。");
                }
            }
        }

        private void TryDownTnbByIDs(PersonModel person, int tryCount, Action<string> callback)
        {
            string idcard = person.idNumber.ToString();

            try
            {
                GetTnbInfo(person);
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
                    callback("EX-糖尿病信息:身份证[" + idcard + "],姓名[" + person.memberName + "]:下载信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;

                    TryDownTnbByIDs(person, tryCount, callback);
                }
                else
                {
                    callback("EX-糖尿病信息:身份证[" + idcard + "],姓名[" + person.memberName + "]:下载信息失败。请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 获取糖尿病key和页面信息
        /// </summary>
        /// <param name="callback"></param>
        private void GetTnbKeyAndInfo(Action<string> callback)
        {
            int PageSum = 0;
            List<PersonModel> personList = GetTnbKeyAndInfo(callback, out PageSum);

            //调方法，便利当前页的表示，获取信息
            GetInfoByPersonList(personList, callback);

            for (int i = 2; i <= PageSum; i++)
            {
                personList.Clear();

                //调方法，遍历当前页标示，获取信息
                personList = this.GetPageNumKeyInfo(i, callback);

                GetInfoByPersonList(personList, callback);
            }
        }

        /// <summary>
        /// 获取指定页码的key和页面信息
        /// </summary>
        /// <param name="m"></param>
        /// <param name="callback"></param>
        private List<PersonModel> GetTnbKeyAndInfo(Action<string> callback, out int pageSum)
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

            string postData = "search=2&siteid=" + key + "&pRgid=&branch=on&dqjg=" + key + "&dXm=&dXb=&grdabhShow=&dSfzh=&dSspq=&dZy=&dYlbxh=&glzb=&dCsrq1=&dCsrq2=&dDazt=1&createuser=&createtime1=&createtime2=&dJd=&dJwh=&dXxdz=&zzgl=&branch=on";
            string returnString = web.PostHttp(baseUrl + "/diabetes/list.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
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
        /// 获取糖尿病信息下载
        /// </summary>
        private void GetInfoByPersonList(List<PersonModel> lstAllPm, Action<string> callback)
        {
            foreach (var pm in lstAllPm)
            {
                TryDownTnbByIDs(pm, 1, callback);

                callback("下载-糖尿病信息..." + currentIndex + "/" + totalRows);

                currentIndex++;
            }
        }

        /// <summary>
        /// 根据标识信息，获取信息，下载
        /// </summary>
        /// <param name="pm"></param>
        private void GetTnbInfo(PersonModel pm)
        {
            DataSet ds = DataSetTmp.TnbDataSet; //数据库表架构
            DataSet dsSave = new DataSet();
            DataTable dtData = null;
            CommonBusiness.CommonDAOBusiness dao = new CommonBusiness.CommonDAOBusiness();
            //获取专档建立，确诊时间
            WebHelper web = new WebHelper();


            // dsSave.Tables.Add(dtInfo);

            List<SFClass> lstSF = GetSFxxLst(pm.pid);

            if (lstSF.Count > 0)
            {
                string postData = "";

                #region  CD_DIABETESFOLLOWUP 随访
                SFClass sf = lstSF[0];
                //http://222.133.17.194:9080/sdcsm/diabetesVisit/toUpdate.action?id=71475
                //获取随访类表
                postData = "id=" + lstSF[0].key;
                string returnString = web.PostHttp(baseUrl + "/diabetesVisit/toUpdate.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                dtData = ds.Tables["CD_DIABETESFOLLOWUP"].Clone();
                DataRow dr = dtData.NewRow();

                dr["IDCardNo"] = pm.idNumber;
                dr["CustomerName"] = pm.memberName;
                var node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                dr["VisitDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='tSfys']");
                dr["VisitDoctor"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value; //随访医生
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXcsfsj']");
                dr["NextVisitDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //随机血糖
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSjxt']");
                dr["RBG"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //餐后俩小时血糖
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dCh2xsxt']");
                dr["PBG"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                var nodes = doc.DocumentNode.SelectNodes("//input[@name='tMqzz'][@checked]");

                string strtmp = "";

                if (nodes != null)
                {
                    foreach (var no in nodes)
                    {
                        if (no.Attributes.Contains("value"))
                        {
                            string noV = no.Attributes["value"].Value;

                            if (noV != "99")
                            {
                                strtmp += "," + GetSymptom(noV);
                            }
                        }
                    }
                }

                dr["Symptom"] = strtmp.TrimStart(',');

                node = doc.DocumentNode.SelectSingleNode("//input[@id='tMqzzqt']");
                dr["SymptomOther"] = node == null || !node.Attributes.Contains("value") ? " " : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSsy']");
                dr["Hypertension"] = node == null || !node.Attributes.Contains("value") ? " " : node.Attributes["value"].Value;
                //身高
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSg']");
                dr["Hight"] = node == null || !node.Attributes.Contains("value") ? " " : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSzy']");
                dr["Hypotension"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dTz']");
                dr["Weight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //  node = doc.DocumentNode.SelectSingleNode("//input[@name='dSg']");
                // dr["Height"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dBmi']");
                dr["BMI"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                // 足背动脉搏动
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dZbdmbd']/option[@selected]");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["DorsalisPedispulse"] = strtmp;

                // 足背动脉搏动，减弱、消失位置
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dZbdmbdzyc']/option[@selected]");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["DorsalisPedispulseType"] = GetDorsalisPedispulse(strtmp);

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dTzqt']");
                dr["PhysicalSymptomMother"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl']");
                dr["DailySmokeNum"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl']");
                dr["DailyDrinkNum"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl']");
                dr["SportTimePerWeek"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj']");
                dr["SportPerMinuteTime"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dShzs']");
                dr["StapleFooddailyg"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dXltz']/option[@selected]");
                dr["PsychoAdjustment"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dZyxw']/option[@selected]");
                dr["ObeyDoctorBehavior"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dKfxt']");
                dr["FPG"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='tThxhdb']");
                dr["HbAlc"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dFzjcrq']");
                dr["ExamDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                // 服药依从性
                node = doc.DocumentNode.SelectSingleNode("//select[@id='tFyycx']/option[@selected]");
                dr["MedicationCompliance"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value.Replace("4", "");

                node = doc.DocumentNode.SelectSingleNode("//input[@name='tYwfzy'][@checked]");
                dr["Adr"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='tFzyxs']");
                dr["AdrEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dDxtfy']/option[@selected]");
                dr["HypoglyceMiarreAction"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //随访分类
                var node3 = doc.DocumentNode.SelectNodes("//input[@name='tBcsffl'][@checked]");
                string fl = "";
                if (node3 != null)
                {
                    foreach (var item in node3)
                    {
                        fl = item.Attributes["value"].Value;
                        break;
                    }
                }
                //f2 += fl;

                //node3 = doc.DocumentNode.SelectNodes("//input[@name='gMbbfz'][@checked]");
                //int dq = 0;
                //fl = "";
                //if (node3 != null)
                //{
                //    foreach (var item in node3)
                //    {
                //        if (int.TryParse(item.Attributes["value"].Value, out dq))
                //        {
                //            f2 += (dq + 4).ToString();
                //        }
                //        //  f2 += item.Attributes["value"].Value + ",";
                //    }
                //}
                dr["VisitType"] = fl.TrimEnd(',');

                //是否转诊
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gZzjl'][@checked]");
                dr["IsReferral"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@id='tYdszl']");
                dr["InsulinType"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value; //胰岛素
                node = doc.DocumentNode.SelectSingleNode("//input[@id='tYdsyl']");
                dr["InsulinUsage"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='tSffs'][@checked]");
                dr["VisitWay"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["VisitWay"] = dr["VisitWay"].ToString() == "99" ? "4" : dr["VisitWay"].ToString();

                node = doc.DocumentNode.SelectSingleNode("//input[@id='tZzyuanyin']");
                dr["ReferralReason"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='tZzkb']");
                dr["ReferralOrg"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dTz2']");
                dr["TargetWeight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dBmi2']");
                dr["BMITarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl2']");
                dr["DailySmokeNumTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl2']");
                dr["DailyDrinkNumTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl2']");
                dr["SportTimePerWeekTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj2']");
                dr["SportPerMinuteTimeTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dShzs2']");
                dr["StapleFooddailygTarget"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //随访医生建议
                node = doc.DocumentNode.SelectSingleNode("//input[@name='tSfysjy']");
                dr["DoctorView"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                #region 2017-10-20 添加

                node = doc.DocumentNode.SelectSingleNode("//input[@name='xybglcs'][@checked]");
                dr["NextMeasures"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='zzlxrjdh']");
                dr["ReferralContacts"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//select[@id='zzjieguo']/option[@selected]");
                dr["ReferralResult"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='remark']");
                dr["Remarks"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='tYdszl1']");
                dr["InsulinAdjustType"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='tYdsyl1']");
                dr["InsulinAdjustUsage"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                #endregion

                dtData.Rows.Add(dr);
                //   dsSave.Tables.Add(dtData);
                string sfrq = Convert.ToDateTime(dr["VisitDate"].ToString()).ToString("yyyy-MM-dd");
                outkey = dao.SaveMainTable(dtData, pm.idNumber, sfrq);

                dtData = ds.Tables["CD_DRUGCONDITION"].Clone();

                // 用药
                nodes = doc.DocumentNode.SelectNodes("//tbody[@id='dyTbody']/tr[position()>1]");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gJyy'][@checked]");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                if (nodes != null && strtmp == "1")
                {
                    #region  CD_DRUGCONDITION

                    foreach (var t in nodes)
                    {
                        var tmpNode = t.SelectSingleNode("td/input[@name='yYwmc']");

                        dr = dtData.NewRow();

                        dr["IDCardNo"] = pm.idNumber;
                        dr["Type"] = "2";
                        dr["OutKey"] = outkey.ToString();
                        dr["Name"] = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;


                        tmpNode = t.SelectSingleNode("td/input[@name='yYwyf']");
                        int pN = 0;

                        /*int.TryParse(t.b04_011_04, out pN);
                        dr["DailyTime"] = pN;*/
                        pN = 0;

                        string tmpET = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;
                        //int.TryParse(tmpET, out pN);
                        dr["DosAge"] = tmpET;
                        dtData.Rows.Add(dr);
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

                    foreach (var t in nodes)
                    {
                        var tmpNode = t.SelectSingleNode("td/input[@name='yYwmctz']");

                        dr = dtData.NewRow();

                        dr["IDCardNo"] = pm.idNumber;
                        dr["Type"] = "8";
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
                        dtData.Rows.Add(dr);
                    }

                    #endregion
                }

                dsSave.Tables.Add(dtData);
                #endregion

                #region  CD_DIABETES_BASEINFO
                string url = baseUrl + "diabetes/toUpdate.action?dGrdabh=" + pm.pid;
                returnString = web.GetHttp(url, "", SysCookieContainer);

                doc = HtmlHelper.GetHtmlDocument(returnString);

                DataTable dtInfo = ds.Tables["CD_DIABETES_BASEINFO"].Clone();

                DataRow dr2 = dtInfo.NewRow();

                dr2["IDCardNo"] = pm.idNumber;
                //dr2["OutKey"] = outkey.ToString();
                dr2["RecordID"] = pm.pid.Substring(0, 17);

                //管理组
                var node2 = doc.DocumentNode.SelectSingleNode("//input[@name='tGlzb'][@checked]");
                dr2["ManagementGroup"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //病历来源
                node2 = doc.DocumentNode.SelectSingleNode("//input[@name='tBlly'][@checked]");
                string strtmp2 = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                dr2["CaseSource"] = GetCaseSource(strtmp2);

                //家族史
                var nodes2 = doc.DocumentNode.SelectNodes("//input[@name='dJzs'][@checked]");
                string fatherHistory = "";

                if (nodes2 != null)
                {
                    foreach (var no in nodes2)
                    {
                        if (no.Attributes.Contains("value"))
                        {
                            string temStr = no.Attributes["value"].Value;
                            switch (temStr)
                            {
                                case "1":
                                    temStr = "1";
                                    break;
                                case "2":
                                    temStr = "2";
                                    break;
                                case "3":
                                    temStr = "3";
                                    break;
                                case "4":
                                    temStr = "4";
                                    break;
                                case "98":
                                    temStr = "5";
                                    break;
                                case "99":
                                    temStr = "6";
                                    break;
                                case "100":
                                    temStr = "7";
                                    break;
                                default:
                                    temStr = "6";
                                    break;
                            }
                            fatherHistory += "," + temStr;
                        }
                    }

                    fatherHistory = fatherHistory.TrimStart(',');
                }

                dr2["FamilyHistory"] = fatherHistory;

                //目前症状
                string symptom = "";
                nodes2 = doc.DocumentNode.SelectNodes("//input[@id='tMqzz'][@checked]");

                if (nodes2 != null)
                {
                    foreach (var no in nodes2)
                    {
                        if (no.Attributes.Contains("value"))
                        {
                            string temStr = no.Attributes["value"].Value;
                            switch (temStr)
                            {
                                case "0":
                                    temStr = "1";
                                    break;
                                case "1":
                                    temStr = "2";
                                    break;
                                case "2":
                                    temStr = "3";
                                    break;
                                case "3":
                                    temStr = "4";
                                    break;
                                case "4":
                                    temStr = "5";
                                    break;
                                case "5":
                                    temStr = "6";
                                    break;
                                case "6":
                                    temStr = "7";
                                    break;
                                case "7":
                                    temStr = "8";
                                    break;
                                case "8":
                                    temStr = "9";
                                    break;
                                case "99":
                                    temStr = "10";
                                    break;
                                default:
                                    temStr = "1";
                                    break;
                            }
                            symptom += "," + temStr;
                        }
                    }

                    symptom = symptom.TrimStart(',');
                }

                dr2["Symptom"] = symptom;

                //糖尿病类型
                string diabetesType = "";
                node2 = doc.DocumentNode.SelectSingleNode("//select[@id='tTnblx']/option[@selected]");
                if (node2 != null && node2.Attributes.Contains("value"))
                {
                    diabetesType = node2.Attributes["value"].Value;

                    if (diabetesType == "99")
                    {
                        diabetesType = "6";
                    }
                }

                dr2["DiabetesType"] = diabetesType;

                //确诊时间
                node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tQzsj']");
                dr2["DiabetesTime"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //确诊单位
                node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tQzdw']");
                dr2["DiabetesWork"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //糖尿病并发症
                nodes2 = doc.DocumentNode.SelectNodes("//input[@id='tnbbfz'][@checked]");

                if (nodes2 != null)
                {
                    dr2["Lesions"] = "1";
                }
                else
                {
                    dr2["Lesions"] = "2";

                    //肾脏病变
                    node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tSzbbn']");
                    dr2["RenalLesionsTime"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                    //神经病变
                    node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tSjbbn']");
                    dr2["NeuropathyTime"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                    //心脏病变
                    node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tXzbbn']");
                    dr2["HeartDiseaseTime"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                    //视网膜病变
                    node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tSwmbbn']");
                    dr2["RetinopathyTime"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                    //足部病变
                    node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tZbbbn']");
                    dr2["FootLesionsTime"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                    //脑血管病变
                    node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tNxgbbn']");
                    dr2["CerebrovascularTime"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                }

                //胰岛素使用
                node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tYds'][@checked]");
                dr2["Insulin"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //胰岛素使用量
                node2 = doc.DocumentNode.SelectSingleNode("//input[@id='tYdsyl']");
                dr2["InsulinWeight"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                //是否终止管理
                node2 = doc.DocumentNode.SelectSingleNode("//input[@id='zzgl'][@checked]");
                dr2["EndManage"] = node2 == null || !node2.Attributes.Contains("value") ? "" : node2.Attributes["value"].Value;

                dtInfo.Rows.Add(dr2);
                #endregion

                dao.SaveDataSet(dsSave, pm.idNumber, "2,8", outkey.ToString());

                dsSave.Tables.Clear();
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

            string postData = "page.currentPage=" + pageNum + "&status=ajax&search=2&siteid=" + key + "&pRgid=&branch=on&dqjg=" + key + "&dXm=&dXb=&grdabhShow=&dSfzh=&dSspq=&dZy=&dYlbxh=&glzb=&dCsrq1=&dCsrq2=&dDazt=1&createuser=&createtime1=&createtime2=&dJd=&dJwh=&dXxdz=&zzgl=&branch=on";
            string returnString = web.PostHttp(baseUrl + "/diabetes/list.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
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
        #endregion

        #region 栏位对应

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

        private string GetDorsalisPedispulse(string code)
        {
            code = code.Trim();
            string returnVal = "";

            switch (code)
            {
                case "1":
                    returnVal = "2";
                    break;
                case "2":
                    returnVal = "1";
                    break;
                case "3":
                    returnVal = "3";
                    break;
            }

            return returnVal;
        }

        private string GetCaseSource(string code)
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

        private string GetCaseSourceForWeb(string code)
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

        private string GetFamilyHistoryForWeb(string code)
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

        private string GetDiabetesTypeForWeb(string code)
        {
            code = code.Trim();
            string returnVal = "";

            switch (code)
            {
                case "6":
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
            //http://222.133.17.194:9080/sdcsm/hypertensionVisit/toAdd.action?dGrdabh=371481010100071701
            //http://222.133.17.194:9080/sdcsm/diabetesVisit/toAdd.action?dGrdabh=371481020010012301
            WebHelper web = new WebHelper();

            string returnString = web.PostHttp(baseUrl + "/diabetesVisit/toAdd.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);


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
