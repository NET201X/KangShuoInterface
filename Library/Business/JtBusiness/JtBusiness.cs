using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using HtmlAgilityPack;
using DAL;
using Model.InfoModel;
using Utilities.Common;

namespace JtBusiness
{
    public class JtBusiness
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
        public CookieContainer SysCookieContainer { get; set; }

        /// <summary>
        /// 待上传数据
        /// </summary>
        public IList<DataSet> lstUploadData = new List<DataSet>();

        public List<PersonModel> lstPerson = new List<PersonModel>();

        public string loginKey { set; get; }

        #endregion

        /// <summary>
        /// 家庭信息下载
        /// </summary>
        /// <param name="callback"></param>
        public void DownJT(Action<string> callback)
        {
            TryDownJT(1, callback);
            GC.Collect();
        }

        public void DownJTByIds(string ids, Action<string> callback)
        {
            try
            {
                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();

                var idsa = ids.Split(',');
                currentIndex = 1;
                foreach (var id in idsa)
                {
                    string jtKey = GetJTKeyByID(id);

                    if (string.IsNullOrEmpty(jtKey))
                    {
                        callback("下载-家庭信息信息档案..." + currentIndex + "/" + idsa.Length);
                        currentIndex++;
                        continue;
                    }

                    GetJtPeopleInfo(jtKey, 1, callback);

                    callback("下载-家庭信息信息档案..." + currentIndex + "/" + idsa.Length);
                    currentIndex++;
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
        /// 上传入口
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="callback"></param>
        public void SaveFamily(Action<string> callback)
        {
            if (lstUploadData.Count == 0)
            {
                callback("EX-家庭信息:无上传数据");
                return;
            }
            int currentIndex = 1;

            foreach (DataSet ds in lstUploadData)
            {
                string idcard = "";
                DataTable familyDT = ds.Tables["ARCHIVE_FAMILY_INFO"];

                if (familyDT != null && familyDT.Rows.Count > 0)
                {
                    idcard = familyDT.Rows[0]["IDCardNo"].ToString();
                }

                if (string.IsNullOrEmpty(idcard))
                {
                    continue;
                }

                TrySaveFamily(idcard, ds, 1, callback);

                callback("上传-家庭信息..." + currentIndex + "/" + lstUploadData.Count);
                currentIndex++;
            }
        }

        #region 上传

        private void TrySaveFamily(string idNumber, DataSet ds, int tryCount, Action<string> callback)
        {
            try
            {
                //家庭档案号
                string jtKey = GetJTKeyByID(idNumber);

                if (!string.IsNullOrEmpty(jtKey))
                {
                    // 修改家庭信息
                    EditJT(jtKey, ds);

                    DataTable dt = ds.Tables["memBer"];

                    foreach (DataRow row in dt.Rows)
                    {
                        string idcardno = row["IDCardNo"].ToString();
                        string strHouseRelation = row["HouseRelation"].ToString();

                        // 户主时跳过
                        if (idcardno.ToUpper() == idNumber.ToUpper())
                        {
                            continue;
                        }

                        CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();

                        PersonModel person = cb.GetGrdaByIDCardNo(idcardno, loginKey, SysCookieContainer);

                        if (person == null)
                        {
                            continue;
                        }

                        string merberID = "";

                        // 没有家庭的，则直接增加
                        if (person.fid == "")
                        {
                            merberID = GetMemID(jtKey, person.pid);

                            if (!string.IsNullOrEmpty(merberID))
                            {
                                AddJTMem(merberID, strHouseRelation, jtKey);
                            }

                            continue;
                        }

                        // 若是已经存在，则不操作。（关系未修改）
                        if (person.fid.ToUpper() == jtKey.ToUpper())
                        {
                            continue;
                        }

                        JTClass jtclass = GetJtXXInfo(person.fid);

                        if (string.IsNullOrEmpty(jtclass.JTKey))
                        {
                            continue;
                        }

                        // 户主变更，删除当前家庭中人员
                        foreach (var p in jtclass.JTPeoples)
                        {
                            if (p.houseRelation == "1")
                            {
                                continue;
                            }

                            DelJTMem(p.pid, p.houseRelation, jtclass.JTKey, jtclass.JTCount);

                            jtclass.JTCount--;
                        }

                        // 删除户主
                        var tmpM = jtclass.JTPeoples.Where(m => m.houseRelation == "1").FirstOrDefault();

                        if (tmpM != null)
                        {
                            DelJTMem(tmpM.pid, tmpM.houseRelation, jtclass.JTKey, 1);
                        }

                        merberID = GetMemID(jtKey, person.pid);

                        if (!string.IsNullOrEmpty(merberID))
                        {
                            AddJTMem(merberID, strHouseRelation, jtKey);
                        }
                    }
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
                    callback("EX-家庭信息:身份证[" + idNumber + "]:上传家庭信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);
                    tryCount++;
                    TrySaveFamily(idNumber, ds, tryCount, callback);
                }
                else
                {
                    callback("EX-家庭信息:身份证[" + idNumber + "]:上传家庭信息失败。请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 获取人员ID
        /// </summary>
        /// <param name="jtKey"></param>
        /// <param name="grdah"></param>
        /// <returns></returns>
        private string GetMemID(string jtKey, string grdah)
        {
            //http://222.133.17.194:9080/sdcsm/home/homeAddToQuery.action?dJtdabh=3714810200100123&jslx=2&condition=371481010340011301

            WebHelper web = new WebHelper();

            StringBuilder postStr = new StringBuilder();

            postStr.Append("dJtdabh=").Append(jtKey);
            postStr.Append("&jslx=2");
            postStr.Append("&condition=").Append(grdah);

            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/home/homeAddToQuery.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            string returnVal = "";

            if (doc == null)
            {
                return returnVal;
            }

            var node = doc.DocumentNode.SelectSingleNode("//table[@class='QueryTable']//tr[position()>1][1]/td[1]/input[@name='id']");

            returnVal = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            return returnVal;
        }

        /// <summary>
        /// 家庭注销人员
        /// </summary>
        /// <param name="grdah"></param>
        /// <param name="strHouseRelation"></param>
        /// <param name="jtKey"></param>
        /// <param name="jtcygsN"></param>
        private void DelJTMem(string grdah, string strHouseRelation, string jtKey, int jtcygsN)
        {
            //http://222.133.17.194:9080/sdcsm/home/hzgxzx.action?dGrdabh='+dGrdabh+'&dJtdabh='+dJtdabh+'&dYhzgx='+dYhzgx+'&jtcygs='+jtcygs;
            //http://222.133.17.194:9080/sdcsm/home/hzgxzx.action?dGrdabh=371481010340011301&dJtdabh=3714810200100128&dYhzgx=4&jtcygs=2
            WebHelper web = new WebHelper();

            StringBuilder postStr = new StringBuilder();

            strHouseRelation = GetHouseRelationForWeb(strHouseRelation);

            postStr.Append("dGrdabh=").Append(grdah);
            postStr.Append("&dJtdabh=").Append(jtKey);
            postStr.Append("&dYhzgx=").Append(strHouseRelation);
            postStr.Append("&jtcygs=").Append(jtcygsN);

            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/home/hzgxzx.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
        }

        /// <summary>
        /// 家庭增加人员
        /// </summary>
        /// <param name="memid"></param>
        /// <param name="strHouseRelation"></param>
        /// <param name="jtKey"></param>
        private void AddJTMem(string memid, string strHouseRelation, string jtKey)
        {
            //http://222.133.17.194:9080/sdcsm/home/homeEnterIn.action?id="+id+"&dYhzgx="+关系+"&dJtdabh="+dJtdabh;

            WebHelper web = new WebHelper();

            StringBuilder postStr = new StringBuilder();

            strHouseRelation = GetHouseRelationForWeb(strHouseRelation);

            postStr.Append("id=").Append(memid);
            postStr.Append("&dYhzgx=").Append(strHouseRelation);
            postStr.Append("&dJtdabh=").Append(jtKey);

            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/home/homeEnterIn.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
        }

        /// <summary>
        /// 修改家庭信息
        /// </summary>
        /// <param name="jtKey"></param>
        /// <param name="ds"></param>
        private void EditJT(string jtKey, DataSet ds)
        {
            WebHelper web = new WebHelper();

            StringBuilder postStr = new StringBuilder();

            DataTable dtJT = ds.Tables["ARCHIVE_FAMILY_INFO"];

            DataRow drJT = dtJT.Rows[0];

            #region 家庭信息

            postStr.Append("djtdabh=").Append(jtKey);
            int qdqxzN = 0;

            //平房
            string strTmp = drJT["HouseType"].ToString();

            if (string.IsNullOrEmpty(strTmp))
            {
                qdqxzN++;
            }

            postStr.Append("&dzflx=").Append(strTmp);

            //厕所
            strTmp = drJT["ToiletType"].ToString();
            strTmp = strTmp == "4" ? "99" : strTmp;

            if (string.IsNullOrEmpty(strTmp))
            {
                qdqxzN++;
            }
            postStr.Append("&dcslx=").Append(strTmp);

            //低保
            strTmp = drJT["IsPoorfy"].ToString();
            strTmp = strTmp == "0" ? "2" : strTmp;

            if (string.IsNullOrEmpty(strTmp))
            {
                qdqxzN++;
            }

            postStr.Append("&dsfdb=").Append(strTmp);

            postStr.Append("&qdqxz=").Append(qdqxzN);

            postStr.Append("&wzd=").Append(((6 - qdqxzN) * 100.0 / 6).ToString("#"));

            //收入
            strTmp = drJT["IncomeAvg"].ToString();
            strTmp = strTmp == "" ? "0" : strTmp;
            postStr.Append("&drjsr=").Append(strTmp);

            //住房面积
            strTmp = drJT["HouseArea"].ToString();
            strTmp = strTmp == "" ? "0" : strTmp;
            postStr.Append("&djzmj=").Append(strTmp);

            //斤植物油/月
            strTmp = drJT["Monthoil"].ToString();
            strTmp = strTmp == "" ? "0" : strTmp;
            postStr.Append("&dcyl=").Append(strTmp);

            //克盐/月
            strTmp = drJT["MonthSalt"].ToString();
            strTmp = strTmp == "" ? "0" : strTmp;
            postStr.Append("&dcyanl=").Append(strTmp);

            #endregion

            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/home/homeInfoSave.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
        }

        private string GetJTKeyByID(string idNumber)
        {
            string jtKey = GetJTKeyByIDUpperOrLower(idNumber.ToUpper());

            return string.IsNullOrEmpty(jtKey) ? GetJTKeyByIDUpperOrLower(idNumber.ToLower()) : jtKey;
        }

        private string GetJTKeyByIDUpperOrLower(string strIDCardNo)
        {
            WebHelper web = new WebHelper();
            //http://222.133.17.194:9080/sdcsm/home/homequery.action?hanxiashu=on&hzsfzh=230101193809096018&dqjg=371481B10001
            StringBuilder postStr = new StringBuilder();
            #region
            postStr.Append("hanxiashu=").Append("on");

            postStr.Append("&hzsfzh=").Append(strIDCardNo);

            if (loginKey.Length == 16)
            {
                postStr.Append("&dqjg=").Append(loginKey.Substring(0, 12));
            }
            else
            {
                postStr.Append("&dqjg=").Append(loginKey.Substring(0, 15));
            }

            #endregion

            string returnVal = "";

            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/home/homequery.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null)
            {
                return returnVal;
            }

            var node = doc.DocumentNode.SelectSingleNode("//table[@class='QueryTable']//tr[position()>1][1]/td[2]");

            if (node != null)
            {
                returnVal = node.InnerText.Trim();
            }

            return returnVal;
        }

        private JTClass GetJtXXInfo(string jtKey)
        {
            JTClass jtInfo = new JTClass();

            WebHelper web = new WebHelper();
            StringBuilder postStr = new StringBuilder();

            postStr.Append("djtdabh=").Append(jtKey);

            //http://222.133.17.194:9080/sdcsm/home/homeshow.action?djtdabh=3714810102100441
            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/home/homeshow.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null)
            {
                return jtInfo;
            }

            var nodetrs = doc.DocumentNode.SelectNodes("//div[@id='jtcyxg']//table[@id='table1']//tr[position()>3]");

            List<PersonModel> lstpm = new List<PersonModel>();

            if (nodetrs != null)
            {
                var nodeJT = doc.DocumentNode.SelectSingleNode("//input[@name='jtcygs']");
                jtInfo.JTCount = nodeJT == null || !nodeJT.Attributes.Contains("value") ? 1 : Convert.ToInt32(nodeJT.Attributes["value"].Value);

                foreach (var jtP in nodetrs)
                {
                    var node = jtP.SelectSingleNode("td[1]/input[1]");
                    var nodeR = jtP.SelectSingleNode("td[2]/select/option[@selected]");

                    PersonModel pm = new PersonModel();

                    pm.pid = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    pm.houseRelation = nodeR == null || !nodeR.Attributes.Contains("value") ? "" : nodeR.Attributes["value"].Value;

                    if (!string.IsNullOrEmpty(pm.pid))
                    {
                        lstpm.Add(pm);
                    }
                }

                jtInfo.JTKey = jtKey;
                jtInfo.JTPeoples = lstpm;
            }

            return jtInfo;
        }
        #endregion

        #region 下载

        /// <summary>
        /// 根据个人档案下载家庭
        /// </summary>
        /// <param name="pm"></param>
        /// <param name="callback"></param>
        public void DownJTByPerson(PersonModel pm, Action<string> callback)
        {
            try
            {
                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                if (pm != null && !string.IsNullOrEmpty(pm.idNumber))
                {
                    if (!string.IsNullOrEmpty(pm.fid))
                    {
                        GetJtPeopleInfo(pm.fid, 1, callback);
                    }
                   
                }

                callback("下载-家庭信息信息档案..." + currentIndex + "/" + totalRows);
                currentIndex++;

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

        private void TryDownJT(int tryCount, Action<string> callback)
        {
            try
            {
                GeJtKeyAndInfo(callback);
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
                    TryDownJT(tryCount, callback);
                }
                else
                {
                    callback("EX-家庭信息:获取家庭信息失败。请确保网路畅通。");
                }
            }
        }

        private void GeJtKeyAndInfo(Action<string> callback)
        {
            int pageSum = 0;
            List<string> listJtKey = GetJtFirstKeyAndInfo(callback, out pageSum);

            //开始获取当前页信息
            GetJtPeoples(listJtKey, callback);

            //翻页
            for (var i = 2; i < pageSum + 1; i++)
            {
                //指定页的人员信息获取，并获取数据
                listJtKey.Clear();
                listJtKey = GetPageInfo(i, callback);
                GetJtPeoples(listJtKey, callback);
            }
        }

        //根据页码，获取指定页信息
        public List<string> GetPageInfo(int pageNum, Action<string> callback)
        {
            WebHelper web = new WebHelper();

            StringBuilder postStr = new StringBuilder();
            postStr.Append("null=");
            postStr.Append("&hanxiashu=on");

            if (loginKey.Length == 16)
            {
                postStr.Append("&dqjg=").Append(loginKey.Substring(0, 12));
            }
            else
            {
                postStr.Append("&dqjg=").Append(loginKey.Substring(0, 15));
            }

            postStr.Append("&page.currentPage=").Append(pageNum);
            postStr.Append("&hzxm=&hzdabh=&starttime=&endtime=&hzsfzh=&djtdabh=&sfhg=&createuser=&ssjd=&ssjwh=&ssxxdz=");
            //http://222.133.17.194:9080/sdcsm/home/homequery.action?dqjg=371481B20001028&hanxiashu=on
            //http://20.1.1.79:9081/sdcsm/home/homequery.action?page.currentPage=2&status=ajax&null=%E5%B8%82%E4%B8%AD%E8%A1%97%E9%81%93%E5%8A%9E%E4%BA%8B%E5%A4%84%E7%A4%BE%E5%8C%BA%E5%8D%AB%E7%94%9F%E6%9C%8D%E5%8A%A1%E4%B8%AD%E5%BF%83&hanxiashu=on&dqjg=371481B10001&hzxm=&hzdabh=&starttime=&endtime=&hzsfzh=&djtdabh=&sfhg=&createuser=&ssjd=&ssjwh=&ssxxdz=&_=1527501912273
            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/home/homequery.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null)
            {
                callback("请求超时，稍后重试");
                return null;
            }
            List<PersonModel> listPerson = new List<PersonModel>();
            var nodes = doc.DocumentNode.SelectNodes("//table[@class='QueryTable']//tr");

            List<string> lstKey = new List<string>();
            for (var i = 1; i < nodes.Count; i++)
            {
                var node = nodes[i].SelectNodes("td");
                if (string.IsNullOrEmpty(node[1].InnerText.Trim()))
                {
                    currentIndex++;
                    continue;
                }

                lstKey.Add(node[1].InnerText.Trim());
            }

            return lstKey;
        }

        /// <summary>
        /// 获取第一页数据
        /// </summary>
        /// <param name="callback"></param>
        private List<string> GetJtFirstKeyAndInfo(Action<string> callback, out int pageSum)
        {
            pageSum = 0;
            WebHelper web = new WebHelper();
            StringBuilder postStr = new StringBuilder();
            postStr.Append("null=");
            postStr.Append("&hanxiashu=on");
            if (loginKey.Length == 16)
            {
                postStr.Append("&dqjg=").Append(loginKey.Substring(0, 12));
            }
            else
            {
                postStr.Append("&dqjg=").Append(loginKey.Substring(0, 15));
            }
            postStr.Append("&hzxm=&hzdabh=&starttime=&endtime=&hzsfzh=&djtdabh=&sfhg=&createuser=&ssjd=&ssjwh=&ssxxdz=");
            //null=&hanxiashu=on&dqjg=371481B10001&hzxm=&hzdabh=&starttime=&endtime=&hzsfzh=&djtdabh=&sfhg=&createuser=&ssjd=&ssjwh=&ssxxdz=
            //http://222.133.17.194:9080/sdcsm/home/homequery.action?dqjg=371481B20001028&hanxiashu=on
            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/home/homequery.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null)
            {
                callback("请求超时，稍后重试");
                return null;
            }

            currentIndex = 1;

            List<string> lstKey = new List<string>();
            var nodes = doc.DocumentNode.SelectNodes("//table[@class='QueryTable']//tr");

            for (var i = 1; i < nodes.Count; i++)
            {
                var node = nodes[i].SelectNodes("td");
                if (string.IsNullOrEmpty(node[1].InnerText.Trim()))
                {
                    currentIndex++;
                    continue;
                }
                lstKey.Add(node[1].InnerText.Trim());
            }

            var divNodes = doc.DocumentNode.SelectSingleNode("//div[@class='page_and_btn']").SelectSingleNode("ul").SelectNodes("li");
            var nodePage = doc.DocumentNode.SelectSingleNode("//input[@id='all']");
            string pages = nodePage == null || !nodePage.Attributes.Contains("value") ? "0" : nodePage.Attributes["value"].Value;
            //string pages = divNodes[10].InnerText;
            //pages = HtmlHelper.GetLastTagValue(pages, "共", "页");

            int.TryParse(pages, out pageSum);

            string rowSum = divNodes[0].InnerText;

            rowSum = rowSum.Substring(rowSum.IndexOf('：') + 1);
            int.TryParse(rowSum, out totalRows);
            return lstKey;
        }

        private void GetJtPeoples(List<string> lstjtKey, Action<string> callback)
        {
            foreach (string jtKey in lstjtKey)
            {
                GetJtPeopleInfo(jtKey, 1, callback);

                callback("下载-家庭信息信息档案..." + currentIndex + "/" + totalRows);

                currentIndex++;
            }
        }

        /// <summary>
        /// 根据信息，获取家庭档案
        /// </summary>
        /// <param name="listPerson"></param>
        /// <param name="callback"></param>
        private void GetJtPeopleInfo(string jtKey, int tryCount, Action<string> callback)
        {
            try
            {
                WebHelper web = new WebHelper();
                StringBuilder postStr = new StringBuilder();

                postStr.Append("djtdabh=").Append(jtKey);

                //http://112.6.122.71:9165/sdcsm/home/homeshow.action?djtdabh=3714240201900434
                //查询请求执行
                string returnString = web.GetHttp(baseUrl + "home/homeshow.action", postStr.ToString(), SysCookieContainer);
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                if (doc == null)
                {
                    return;
                }

                var nodetrs = doc.DocumentNode.SelectNodes("//div[@id='jtcyxg']//table[@id='table1']//tr[position()>3]");

                List<PersonModel> lstpm = new List<PersonModel>();

                PersonModel pmMaster = new PersonModel();

                if (nodetrs != null)
                {
                    var node0 = nodetrs[0].SelectSingleNode("td[1]/input[1]");
                    var nodeR0 = nodetrs[0].SelectSingleNode("td[2]/select/option[@selected]");

                    pmMaster.pid = node0 == null || !node0.Attributes.Contains("value") ? "" : node0.Attributes["value"].Value;
                    string pMNumber = "";

                    if (!string.IsNullOrEmpty(pmMaster.pid))
                    {
                        pMNumber = GetGrdaInfo(pmMaster, "");
                    }

                    if (string.IsNullOrEmpty(pMNumber))
                    {
                        return;
                    }

                    //家庭信息

                    DataSet dsT = GetDataTableTmp();

                    DataTable baseInfo = dsT.Tables["ARCHIVE_FAMILY_INFO"].Clone();
                    DataRow dr = baseInfo.NewRow();

                    dr["RecordID"] = jtKey;
                    dr["FamilyRecordID"] = pMNumber;
                    dr["IDCardNo"] = pMNumber;

                    var nodeJT = doc.DocumentNode.SelectSingleNode("//select[@id='dcslx']/option[@selected]");
                    dr["ToiletType"] = nodeJT == null || !nodeJT.Attributes.Contains("value") ? "" : nodeJT.Attributes["value"].Value;
                    dr["ToiletType"] = dr["ToiletType"].ToString() == "99" ? "4" : dr["ToiletType"];

                    nodeJT = doc.DocumentNode.SelectSingleNode("//select[@id='dzflx']/option[@selected]");
                    dr["HouseType"] = nodeJT == null || !nodeJT.Attributes.Contains("value") ? "" : nodeJT.Attributes["value"].Value;

                    nodeJT = doc.DocumentNode.SelectSingleNode("//select[@id='dsfdb']/option[@selected]");

                    string strPoor = nodeJT == null || !nodeJT.Attributes.Contains("value") ? "" : nodeJT.Attributes["value"].Value;
                    dr["IsPoorfy"] = strPoor == "2" ? "0" : strPoor;

                    nodeJT = doc.DocumentNode.SelectSingleNode("//input[@id='djzmj']");
                    dr["HouseArea"] = nodeJT == null || !nodeJT.Attributes.Contains("value") ? "" : nodeJT.Attributes["value"].Value;

                    nodeJT = doc.DocumentNode.SelectSingleNode("//input[@id='drjsr']");
                    dr["IncomeAvg"] = nodeJT == null || !nodeJT.Attributes.Contains("value") ? "" : nodeJT.Attributes["value"].Value;

                    nodeJT = doc.DocumentNode.SelectSingleNode("//input[@id='dcyanl']");
                    dr["Monthoil"] = nodeJT == null || !nodeJT.Attributes.Contains("value") ? "" : nodeJT.Attributes["value"].Value;

                    nodeJT = doc.DocumentNode.SelectSingleNode("//input[@id='dcyl']");
                    dr["MonthSalt"] = nodeJT == null || !nodeJT.Attributes.Contains("value") ? "" : nodeJT.Attributes["value"].Value;

                    nodeJT = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                    dr["CreatedDate"] = nodeJT == null || !nodeJT.Attributes.Contains("value") ? "" : nodeJT.Attributes["value"].Value;

                    baseInfo.Rows.Add(dr);

                    DataSet saveDS = new DataSet();
                    saveDS.Tables.Add(baseInfo);

                    CommonBusiness.CommonDAOBusiness dao = new CommonBusiness.CommonDAOBusiness();
                    dao.SaveDataSet(saveDS, pMNumber);
                    saveDS.Tables.Clear();

                    // 排除户主，从1开始
                    //for (int i = 1; i < nodetrs.Count; i++)
                    //{
                    //    var jtP = nodetrs[i];
                    //    var node = jtP.SelectSingleNode("td[1]/input[1]");
                    //    var nodeR = jtP.SelectSingleNode("td[2]/select/option[@selected]");

                    //    PersonModel pm = new PersonModel();

                    //    pm.pid = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //    if (!string.IsNullOrEmpty(pm.pid))
                    //    {
                    //        GetGrdaInfo(pm, pMNumber);
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);

                if (tryCount < MaxtryCount)
                {
                    callback("EX-家庭档案:家庭档案号[" + jtKey + "]:下载信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    GetJtPeopleInfo(jtKey, tryCount, callback);
                }
                else
                {
                    callback("EX-家庭档案:家庭档案号[" + jtKey + "]:下载信息失败。请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 获取基本信息
        /// </summary>
        /// <param name="person"></param>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        private string GetGrdaInfo(PersonModel person, string pNumberMaster)
        {
            WebHelper web = new WebHelper();

            string postData = "dah=" + person.pid + "&tz=2";
            string returnString = web.PostHttp(baseUrl + "/healthArchives/updateArchives.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            DataSet dsT = GetDataTableTmp();

            DataTable baseInfo = dsT.Tables["ARCHIVE_BASEINFO"].Clone();
            DataRow dr = baseInfo.NewRow();

            var node = doc.DocumentNode.SelectSingleNode("//input[@id='dSfzh']");

            string idnumber = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            if (string.IsNullOrEmpty(idnumber))
            {
                return "";
            }

            dr["IDCardNo"] = idnumber;
            dr["RecordID"] = person.pid;
            //与户主关系
            node = doc.DocumentNode.SelectSingleNode("//select[@id='dYhzgx']/option[@selected]");

            string strH = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

            dr["HouseRelation"] = GetHouseRelation(strH);

            dr["FamilyIDCardNo"] = string.IsNullOrEmpty(pNumberMaster) ? idnumber : pNumberMaster;

            baseInfo.Rows.Add(dr);

            DataSet saveDS = new DataSet();
            saveDS.Tables.Add(baseInfo);

            CommonBusiness.CommonDAOBusiness dao = new CommonBusiness.CommonDAOBusiness();
            dao.SaveDataSet(saveDS, idnumber);
            saveDS.Tables.Clear();

            return string.IsNullOrEmpty(pNumberMaster) ? idnumber : pNumberMaster;
        }

        #endregion

        #region 栏位对应

        private string GetHouseRelation(string code)
        {
            string tem = "";
            switch (code)
            {
                case "1":
                    tem = "1";
                    break;
                case "2":
                    tem = "2";
                    break;
                case "3":
                    tem = "7";
                    break;
                case "4":
                    tem = "13";
                    break;
                case "5":
                    tem = "3";
                    break;

                case "7":
                    tem = "5";
                    break;
                case "8":
                    tem = "9";
                    break;
                case "9":
                    tem = "10";
                    break;
                case "10":
                    tem = "11";
                    break;
                case "6":
                case "11":
                case "12":
                case "13":
                case "99":
                    tem = "15";
                    break;
            }

            return tem;
        }

        private string GetHouseRelationForWeb(string code)
        {
            string tem = "99";
            switch (code)
            {
                case "2":
                    tem = "2";
                    break;
                case "3":
                case "4":
                    tem = "5";
                    break;
                case "5":
                case "6":
                    tem = "7";
                    break;
                case "7":
                case "8":
                    tem = "3";
                    break;
                case "9":
                    tem = "8";
                    break;
                case "10":
                    tem = "9";
                    break;
                case "11":
                case "12":
                    tem = "10";
                    break;
                case "13":
                case "14":
                    tem = "4";
                    break;
                case "15":
                    tem = "99";
                    break;
            }

            return tem;
        }

        #endregion

        /// <summary>
        /// 模版
        /// </summary>
        /// <returns></returns>
        private DataSet GetDataTableTmp()
        {
            DataSet ds = new DataSet();

            #region 基本信息
            DataTable baseinfoDT = new DataTable();
            baseinfoDT.TableName = "ARCHIVE_BASEINFO";

            baseinfoDT.Columns.Add("RecordID");
            baseinfoDT.Columns.Add("IDCardNo");
            baseinfoDT.Columns.Add("FamilyIDCardNo");
            baseinfoDT.Columns.Add("HouseRelation");

            ds.Tables.Add(baseinfoDT);
            #endregion

            #region 家庭
            DataTable dtF = new DataTable();

            dtF.TableName = "ARCHIVE_FAMILY_INFO";

            dtF.Columns.Add("RecordID");
            dtF.Columns.Add("FamilyRecordID");
            dtF.Columns.Add("IDCardNo");
            dtF.Columns.Add("ToiletType");
            dtF.Columns.Add("HouseType");
            dtF.Columns.Add("IsPoorfy");

            dtF.Columns.Add("HouseArea");
            dtF.Columns.Add("IncomeAvg");

            dtF.Columns.Add("Monthoil");
            dtF.Columns.Add("MonthSalt");
            dtF.Columns.Add("CreatedDate");

            ds.Tables.Add(dtF);
            #endregion

            return ds;
        }
    }

    /// <summary>
    /// 简易家庭信息
    /// </summary>
    public class JTClass
    {
        public string JTKey { get; set; }

        public int JTCount { get; set; }

        public List<PersonModel> JTPeoples { get; set; }
    }
}
