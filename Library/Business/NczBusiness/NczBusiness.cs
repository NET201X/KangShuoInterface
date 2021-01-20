using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Net;
using Model.InfoModel;
using DAL;
using HtmlAgilityPack;
using Utilities.Common;

namespace NczBusiness
{
    public class NczBusiness
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

        public int outkey = 0;

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


        #endregion

        /// <summary>
        /// 根据身份证号下载数据入口
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="callback"></param>
        public void DownByIDs(string ids, Action<string> callback)
        {
            try
            {
                var idsa = ids.Split(',');
                int cIndex = 1;
                foreach (string id in idsa)
                {
                    if (id == "")
                    {
                        callback("下载-脑卒中信息..." + cIndex + "/" + idsa.Length);
                        cIndex++;
                        continue;
                    }
                    CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                    PersonModel person = cb.GetGrdaByIDCardNo(id, loginkey, SysCookieContainer);

                    if (person != null && !string.IsNullOrEmpty(person.pid))
                    {
                        TryDownByIDs(person, 1, callback);
                    }

                    callback("下载-脑卒中信息..." + cIndex + "/" + idsa.Length);
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
        /// 脑卒中全部下载入口
        /// </summary>
        /// <param name="callback"></param>
        public void DownInfo(Action<string> callback)
        {
            currentIndex = 1;

            TryDownInfo(1, callback);
            GC.Collect();
        }

        public void SaveInfo(Action<string> callback)
        {
            int PcurrentIndex = 1;

            foreach (DataSet ds in lstUploadData)
            {
                TrySave(ds, 1, callback);

                callback("上传-脑卒中信息..." + PcurrentIndex + "/" + lstUploadData.Count);
                PcurrentIndex++;
            }
        }

        #region 上传

        /// <summary>
        /// 尝试3次
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        private void TrySave(DataSet ds, int tryCount, Action<string> callback)
        {
            string idcard = "";
            try
            {

                DataTable dtVisit = ds.Tables["CD_STROKE_FOLLOWUP"];

                if (dtVisit == null || dtVisit.Rows.Count <= 0)
                {
                    return;
                }

                idcard = dtVisit.Rows[0]["IDCardNo"].ToString();

                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                PersonModel pm = cb.GetGrdaByIDCardNo(idcard, loginkey, SysCookieContainer);

                if (pm == null || string.IsNullOrEmpty(pm.pid))
                {
                    callback("EX-脑卒中信息:身份证[" + idcard + "]:平台尚未建档或者档案状态为非活动!");
                    return;
                }

                List<SFClass> lstSF = GetSFxxLst(pm.pid);

                string padSFDate = Convert.ToDateTime(dtVisit.Rows[0]["FollowupDate"]).ToString("yyyy-MM-dd");

                var sfInfo = lstSF.Where(m => m.sfDate == padSFDate).ToList();

                string msg = "";

                // 修改
                if (sfInfo.Count > 0)
                {
                    msg = EditInfo(ds, pm, padSFDate, sfInfo[0].key);
                }
                else
                {
                    msg = AddInfo(ds, pm, padSFDate);
                }

                if (!string.IsNullOrEmpty(msg))
                {
                    callback("EX-脑卒中信息:身份证[" + idcard + "]:" + msg);
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
                    TrySave(ds, tryCount, callback);
                }
                else
                {
                    callback("EX-上传脑卒中信息失败，请确保网路畅通");
                }
            }
        }

        /// <summary>
        /// 新增随访
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pm"></param>
        /// <param name="padSFDate"></param>
        private string AddInfo(DataSet ds, PersonModel pm, string padSFDate)
        {
            //http://20.1.1.124:9000/sdcsm/strokeVisit/toAdd.action?dGrdabh=371482110010115101
            WebHelper web = new WebHelper();
            string url = baseUrl + "strokeVisit/toAdd.action?dGrdabh=" + pm.pid;
            string returnString = web.GetHttp(url, "", SysCookieContainer);

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

            /*node = doc.DocumentNode.SelectSingleNode("//input[@name='happentime']");
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

            int pNqdqxz = 0;

            #region 2.0 屏蔽
            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nNzzlx'][@checked]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&nNzzlx=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//select[@name='nNzzbw']/option[@selected]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&nNzzbw=").Append(strTmp);

            //var nodes = doc.DocumentNode.SelectNodes("//input[@name='nGrbs'][@checked]");

            //if (nodes == null)
            //{
            //    pNqdqxz++;
            //}
            //else
            //{
            //    foreach (var n in nodes)
            //    {
            //        if (n.Attributes.Contains("value"))
            //        {
            //            sbPost.Append("&nGrbs=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='nBfzqk'][@checked]");

            //if (nodes == null)
            //{
            //    pNqdqxz++;
            //}
            //else
            //{
            //    foreach (var n in nodes)
            //    {
            //        if (n.Attributes.Contains("value"))
            //        {
            //            sbPost.Append("&nBfzqk=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nBfzqkqt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //sbPost.Append("&nBfzqkqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='nXfzzzz'][@checked]");

            //if (nodes == null)
            //{
            //    pNqdqxz++;
            //}
            //else
            //{
            //    foreach (var n in nodes)
            //    {
            //        if (n.Attributes.Contains("value"))
            //        {
            //            sbPost.Append("&nXfzzzz=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nXfzzzzqt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&nXfzzzzqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//select[@name='mShzlnl']/option[@selected]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&mShzlnl=").Append(strTmp);

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='nKfzlfs'][@checked]");

            //if (nodes == null)
            //{
            //    pNqdqxz++;
            //}
            //else
            //{
            //    foreach (var n in nodes)
            //    {
            //        if (n.Attributes.Contains("value"))
            //        {
            //            sbPost.Append("&nKfzlfs=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nKfzlfsqt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //sbPost.Append("&nKfzlfsqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&mXysl=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&mYjsl=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&mYdpl=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&mYdcxsj=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dBmi']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //sbPost.Append("&dBmi=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dYw']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&dYw=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dKfxt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&dKfxt=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//select[@name='nZtknhf']/option[@selected]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&nZtknhf=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nBcsfysjy']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&nBcsfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            #endregion


 
            #endregion

            #region CD_HYPERTENSIONFOLLOWUP

            DataTable dt = ds.Tables["CD_STROKE_FOLLOWUP"];

            DataRow dr = dt.Rows[0];

            sbPost.Append("&happentime=").Append(padSFDate);

            strTmp = dr["Height"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dSg=").Append(strTmp);


            strTmp = dr["NextFollowupDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextFollowupDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfsj=").Append(strTmp);

            strTmp = dr["FollowupDoctor"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&nSfys=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //随访医生建议
            strTmp = dr["DoctorView"].ToString();

            if (!string.IsNullOrEmpty(strTmp))
            {
                sbPost.Append("&nBcsfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            }
            else
            {
                sbPost.Append("&nBcsfysjy=");
            }


            var strTmpA = dr["Symptom"].ToString().Split(',');
            strTmp = "";
            foreach (string t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t))
                {
                    sbPost.Append("&nMqzz=").Append(GetSymptomForWeb(t));
                    strTmp = "1";
                }
            }
            sbPost.Append("&nMqzzqt=").Append(CommonExtensions.GetUrlEncodeVal(dr["SymptomOther"].ToString()));

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            strTmp = dr["Hypertension"].ToString();

            double dtmp = 0;

            if (strTmp != "" && double.TryParse(strTmp, out dtmp))
            {
                strTmp = dtmp.ToString("#");
            }

            sbPost.Append("&dSsy=").Append(strTmp);

            string strTmp2 = dr["Hypotension"].ToString();

            if (string.IsNullOrEmpty(strTmp) || string.IsNullOrEmpty(strTmp2))
            {
                pNqdqxz++;
            }

            if (strTmp2 != "" && double.TryParse(strTmp2, out dtmp))
            {
                strTmp2 = dtmp.ToString("#");
            }

            sbPost.Append("&dSzy=").Append(strTmp2);


            strTmp = dr["Weight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTz=").Append(strTmp);

            strTmp = dr["MedicationCompliance"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&nFyycx=").Append(strTmp);

            strTmp = dr["FollowupType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&nCcsffl=").Append(strTmp);

            strTmp = dr["FollowupWay"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&nSffs=").Append(strTmp);

            #region 2.0 新增

            //此次随访分类其他 并发症 nCcsfflbfz 
            sbPost.Append("&nCcsfflbfz=").Append(HtmlHelper.GetUrlEncodeVal(dr["FollowupTypeOther"].ToString()));

            //脑卒中类型 nNzzlx 
            sbPost.Append("&nNzzlx=").Append(dr["StrokeType"].ToString());

            //脑卒中部位 Strokelocation 
            string bw = dr["Strokelocation"].ToString();
            int d2 = 0;
            if (!string.IsNullOrWhiteSpace(bw) && int.TryParse(bw,out d2) && !bw.Contains("0")) {
                bw = (d2 - 1).ToString(); 
            }
            sbPost.Append("&nNzzbw=").Append(bw.Replace("0",""));

            //个人病史 MedicalHistory
            bw = dr["MedicalHistory"].ToString();
            d2 = 0;
            var st2 = bw.Split(',');
            foreach (var item in st2)
            {
                if (int.TryParse(item, out d2)) {                    
                    sbPost.Append("&nGrbs=").Append((d2 - 1).ToString().Replace("0", "100"));
                }             
            }

            //脑卒中并发症情况 Syndrome
            bw = dr["Syndrome"].ToString();
            d2 = 0;
            var st3 = bw.Split(',');
            foreach (var item in st3)
            {
                if (int.TryParse(item, out d2))
                {
                    sbPost.Append("&nBfzqk=").Append((d2 - 1).ToString().Replace("5", "99"));
                }
            }

            //脑卒中并发症其他 SyndromeOther 
            sbPost.Append("&nBfzqkqt=").Append(HtmlHelper.GetUrlEncodeVal(dr["SyndromeOther"].ToString()));


            //新发卒中症状 NewSymptom
            bw = dr["NewSymptom"].ToString();
            d2 = 0;
             st3 = bw.Split(',');
            foreach (var item in st3)
            {
                if (int.TryParse(item, out d2))
                {
                    sbPost.Append("&nXfzzzz=").Append((d2 - 1).ToString());
                }
            }


            //新发卒中症状其他 NewSymptomOther 
            sbPost.Append("&nXfzzzzqt=").Append(HtmlHelper.GetUrlEncodeVal(dr["NewSymptomOther"].ToString()));

            // 每日吸烟量 SmokeDay 
            sbPost.Append("&mXysl=").Append(dr["SmokeDay"].ToString().Replace(".00",""));

            // 每日饮酒量 SmokeDay 
            sbPost.Append("&mYjsl=").Append(dr["DrinkDay"].ToString());

            // 每日饮酒量 SmokeDay 
            sbPost.Append("&mYdpl=").Append(dr["SportWeek"].ToString());

            //每周运动时间 SportMinute 
            sbPost.Append("&mYdcxsj=").Append(dr["SportMinute"].ToString());

            //空腹血糖 FPGL 
            sbPost.Append("&dKfxt=").Append(dr["FPGL"].ToString());

            //身高 Height 
            sbPost.Append("&dSg=").Append(dr["Height"].ToString());

            //BMI
            sbPost.Append("&dBmi=").Append(dr["BMI"].ToString());

            //腰围 Waistline 
            sbPost.Append("&dYw=").Append(dr["Waistline"].ToString());

            //LifeSelfCare 生活能否自理  
            string shzl = dr["LifeSelfCare"].ToString();
            int s5 = 0;
            if (int.TryParse(shzl, out s5)) {
                shzl = (s5 - 1).ToString();
            }
            sbPost.Append("&mShzlnl=").Append(shzl);
            
            //肢体恢复情况 
            shzl = dr["LimbRecover"].ToString();
             s5 = 0;
            if (int.TryParse(shzl, out s5))
            {
                shzl = (s5 - 1).ToString();
            }
            sbPost.Append("&nZtknhf=").Append(shzl);

            //康复治疗的措施 RecoveryCure  
            shzl = dr["RecoveryCure"].ToString();
            s5 = 0;
            var kfzl = shzl.Split(',');
            foreach (var item in kfzl)
            {
                if (int.TryParse(item, out s5))
                {
                    shzl = (s5 - 1).ToString();
                }
                sbPost.Append("&nKfzlfs=").Append(shzl.Replace("4", "99"));
            }

            //康复治疗措施其他 RecoveryCureOther 
            sbPost.Append("&nKfzlfsqt=").Append(HtmlHelper.GetUrlEncodeVal(dr["RecoveryCureOther"].ToString()));

            //居民签名
            sbPost.Append("&sdjmqm=").Append(HtmlHelper.GetUrlEncodeVal(pm.memberName));

            #endregion


            #endregion

            #region CD_DRUGCONDITION

            dt = ds.Tables["CD_DRUGCONDITION"];

            if (dt != null && dt.Rows.Count > 0)
            {

                sbPost.Append("&gJyy=1");
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    dr = dt.Rows[i];
                    sbPost.Append("&yYwmc=").Append(CommonExtensions.GetUrlEncodeVal(dr["Name"].ToString()));
                  //  string strYF = "每日" + dt.Rows[i]["DailyTime"].ToString() + "次,每次" + dt.Rows[i]["EveryTimeMg"].ToString() + "mg";
                    //string strYF = dt.Rows[i]["EveryTimeMg"].ToString() + dt.Rows[i]["drugtype"].ToString() + " po " + GetYaopinYongfa(dt.Rows[i]["DailyTime"].ToString());
                    string strYF = dt.Rows[i]["DosAge"].ToString();
                    sbPost.Append("&yYwyf=").Append(CommonExtensions.GetUrlEncodeVal(strYF));
                }
            }
            else
            {
                sbPost.Append("&yYwmc=");
                sbPost.Append("&yYwyf=");
                sbPost.Append("&gJyy=2");
            }

            #endregion

            sbPost.Append("&qdqxz=").Append(pNqdqxz);
            sbPost.Append("&wzd=").Append(((26 - pNqdqxz) * 100.0 / 26).ToString("#"));

            // 新增
            //http://20.1.1.124:9000/sdcsm/strokeVisit/add.action
            returnString = web.PostHttp(baseUrl + "strokeVisit/add.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            return "";
        }

        /// <summary>
        /// 修改随访
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pm"></param>
        /// <param name="zxKey"></param>
        /// <param name="padSFDate"></param>
        private string EditInfo(DataSet ds, PersonModel pm, string padSFDate, string key)
        {
            //http://20.1.1.124:9000/sdcsm/strokeVisit/toUpdate.action?id=B616B031ECB546B8E0530100007F7EC8
            WebHelper web = new WebHelper();
            string url = baseUrl + "strokeVisit/toUpdate.action?id=" + key;
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

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nNzzlx'][@checked]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&nNzzlx=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//select[@name='nNzzbw']/option[@selected]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&nNzzbw=").Append(strTmp);

            //var nodes = doc.DocumentNode.SelectNodes("//input[@name='nGrbs'][@checked]");

            //if (nodes == null)
            //{
            //    pNqdqxz++;
            //}
            //else
            //{
            //    foreach (var n in nodes)
            //    {
            //        if (n.Attributes.Contains("value"))
            //        {
            //            sbPost.Append("&nGrbs=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='nBfzqk'][@checked]");

            //if (nodes == null)
            //{
            //    pNqdqxz++;
            //}
            //else
            //{
            //    foreach (var n in nodes)
            //    {
            //        if (n.Attributes.Contains("value"))
            //        {
            //            sbPost.Append("&nBfzqk=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nBfzqkqt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

           // sbPost.Append("&nBfzqkqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='nXfzzzz'][@checked]");

            //if (nodes == null)
            //{
            //    pNqdqxz++;
            //}
            //else
            //{
            //    foreach (var n in nodes)
            //    {
            //        if (n.Attributes.Contains("value"))
            //        {
            //            sbPost.Append("&nXfzzzz=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nXfzzzzqt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

          //  sbPost.Append("&nXfzzzzqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//select[@name='mShzlnl']/option[@selected]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&mShzlnl=").Append(strTmp);

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='nKfzlfs'][@checked]");

            //if (nodes == null)
            //{
            //    pNqdqxz++;
            //}
            //else
            //{
            //    foreach (var n in nodes)
            //    {
            //        if (n.Attributes.Contains("value"))
            //        {
            //            sbPost.Append("&nKfzlfs=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='nKfzlfsqt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //sbPost.Append("&nKfzlfsqt=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&mXysl=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&mYjsl=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&mYdpl=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&mYdcxsj=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dBmi']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //sbPost.Append("&dBmi=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dYw']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&dYw=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dKfxt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&dKfxt=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//select[@name='nZtknhf']/option[@selected]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&nZtknhf=").Append(strTmp);

          //  node = doc.DocumentNode.SelectSingleNode("//input[@name='nBcsfysjy']");
          //  strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

          //  if (string.IsNullOrEmpty(strTmp))
          //  {
          //      pNqdqxz++;
          //  }
                  
          //sbPost.Append("&nBcsfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            #endregion

            #region CD_HYPERTENSIONFOLLOWUP

            DataTable dt = ds.Tables["CD_STROKE_FOLLOWUP"];

            DataRow dr = dt.Rows[0];
            //身高
            strTmp = dr["Height"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dSg=").Append(strTmp);

            //下次随访日期
            strTmp = dr["NextFollowupDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextFollowupDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfsj=").Append(strTmp);
            //随访医生
            strTmp = dr["FollowupDoctor"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&nSfys=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            //症状
            var strTmpA = dr["Symptom"].ToString().Split(',');
            strTmp = "";
            foreach (string t in strTmpA)
            {
                if (!string.IsNullOrEmpty(t))
                {
                    sbPost.Append("&nMqzz=").Append(GetSymptomForWeb(t));
                    strTmp = "1";
                }
            }
            //症状其他
            sbPost.Append("&nMqzzqt=").Append(CommonExtensions.GetUrlEncodeVal(dr["SymptomOther"].ToString()));

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            //血压
            strTmp = dr["Hypertension"].ToString();

            double dtmp = 0;

            if (strTmp != "" && double.TryParse(strTmp, out dtmp))
            {
                strTmp = dtmp.ToString("#");
            }

            sbPost.Append("&dSsy=").Append(strTmp);

            string strTmp2 = dr["Hypotension"].ToString();

            if (string.IsNullOrEmpty(strTmp) || string.IsNullOrEmpty(strTmp2))
            {
                pNqdqxz++;
            }

            if (strTmp2 != "" && double.TryParse(strTmp2, out dtmp))
            {
                strTmp2 = dtmp.ToString("#");
            }

            sbPost.Append("&dSzy=").Append(strTmp2);

            //体重
            strTmp = dr["Weight"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&dTz=").Append(strTmp);
            //服药依从性
            strTmp = dr["MedicationCompliance"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&nFyycx=").Append(strTmp);

            //随访医生建议
            strTmp = dr["DoctorView"].ToString();

            if (!string.IsNullOrEmpty(strTmp))
            {
                sbPost.Append("&nBcsfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            }
            else {
                sbPost.Append("&nBcsfysjy=");
            }

 
            //随访分类
            strTmp = dr["FollowupType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&nCcsffl=").Append(strTmp);
            //随访方式
            strTmp = dr["FollowupWay"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&nSffs=").Append(strTmp);

            #region 2.0 新增

            //此次随访分类其他 并发症 nCcsfflbfz 
            sbPost.Append("&nCcsfflbfz=").Append(HtmlHelper.GetUrlEncodeVal(dr["FollowupTypeOther"].ToString()));

            //脑卒中类型 nNzzlx 
            sbPost.Append("&nNzzlx=").Append(dr["StrokeType"].ToString());

            //脑卒中部位 Strokelocation 
            string bw = dr["Strokelocation"].ToString();
            int d2 = 0;
            if (!string.IsNullOrWhiteSpace(bw) && int.TryParse(bw, out d2) && !bw.Contains("0"))
            {
                bw = (d2 - 1).ToString();
            }
            sbPost.Append("&nNzzbw=").Append(bw.Replace("0", ""));

            //个人病史 MedicalHistory
            bw = dr["MedicalHistory"].ToString();
            d2 = 0;
            var st2 = bw.Split(',');
            foreach (var item in st2)
            {
                if (int.TryParse(item, out d2))
                {
                    sbPost.Append("&nGrbs=").Append((d2 - 1).ToString().Replace("0", "100"));
                }
            }

            //脑卒中并发症情况 Syndrome
            bw = dr["Syndrome"].ToString();
            d2 = 0;
            var st3 = bw.Split(',');
            foreach (var item in st3)
            {
                if (int.TryParse(item, out d2))
                {
                    sbPost.Append("&nBfzqk=").Append((d2 - 1).ToString().Replace("5", "99"));
                }
            }

            //脑卒中并发症其他 SyndromeOther 
            sbPost.Append("&nBfzqkqt=").Append(HtmlHelper.GetUrlEncodeVal(dr["SyndromeOther"].ToString()));


            //新发卒中症状 NewSymptom
            bw = dr["NewSymptom"].ToString();
            d2 = 0;
            st3 = bw.Split(',');
            foreach (var item in st3)
            {
                if (int.TryParse(item, out d2))
                {
                    sbPost.Append("&nXfzzzz=").Append((d2 - 1).ToString());
                }
            }

            //新发卒中症状其他 NewSymptomOther 
            sbPost.Append("&nXfzzzzqt=").Append(HtmlHelper.GetUrlEncodeVal(dr["NewSymptomOther"].ToString()));

            // 每日吸烟量 SmokeDay 
            sbPost.Append("&mXysl=").Append(dr["SmokeDay"].ToString().Replace(".00", ""));

            // 每日饮酒量 SmokeDay 
            sbPost.Append("&mYjsl=").Append(dr["DrinkDay"].ToString());

            // 每日饮酒量 SmokeDay 
            sbPost.Append("&mYdpl=").Append(dr["SportWeek"].ToString());

            //每周运动时间 SportMinute 
            sbPost.Append("&mYdcxsj=").Append(dr["SportMinute"].ToString());

            //空腹血糖 FPGL 
            sbPost.Append("&dKfxt=").Append(dr["FPGL"].ToString());

            //身高 Height 
            sbPost.Append("&dSg=").Append(dr["Height"].ToString());

            //BMI
            sbPost.Append("&dBmi=").Append(dr["BMI"].ToString());

            //腰围 Waistline 
            sbPost.Append("&dYw=").Append(dr["Waistline"].ToString());

            //LifeSelfCare 生活能否自理  
            string shzl = dr["LifeSelfCare"].ToString();
            int s5 = 0;
            if (int.TryParse(shzl, out s5))
            {
                shzl = (s5 - 1).ToString();
            }
            sbPost.Append("&mShzlnl=").Append(shzl);

            //肢体恢复情况 
            shzl = dr["LimbRecover"].ToString();
            s5 = 0;
            if (int.TryParse(shzl, out s5))
            {
                shzl = (s5 - 1).ToString();
            }
            sbPost.Append("&nZtknhf=").Append(shzl);

            //康复治疗的措施 RecoveryCure  
            shzl = dr["RecoveryCure"].ToString();
            s5 = 0;
            var kfzl = shzl.Split(',');
            foreach (var item in kfzl)
            {
                if (int.TryParse(item, out s5))
                {
                    shzl = (s5 - 1).ToString();
                }
                sbPost.Append("&nKfzlfs=").Append(shzl.Replace("4", "99"));
            }
         
            //康复治疗措施其他 RecoveryCureOther 
            sbPost.Append("&nKfzlfsqt=").Append(HtmlHelper.GetUrlEncodeVal(dr["RecoveryCureOther"].ToString()));

            //居民签名
            sbPost.Append("&sdjmqm=").Append(HtmlHelper.GetUrlEncodeVal(pm.memberName));

            #endregion

            #endregion

            #region CD_DRUGCONDITION

            dt = ds.Tables["CD_DRUGCONDITION"];

            if (dt != null && dt.Rows.Count > 0)
            {

                sbPost.Append("&gJyy=1");
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    dr = dt.Rows[i];
                    sbPost.Append("&yYwmc=").Append(CommonExtensions.GetUrlEncodeVal(dr["Name"].ToString()));
                  //  string strYF = "每日" + dt.Rows[i]["DailyTime"].ToString() + "次,每次" + dt.Rows[i]["EveryTimeMg"].ToString() + "mg";
                    //string strYF = dt.Rows[i]["EveryTimeMg"].ToString() + dt.Rows[i]["drugtype"].ToString() + " po " + GetYaopinYongfa(dt.Rows[i]["DailyTime"].ToString());
                    string strYF = dt.Rows[i]["DosAge"].ToString();
                    sbPost.Append("&yYwyf=").Append(CommonExtensions.GetUrlEncodeVal(strYF));
                }
            }
            else
            {
                sbPost.Append("&yYwmc=");
                sbPost.Append("&yYwyf=");
                sbPost.Append("&gJyy=2");
            }

            #endregion

            sbPost.Append("&qdqxz=").Append(pNqdqxz);
            sbPost.Append("&wzd=").Append(((26 - pNqdqxz) * 100.0 / 26).ToString("#"));

            // 修改
            //http://20.1.1.124:9000/sdcsm/strokeVisit/update.action
            returnString = web.PostHttp(baseUrl + "strokeVisit/update.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

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
            callback("下载-脑卒中信息..." + currentIndex + "/" + totalRows);

            if (pm != null && !string.IsNullOrEmpty(pm.pid))
            {
                TryDownByIDs(pm, 1, callback);
            }

            currentIndex++;
        }

        /// <summary>
        ///  尝试3次下载
        /// </summary>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param> 
        private void TryDownInfo(int tryCount, Action<string> callback)
        {
            try
            {
                GetKeyAndInfo(callback);
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
                    TryDownInfo(tryCount, callback);
                }
                else
                {
                    callback("EX-下载脑卒中信息失败，请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 获取脑卒中key和页面信息
        /// </summary>
        /// <param name="callback"></param>
        private void GetKeyAndInfo(Action<string> callback)
        {
            int PageSum = 0;
            List<PersonModel> personList = GetKeyAndInfo(callback, out PageSum);

            //调方法，便利当前页的表示，获取信息
            GetInfoByPersonList(personList, callback);

            for (int i = 2; i <= PageSum; i++)
            {
                personList.Clear();

                //调方法，遍历当前页标示，获取信息
                personList = GetPageNumKeyInfo(i, callback);

                GetInfoByPersonList(personList, callback);
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

            string postData = "page.currentPage=" + pageNum + "&search=4&siteid=" + key + "&branch=on&dDazt=1&dqjg=" + key;
            string returnString = web.PostHttp(baseUrl + "/stroke/list.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
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

        /// <summary>
        /// 获取脑卒中信息下载
        /// </summary>
        private void GetInfoByPersonList(List<PersonModel> lstAllPm, Action<string> callback)
        {
            foreach (var pm in lstAllPm)
            {
                TryDownByIDs(pm, 1, callback);

                callback("下载-脑卒中信息..." + currentIndex + "/" + totalRows);

                currentIndex++;
            }
        }

        /// <summary>
        /// 获取指定页码的key和页面信息
        /// </summary>
        /// <param name="m"></param>
        /// <param name="callback"></param>
        private List<PersonModel> GetKeyAndInfo(Action<string> callback, out int pageSum)
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

            // http://222.133.17.194:9080/sdcsm/stroke/list.action?search=4&siteid=371481B10001&branch=on&dDazt=1
            string postData = "search=4&siteid=" + key + "&branch=on&dDazt=1&dqjg=" + key;
            string returnString = web.PostHttp(baseUrl + "/stroke/list.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
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

        private void TryDownByIDs(PersonModel person, int tryCount, Action<string> callback)
        {
            string idcard = person.idNumber.ToString();

            try
            {
                GetInfo(person);
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
                    callback("EX-脑卒中信息:身份证[" + idcard + "]:下载信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;

                    TryDownByIDs(person, tryCount, callback);
                }
                else
                {
                    callback("EX-脑卒中信息:身份证[" + idcard + "]:下载信息失败。请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 根据标识信息，获取信息，下载
        /// </summary>
        /// <param name="pm"></param>
        private void GetInfo(PersonModel pm)
        {
            DataSet ds = DataSetTmp.NczDataSet; //数据库表架构
            DataSet dsSave = new DataSet();
            CommonBusiness.CommonDAOBusiness dao = new CommonBusiness.CommonDAOBusiness();
            DataTable dtData = null;

            //获取专档建立，确诊时间
            WebHelper web = new WebHelper();

            string postData = "";
            string returnString = "";
            
            #region  CD_STROKE_FOLLOWUP 随访

            List<SFClass> lstSF = GetSFxxLst(pm.pid);

            string strtmp = "";
            if (lstSF.Count > 0)
            {
                SFClass sf = lstSF[0];
                //http://20.1.1.124:9000/sdcsm/strokeVisit/toUpdate.action?id=B616B031ECB546B8E0530100007F7EC8
                //获取随访类表
                postData = "id=" + lstSF[0].key;
                returnString = web.GetHttp(baseUrl + "strokeVisit/toUpdate.action?"+ postData, "", SysCookieContainer);
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                dtData = ds.Tables["CD_STROKE_FOLLOWUP"].Clone();
                DataRow dr = dtData.NewRow();

                dr["IDCardNo"] = pm.idNumber;
                //随访日期
                var node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                dr["FollowupDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                string sfrq = Convert.ToDateTime(dr["FollowupDate"] .ToString()).ToString("yyyy-MM-dd");
                //随访医生
                node = doc.DocumentNode.SelectSingleNode("//input[@name='nSfys']");
                dr["FollowupDoctor"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //下次随访日期
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXcsfsj']");
                dr["NextFollowupDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //症状
                var nodes = doc.DocumentNode.SelectNodes("//input[@name='nMqzz'][@checked]");
                if (nodes != null)
                {
                    foreach (var no in nodes)
                    {
                        if (no.Attributes.Contains("value"))
                        {
                            string temStr = no.Attributes["value"].Value;

                            strtmp += "," + GetSymptom(temStr);
                        }
                    }
                }

                dr["Symptom"] = strtmp.TrimStart(',');
                //其他症状
                node = doc.DocumentNode.SelectSingleNode("//input[@id='nMqzzqt']");
                dr["SymptomOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血压
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSsy']");
                dr["Hypertension"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSzy']");
                dr["Hypotension"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //体重
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dTz']");
                dr["Weight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //身高
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSg']");
                dr["Height"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //体症其他
                //node = doc.DocumentNode.SelectSingleNode("//input[@name='dTz']");
                //dr["SignOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                ////吸烟
                //node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl']");
                //strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //strtmp = strtmp == "" ? "" : "吸烟情况:" + strtmp + "支/天";

                //string strtt = strtmp;
                ////饮酒
                //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl']");
                //strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //strtmp = strtmp == "" ? "" : "饮酒情况:" + strtmp + "两/天";

                //strtt = strtt == "" ? strtmp : strtt + ";" + strtmp;

                ////支/天
                //dr["SmokeDrinkAttention"] = strtt;

                //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl']");
                //strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //strtmp = strtmp == "" ? "" : "运动频率:" + strtmp + "次/周";

                //strtt = strtmp;

                //node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj']");
                //strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //strtmp = strtmp == "" ? "" : "每次持续时间:" + strtmp + "分钟/次";

                //strtt = strtt == "" ? strtmp : strtt + ";" + strtmp;

                //dr["SportAttention"] = strtt;

                //dr["EatSaltAttention"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //dr["PsychicAdjust"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //dr["ObeyDoctorBehavio"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //dr["AssistantExam"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //服药依从性
                node = doc.DocumentNode.SelectSingleNode("//select[@name='nFyycx']/option[@selected]");
                dr["MedicationCompliance"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                var node2 = doc.DocumentNode.SelectNodes("//input[@name='nCcsffl'][@checked]");
                string st4 = "";
                if (node2 != null) {
                    foreach (var item in node2)
                    {
                        st4 += item.Attributes["value"].Value+",";
                    }
                }
                dr["FollowupType"] =st4.TrimEnd(',');

                //dr["ReferralReason"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //dr["ReferralOrg"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //随访医生建议
                node = doc.DocumentNode.SelectSingleNode("//input[@name='nBcsfysjy']");
                dr["DoctorView"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //随访方式
                node = doc.DocumentNode.SelectSingleNode("//input[@name='nSffs'][@checked]");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["FollowupWay"] = strtmp == "99" ? "" : strtmp;

                dr["RecordID"] = pm.pid.Substring(0, 17); ;

                #region 2.0 新增

                //此次随访分类其他 FollowupTypeOther  并发症
                node = doc.DocumentNode.SelectSingleNode("//input[@name='nCcsfflbfz']");
                dr["FollowupTypeOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //脑卒中类型 StrokeType 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='nNzzlx'][@checked]");
                dr["StrokeType"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //脑卒中部位 Strokelocation 
                node = doc.DocumentNode.SelectSingleNode("//select[@name='nNzzbw']/option[@selected]");
                string bw = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;             
                int r2 = 0;
                if (!string.IsNullOrWhiteSpace(bw) && int.TryParse(bw,out r2) ) {
                    bw = (r2 + 1).ToString();
                }
                dr["Strokelocation"] = string.IsNullOrWhiteSpace(bw) ? "1" : bw;

                //个人病史 MedicalHistory
                node2 = doc.DocumentNode.SelectNodes("//input[@name='nGrbs'][@checked]");
                string grbs = "";
                int i3 = 0;
                if (node2 != null) {
                    foreach (var item in node2)
                    {
                        if (int.TryParse(item.Attributes["value"].Value, out i3)) {
                             grbs+= (i3 + 1).ToString()+",";
                                
                        }
                    //    grbs += grbs + ","; 
                    }
                }
                dr["MedicalHistory"] = grbs.Replace("101","1");

                //脑卒中并发症情况 Syndrome
                node2 = doc.DocumentNode.SelectNodes("//input[@name='nBfzqk'][@checked]");
                 grbs = "";
                 i3 = 0;
                if (node2 != null)
                {
                    foreach (var item in node2)
                    {
                        if (int.TryParse(item.Attributes["value"].Value, out i3))
                        {
                            grbs += (i3 + 1).ToString() + ",";

                        }
                    //    grbs += grbs + ",";
                    }
                }
                dr["Syndrome"] = grbs.Replace("100", "6").TrimEnd(',');

                //脑卒中并发症其他 SyndromeOther 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='nBfzqkqt']");
                dr["SyndromeOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;




                //新发卒中症状 NewSymptom
                node2 = doc.DocumentNode.SelectNodes("//input[@name='nXfzzzz'][@checked]");
                grbs = "";
                i3 = 0;
                if (node2 != null)
                {
                    foreach (var item in node2)
                    {
                        if (int.TryParse(item.Attributes["value"].Value, out i3))
                        {
                            grbs += (i3 + 1).ToString() + ",";

                        }
                      //  grbs += grbs + ",";
                    }
                }
                dr["NewSymptom"] = grbs.Replace("12", "").Replace("13", "").TrimEnd(',');

                //新发卒中症状其他 NewSymptomOther 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='nXfzzzzqt']");
                dr["NewSymptomOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                // 每日吸烟量 SmokeDay 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl']");
                dr["SmokeDay"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                // 每日饮酒量 SmokeDay 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl']");
                dr["DrinkDay"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //每周运动次数 SportWeek 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl']");
                dr["SportWeek"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //每周运动时间 SportMinute 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj']");
                dr["SportMinute"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //空腹血糖 FPGL 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dKfxt']");
                dr["FPGL"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //身高 Height 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSg']");
                dr["Height"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //BMI
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dBmi']");
                dr["BMI"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //腰围 Waistline 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dYw']");
                dr["Waistline"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


                //LifeSelfCare 生活能否自理  
                node = doc.DocumentNode.SelectSingleNode("//select[@name='mShzlnl']/option[@selected]");
                int sh=0;
                string shzl= node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                if(int.TryParse(shzl,out sh)){
                    shzl = (sh + 1).ToString();  
                }
                dr["LifeSelfCare"] = string.IsNullOrWhiteSpace(shzl) ? "1" : shzl;

                //肢体功能恢复情况 LimbRecover 
                node = doc.DocumentNode.SelectSingleNode("//select[@name='nZtknhf']/option[@selected]");
                 sh = 0;
                 shzl = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                if (int.TryParse(shzl, out sh))
                {
                    shzl = (sh + 1).ToString();
                }
                dr["LimbRecover"] = string.IsNullOrWhiteSpace(shzl) ? "1" : shzl;

                //康复治疗的措施 RecoveryCure 
                node2 = doc.DocumentNode.SelectNodes("//input[@name='nKfzlfs'][@checked]");
                grbs = "";
                i3 = 0;
                if (node2 != null)
                {
                    foreach (var item in node2)
                    {
                        if (int.TryParse(item.Attributes["value"].Value, out i3))
                        {
                            grbs += (i3 + 1).ToString() + ",";

                        }
                    //    grbs += item + ",";
                    }
                }
                dr["RecoveryCure"] = grbs.Replace("100", "5").TrimEnd(',');

                //康复治疗措施其他 RecoveryCureOther 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='nKfzlfsqt']");
                dr["RecoveryCureOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                #endregion

                dtData.Rows.Add(dr);
                outkey= dao.SaveMainTable(dtData, pm.idNumber, sfrq);

                nodes = doc.DocumentNode.SelectNodes("//tbody[@id='dyTbody']/tr[position()>1]");

                node = doc.DocumentNode.SelectSingleNode("//input[@name='gJyy'][@checked]");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                if (nodes != null && strtmp == "1")
                {
                    #region  CD_DRUGCONDITION

                    dtData = ds.Tables["CD_DRUGCONDITION"].Clone();

                    foreach (var t in nodes)
                    {
                        var tmpNode = t.SelectSingleNode("td/input[@name='yYwmc']");

                        dr = dtData.NewRow();

                        dr["IDCardNo"] = pm.idNumber;
                        dr["Type"] = "5";
                        dr["OutKey"] = outkey.ToString();
                        dr["Name"] = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;

                        tmpNode = t.SelectSingleNode("td/input[@name='yYwyf']");
                        int pN = 0;
                        pN = 0;
                        string tmpET = tmpNode == null || !tmpNode.Attributes.Contains("value") ? "" : tmpNode.Attributes["value"].Value;
                        dr["DosAge"] = tmpET;
                        dtData.Rows.Add(dr);
                    }

                    #endregion

                    dsSave.Tables.Add(dtData);
                }

            }
            #endregion
            
            #region  CD_DIABETES_BASEINFO

            /*DataTable dtInfo = ds.Tables["CD_STROKE_BASEINFO"].Clone();

            DataRow dr = dtInfo.NewRow();

            dr["IDCardNo"] = pm.idNumber;
            dr["RecordID"] = pm.pid.Substring(0, 17);

            var node = doc.DocumentNode.SelectSingleNode("//input[@name='nBlly'][@checked]");
            // 病例来源
            dr["IllSource"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            node = doc.DocumentNode.SelectSingleNode("//input[@id='nFbsj']");
            // 发病时间
            dr["IllTime"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            node = doc.DocumentNode.SelectSingleNode("//input[@id='nZzyy']");
            // 诊断医院
            dr["DiagnosisHource"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //家族史
            var nodes = doc.DocumentNode.SelectNodes("//input[@name='dJzs'][@checked]");
            string strtmp = "";

            if (nodes != null)
            {
                foreach (var no in nodes)
                {
                    if (no.Attributes.Contains("value"))
                    {
                        strtmp += "," + GetFamilyHistory(no.Attributes["value"].Value);
                    }
                }

                strtmp = strtmp.TrimStart(',');
            }

            dr["Familyhistory"] = strtmp;
            nodes = doc.DocumentNode.SelectNodes("//input[@name='nJzszz'][@checked]");
            //就诊时状况
            strtmp = "";
            if (nodes != null)
            {
                foreach (var no in nodes)
                {
                    if (no.Attributes.Contains("value"))
                    {
                        strtmp += "," + GetHosState(no.Attributes["value"].Value);
                    }
                }

                strtmp = strtmp.TrimStart(',');
            }

            dr["HosState"] = strtmp;

            node = doc.DocumentNode.SelectSingleNode("//input[@id='nMrs']");
            strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            decimal dtmp = 0;

            decimal.TryParse(strtmp, out dtmp);

            // MRS评分
            dr["Mrs"] = dtmp;

            //管理组别
            node = doc.DocumentNode.SelectSingleNode("//select[@id='nGlzz']/option[@selected]");
            dr["GroupLevel"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //脑卒中危险因素
            nodes = doc.DocumentNode.SelectNodes("//input[@name='nWxys'][@checked]");
            strtmp = "";
            if (nodes != null)
            {
                foreach (var no in nodes)
                {
                    if (no.Attributes.Contains("value"))
                    {
                        if (no.Attributes["value"].Value == "99")
                        {
                            strtmp += ",11";
                        }
                        else
                        {
                            strtmp += "," + GetDangerousElement(no.Attributes["value"].Value);
                        }
                    }
                }

                strtmp = strtmp.TrimStart(',');
            }

            dr["DangerousElement"] = strtmp;

            //脑卒中危险因素其他
            node = doc.DocumentNode.SelectSingleNode("//input[@id='nWxysqt']");
            dr["DgrElementOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            node = doc.DocumentNode.SelectSingleNode("//input[@id='nCt']");
            //CT检查结果
            dr["Ct"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //MRI检查结果
            node = doc.DocumentNode.SelectSingleNode("//input[@id='nMri']");
            dr["Mri"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //脑卒中分类
            node = doc.DocumentNode.SelectSingleNode("//select[@id='nNzzfl']/option[@selected]");
            dr["StrokeType"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //脑卒中部位
            node = doc.DocumentNode.SelectSingleNode("//select[@id='nNzzbw']/option[@selected]");
            dr["StrokePosition"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //自理能力
            node = doc.DocumentNode.SelectSingleNode("//input[@name='nShzlnl'][@checked]");
            dr["SelfAbility"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //服药依从性
            node = doc.DocumentNode.SelectSingleNode("//select[@id='nFyycx']/option[@selected]");
            dr["DrugsRely"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //特殊治疗
            nodes = doc.DocumentNode.SelectNodes("//input[@name='nWxys'][@checked]");
            strtmp = "";
            if (nodes != null)
            {
                foreach (var no in nodes)
                {
                    if (no.Attributes.Contains("value"))
                    {
                        strtmp += "," + GetSpecialTreatment(no.Attributes["value"].Value);
                    }
                }

                strtmp = strtmp.TrimStart(',');
            }

            dr["SpecialTreatment"] = strtmp;
            //非药物治疗措施
            nodes = doc.DocumentNode.SelectNodes("//input[@name='nWxys'][@checked]");
            strtmp = "";
            if (nodes != null)
            {
                foreach (var no in nodes)
                {
                    if (no.Attributes.Contains("value"))
                    {
                        if (no.Attributes["value"].Value == "99")
                        {
                            strtmp += ",9";
                        }
                        else
                        {
                            strtmp += "," + GetOtherTreatment(no.Attributes["value"].Value);
                        }
                    }
                }

                strtmp = strtmp.TrimStart(',');
            }

            dr["OtherTreatment"] = strtmp;
            //是否终止管理
            node = doc.DocumentNode.SelectSingleNode("//input[@name='zzgl'][@checked]");
            dr["StopManager"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            node = doc.DocumentNode.SelectSingleNode("//input[@name='nZzglrq']");
            dr["StopTime"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            node = doc.DocumentNode.SelectSingleNode("//input[@name='nZzly'][@checked]");
            dr["StopReason"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
            dr["OccurTime"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            dtInfo.Rows.Add(dr);

            dsSave.Tables.Add(dtInfo);*/
            #endregion

            dao.SaveDataSet(dsSave, pm.idNumber, "5",outkey.ToString());

            dsSave.Tables.Clear();
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
            //症状（以英文逗号分隔）1无症状　2猝然昏扑 3不省人事4口眼歪斜 5半身不遂6舌强语蹇7.智力障碍
            switch (code)
            {
                case "0":
                    returnVal = "1";
                    break;
                case "1":
                    returnVal = "4";
                    break;
                case "2":
                    returnVal = "5";
                    break;
                case "3":
                    returnVal = "6";
                    break;
                case "4":
                    returnVal = "7";
                    break;
                case "99":
                    returnVal = "8";
                    break;

            }
            return returnVal;
        }
        private string GetSymptomForWeb(string code)
        {
            code = code.Trim();
            string returnVal = "";
            //症状（以英文逗号分隔）1无症状　2猝然昏扑 3不省人事4口眼歪斜 5半身不遂6舌强语蹇7.智力障碍
            switch (code)
            {
                case "1":
                    returnVal = "0";
                    break;
                case "4":
                    returnVal = "1";
                    break;
                case "5":
                    returnVal = "2";
                    break;
                case "6":
                    returnVal = "3";
                    break;
                case "7":
                    returnVal = "4";
                    break;
                case "8":
                    returnVal = "99";
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

        private string GetFamilyHistory(string code)
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

        private string GetHosState(string code)
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

        private string GetDangerousElement(string code)
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

        private string GetSpecialTreatment(string code)
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

        private string GetOtherTreatment(string code)
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




        #endregion

        private List<SFClass> GetSFxxLst(string key)
        {
            List<SFClass> lstSF = new List<SFClass>();

            string postData = "dGrdabh=" + key;
            //http://20.1.1.124:9000/sdcsm/strokeVisit/toAdd.action?dGrdabh=371482110010115101
            WebHelper web = new WebHelper();

            string returnString = web.GetHttp(baseUrl + "strokeVisit/toAdd.action?"+ postData, "", SysCookieContainer);

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
