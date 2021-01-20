using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Data;
using DAL;
using Utilities.Common;
using Model.InfoModel;
using HtmlAgilityPack;

namespace GxbBusiness
{
    public class GxbBusiness
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
                        callback("下载-冠心病信息..." + cIndex + "/" + idsa.Length);
                        cIndex++;
                        continue;
                    }
                    CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                    PersonModel person = cb.GetGrdaByIDCardNo(id, loginkey, SysCookieContainer);

                    if (person != null && !string.IsNullOrEmpty(person.pid))
                    {
                        TryDownByIDs(person, 1, callback);
                    }

                    callback("下载-冠心病信息..." + cIndex + "/" + idsa.Length);
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
        /// 冠心病全部下载入口
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

                callback("上传-冠心病信息..." + PcurrentIndex + "/" + lstUploadData.Count);
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
                DataTable dtVisit = ds.Tables["CD_CHD_FOLLOWUP"];

                if (dtVisit == null || dtVisit.Rows.Count <= 0)
                {
                    return;
                }

                idcard = dtVisit.Rows[0]["IDCardNo"].ToString();

                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                PersonModel pm = cb.GetGrdaByIDCardNo(idcard, loginkey, SysCookieContainer);

                if (pm == null || string.IsNullOrEmpty(pm.pid))
                {
                    callback("EX-冠心病信息:身份证[" + idcard + "]:平台尚未建档或者档案状态为非活动!");
                    return;
                }

                List<SFClass> lstSF = GetSFxxLst(pm.pid);

                string padSFDate = Convert.ToDateTime(dtVisit.Rows[0]["VisitDate"]).ToString("yyyy-MM-dd");

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
                    callback("EX-冠心病信息:身份证[" + idcard + "]:" + msg);
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
                    callback("EX-冠心病信息:身份证[" + idcard + "]:上传冠心病信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    TrySave(ds, tryCount, callback);
                }
                else
                {
                    callback("EX-冠心病信息:身份证[" + idcard + "]:上传冠心病信息失败。请确保网路畅通。");
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
            //http://222.133.17.194:9080/sdcsm/coronaryVisit/toAddWithDirect.action?dGrdabh=371481020010013201
            WebHelper web = new WebHelper();
            string url = baseUrl + "coronaryVisit/toAddWithDirect.action?dGrdabh=" + pm.pid;
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

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gBcsfysjy']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //sbPost.Append("&gBcsfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));


            int pNqdqxz = 0;
            //冠心病类型
            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gGxblx'][@checked]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&gGxblx=").Append(strTmp);

            //var nodes = doc.DocumentNode.SelectNodes("//input[@name='gMqzz'][@checked]");

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
            //            sbPost.Append("&gMqzz=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dBmi']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //sbPost.Append("&dBmi=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dKfxt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&dKfxt=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dGmddb']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&dGmddb=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dDmddb']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&dDmddb=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dGysz']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&dGysz=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dZdgc']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&dZdgc=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXdtjc']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXdtjc=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXdtydfh']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXdtydfh=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXzcc']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXzcc=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gGzdmzy']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gGzdmzy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXjmx']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXjmx=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));


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

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='gTszl'][@checked]");

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
            //            sbPost.Append("&gTszl=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='gFywzlcs'][@checked]");

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
            //            sbPost.Append("&gFywzlcs=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            #endregion

            #region CD_CHD_FOLLOWUP

            DataTable dt = ds.Tables["CD_CHD_FOLLOWUP"];

            DataRow dr = dt.Rows[0];

            sbPost.Append("&happentime=").Append(padSFDate);

            strTmp = dr["Height"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dSg=").Append(strTmp);

            strTmp = dr["NextVisitDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextVisitDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfsj=").Append(strTmp);


            strTmp = dr["VisitDoctor"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gSfys=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));


            strTmp = dr["Systolic"].ToString();

            double dtmp = 0;

            if (strTmp != "" && double.TryParse(strTmp, out dtmp))
            {
                strTmp = dtmp.ToString("#");
            }

            sbPost.Append("&dSsy=").Append(strTmp);

            string strTmp2 = dr["Diastolic"].ToString();

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

            strTmp = dr["Compliance"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gFyycx=").Append(strTmp);

            strTmp = dr["FollowType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gCcsffl=").Append(strTmp);

            strTmp = dr["VisitType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gSffs=").Append(string.IsNullOrEmpty(strTmp) ? "" : strTmp.Replace("4", "99"));


            //随访医生建议
            strTmp = dr["DoctorView"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gBcsfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));
            #region 2.0新增字段
            //冠心病类型  ChdType                   
            sbPost.Append("&gGxblx=").Append(dr["ChdType"].ToString().Replace("6", "99").Replace("5", "6"));

            //症状  Symptom 
            string str = dr["Symptom"].ToString();
            var sr = str.Split(',');
            foreach (var item in sr)
            {
                sbPost.Append("&gMqzz=").Append(Getzzsc(item));
            }

            //身高 Height 
            sbPost.Append("&dSg=").Append(dr["Height"].ToString());

            //BMI 
            sbPost.Append("&dBmi=").Append(dr["BMI"].ToString());

            //空腹血糖 FPGL 
            sbPost.Append("&dKfxt=").Append(dr["FPGL"].ToString());

            //总胆固醇 TC 
            sbPost.Append("&dZdgc=").Append(dr["TC"].ToString());

            //甘油三酯 TG 
            sbPost.Append("&dGysz=").Append(dr["TG"].ToString());

            //血清低密度 LowCho 
            sbPost.Append("&dDmddb=").Append(dr["LowCho"].ToString());

            //血清高密度 HeiCho 
            sbPost.Append("&dGmddb=").Append(dr["HeiCho"].ToString());

            //心电图检查结果 EcgCheckResult 
            sbPost.Append("&gXdtjc=").Append(HtmlHelper.GetUrlEncodeVal(dr["EcgCheckResult"].ToString()));

            //心电图运动符合 EcgExerciseResult 
            sbPost.Append("&gXdtydfh=").Append(HtmlHelper.GetUrlEncodeVal(dr["EcgExerciseResult"].ToString()));

            //冠影动脉造血效果 CAG 
            sbPost.Append("&gGzdmzy=").Append(HtmlHelper.GetUrlEncodeVal(dr["CAG"].ToString()));

            //心肌酶学 EnzymesResult 
            sbPost.Append("&gXjmx=").Append(HtmlHelper.GetUrlEncodeVal(dr["EnzymesResult"].ToString()));

            //心脏彩超 HeartCheckResult 
            sbPost.Append("&gXzcc=").Append(HtmlHelper.GetUrlEncodeVal(dr["HeartCheckResult"].ToString()));

            //每日吸烟量 SmokeDay 
            sbPost.Append("&mXysl=").Append(dr["SmokeDay"].ToString().Replace(".00", ""));


            //每日饮酒量 DrinkDay 
            sbPost.Append("&mYjsl=").Append(dr["DrinkDay"].ToString().Replace(".00", ""));

            //每周运动次数 SportWeek 
            sbPost.Append("&mYdpl=").Append(dr["SportWeek"].ToString());

            //每次运动时间 SportMinute 
            sbPost.Append("&mYdcxsj=").Append(dr["SportMinute"].ToString());

            //特殊治疗 SpecialTreated   gTszl
            string tszl = dr["SpecialTreated"].ToString();
            string srr = "";
            if (!string.IsNullOrWhiteSpace(tszl))
            {
                var tss = tszl.Split(',');
                int i3 = 0;
                foreach (var item in tss)
                {
                    if (int.TryParse(item, out i3))
                    {
                        srr += (i3 - 1).ToString();
                    }
                    sbPost.Append("&gTszl=").Append(srr);
                    srr = "";
                }
            }

            //非药物治疗措施 NondrugTreat 
            srr = dr["NondrugTreat"].ToString();
            int i4 = 0;
            string sr4 = "";
            if (!string.IsNullOrWhiteSpace(srr))
            {
                var yw = srr.Split(',');
                foreach (var item in yw)
                {
                    if (int.TryParse(item, out i4))
                    {
                        sr4 += (i4 - 1).ToString();
                    }
                    sbPost.Append("&gFywzlcs=").Append(sr4.Replace("0", "99"));
                    sr4 = "";
                }
            }

            //并发症其他 Syndromeother 
            sbPost.Append("&gCcsfflbfz=").Append(HtmlHelper.GetUrlEncodeVal(dr["Syndromeother"].ToString()));
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
                    //   string strYF = "每日" + dt.Rows[i]["DailyTime"].ToString() + "次,每次" + dt.Rows[i]["EveryTimeMg"].ToString() + "mg";
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
            sbPost.Append("&wzd=").Append(((27 - pNqdqxz) * 100.0 / 27).ToString("#"));

            // （手动）居民签名
            sbPost.Append("&sdjmqm=").Append(HtmlHelper.GetUrlEncodeVal(pm.memberName));

            // 新增
            returnString = web.PostHttp(baseUrl + "coronaryVisit/add.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return "新增失败！";
            }

            if (returnString.Contains("出现异常"))
            {
                return "新增失败！";
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
        private string EditInfo(DataSet ds, PersonModel pm, string padSFDate, string key)
        {
            //http://222.133.17.194:9080/sdcsm/coronaryVisit/toUpdate.action?id=14804
            WebHelper web = new WebHelper();
            string url = baseUrl + "coronaryVisit/toUpdate.action?id=" + key;
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

            sbPost.Append("&gMqzzqt=");
            //医生建议
            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gBcsfysjy']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //sbPost.Append("&gBcsfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));


            int pNqdqxz = 0;

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gGxblx'][@checked]");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&gGxblx=").Append(strTmp);

            //var nodes = doc.DocumentNode.SelectNodes("//input[@name='gMqzz'][@checked]");

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
            //            sbPost.Append("&gMqzz=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dBmi']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //sbPost.Append("&dBmi=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dKfxt']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&dKfxt=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dGmddb']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}

            //sbPost.Append("&dGmddb=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dDmddb']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&dDmddb=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dGysz']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&dGysz=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='dZdgc']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&dZdgc=").Append(strTmp);

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXdtjc']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXdtjc=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXdtydfh']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXdtydfh=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXzcc']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXzcc=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gGzdmzy']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gGzdmzy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //node = doc.DocumentNode.SelectSingleNode("//input[@name='gXjmx']");
            //strTmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
            //if (string.IsNullOrEmpty(strTmp))
            //{
            //    pNqdqxz++;
            //}
            //sbPost.Append("&gXjmx=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));


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

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='gTszl'][@checked]");

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
            //            sbPost.Append("&gTszl=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            //nodes = doc.DocumentNode.SelectNodes("//input[@name='gFywzlcs'][@checked]");

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
            //            sbPost.Append("&gFywzlcs=").Append(n.Attributes.Contains("value"));
            //        }
            //    }
            //}

            #endregion

            #region CD_CHD_FOLLOWUP

            DataTable dt = ds.Tables["CD_CHD_FOLLOWUP"];

            DataRow dr = dt.Rows[0];

            strTmp = dr["Height"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }
            sbPost.Append("&dSg=").Append(strTmp);

            strTmp = dr["NextVisitDate"].ToString() == "" ? Convert.ToDateTime(padSFDate).AddYears(1).ToString("yyyy-MM-dd") : Convert.ToDateTime(dr["NextVisitDate"]).ToString("yyyy-MM-dd");
            sbPost.Append("&gXcsfsj=").Append(strTmp);


            strTmp = dr["VisitDoctor"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gSfys=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));

            //随访医生建议
            strTmp = dr["DoctorView"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gBcsfysjy=").Append(CommonExtensions.GetUrlEncodeVal(strTmp));


            strTmp = dr["Systolic"].ToString();

            double dtmp = 0;

            if (strTmp != "" && double.TryParse(strTmp, out dtmp))
            {
                strTmp = dtmp.ToString("#");
            }

            sbPost.Append("&dSsy=").Append(strTmp);

            string strTmp2 = dr["Diastolic"].ToString();

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

            strTmp = dr["Compliance"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gFyycx=").Append(strTmp);

            strTmp = dr["FollowType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gCcsffl=").Append(strTmp);

            strTmp = dr["VisitType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                pNqdqxz++;
            }

            sbPost.Append("&gSffs=").Append(string.IsNullOrEmpty(strTmp) ? "" : strTmp.Replace("4", "99"));

            #region 2.0新增字段
            //冠心病类型  ChdType                   
            sbPost.Append("&gGxblx=").Append(dr["ChdType"].ToString().Replace("6", "99").Replace("5", "6"));

            //症状  Symptom 
            string str = dr["Symptom"].ToString();
            var sr = str.Split(',');
            foreach (var item in sr)
            {
                sbPost.Append("&gMqzz=").Append(Getzzsc(item));
            }

            //身高 Height 
            sbPost.Append("&dSg=").Append(dr["Height"].ToString());

            //BMI 
            sbPost.Append("&dBmi=").Append(dr["BMI"].ToString());

            //空腹血糖 FPGL 
            sbPost.Append("&dKfxt=").Append(dr["FPGL"].ToString());

            //总胆固醇 TC 
            sbPost.Append("&dZdgc=").Append(dr["TC"].ToString());

            //甘油三酯 TG 
            sbPost.Append("&dGysz=").Append(dr["TG"].ToString());

            //血清低密度 LowCho 
            sbPost.Append("&dDmddb=").Append(dr["LowCho"].ToString());

            //血清高密度 HeiCho 
            sbPost.Append("&dGmddb=").Append(dr["HeiCho"].ToString());

            //心电图检查结果 EcgCheckResult 
            sbPost.Append("&gXdtjc=").Append(HtmlHelper.GetUrlEncodeVal(dr["EcgCheckResult"].ToString()));

            //心电图运动符合 EcgExerciseResult 
            sbPost.Append("&gXdtydfh=").Append(HtmlHelper.GetUrlEncodeVal(dr["EcgExerciseResult"].ToString()));

            //冠影动脉造血效果 CAG 
            sbPost.Append("&gGzdmzy=").Append(HtmlHelper.GetUrlEncodeVal(dr["CAG"].ToString()));

            //心肌酶学 EnzymesResult 
            sbPost.Append("&gXjmx=").Append(HtmlHelper.GetUrlEncodeVal(dr["EnzymesResult"].ToString()));

            //心脏彩超 HeartCheckResult 
            sbPost.Append("&gXzcc=").Append(HtmlHelper.GetUrlEncodeVal(dr["HeartCheckResult"].ToString()));

            //每日吸烟量 SmokeDay 
            sbPost.Append("&mXysl=").Append(dr["SmokeDay"].ToString().Replace(".00", ""));


            //每日饮酒量 DrinkDay 
            sbPost.Append("&mYjsl=").Append(dr["DrinkDay"].ToString().Replace(".00", ""));

            //每周运动次数 SportWeek 
            sbPost.Append("&mYdpl=").Append(dr["SportWeek"].ToString());

            //每次运动时间 SportMinute 
            sbPost.Append("&mYdcxsj=").Append(dr["SportMinute"].ToString());

            //特殊治疗 SpecialTreated   gTszl
            string tszl = dr["SpecialTreated"].ToString();
            string srr = "";
            if (!string.IsNullOrWhiteSpace(tszl))
            {
                var tss = tszl.Split(',');
                int i3 = 0;
                foreach (var item in tss)
                {
                    if (int.TryParse(item, out i3))
                    {
                        srr += (i3 - 1).ToString();
                    }
                    sbPost.Append("&gTszl=").Append(srr);
                    srr = "";
                }
            }

            //非药物治疗措施 NondrugTreat 
            srr = dr["NondrugTreat"].ToString();
            int i4 = 0;
            string sr4 = "";
            if (!string.IsNullOrWhiteSpace(srr))
            {
                var yw = srr.Split(',');
                foreach (var item in yw)
                {
                    if (int.TryParse(item, out i4))
                    {
                        sr4 += (i4 - 1).ToString();
                    }
                    sbPost.Append("&gFywzlcs=").Append(sr4.Replace("0", "99"));
                    sr4 = "";
                }
            }

            //并发症其他 Syndromeother 
            sbPost.Append("&gCcsfflbfz=").Append(HtmlHelper.GetUrlEncodeVal(dr["Syndromeother"].ToString()));

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
                    // string strYF = "每日" + dt.Rows[i]["DailyTime"].ToString() + "次,每次" + dt.Rows[i]["EveryTimeMg"].ToString() + "mg";
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
            sbPost.Append("&wzd=").Append(((27 - pNqdqxz) * 100.0 / 27).ToString("#"));

            // （手动）居民签名
            sbPost.Append("&sdjmqm=").Append(HtmlHelper.GetUrlEncodeVal(pm.memberName));

            //http://222.133.17.194:9080/sdcsm/coronaryVisit/update.action
            // 修改
            returnString = web.PostHttp(baseUrl + "coronaryVisit/update.action", sbPost.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return "更新失败！";
            }

            if (returnString.Contains("出现异常"))
            {
                return "更新失败！";
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
            callback("下载-冠心病信息..." + currentIndex + "/" + totalRows);

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
                    callback("EX-下载冠心病信息失败，请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 获取冠心病key和页面信息
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

            string postData = "page.currentPage=" + pageNum + "&search=1&siteid=" + key + "&branch=on&dDazt=1&dqjg=" + key;
            string returnString = web.PostHttp(baseUrl + "/coronary/list.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);

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
        /// 获取冠心病信息下载
        /// </summary>
        private void GetInfoByPersonList(List<PersonModel> lstAllPm, Action<string> callback)
        {
            foreach (var pm in lstAllPm)
            {
                TryDownByIDs(pm, 1, callback);

                callback("下载-冠心病信息..." + currentIndex + "/" + totalRows);

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

            // http://222.133.17.194:9080/sdcsm/stroke/list.action?search=4&siteid=371481B10001&branch=on&dDazt=1\
            //http://222.133.17.194:9080/sdcsm/coronary/list.action?search=1&siteid=371481B10001&branch=on&dDazt=1
            string postData = "search=1&siteid=" + key + "&branch=on&dDazt=1&dqjg=" + key;
            string returnString = web.PostHttp(baseUrl + "/coronary/list.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
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
                    callback("EX-冠心病信息:身份证[" + idcard + "]:下载信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;

                    TryDownByIDs(person, tryCount, callback);
                }
                else
                {
                    callback("EX-冠心病信息:身份证[" + idcard + "]:下载信息失败。请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 根据标识信息，获取信息，下载
        /// </summary>
        /// <param name="pm"></param>
        private void GetInfo(PersonModel pm)
        {
            DataSet ds = DataSetTmp.GxbDataSet; //数据库表架构
            DataSet dsSave = new DataSet();

            WebHelper web = new WebHelper();
            CommonBusiness.CommonDAOBusiness dao = new CommonBusiness.CommonDAOBusiness();
            string postData = "";
            string returnString = "";

            #region  CD_CHD_FOLLOWUP 随访

            List<SFClass> lstSF = GetSFxxLst(pm.pid);

            string strtmp = "";
            if (lstSF.Count > 0)
            {
                SFClass sf = lstSF[0];
                //http://222.133.17.194:9080/sdcsm/coronaryVisit/toUpdate.action?id=14804

                //获取随访类表
                postData = "id=" + lstSF[0].key;
                returnString = web.PostHttp(baseUrl + "/coronaryVisit/toUpdate.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                DataTable dtData = ds.Tables["CD_CHD_FOLLOWUP"].Clone();
                DataRow dr = dtData.NewRow();


                dr["RecordID"] = pm.pid.Substring(0, 17);
                dr["IDCardNo"] = pm.idNumber;

                var node = doc.DocumentNode.SelectSingleNode("//input[@name='dSsy']");
                dr["Systolic"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSzy']");
                dr["Diastolic"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dTz']");
                dr["Weight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSg']");
                dr["Height"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl']");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                strtmp = strtmp == "" ? "" : "吸烟情况:" + strtmp + "支/天";

                string strtt = strtmp;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl']");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                strtmp = strtmp == "" ? "" : "饮酒情况:" + strtmp + "两/天";

                strtt = strtt == "" ? strtmp : strtt + ";" + strtmp;

                //支/天
                dr["Smoking"] = strtt;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl']");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                strtmp = strtmp == "" ? "" : "运动频率:" + strtmp + "次/周";

                strtt = strtmp;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj']");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                strtmp = strtmp == "" ? "" : "每次持续时间:" + strtmp + "分钟/次";

                strtt = strtt == "" ? strtmp : strtt + ";" + strtmp;

                dr["Sports"] = strtt;

                // 服药依从性
                node = doc.DocumentNode.SelectSingleNode("//select[@name='gFyycx']/option[@selected]");
                dr["Compliance"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value.Replace("4", "");

                //随访方式
                var node3 = doc.DocumentNode.SelectNodes("//input[@name='gCcsffl'][@checked]");
                string sffs = "";
                if (null != node3)
                {
                    foreach (var item in node3)
                    {
                        sffs += item.Attributes["value"].Value + ",";
                    }
                }

                dr["FollowType"] = sffs.TrimEnd(',');

                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXcsfsj']");
                dr["NextVisitDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSfys']");
                dr["VisitDoctor"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                dr["VisitDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSffs'][@checked]");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                dr["VisitType"] = strtmp == "99" ? "4" : strtmp;

                //随访医生建议
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gBcsfysjy']");
                dr["DoctorView"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //dr["Symptom"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                string sfrq = Convert.ToDateTime(dr["VisitDate"].ToString()).ToString("yyyy-MM-dd");

                #region 2.0新增字段
                //冠心病类型
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gGxblx'][@checked]");
                dr["ChdType"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value.Replace("6", "5").Replace("99", "6");

                //  症状 Symptom
                var node2 = doc.DocumentNode.SelectNodes("//input[@name='gMqzz'][@checked]");
                string st4 = "";
                if (node2 != null)
                {

                    foreach (var on in node2)
                    {
                        st4 += Getzz(on.Attributes["value"].Value) + ",";
                    }
                }

                dr["Symptom"] = st4.TrimEnd(',');

                //身高  Height 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dSg']");
                dr["Height"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


                //体质指数  BMI 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dBmi']");
                dr["BMI"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //空腹血糖 FPGL 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dKfxt']");
                dr["FPGL"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //总胆固醇 TC 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dZdgc']");
                dr["TC"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //甘油三脂 TG 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGysz']");
                dr["TG"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //血清低密度脂蛋白胆固醇 LowCho 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dDmddb']");
                dr["LowCho"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //血清高密度脂蛋白胆固醇 HeiCho 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGmddb']");
                dr["HeiCho"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //心电图检查结果 EcgCheckResult 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXdtjc']");
                dr["EcgCheckResult"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //心电图运动负荷试验结果 EcgExerciseResult 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXdtydfh']");
                dr["EcgExerciseResult"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //冠状动脉造影结果 CAG 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gGzdmzy']");
                dr["CAG"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //心肌酶学检查结果 EnzymesResult 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXjmx']");
                dr["EnzymesResult"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


                //心脏彩超检查结果 HeartCheckResult 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXzcc']");
                dr["HeartCheckResult"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                // 每日吸烟量 SmokeDay
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mXysl']");
                dr["SmokeDay"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                // 每日饮酒 DrinkDay
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYjsl']");
                dr["DrinkDay"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //每周运动次数 SportWeek 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdpl']");
                dr["SportWeek"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //每次运动时间SportMinute 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='mYdcxsj']");
                dr["SportMinute"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //特殊治疗 SpecialTreated 
                node2 = doc.DocumentNode.SelectNodes("//input[@name='gTszl'][@checked]");
                string tszl = "";
                if (node2 != null)
                {
                    foreach (var item in node2)
                    {
                        int i = 0;
                        if (int.TryParse(item.Attributes["value"].Value, out i))
                        {
                            tszl += i + 1 + ",";
                        }
                    }
                }
                //  string tszl = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


                dr["SpecialTreated"] = tszl.ToString().TrimEnd(',');

                //非药物治疗措施  NondrugTreat 
                node2 = doc.DocumentNode.SelectNodes("//input[@name='gFywzlcs'][@checked]");
                //string fyw= node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                string fyw = "";
                if (node2 != null)
                {
                    foreach (var item in node2)
                    {
                        int i2 = 0;
                        if (int.TryParse(item.Attributes["value"].Value, out i2))
                        {
                            fyw += i2 + 1 + ",";
                        }

                    }

                }

                dr["NondrugTreat"] = fyw.Replace("100", "1").TrimEnd(',');

                // 并发症其他 Syndromeother 
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gCcsfflbfz']");
                dr["Syndromeother"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //是否转诊 IsReferral 
                // node = doc.DocumentNode.SelectSingleNode("//input[@name='gCcsfflbfz']");
                //   dr["IsReferral"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                #endregion

                dtData.Rows.Add(dr);
                // dsSave.Tables.Add(dtData);
                outkey = dao.SaveMainTable(dtData, pm.idNumber, sfrq);
                var nodes = doc.DocumentNode.SelectNodes("//tbody[@id='dyTbody']/tr[position()>1]");

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
                        dr["Type"] = "4";
                        dr["OutKey"] = outkey;
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

                    dsSave.Tables.Add(dtData);
                }

            }
            #endregion

            dao.SaveDataSet(dsSave, pm.idNumber, "4", outkey.ToString());

            dsSave.Tables.Clear();
        }

        #endregion

        private List<SFClass> GetSFxxLst(string key)
        {
            List<SFClass> lstSF = new List<SFClass>();

            string postData = "dGrdabh=" + key;
            //http://222.133.17.194:9080/sdcsm/coronaryVisit/toAdd.action?dGrdabh=371481020010013201

            WebHelper web = new WebHelper();

            string returnString = web.PostHttp(baseUrl + "coronaryVisit/toAdd.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);

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

        #region
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

        //处理症状  下载
        private string Getzz(string s)
        {
            string str = "";
            switch (s)
            {
                case "0":
                    str = "1";
                    break;
                case "1":
                    str = "5";
                    break;
                case "2":
                    str = "6";
                    break;
                case "3":
                    str = "7";
                    break;
                case "4":
                    str = "8";
                    break;
                case "5":
                    str = "9";
                    break;
                case "6":
                    str = "10";
                    break;
            }
            return str;
        }


        //处理症状  上传
        private string Getzzsc(string s)
        {
            string str = "";
            switch (s)
            {
                case "1":
                    str = "0";
                    break;
                case "5":
                    str = "1";
                    break;
                case "6":
                    str = "2";
                    break;
                case "7":
                    str = "3";
                    break;
                case "8":
                    str = "4";
                    break;
                case "9":
                    str = "5";
                    break;
                case "10":
                    str = "6";
                    break;
            }
            return str;
        }

        #endregion
    }
}
