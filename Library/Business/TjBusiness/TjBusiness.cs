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
using System.Drawing;
using System.IO;
using System.Threading;
using System.Xml;

namespace TjBusiness
{
    public class TjBusiness
    {
        #region

        string baseUrl = Config.GetValue("baseUrl");
        string qzUrl = Config.GetValue("qzUrl");
        string key = Config.GetValue("qzkey");
        string operate = Config.GetValue("qzOperate");
        string CreateTimeSameTj = Config.GetValue("CreateTimeSameTj");

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
        // public string SysCookie { get; set; }

        /// <summary>
        /// 系统cookie
        /// </summary>
        public CookieContainer SysCookieContainer { get; set; }

        /// <summary>
        /// 待上传数据
        /// </summary>
        public IList<DataSet> lstUploadData = new List<DataSet>();

        //public List<OrganizesData> lstOrganizesData = new List<OrganizesData>();

        public List<PersonModel> lstPerson = new List<PersonModel>();

        CommonBusiness.CommonDAOBusiness cDao = new CommonBusiness.CommonDAOBusiness();

        #endregion

        public List<Town> townList { set; get; }
        public string loginKey { set; get; }

        public string serverPath = "";

        public bool delSameTj = false;

        public bool uploadSign = false;
        public bool uploadTj = false;
        public bool uploadPic = false;


        #region 人群分类下载条件 2017-05-03添加
        public QueryList querylist { set; get; }

        public int count = 0;
        private DateTime lasttime;
        #endregion

        /// <summary>
        /// 下载体检信息入口-All
        /// </summary>
        /// <param name="callback"></param>
        public void DownLoadTj(Action<string> callback)
        {
            //TryDownLoadTj(1, callback);
            GC.Collect();
        }
        /// <summary>
        /// 下载体检信息入口-ID
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="callback"></param>
        public void DownLoadTJByIds(string ids, Action<string> callback)
        {
            try
            {
                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                currentIndex = 1;
                var idsa = ids.Split(',');
                foreach (var id in idsa)
                {
                    PersonModel person = cb.GetGrdaByIDCardNo(id, loginKey, SysCookieContainer);
                    if (person == null)
                    {
                        callback("下载-体检信息档案..." + currentIndex + "/" + idsa.Length);
                        currentIndex++;
                        continue;
                    }

                    GetTjInfoByPersonModel(person, 1, callback);
                    callback("下载-个人基本信息档案..." + currentIndex + "/" + idsa.Length);
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
        /// 根据个人档案查询体检
        /// </summary>
        /// <param name="pm"></param>
        /// <param name="callback"></param>
        public void DownInfoByPerson(PersonModel pm, Action<string> callback)
        {
            callback("下载-体检信息档案..." + currentIndex + "/" + totalRows);

            if (pm != null && !string.IsNullOrEmpty(pm.pid))
            {
                GetTjInfoByPersonModel(pm, 1, callback);
            }
            currentIndex++;
        }

        /// <summary>
        /// 上传体检信息入口-SaveOrUpdate
        /// </summary>
        /// <param name="callback"></param>
        public void SaveTJ(Action<string> callback)
        {
            if (lstUploadData == null)
            {
                callback("EX-无体检信息上传。");
                return;
            }
            currentIndex = 1;
            foreach (DataSet ds in lstUploadData)
            {
                DataTable dt = ds.Tables["ARCHIVE_CUSTOMERBASEINFO"];

                if (dt != null && dt.Rows.Count > 0)
                {
                    string ids = dt.Rows[0]["ID"].ToString();
                    string outke = dt.Rows[0]["IDCardNo"].ToString();
                    DataSet dsData = cDao.GetTjDataSet(ids, outke);//根据体检基本信息表ID查询体检信息  
                    TrySaveTj(dsData, 1, callback);
                }
                callback("上传-体检信息..." + currentIndex + "/" + lstUploadData.Count);

                currentIndex++;
                if (baseUrl.Contains("sdcsm_new"))
                {
                    System.Threading.Thread.Sleep((3) * 1000);
                }
            }
        }

        #region Update
        private void TrySaveTj(DataSet ds, int tryCount, Action<string> callback)
        {
            string idcard = ds.Tables["ARCHIVE_CUSTOMERBASEINFO"].Rows[0]["IDCardNo"].ToString();
            string checkedDate = ds.Tables["ARCHIVE_CUSTOMERBASEINFO"].Rows[0]["CheckDate"].ToString();
            string name = ds.Tables["ARCHIVE_CUSTOMERBASEINFO"].Rows[0]["CustomerName"].ToString();
            string outkey = ds.Tables["ARCHIVE_CUSTOMERBASEINFO"].Rows[0]["ID"].ToString();
            string fkdate = cDao.GetTjSignInfo(idcard, outkey);

            if (!string.IsNullOrWhiteSpace(checkedDate))
            {
                checkedDate = Convert.ToDateTime(checkedDate).ToString("yyyy-MM-dd");
            }
            try
            {
                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                PersonModel person = cb.GetGrdaByIDCardNo(idcard, loginKey, SysCookieContainer);

                if (person == null)
                {
                    callback("EX-体检档案:身份证[" + idcard + "],姓名[" + name + "]:平台尚未建档或者档案状态为非活动!");
                    return;
                }

                //更新平台档案号到终端
                int i = cDao.UpdateRecordId(idcard, person.pid);

                //查询体检列表
                var sfList = GetCheckedDate(person);
                string tjKey = "";
                var tjList = sfList.Where(m => m.sfDate == checkedDate).ToList();
                //bool bo = GetCheckedDate(person, checkedDate, out tjKey);

                string msg = "";
                if (uploadTj)
                {
                    if (tjList.Count > 0)
                    {
                        if (delSameTj)
                        {
                            //存在多笔相同数据，删除多余的
                            if (tjList.Count > 1)
                            {
                                for (int j = 1; j < tjList.Count; j++)
                                {
                                    DeleteTjxx(person, tjList[j].key);
                                }
                            }
                        }

                        //update 
                        tjKey = tjList[0].key;
                        msg = UpdateTj(person, tjKey, ds, fkdate, callback);
                    }
                    else
                    {
                        //禹城平台限制间隔2-3分钟之内的新建体检数据会上传不成功，需要暂停线程2分45秒钟后上传(20.1.1.37-禹城新平台网址)
                        string isDelayed = "";
                        try
                        {
                            isDelayed = Config.GetValue("isDelayed");
                        }
                        catch (Exception ex)
                        {
                        }

                        // 如果有配置延时上传或者平台地址为如下几个时，都需要延时上传
                        //if (isDelayed == "1" || baseUrl.Contains("222.133.47.126") || baseUrl.Contains("222.132.49.202"))
                        //{
                        if (count == 0)
                        {
                            lasttime = System.DateTime.Now;
                        }
                        else
                        {

                            TimeSpan span = DateTime.Now - lasttime;
                            int sleep = Convert.ToInt32(span.TotalSeconds);
                            string settime = "15";//Config.GetValue("sleeptime");

                            if (isDelayed == "1")
                            {
                                settime = Config.GetValue("sleeptime");
                            }
                            if (string.IsNullOrEmpty(settime))
                            {
                                settime = "135";
                            }

                            if (sleep <= int.Parse(settime))
                            {
                                sleep = int.Parse(settime) - sleep;
                                System.Threading.Thread.Sleep((sleep) * 1000);
                            }

                            lasttime = System.DateTime.Now;
                        }
                        //}

                        //save
                        msg = SaveTj(person, ds, fkdate, callback);

                        count++;
                    }
                }

                string qzmsg = "";
                string tpmsg = "";
                //上传签名
                if (uploadSign || uploadPic)
                {
                    sfList = GetCheckedDate(person);
                    tjKey = "";
                    tjList = sfList.Where(m => m.sfDate == checkedDate).ToList();

                    if (tjList != null && tjList.Count > 0)
                    {
                        tjKey = tjList[0].key;

                        //上传签字
                        if (uploadSign)
                        {
                            qzmsg = UploadSign(person, tjKey, checkedDate, fkdate, callback);
                        }

                        //上传B超心电图片
                        if (uploadPic)
                        {
                            tpmsg = UploadPic(person, tjKey, checkedDate, callback);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(msg) || !string.IsNullOrEmpty(qzmsg) || !string.IsNullOrEmpty(tpmsg))
                {
                    callback("EX-体检档案:身份证[" + idcard + "],姓名[" + name + "]:" + msg + " " + qzmsg + " " + tpmsg);
                }

                #region 老年人上传
                #region  生活能力

                DataTable dt = ds.Tables["OLDER_SELFCAREABILITY"];

                LnrBusiness.LnrBusiness lnr = new LnrBusiness.LnrBusiness();

                lnr.DefultDoctor = DefultDoctor;
                lnr.DefultDoctorName = DefultDoctorName;

                lnr.loginkey = loginKey;
                lnr.SysCookieContainer = SysCookieContainer;

                if (dt != null && dt.Rows.Count > 0)
                {
                    msg = "";
                    if (person == null || string.IsNullOrEmpty(person.pid))
                    {
                        return;
                    }

                    List<SFClass> lstSF = lnr.GetSFxxLst(person.pid);

                    string padSFDate = Convert.ToDateTime(dt.Rows[0]["FollowUpDate"]).ToString("yyyy-MM-dd");
                    var sfInfo2 = lstSF.Where(m => m.sfDate == padSFDate).ToList();

                    if (sfInfo2.Count > 0)
                    {
                        //更新随访
                        msg = lnr.UpdateLnr(ds, person, padSFDate, sfInfo2[0].key);
                    }
                    else
                    {
                        //新增随访
                        msg = lnr.AddLnr(ds, person, padSFDate);
                    }

                    if (!string.IsNullOrEmpty(msg))
                    {
                        callback("EX-生活自理能力:身份证[" + idcard + "],姓名[" + name + "]:" + msg);
                    }
                }
                #endregion

                #region 中医辨识-只上传体检表自理能力，不传中医体质
                //string zybj = Config.GetValue("uploadZybj");

                ////配置文件配置上传中医保健时才上传
                //if (zybj == "1")
                //{
                //    DataTable dt2 = ds.Tables["OLD_MEDICINE_CN"];
                //    if (dt2 == null || dt2.Rows.Count < 1)
                //    {
                //        return;
                //    }
                //    string id2 = dt2.Rows[0]["IDCardNo"].ToString();
                //    string dat2 = string.IsNullOrWhiteSpace(dt2.Rows[0]["RecordDate"].ToString()) ? "" : Convert.ToDateTime(dt2.Rows[0]["RecordDate"].ToString()).ToString("yyyy-MM-dd");

                //    List<SFClass> sflist = lnr.GetZylst2(person.idNumber, person.pid);
                //    var sfInfo = sflist.Where(m => m.sfDate == dat2).ToList();
                //    if (sfInfo.Count > 0)
                //    {
                //        lnr.Updatezlbs(ds, person, sflist[0]);
                //    }
                //    else
                //    {
                //        lnr.ADDzybs(ds, person);
                //    }
                //}
                #endregion
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
                    callback("EX-体检档案:身份证[" + idcard + "],姓名[" + name + "]:上传体检档案信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    TrySaveTj(ds, tryCount, callback);
                }
                else
                {
                    callback("EX-体检档案:身份证[" + idcard + "],姓名[" + name + "]:上传体检档案信息失败。请确保网路畅通。");
                }
            }

        }
        /// <summary>
        /// 新增体检信息
        /// </summary>
        /// <param name="person"></param>
        private string SaveTj(PersonModel person, DataSet ds, string fkdate, Action<string> call)
        {
            for (int index = 0; index < 5; index++)
            {
                try
                {
                    WebHelper web = new WebHelper();
                    DataRow baseinfoRow = ds.Tables["ARCHIVE_CUSTOMERBASEINFO"].Rows[0];
                    object renQun = baseinfoRow["PopulationType"];
                    StringBuilder sbStr = new StringBuilder();

                    // 中联佳裕禹城2.4版本修改 2017-01-17修改
                    #region 中联佳裕2.4版本修改 2017-01-17修改

                    //http://222.133.47.126:9080/sdcsm/health/healthtoAdd.action?dGrdabh=371482010010143401

                    string retString = web.PostHttp(baseUrl + "/health/healthtoAdd.action?dGrdabh=" + person.pid + "&ts=", "", "application/x-www-form-urlencoded", SysCookieContainer);
                    if (string.IsNullOrEmpty(retString))
                    {
                        return "新增失败！请确保网路畅通。";
                    }

                    string ysfield = ""; // 责任医生上传字段名
                    string ycgxyfield = "";//右侧高血压上传字段名
                    string ycdxyfield = "";//右侧低血压上传字段名
                    string twfield = "";//体温上传字段名
                    string sgfield = "";//身高上传字段名
                    string tzfield = "";//体重上传字段名
                    string ywfield = "";//腰围上传字段名
                    string lnrzlnl = "";

                    // 2017-07-17 v2.5.0添加
                    string field4 = "";
                    string field4Val = "";
                    string gwgxy = "";
                    string gwgxyVal = "";
                    int jkzdcount = 0;

                    HtmlDocument doc1 = HtmlHelper.GetHtmlDocument(retString);

                    if (doc1 == null)
                    {
                        return "新增失败！请确保网路畅通。";
                    }

                    var node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='field2']").ParentNode.SelectSingleNode("input[@onchange]");
                    if (node1 == null)
                    {
                        node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='field2']").ParentNode.SelectSingleNode("input[@onkeydown]");
                    }
                    ysfield = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;

                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gXyyc1']").ParentNode.SelectSingleNode("input[@onchange][1]");
                    if (node1 == null)
                    {
                        node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gXyyc1']").ParentNode.SelectSingleNode("input[@onkeydown]");
                    }
                    ycgxyfield = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;

                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gXyyc1']").ParentNode.SelectSingleNode("input[@onchange][2]");
                    if (node1 == null)
                    {
                        node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gXyyc1']").ParentNode.SelectSingleNode("input[@onkeydown][2]");
                    }
                    ycdxyfield = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;

                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gTw']").ParentNode.SelectSingleNode("input[@onchange]");
                    if (node1 == null)
                    {
                        node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gTw']").ParentNode.SelectSingleNode("input[@onkeydown]");
                    }
                    twfield = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;

                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gSg']").ParentNode.SelectSingleNode("input[@onchange]");
                    if (node1 == null)
                    {
                        node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gSg']").ParentNode.SelectSingleNode("input[@onkeydown]");
                    }
                    sgfield = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;

                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gTzh']").ParentNode.SelectSingleNode("input[@onchange]");
                    if (node1 == null)
                    {
                        node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gTzh']").ParentNode.SelectSingleNode("input[@onkeydown]");
                    }
                    tzfield = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;

                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gYw']").ParentNode.SelectSingleNode("input[@onchange]");
                    if (node1 == null)
                    {
                        node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gYw']").ParentNode.SelectSingleNode("input[@onkeydown]");
                    }
                    ywfield = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;

                    //2017-07-17 v2.5.0新加
                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='gwgxy']");
                    gwgxy = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;
                    gwgxyVal = node1 == null || !node1.Attributes.Contains("value") ? "" : node1.Attributes["value"].Value;

                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='field4']");
                    field4 = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;
                    field4Val = node1 == null || !node1.Attributes.Contains("value") ? "" : node1.Attributes["value"].Value;

                    var nodes = doc1.DocumentNode.SelectNodes("//input[@name='gJkzd']");
                    if (nodes != null)
                    {
                        jkzdcount = nodes.Count;
                    }

                    node1 = doc1.DocumentNode.SelectSingleNode("//input[@name='lnrzkpg']");
                    lnrzlnl = node1 != null ? "1" : "";

                    #endregion

                    sbStr.Append("timeflag=").Append("");
                    sbStr.Append("&qdqxz=").Append("1");
                    sbStr.Append("&wzd=").Append("100");
                    sbStr.Append("&dSfzh=").Append(person.idNumber);
                    sbStr.Append("&id=");
                    sbStr.Append("&upjktjFlag=0");
                    sbStr.Append("&status=").Append("");

                    //2017-07-17 v2.5.0新加
                    if (!string.IsNullOrEmpty(gwgxy))
                    {
                        sbStr.Append("&").Append(gwgxy).Append("=").Append(gwgxyVal);
                    }
                    if (!string.IsNullOrEmpty(field4))
                    {
                        sbStr.Append("&").Append(field4).Append("=").Append(field4Val);
                    }

                    #region 体检信息

                    #region ARCHIVE_CUSTOMERBASEINFO

                    //档案号
                    sbStr.Append("&dGrdabh=").Append(person.pid);

                    //生日
                    sbStr.Append("&tjkInfo.dCsrq=").Append(baseinfoRow["Birthday"].ToString() == "" ? "" : Convert.ToDateTime(baseinfoRow["Birthday"]).ToString("yyyy-MM-dd"));

                    //体检时间
                    string tjDate = baseinfoRow["CheckDate"].ToString() == "" ? DateTime.Now.ToString("yyyy-MM-dd") : Convert.ToDateTime(baseinfoRow["CheckDate"]).ToString("yyyy-MM-dd");
                    sbStr.Append("&happentime=").Append(tjDate);

                    //责任医生
                    sbStr.Append("&field2=").Append(HtmlHelper.GetUrlEncodeVal(baseinfoRow["Doctor"].ToString()));
                    if (!string.IsNullOrEmpty(ysfield))
                    {
                        sbStr.Append("&").Append(ysfield).Append("=").Append(HtmlHelper.GetUrlEncodeVal(baseinfoRow["Doctor"].ToString()));
                    }

                    //症状
                    var tem = baseinfoRow["Symptom"].ToString().Split(',');
                    foreach (var s in tem)
                    {
                        sbStr.Append("&gZhzh=").Append(s == "25" ? "99" : s);
                    }
                    sbStr.Append("&gZzqt=").Append(HtmlHelper.GetUrlEncodeVal(baseinfoRow["Other"].ToString()));

                    //签字板
                    sbStr.Append("&tjzkysqm=");

                    #endregion

                    #region  ARCHIVE_GENERALCONDITION

                    DataRow yibanDR = ds.Tables["ARCHIVE_GENERALCONDITION"].Rows[0];

                    //体温
                    sbStr.Append("&gTw=").Append(yibanDR["AnimalHeat"]);

                    //脉率
                    string pulseRate = yibanDR["PulseRate"].ToString();

                    if (pulseRate != "")
                    {
                        pulseRate = Math.Floor(double.Parse(pulseRate)).ToString();
                    }

                    sbStr.Append("&gMb=").Append(pulseRate);

                    //呼吸频率
                    string breathRate = yibanDR["BreathRate"].ToString();

                    if (breathRate != "")
                    {
                        breathRate = Math.Floor(double.Parse(breathRate)).ToString();
                    }

                    sbStr.Append("&gHx=").Append(breathRate);

                    //左侧高
                    sbStr.Append("&gXyzc1=").Append(yibanDR["LeftHeight"]);

                    //左侧低
                    sbStr.Append("&gXyzc2=").Append(yibanDR["LeftPre"]);

                    //左侧：原因
                    sbStr.Append("&zcyy=").Append(HtmlHelper.GetUrlEncodeVal(yibanDR["LeftReason"].ToString()));

                    //右侧高
                    sbStr.Append("&gXyyc1=").Append(yibanDR["RightHeight"]);

                    //中联佳裕禹城2.4版本修改 2017-01-17修改
                    if (!string.IsNullOrEmpty(ycgxyfield))
                    {
                        sbStr.Append("&").Append(ycgxyfield).Append("=").Append(yibanDR["RightHeight"]);
                    }
                    if (!string.IsNullOrEmpty(ycdxyfield))
                    {
                        sbStr.Append("&").Append(ycdxyfield).Append("=").Append(yibanDR["RightPre"]);
                    }

                    //右侧低
                    sbStr.Append("&gXyyc2=").Append(yibanDR["RightPre"]);

                    //右：原因
                    sbStr.Append("&ycyy=").Append(HtmlHelper.GetUrlEncodeVal(yibanDR["RightReason"].ToString()));

                    //是否加入高血压管理
                    sbStr.Append("&nrgxygl=").Append(renQun == null || renQun.ToString().Contains("6") ? "3" : "1");

                    //sbStr.Append("&nrgxyglold=").Append("");
                    //身高
                    sbStr.Append("&gSg=").Append(yibanDR["Height"]);

                    //体重
                    sbStr.Append("&gTzh=").Append(yibanDR["Weight"]);

                    //腰围
                    sbStr.Append("&gYw=").Append(yibanDR["Waistline"]);

                    //中联佳裕禹城2.4版本修改 2017-03-14修改
                    if (!string.IsNullOrEmpty(twfield))
                    {
                        sbStr.Append("&").Append(twfield).Append("=").Append(yibanDR["AnimalHeat"]);
                    }
                    if (!string.IsNullOrEmpty(sgfield))
                    {
                        sbStr.Append("&").Append(sgfield).Append("=").Append(yibanDR["Height"]);
                    }
                    if (!string.IsNullOrEmpty(tzfield))
                    {
                        sbStr.Append("&").Append(tzfield).Append("=").Append(yibanDR["Weight"]);
                    }
                    if (!string.IsNullOrEmpty(ywfield))
                    {
                        string yw = yibanDR["Waistline"].ToString();
                        if (!string.IsNullOrEmpty(yw))
                        {
                            yw = decimal.Parse(yw).ToString("0");
                        }
                        sbStr.Append("&").Append(ywfield).Append("=").Append(yw);
                    }

                    //体质指数
                    sbStr.Append("&gTzhzh=").Append(yibanDR["BMI"]);

                    //if (baseUrl.Contains("sdcsm_new"))
                    //{
                    if (lnrzlnl == "1")
                    {
                        //老年人健康状态
                        sbStr.Append("&lnrzkpg=").Append(yibanDR["OldHealthStaus"]);

                        //老年人生活自理能力
                        sbStr.Append("&lnrzlpg=").Append(yibanDR["OldSelfCareability"]);

                        //老年人认知能力
                        sbStr.Append("&gLnrrz=").Append(yibanDR["OldRecognise"]);

                        //老年人情感
                        sbStr.Append("&gLnrqg=").Append(yibanDR["OldEmotion"]);

                        //智力能力评分
                        sbStr.Append("&gLnrrzfen=").Append(yibanDR["InterScore"]);

                        //老年人抑郁评分
                        sbStr.Append("&gLnrqgfen=").Append(yibanDR["GloomyScore"]);
                    }
                    //}
                    ////老年人是否规范管理
                    //sbStr.Append("&lNrgfgl=").Append(yibanDR["OldMange"]);

                    //签字板
                    sbStr.Append("&tjybqkysqm=");

                    #endregion

                    #region ARCHIVE_LIFESTYLE

                    DataRow lifeDR = ds.Tables["ARCHIVE_LIFESTYLE"].Rows[0];

                    //每次锻炼时间
                    sbStr.Append("&gMcdlsj=").Append(lifeDR["ExerciseTimes"]);

                    //坚持锻炼时间
                    sbStr.Append("&gJcdlsj=").Append(lifeDR["ExcisepersistTime"]);

                    //锻炼方式
                    string str = lifeDR["ExerciseExistenseOther"].ToString();
                    string strtmp = lifeDR["ExerciseExistense"].ToString();
                    sbStr.Append("&gDlfs=").Append(string.IsNullOrEmpty(strtmp) ? "" : HtmlHelper.GetUrlEncodeVal(strtmp.Replace("1", "散步").Replace("2", "跑步").Replace("3", "广场舞").Replace("4", "") + str));

                    //平均一天吸烟量
                    sbStr.Append("&gRxyl=").Append(lifeDR["SmokeDayNum"]);

                    //开始吸烟年龄
                    sbStr.Append("&gKsxynl=").Append(lifeDR["SmokeAgeStart"]);

                    //戒烟年龄
                    sbStr.Append("&gJynl=").Append(lifeDR["SmokeAgeForbiddon"]);

                    //日饮酒量
                    sbStr.Append("&gRyjl=").Append(lifeDR["DayDrinkVolume"]);

                    //戒酒年龄
                    sbStr.Append("&gJjnl=").Append(lifeDR["ForbiddonAge"]);

                    //开始饮酒年龄
                    sbStr.Append("&gKsyjnl=").Append(lifeDR["DrinkStartAge"]);

                    //是否已戒酒
                    sbStr.Append("&gSfjj=").Append(lifeDR["IsDrinkForbiddon"]);

                    //近一年是否醉酒
                    sbStr.Append("&gYnnsfyj=").Append(lifeDR["DrinkThisYear"]);

                    //饮酒种类
                    tem = lifeDR["DrinkType"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gYjzl=").Append(n == "5" ? "99" : n);
                    }

                    //饮酒种类其他
                    sbStr.Append("&gYjzlqt=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["DrinkTypeOther"].ToString()));

                    //饮食习惯
                    tem = lifeDR["DietaryHabit"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        if (string.IsNullOrEmpty(n)) continue;
                        sbStr.Append("&gYsxg=").Append(n);
                    }

                    //锻炼频率
                    sbStr.Append("&gDlpl=").Append(lifeDR["ExerciseRate"]);

                    //吸烟状况
                    sbStr.Append("&gXyzk=").Append(lifeDR["SmokeCondition"]);

                    //饮酒频率
                    sbStr.Append("&gYjpl=").Append(lifeDR["DrinkRate"]);

                    //职业危害史有无
                    sbStr.Append("&gYwzybl=").Append(lifeDR["CareerHarmFactorHistory"].ToString() == "2" ? "3" : "1");

                    //工种
                    sbStr.Append("&gJtzy=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["WorkType"].ToString()));

                    //工作时间
                    sbStr.Append("&gCysj=").Append(lifeDR["WorkTime"]);

                    //粉尘
                    sbStr.Append("&fenchen=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Dust"].ToString()));

                    //粉尘防护无有
                    sbStr.Append("&fchcs=").Append(lifeDR["DustProtect"] != null && lifeDR["DustProtect"].ToString() == "1" ? "1" : "3");
                    sbStr.Append("&fchy=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["DustProtectEx"].ToString()));

                    //放射
                    sbStr.Append("&gShexian=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Radiogen"].ToString()));

                    //放射防护无有
                    sbStr.Append("&gSxfhcs=").Append(lifeDR["RadiogenProtect"] != null && lifeDR["RadiogenProtect"].ToString() == "1" ? "1" : "3");
                    sbStr.Append("&gSxfhcsqt=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["RadiogenProtectEx"].ToString()));

                    //物理
                    sbStr.Append("&wuliyinsu=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Physical"].ToString()));

                    //物理防护无有
                    sbStr.Append("&wlcs=").Append(lifeDR["PhysicalProtect"] != null && lifeDR["PhysicalProtect"].ToString() == "1" ? "1" : "3");
                    sbStr.Append("&wly=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["PhysicalProtectEx"].ToString()));

                    //化学
                    sbStr.Append("&gHxp=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Chem"].ToString()));

                    //化学防护无有
                    sbStr.Append("&gHxpfhcs=").Append(lifeDR["ChemProtect"] != null && lifeDR["ChemProtect"].ToString() == "1" ? "1" : "3");
                    sbStr.Append("&gHxpfhcsjt=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["ChemProtectEx"].ToString()));

                    //其他危害
                    sbStr.Append("&blqita=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Other"].ToString()));

                    //其他危害防护无有
                    sbStr.Append("&blqtcs=").Append(lifeDR["OtherProtect"] != null && lifeDR["OtherProtect"].ToString() == "1" ? "1" : "3");
                    //
                    sbStr.Append("&qty=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["OtherProtectEx"].ToString()));

                    //签字板
                    sbStr.Append("&tjshfsysqm=");

                    #endregion

                    #region ARCHIVE_VISCERAFUNCTION

                    if (ds.Tables["ARCHIVE_VISCERAFUNCTION"] != null && ds.Tables["ARCHIVE_VISCERAFUNCTION"].Rows.Count > 0)
                    {
                        DataRow zangQi = ds.Tables["ARCHIVE_VISCERAFUNCTION"].Rows[0];

                        //口唇
                        sbStr.Append("&gKouchun=").Append(!string.IsNullOrEmpty(zangQi["Lips"].ToString()) ? zangQi["Lips"].ToString().Replace("0", "") : "");

                        //kchqt 口唇其他
                        sbStr.Append("&kchqt=").Append(HtmlHelper.GetUrlEncodeVal(zangQi["LipsEx"].ToString()));

                        // 齿列
                        string chilie = zangQi["ToothResides"].ToString();

                        if (!string.IsNullOrEmpty(chilie))
                        {
                            var temp = chilie.Split(',');
                            foreach (var n in temp)
                            {
                                sbStr.Append("&gChilei=").Append(n);
                            }
                        }

                        //缺齿
                        if (chilie.Contains("2"))
                        {
                            string quechi = zangQi["HypodontiaEx"].ToString();
                            if (!string.IsNullOrEmpty(quechi) && quechi.Contains("#"))
                            {
                                var v = quechi.Split('#');
                                sbStr.Append("&quechi1=").Append(CommonExtensions.GetUrlEncodeVal(v[0]));

                                sbStr.Append("&quechi2=").Append(CommonExtensions.GetUrlEncodeVal(v[1]));

                                sbStr.Append("&quechi3=").Append(CommonExtensions.GetUrlEncodeVal(v[2]));

                                sbStr.Append("&quechi4=").Append(CommonExtensions.GetUrlEncodeVal(v[3]));
                            }
                        }

                        //龋齿
                        if (chilie.Contains("3"))
                        {
                            string quchi = zangQi["SaprodontiaEx"].ToString();
                            if (!string.IsNullOrEmpty(quchi) && quchi.Contains("#"))
                            {
                                var v = quchi.Split('#');
                                sbStr.Append("&quchi1=").Append(CommonExtensions.GetUrlEncodeVal(v[0]));

                                sbStr.Append("&quchi2=").Append(CommonExtensions.GetUrlEncodeVal(v[1]));

                                sbStr.Append("&quchi3=").Append(CommonExtensions.GetUrlEncodeVal(v[2]));

                                sbStr.Append("&quchi4=").Append(CommonExtensions.GetUrlEncodeVal(v[3]));
                            }
                        }

                        //龋齿
                        if (chilie.Contains("4"))
                        {
                            string yichi = zangQi["DentureEx"].ToString();
                            if (!string.IsNullOrEmpty(yichi) && yichi.Contains("#"))
                            {
                                var v = yichi.Split('#');
                                sbStr.Append("&yichi1=").Append(CommonExtensions.GetUrlEncodeVal(v[0]));

                                sbStr.Append("&yichi2=").Append(CommonExtensions.GetUrlEncodeVal(v[1]));

                                sbStr.Append("&yichi3=").Append(CommonExtensions.GetUrlEncodeVal(v[2]));

                                sbStr.Append("&yichi4=").Append(CommonExtensions.GetUrlEncodeVal(v[3]));
                            }
                        }

                        //齿列其他
                        sbStr.Append("&chlqt=").Append(HtmlHelper.GetUrlEncodeVal(zangQi["ToothResidesOther"].ToString()));

                        //PharyngealEx 咽部其他
                        sbStr.Append("&ybqt=").Append(HtmlHelper.GetUrlEncodeVal(zangQi["PharyngealEx"].ToString()));

                        //咽部
                        sbStr.Append("&gYanbu=").Append(!string.IsNullOrEmpty(zangQi["Pharyngeal"].ToString()) ? zangQi["Pharyngeal"].ToString().Replace("0", "") : "");

                        //左眼
                        sbStr.Append("&gZysl=").Append(zangQi["LeftView"]);

                        //右眼
                        sbStr.Append("&gYysl=").Append(zangQi["RightView"]);

                        //左眼矫正
                        sbStr.Append("&gZyjz=").Append(zangQi["LeftEyecorrect"]);

                        //右眼矫正
                        sbStr.Append("&gYyjz=").Append(zangQi["RightEyecorrect"]);

                        //听力
                        sbStr.Append("&gTl=").Append(zangQi["Listen"]);

                        //运动功能
                        sbStr.Append("&gYdgn=").Append(zangQi["SportFunction"]);
                    }

                    //签字板
                    sbStr.Append("&tjzqgnysqm=");

                    #endregion

                    #region ARCHIVE_PHYSICALEXAM

                    DataRow chatiDR = ds.Tables["ARCHIVE_PHYSICALEXAM"].Rows[0];

                    //眼底
                    sbStr.Append("&gYand=").Append(chatiDR["EyeRound"]);
                    sbStr.Append("&gYandyi=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["EyeRoundEx"].ToString()));

                    //眼底签字
                    //手动签字
                    sbStr.Append("&sdtjyandiysqm=");

                    //签字板
                    sbStr.Append("&tjyandiysqm=");

                    //皮肤
                    sbStr.Append("&gPfgm=").Append(!string.IsNullOrEmpty(chatiDR["Skin"].ToString()) ? chatiDR["Skin"].ToString().Replace("7", "99") : "");
                    sbStr.Append("&gPfqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["SkinEx"].ToString()));

                    //签字板
                    sbStr.Append("&tjchatiysqm=");

                    //巩膜
                    sbStr.Append("&gGongmo=").Append(chatiDR["Sclere"] != null ? chatiDR["Sclere"].ToString().Replace("4", "99") : "");
                    sbStr.Append("&gGmqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["SclereEx"].ToString()));

                    //淋巴
                    sbStr.Append("&gLbj=").Append(chatiDR["Lymph"]);
                    sbStr.Append("&gLbjqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["LymphEx"].ToString()));

                    //桶状胸
                    sbStr.Append("&gTzx=").Append(chatiDR["BarrelChest"] != null && chatiDR["BarrelChest"].ToString() == "1" ? "2" : "1");

                    //呼吸音
                    sbStr.Append("&gHxy=").Append(chatiDR["BreathSounds"]);
                    sbStr.Append("&gHxyyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["BreathSoundsEx"].ToString()));

                    //罗音
                    sbStr.Append("&gLy=").Append(chatiDR["Rale"]);
                    sbStr.Append("&gLyyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["RaleEx"].ToString()));

                    //杂音
                    sbStr.Append("&gZayin=").Append(chatiDR["Noise"]);
                    sbStr.Append("&gZayinyo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["NoiseEx"].ToString()));

                    //压痛
                    sbStr.Append("&gYato=").Append(chatiDR["PressPain"]);
                    sbStr.Append("&gYatoyo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["PressPainEx"].ToString()));

                    //包块
                    sbStr.Append("&gBk=").Append(chatiDR["EnclosedMass"]);
                    sbStr.Append("&gBkyo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["EnclosedMassEx"].ToString()));

                    //肝大
                    sbStr.Append("&gGanda=").Append(chatiDR["Liver"]);
                    sbStr.Append("&gGandayo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["LiverEx"].ToString()));

                    //脾大
                    sbStr.Append("&gPida=").Append(chatiDR["Spleen"]);
                    sbStr.Append("&gPidayo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["SpleenEx"].ToString()));

                    //浊音
                    sbStr.Append("&gZhuoyin=").Append(chatiDR["Voiced"]);
                    sbStr.Append("&gZhuoyinyo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["VoicedEx"].ToString()));

                    //肛门指诊
                    strtmp = chatiDR["Anus"].ToString();
                    sbStr.Append("&gGmzhzh=").Append(string.IsNullOrEmpty(strtmp) ? "" : strtmp.Replace("5", "99"));

                    //肛门指诊其他 
                    sbStr.Append("&gGmzhzhyi=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["AnusEx"].ToString()));

                    //肛门指诊签字
                    //手动签字
                    sbStr.Append("&sdtjgmzzysqm=");

                    //签字板
                    sbStr.Append("&tjgmzzysqm=");

                    //心率
                    decimal dec = 0;
                    object obDBnull = DBNull.Value;
                    chatiDR["HeartRate"] = Decimal.TryParse(chatiDR["HeartRate"].ToString(), out dec) ? dec.ToString("0") : obDBnull;
                    sbStr.Append("&gXinlv=").Append(chatiDR["HeartRate"]);

                    //心律
                    sbStr.Append("&gXinlvci=").Append(chatiDR["HeartRhythm"]);

                    //下肢水肿
                    sbStr.Append("&gXzsz=").Append(GetWebEdemaCodeByPadCode(chatiDR["Edema"]));

                    //足背，动脉搏动
                    sbStr.Append("&gZbdmmd=").Append(chatiDR["FootBack"]);

                    //查体其他
                    sbStr.Append("&gCtqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["Other"].ToString()));

                    //查体其他签字
                    //手动签字
                    sbStr.Append("&sdtjctqtysqm=");

                    //签字板
                    sbStr.Append("&tjctqtysqm=");

                    //乳腺
                    tem = chatiDR["Breast"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gRuxian=").Append(n == "5" ? "99" : n);
                    }
                    //乳腺异常
                    sbStr.Append("&gRuxianqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["BreastEx"].ToString()));

                    //乳腺签字
                    //手动签字
                    sbStr.Append("&sdtjrxysqm=");

                    //签字板
                    sbStr.Append("&tjrxysqm=");

                    //外阴
                    sbStr.Append("&gWaiyin=").Append(chatiDR["Vulva"]);

                    //外阴异常
                    sbStr.Append("&gWaiyinyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["VulvaEx"].ToString()));

                    //阴道
                    sbStr.Append("&gYindao=").Append(chatiDR["Vagina"]);
                    sbStr.Append("&gYindaoyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["VaginaEx"].ToString()));

                    //宫颈
                    sbStr.Append("&gGongjing=").Append(chatiDR["CervixUteri"]);
                    sbStr.Append("&gGongjingyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["CervixUteriEx"].ToString()));

                    //宫体
                    sbStr.Append("&gGongti=").Append(chatiDR["Corpus"]);
                    sbStr.Append("&gGongtiyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["CorpusEx"].ToString()));

                    //附件
                    sbStr.Append("&gFujian=").Append(chatiDR["Attach"]);
                    sbStr.Append("&gFujianyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["AttachEx"].ToString()));

                    #endregion

                    XmlNodeList xmlNodes = null;
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load("D:\\QCSoft\\QCSoftPlatformV2.0\\SHValueRange.xml");
                    if (xmldoc != null)
                    {
                        xmlNodes = xmldoc.SelectNodes("//NewDataSet/Table1");
                    }

                    #region  ARCHIVE_ASSISTCHECK

                    string returnString = HtmlHelper.GetTagValue(retString, "<body>", "</body>");
                    DataRow fuzhuDR = ds.Tables["ARCHIVE_ASSISTCHECK"].Rows[0];

                    // 正常和异常字段的个数
                    int zc = 0;
                    int yc = 0;

                    //血红蛋白
                    string xhdb = fuzhuDR["HB"].ToString();
                    string id = getInputName("血常规*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(xhdb);

                    strtmp = GetZcOrYc(xmlNodes, "HB", xhdb);
                    sbStr.Append("&xhdb=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //白细胞
                    string bxb = fuzhuDR["WBC"].ToString();

                    id = getInputName("血常规*", returnString, "2", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(bxb);

                    strtmp = GetZcOrYc(xmlNodes, "WBC", bxb);
                    sbStr.Append("&bxb=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //血小板
                    string xxb = fuzhuDR["PLT"].ToString();

                    id = getInputName("血常规*", returnString, "3", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(xxb);

                    strtmp = GetZcOrYc(xmlNodes, "PLT", xxb);
                    sbStr.Append("&xxb=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    if (yc > 0)
                    {
                        sbStr.Append("&xcg=").Append("2");
                    }
                    else if (zc > 0)
                    {
                        sbStr.Append("&xcg=").Append("1");
                    }

                    //血常规其他
                    string xcgqt = fuzhuDR["BloodOther"].ToString();
                    sbStr.Append("&gXcgqt=").Append(HtmlHelper.GetUrlEncodeVal(xcgqt));

                    zc = 0;
                    yc = 0;
                    strtmp = "";

                    //尿蛋白
                    string ndb = fuzhuDR["PRO"].ToString();

                    id = getInputName("尿常规*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(ndb));

                    strtmp = GetNcg(ndb);
                    sbStr.Append("&ndb=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //尿糖
                    string nt = fuzhuDR["GLU"].ToString();

                    id = getInputName("尿常规*", returnString, "2", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(nt));

                    strtmp = GetNcg(nt);
                    sbStr.Append("&nt=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //尿酮体
                    string ntt = fuzhuDR["KET"].ToString();

                    id = getInputName("尿常规*", returnString, "3", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(ntt));

                    strtmp = GetNcg(ntt);
                    sbStr.Append("&ntt=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //尿潜血
                    string nqx = fuzhuDR["BLD"].ToString();

                    id = getInputName("<br/>尿潜血", returnString, "1", "<br/>其他", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(nqx));

                    strtmp = GetNcg(nqx);
                    sbStr.Append("&nqx=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //尿常规其他
                    string ncgqt = fuzhuDR["UrineOther"].ToString();
                    sbStr.Append("&gNcgqt=").Append(HtmlHelper.GetUrlEncodeVal(ncgqt));

                    if (yc > 0)
                    {
                        sbStr.Append("&ncg=").Append("2");
                    }
                    else if (zc > 0)
                    {
                        sbStr.Append("&ncg=").Append("1");
                    }

                    //是否为糖尿病
                    sbStr.Append("&nrtnbgl=").Append(renQun.ToString().Contains("7") ? "3" : "1");

                    //sbStr.Append("&nrtnbglold=").Append("1");

                    //空腹血糖
                    string kfxt = fuzhuDR["FPGL"].ToString();

                    id = getInputName("空腹血糖*", returnString, "1", "</tr>", "d_wb");
                    sbStr.Append("&" + id + "=").Append(kfxt);

                    strtmp = GetZcOrYc(xmlNodes, "FPGL", kfxt);
                    sbStr.Append("&kfxt=").Append(strtmp);

                    // 餐后2小时血糖
                    string chxt = fuzhuDR["FPGDL"].ToString();
                    id = getInputName("餐后2H血糖*", returnString, "1", "</tr>", "d_wb");
                    sbStr.Append("&" + id + "=").Append(chxt);

                    //心电图
                    string xdt = fuzhuDR["ECG"].ToString();
                    id = getCheckBoxName("心电图*", returnString, "1", "</tr>");

                    if (!string.IsNullOrEmpty(xdt))
                    {
                        var lst = xdt.Split(',');

                        foreach (var item in lst)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                sbStr.Append("&" + id + "=").Append(GetWebECGByPadCode(item));
                                //break;
                            }
                        }
                    }
                    else
                    {
                        sbStr.Append("&gXindt=");
                    }

                    id = getInputName("心电图*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["ECGEx"].ToString()));

                    // 签字板
                    sbStr.Append("&tjxdtysqm=");

                    //尿微量白蛋白
                    string nwlbdb = fuzhuDR["ALBUMIN"].ToString();
                    id = getInputName("尿微量白蛋白*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(nwlbdb);

                    // 签字板
                    sbStr.Append("&tjnwlbdbysqm=");

                    //大便潜血
                    string dbqx = fuzhuDR["FOB"].ToString();
                    id = getRadioName("大便潜血*", returnString, "1", "</tr>");
                    sbStr.Append("&" + id + "=").Append(dbqx);

                    // 签字板
                    sbStr.Append("&tjfzjcdbqxysqm=");

                    //糖化血红蛋白
                    string thxhdb = fuzhuDR["HBALC"].ToString();
                    id = getInputName("糖化血红蛋白*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(thxhdb);

                    //乙型肝炎表面抗原
                    string yxgy = fuzhuDR["HBSAG"].ToString();
                    id = getRadioName("乙型肝炎表面抗原*", returnString, "1", "</tr>");
                    sbStr.Append("&" + id + "=").Append(yxgy);

                    zc = 0;
                    yc = 0;

                    //血清谷丙转氨酶
                    string xqgb = fuzhuDR["SGPT"].ToString();

                    id = getInputName("肝功能*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(xqgb);

                    strtmp = GetZcOrYc(xmlNodes, "SGPT", xqgb);
                    sbStr.Append("&gbzam=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //血清谷草转氨酶
                    string xqgc = fuzhuDR["GOT"].ToString();

                    id = getInputName("肝功能*", returnString, "2", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(xqgc);

                    strtmp = GetZcOrYc(xmlNodes, "GOT", xqgc);
                    sbStr.Append("&gczam=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //白蛋白
                    string bdb = fuzhuDR["BP"].ToString();

                    id = getInputName("肝功能*", returnString, "3", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(bdb);

                    strtmp = GetZcOrYc(xmlNodes, "BP", bdb);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //总胆红素
                    string zdhs = fuzhuDR["TBIL"].ToString();

                    id = getInputName("肝功能*", returnString, "4", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(zdhs);

                    strtmp = GetZcOrYc(xmlNodes, "TBIL", zdhs);
                    sbStr.Append("&zdhs=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //结合胆红素
                    string jhdhs = fuzhuDR["CB"].ToString();
                    string tmp = HtmlHelper.GetTagValue(returnString, "肝功能*", "</tr>");
                    id = HtmlHelper.GetTagValue(tmp, "结合胆红素", "</td>");
                    id = HtmlHelper.GetTagValue(id, "<input type=\"text\" name=\"", "\"");
                    sbStr.Append("&" + id + "=").Append(jhdhs);

                    strtmp = GetZcOrYc(xmlNodes, "CB", jhdhs);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    if (yc > 0)
                    {
                        sbStr.Append("&ggn=").Append("2");
                    }
                    else if (zc > 0)
                    {
                        sbStr.Append("&ggn=").Append("1");
                    }

                    zc = 0;
                    yc = 0;

                    //血清肌酐
                    string xqjg = fuzhuDR["SCR"].ToString();

                    id = getInputName("肾功能*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(xqjg);

                    strtmp = GetZcOrYc(xmlNodes, "SCR", xqjg);
                    sbStr.Append("&xqjg=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //血尿素氮
                    string xnsd = fuzhuDR["BUN"].ToString();

                    id = getInputName("肾功能*", returnString, "2", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(xnsd);

                    strtmp = GetZcOrYc(xmlNodes, "BUN", xnsd);
                    sbStr.Append("&nsd=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //血钾浓度
                    string xjnd = fuzhuDR["PC"].ToString();
                    tmp = HtmlHelper.GetTagValue(returnString, "肾功能*", "</tr>");
                    id = HtmlHelper.GetTagValue(tmp, "血钾浓度", "umol/L,");
                    id = HtmlHelper.GetTagValue(id, "<input type=\"text\" name=\"", "\"");
                    sbStr.Append("&" + id + "=").Append(xjnd);

                    strtmp = GetZcOrYc(xmlNodes, "PC", xjnd);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //血钠浓度
                    string xnnd = fuzhuDR["HYPE"].ToString();
                    id = HtmlHelper.GetTagValue(tmp, "血钠浓度", "mmol/L");
                    id = HtmlHelper.GetTagValue(id, "<input type=\"text\" name=\"", "\"");
                    sbStr.Append("&" + id + "=").Append(xnnd);

                    strtmp = GetZcOrYc(xmlNodes, "HYPE", xnnd);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    if (yc > 0)
                    {
                        sbStr.Append("&sgn=").Append("2");
                    }
                    else if (zc > 0)
                    {
                        sbStr.Append("&sgn=").Append("1");
                    }

                    zc = 0;
                    yc = 0;

                    //总胆固醇
                    string zdgc = fuzhuDR["TC"].ToString();

                    id = getInputName("血脂*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(zdgc);

                    strtmp = GetZcOrYc(xmlNodes, "TC", zdgc);
                    sbStr.Append("&zdgc=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //甘油三酯
                    string gysz = fuzhuDR["TG"].ToString();

                    id = getInputName("血脂*", returnString, "2", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(gysz);

                    strtmp = GetZcOrYc(xmlNodes, "TG", gysz);
                    sbStr.Append("&gysz=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //血清低密度脂蛋白胆固醇
                    string xqdmd = fuzhuDR["LowCho"].ToString();

                    id = getInputName("血脂*", returnString, "3", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(xqdmd);

                    strtmp = GetZcOrYc(xmlNodes, "LowCho", xqdmd);
                    sbStr.Append("&dmd=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    //血清高密度脂蛋白胆固醇
                    string xqgmd = fuzhuDR["HeiCho"].ToString();

                    id = getInputName("血脂*", returnString, "4", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(xqgmd);

                    strtmp = GetZcOrYc(xmlNodes, "HeiCho", xqgmd);
                    sbStr.Append("&gmd=").Append(strtmp);
                    if (strtmp == "1")
                    {
                        zc++;
                    }
                    else if (strtmp == "2")
                    {
                        yc++;
                    }

                    if (yc > 0)
                    {
                        sbStr.Append("&xz=").Append("2");
                    }
                    else if (zc > 0)
                    {
                        sbStr.Append("&xz=").Append("1");
                    }

                    //胸部X片
                    id = getRadioName("胸部X线片*", returnString, "1", "</tr>");
                    sbStr.Append("&" + id + "=").Append(fuzhuDR["CHESTX"]);

                    id = getInputName("胸部X线片*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["CHESTXEx"].ToString()));

                    // 胸部X线片手动签字
                    sbStr.Append("&sdtjxbxxpysqm=");

                    // 签字板
                    sbStr.Append("&tjxbxxpysqm=");

                    // 腹部B超
                    id = getRadioName("B 超*", returnString, "1", "</tr>");
                    string fbBc = fuzhuDR["BCHAO"].ToString();
                    sbStr.Append("&" + id + "=").Append(fbBc);

                    id = getInputName("B 超*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["BCHAOEx"].ToString()));

                    // 腹部B超签字板
                    sbStr.Append("&tjfbbcysqm=");

                    //宫颈涂片
                    id = getRadioName("宫颈涂片*", returnString, "1", "</tr>");
                    sbStr.Append("&" + id + "=").Append(fuzhuDR["CERVIX"]);

                    id = getInputName("宫颈涂片*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["CERVIXEx"].ToString()));

                    //其他
                    node1 = doc1.DocumentNode.SelectSingleNode("//div[@id='tjfzjcqtysqm']").ParentNode.ParentNode.SelectSingleNode("td[2]/input[1]");
                    id = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["Other"].ToString()));

                    // 辅助检查其他 手动签字
                    sbStr.Append("&sdtjfzjcqtysqm=");

                    // 签字板
                    sbStr.Append("&tjfzjcqtysqm=");

                    #region 2017-10-20添加新字段

                    // 血型
                    string xx = fuzhuDR["BloodType"].ToString();
                    id = getInputName("血型*", returnString, "1", "<div", "input_5");
                    sbStr.Append("&" + id + "=").Append(GetWebBloodTypeByPadCode(xx));

                    string rh = fuzhuDR["RH"].ToString();
                    id = getInputName("血型*", returnString, "2", "<div", "input_5");
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(GetWebRHByPadCode(rh)));

                    //签字板
                    sbStr.Append("&tjfzjcxxysqm=");

                    //同型半胱氨酸
                    string txbg = fuzhuDR["HCY"].ToString();
                    id = getInputName("同型半胱氨酸*", returnString, "1", "</tr>", "input_5");
                    sbStr.Append("&" + id + "=").Append(txbg);

                    // 其他B超
                    string qtBc = fuzhuDR["BCHAOther"].ToString();
                    node1 = doc1.DocumentNode.SelectSingleNode("//div[@id='tjfbbcqtysqm']").ParentNode.ParentNode.SelectSingleNode("td[1]/input[1]");
                    id = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;
                    sbStr.Append("&" + id + "=").Append(qtBc);

                    node1 = doc1.DocumentNode.SelectSingleNode("//div[@id='tjfbbcqtysqm']").ParentNode.ParentNode.SelectSingleNode("td[1]/input[3]");
                    id = node1 == null || !node1.Attributes.Contains("name") ? "" : node1.Attributes["name"].Value;
                    sbStr.Append("&" + id + "=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["BCHAOtherEx"].ToString()));

                    // 签字板
                    sbStr.Append("&tjfbbcqtysqm=");

                    #endregion

                    #endregion

                    #region ARCHIVE_MEDI_PHYS_DIST

                    if (ds.Tables["ARCHIVE_MEDI_PHYS_DIST"] != null && ds.Tables["ARCHIVE_MEDI_PHYS_DIST"].Rows.Count > 0)
                    {
                        DataRow zhongyiDR = ds.Tables["ARCHIVE_MEDI_PHYS_DIST"].Rows[0];

                        //平和质
                        sbStr.Append("&gPhz=").Append(zhongyiDR["Mild"]);

                        //气虚质
                        sbStr.Append("&gQxz=").Append(zhongyiDR["Faint"]);

                        //阳虚质
                        sbStr.Append("&gYangxz=").Append(zhongyiDR["Yang"]);

                        //阴虚质
                        sbStr.Append("&gYinxz=").Append(zhongyiDR["Yin"]);

                        //痰湿质
                        sbStr.Append("&gTsz=").Append(zhongyiDR["PhlegmDamp"]);

                        //湿热质
                        sbStr.Append("&gSrz=").Append(zhongyiDR["Muggy"]);

                        //血瘀质
                        sbStr.Append("&gXyz=").Append(zhongyiDR["BloodStasis"]);

                        //气郁质
                        sbStr.Append("&gQyz=").Append(zhongyiDR["QiConstraint"]);

                        //特禀质
                        sbStr.Append("&gTbz=").Append(zhongyiDR["Characteristic"]);
                    }

                    #endregion

                    #region ARCHIVE_HEALTHQUESTION

                    DataRow jiankangDR = ds.Tables["ARCHIVE_HEALTHQUESTION"].Rows[0];

                    //脑血管
                    tem = jiankangDR["BrainDis"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gNxgjb=").Append(n == "6" ? "99" : n);
                    }
                    if (tem != null && tem.Count() > 0)
                    {
                        sbStr.Append("&gNxgjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["BrainOther"].ToString()));
                    }

                    //肾脏
                    tem = jiankangDR["RenalDis"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gSzjb=").Append(n == "6" ? "99" : n);
                    }
                    if (tem != null && tem.Count() > 0)
                    {
                        sbStr.Append("&gSzjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["RenalOther"].ToString()));
                    }

                    //心血管疾病
                    tem = jiankangDR["HeartDis"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gXzjb=").Append(n == "10" ? "99" : n);
                    }
                    if (tem != null && tem.Count() > 0)
                    {
                        sbStr.Append("&gXzjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["HeartOther"].ToString()));
                    }

                    //眼部
                    tem = jiankangDR["EyeDis"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gYbjb=").Append(n == "5" ? "99" : n);
                    }
                    if (tem != null && tem.Count() > 0)
                    {
                        sbStr.Append("&gYbjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["EyeOther"].ToString()));
                    }

                    //神经系统
                    tem = jiankangDR["NerveDis"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gSjxtjb=").Append(GetWebNerveDisByPadCode(n));
                    }
                    if (tem != null && tem.Count() > 0)
                    {
                        //神经系统其他
                        sbStr.Append("&gSjxtjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["NerveOther"].ToString()));
                    }

                    //其他
                    tem = jiankangDR["ElseDis"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gQtxtjb=").Append(GetWebElseDisByPadCode(n));
                    }
                    if (tem != null && tem.Count() > 0)
                    {
                        //其他-其他
                        sbStr.Append("&gQtxtjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["ElseOther"].ToString()));
                    }

                    // 签字板
                    sbStr.Append("&tjxczyjkwtysqm=");

                    #endregion

                    #region ARCHIVE_HOSPITALHISTORY

                    DataTable zhuyuanDT = ds.Tables["ARCHIVE_HOSPITALHISTORY"];

                    if (zhuyuanDT == null || zhuyuanDT.Rows.Count == 0)
                    {
                        sbStr.Append("&zyzlqkyw=").Append("2");
                    }
                    else
                    {
                        int i = 0;
                        foreach (DataRow dr in zhuyuanDT.Rows)
                        {
                            if (!string.IsNullOrEmpty(dr["InHospitalDate"].ToString()))
                            {
                                i++;

                                //入院日期
                                sbStr.Append("&zRyjcrq=").Append(dr["InHospitalDate"].ToString() != "" ? Convert.ToDateTime(dr["InHospitalDate"]).ToString("yyyy-MM-dd") : "");

                                //出院日期
                                sbStr.Append("&zCyccrq=").Append(dr["OutHospitalDate"].ToString() != "" ? Convert.ToDateTime(dr["OutHospitalDate"]).ToString("yyyy-MM-dd") : "");

                                //原因
                                sbStr.Append("&zYuanyin=").Append(HtmlHelper.GetUrlEncodeVal(dr["Reason"].ToString()));

                                //机构
                                sbStr.Append("&zYljgmc=").Append(HtmlHelper.GetUrlEncodeVal(dr["HospitalName"].ToString()));

                                //编号
                                sbStr.Append("&zBingah=").Append(HtmlHelper.GetUrlEncodeVal(dr["IllcaseNum"].ToString()));

                                //疑似：区分住院史和家庭病床史
                                sbStr.Append("&zType=").Append("1");
                            }
                        }
                        if (i == 0)
                        {
                            sbStr.Append("&zyzlqkyw=").Append("2");
                        }
                        else
                        {
                            sbStr.Append("&zyzlqkyw=").Append("1");
                        }
                    }

                    #endregion

                    #region ARCHIVE_FAMILYBEDHISTORY

                    DataTable familyDT = ds.Tables["ARCHIVE_FAMILYBEDHISTORY"];

                    if (familyDT == null || familyDT.Rows.Count == 0)
                    {
                        sbStr.Append("&jzbcsyw=").Append("2");
                    }
                    else
                    {
                        int i = 0;
                        foreach (DataRow familyDR in familyDT.Rows)
                        {
                            if (!string.IsNullOrEmpty(familyDR["InHospitalDate"].ToString()))
                            {
                                i++;

                                //建床日期
                                sbStr.Append("&zRyjcrq=").Append(familyDR["InHospitalDate"].ToString() != "" ? Convert.ToDateTime(familyDR["InHospitalDate"]).ToString("yyyy-MM-dd") : "");

                                //撤床日期
                                sbStr.Append("&zCyccrq=").Append(familyDR["OutHospitalDate"].ToString() != "" ? Convert.ToDateTime(familyDR["OutHospitalDate"]).ToString("yyyy-MM-dd") : "");

                                //原因
                                sbStr.Append("&zYuanyin=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["Reasons"].ToString()));

                                //医疗机构
                                sbStr.Append("&zYljgmc=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["HospitalName"].ToString()));

                                //病案号
                                sbStr.Append("&zBingah=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["IllcaseNums"].ToString()));

                                //疑似：区分住院史和家庭病床史
                                sbStr.Append("&zType=").Append("2");
                            }
                        }
                        if (i == 0)
                        {
                            sbStr.Append("&jzbcsyw=").Append("2");
                        }
                        else
                        {
                            sbStr.Append("&jzbcsyw=").Append("1");
                        }
                    }

                    #endregion

                    #region ARCHIVE_MEDICATION

                    DataTable yongYaoDT = ds.Tables["ARCHIVE_MEDICATION"];

                    if (yongYaoDT == null || yongYaoDT.Rows.Count == 0)
                    {
                        sbStr.Append("&zyyyqkyw=").Append("2");
                    }
                    else
                    {
                        int i = 0;
                        foreach (DataRow yaoDR in yongYaoDT.Rows)
                        {
                            if (!string.IsNullOrEmpty(yaoDR["MedicinalName"].ToString()))
                            {
                                i++;

                                //药物名称
                                sbStr.Append("&yYwmc=").Append(HtmlHelper.GetUrlEncodeVal(yaoDR["MedicinalName"].ToString()));

                                //用法
                                sbStr.Append("&yYongfa=").Append(HtmlHelper.GetUrlEncodeVal(yaoDR["UseAge"].ToString()));

                                //用量
                                strtmp = yaoDR["UseNum"].ToString();
                                sbStr.Append("&yYongl=").Append(string.IsNullOrEmpty(strtmp) ? "" : HtmlHelper.GetUrlEncodeVal(strtmp.Replace(",", "，")));

                                //用药时间
                                sbStr.Append("&yYysj=").Append(HtmlHelper.GetUrlEncodeVal(yaoDR["StartTime"].ToString()));

                                //服药依从性
                                sbStr.Append("&yFyycx=").Append(GetWebYongYaoQingKuang(yaoDR["PillDependence"].ToString()));
                            }
                        }
                        if (i == 0)
                        {
                            sbStr.Append("&zyyyqkyw=").Append("2");
                        }
                        else
                        {
                            sbStr.Append("&zyyyqkyw=").Append("1");
                        }
                    }

                    // 签字板
                    sbStr.Append("&tjzyyyqkysqm=");

                    #endregion

                    #region ARCHIVE_INOCULATIONHISTORY

                    DataTable yiCongDT = ds.Tables["ARCHIVE_INOCULATIONHISTORY"];

                    if (yiCongDT == null || yiCongDT.Rows.Count == 0)
                    {
                        sbStr.Append("&fmyghyfyw=").Append("2");
                    }
                    else
                    {
                        int i = 0;
                        foreach (DataRow dr in yiCongDT.Rows)
                        {
                            if (!string.IsNullOrEmpty(dr["PillName"].ToString()))
                            {
                                i++;

                                //名称
                                sbStr.Append("&fJzmc=").Append(HtmlHelper.GetUrlEncodeVal(dr["PillName"].ToString()));

                                //时间
                                sbStr.Append("&fJzrq=").Append(dr["InoculationDate"] == null ? "" : Convert.ToDateTime(dr["InoculationDate"]).ToString("yyyy-MM-dd"));

                                //机构
                                sbStr.Append("&fJzjg=").Append(HtmlHelper.GetUrlEncodeVal(dr["InoculationHistory"].ToString()));
                            }
                        }
                        if (i == 0)
                        {
                            sbStr.Append("&fmyghyfyw=").Append("2");
                        }
                        else
                        {
                            sbStr.Append("&fmyghyfyw=").Append("1");
                        }
                    }

                    #endregion

                    #region ARCHIVE_ASSESSMENTGUIDE

                    DataTable pingjiaDT = ds.Tables["ARCHIVE_ASSESSMENTGUIDE"];

                    if (pingjiaDT == null || pingjiaDT.Rows.Count == 0)
                    {
                        sbStr.Append("&gJkpj=").Append("1");
                    }
                    else
                    {
                        DataRow pingjiaDR = pingjiaDT.Rows[0];

                        if (pingjiaDR["IsNormal"].ToString() == "2")
                        {
                            sbStr.Append("&gJkpj=").Append("2");
                            string yc1 = pingjiaDR["Exception1"].ToString();
                            string yc2 = pingjiaDR["Exception2"].ToString();
                            string yc3 = pingjiaDR["Exception3"].ToString();
                            string yc4 = pingjiaDR["Exception4"].ToString();

                            string strTemp = "";

                            //异常信息超过150个字节，自动换到下行
                            int i = CommonExtensions.Getlenght(yc1);
                            while (i > 150)
                            {
                                if (yc1.Contains(";"))
                                {
                                    strTemp = yc1.Substring(yc1.LastIndexOf(';') + 1);
                                    yc1 = yc1.Substring(0, yc1.LastIndexOf(';'));
                                    yc2 = (strTemp + ";" + yc2).TrimEnd(';');
                                    i = CommonExtensions.Getlenght(yc1);
                                }
                                else
                                {
                                    break;
                                }

                            }

                            i = CommonExtensions.Getlenght(yc2);
                            while (i > 150)
                            {
                                if (yc2.Contains(";"))
                                {
                                    strTemp = yc2.Substring(yc2.LastIndexOf(';') + 1);
                                    yc2 = yc2.Substring(0, yc2.LastIndexOf(';'));
                                    yc3 = (strTemp + ";" + yc3).TrimEnd(';');
                                    i = CommonExtensions.Getlenght(yc2);
                                }
                                else
                                {
                                    break;
                                }
                            }

                            i = CommonExtensions.Getlenght(yc3);
                            while (i > 150)
                            {
                                if (yc3.Contains(";"))
                                {
                                    strTemp = yc3.Substring(yc3.LastIndexOf(';') + 1);
                                    yc3 = yc3.Substring(0, yc3.LastIndexOf(';'));
                                    yc4 = (strTemp + ";" + yc4).TrimEnd(';');
                                    i = CommonExtensions.Getlenght(yc3);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            i = CommonExtensions.Getlenght(yc4);
                            if (i > 150)
                            {
                                yc4 = CommonExtensions.cutSubstring(yc4, 150);
                            }

                            sbStr.Append("&gJkpjyc1=").Append(HtmlHelper.GetUrlEncodeVal(yc1));

                            sbStr.Append("&gJkpjyc2=").Append(HtmlHelper.GetUrlEncodeVal(yc2));

                            sbStr.Append("&gJkpjyc3=").Append(HtmlHelper.GetUrlEncodeVal(yc3));

                            sbStr.Append("&gJkpjyc4=").Append(HtmlHelper.GetUrlEncodeVal(yc4));
                        }
                        else
                        {
                            sbStr.Append("&gJkpj=").Append("1");
                        }

                        var zhidao = pingjiaDR["HealthGuide"];

                        foreach (var s in zhidao.ToString().Split(','))
                        {
                            if (jkzdcount == 3)
                            {
                                if (s == "4")
                                {
                                    continue;
                                }
                            }
                            sbStr.Append("&gJkzd=").Append(GetHealthGuideCodeForWeb(s));
                        }
                        var temStr = pingjiaDR["DangerControl"];
                        foreach (var t in temStr.ToString().Split(','))
                        {
                            sbStr.Append("&gWxyskz=").Append(GetDangerControlCodeByPadCode(t));
                        }

                        //减体重
                        string arm = !string.IsNullOrEmpty(pingjiaDR["Arm"].ToString()) ? pingjiaDR["Arm"].ToString().ToLower().Replace("kg", "") : "";
                        sbStr.Append("&gWxystz=").Append(HtmlHelper.GetUrlEncodeVal(arm));

                        //疫苗
                        sbStr.Append("&gWsysym=").Append(HtmlHelper.GetUrlEncodeVal(pingjiaDR["VaccineAdvice"].ToString()));

                        //其他
                        sbStr.Append("&gWxysqt=").Append(HtmlHelper.GetUrlEncodeVal(pingjiaDR["Other"].ToString()));

                        //威海地区有减腹围选项
                        if (baseUrl.Contains("sdcsm_new"))
                        {
                            //减腹围
                            sbStr.Append("&gWxysjfw=").Append(HtmlHelper.GetUrlEncodeVal(pingjiaDR["WaistlineArm"].ToString()));
                        }
                    }

                    // 健康评价签字板
                    sbStr.Append("&tjjkpjysqm=");

                    // 健康指导签字板
                    sbStr.Append("&tjjkzdysqm=");

                    #endregion

                    #endregion

                    #region 手动签字

                    // 签字维护
                    DataTable dtSign = cDao.GetTjSignData();

                    if (dtSign != null && dtSign.Rows.Count > 0)
                    {
                        DataRow drSign = dtSign.Rows[0];

                        // 症状
                        sbStr.Append("&sdtjzkysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["SymptomSn"].ToString()));

                        // 一般情况
                        sbStr.Append("&sdtjybqkysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["GeneralConditionSn"].ToString()));

                        // 生活方式
                        sbStr.Append("&sdtjshfsysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["LifeStyleSn"].ToString()));

                        // 脏器功能
                        sbStr.Append("&sdtjzqgnysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["OrgansFunctionSn"].ToString()));

                        // 查体
                        sbStr.Append("&sdtjchatiysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["PSkinSn"].ToString()));

                        // 辅助检查 尿微量白蛋白
                        if (!string.IsNullOrEmpty(nwlbdb))
                        {
                            sbStr.Append("&sdtjnwlbdbysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AssistQtSn"].ToString()));
                        }
                        else
                        {
                            sbStr.Append("&sdtjnwlbdbysqm=");
                        }

                        // 血型签字
                        if (!string.IsNullOrEmpty(xx) || !string.IsNullOrEmpty(rh) || !string.IsNullOrEmpty(xhdb) || !string.IsNullOrEmpty(bxb) || !string.IsNullOrEmpty(xxb) || !string.IsNullOrEmpty(xcgqt) ||
                            !string.IsNullOrEmpty(ndb) || !string.IsNullOrEmpty(nt) || !string.IsNullOrEmpty(ntt) || !string.IsNullOrEmpty(nqx) || !string.IsNullOrEmpty(ncgqt) || !string.IsNullOrEmpty(kfxt) ||
                            !string.IsNullOrEmpty(chxt) || !string.IsNullOrEmpty(txbg))
                        {
                            sbStr.Append("&sdtjfzjcxxysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AssistQtSn"].ToString()));
                        }
                        else
                        {
                            sbStr.Append("&sdtjfzjcxxysqm=");
                        }

                        // 大便潜血
                        if (!string.IsNullOrEmpty(dbqx) || !string.IsNullOrEmpty(thxhdb) || !string.IsNullOrEmpty(yxgy) || !string.IsNullOrEmpty(xqgb) || !string.IsNullOrEmpty(xqgc) || !string.IsNullOrEmpty(bdb) ||
                            !string.IsNullOrEmpty(zdhs) || !string.IsNullOrEmpty(jhdhs) || !string.IsNullOrEmpty(xqjg) || !string.IsNullOrEmpty(xnsd) || !string.IsNullOrEmpty(xjnd) || !string.IsNullOrEmpty(xnnd) ||
                            !string.IsNullOrEmpty(zdgc) || !string.IsNullOrEmpty(gysz) || !string.IsNullOrEmpty(xqdmd) || !string.IsNullOrEmpty(xqgmd))
                        {
                            sbStr.Append("&sdtjfzjcdbqxysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AssistQtSn"].ToString()));
                        }
                        else
                        {
                            sbStr.Append("&sdtjfzjcdbqxysqm=");
                        }

                        // 心电图
                        if (!string.IsNullOrEmpty(xdt))
                        {
                            sbStr.Append("&sdtjxdtysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AECGSn"].ToString()));
                        }
                        else
                        {
                            sbStr.Append("&sdtjxdtysqm=");
                        }

                        // 腹部B超
                        if (!string.IsNullOrEmpty(fbBc))
                        {
                            sbStr.Append("&sdtjfbbcysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["ABtypeUltrasonicSn"].ToString()));
                        }
                        else
                        {
                            sbStr.Append("&sdtjfbbcysqm=");
                        }

                        // 其他B超
                        if (!string.IsNullOrEmpty(qtBc))
                        {
                            sbStr.Append("&sdtjfbbcqtysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["ABtypeQtSn"].ToString()));
                        }
                        else
                        {
                            sbStr.Append("&sdtjfbbcqtysqm=");
                        }

                        // 现存主要健康问题
                        sbStr.Append("&sdtjxczyjkwtysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["PhysicalQtSn"].ToString()));

                        // 主要用药情况
                        sbStr.Append("&sdtjzyyyqkysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AMAUSn"].ToString()));

                        // 健康评价
                        sbStr.Append("&sdtjjkpjysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["HealthAssessmentSn"].ToString()));

                        // 健康指导
                        sbStr.Append("&sdtjjkzdysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["HealthGuidanceSn"].ToString()));

                        // 反馈人
                        sbStr.Append("&sdfkrqz=").Append(HtmlHelper.GetUrlEncodeVal(drSign["PersonalFb"].ToString()));
                    }
                    else
                    {
                        sbStr.Append("&sdtjzkysqm=");
                        sbStr.Append("&sdtjybqkysqm=");
                        sbStr.Append("&sdtjshfsysqm=");
                        sbStr.Append("&sdtjzqgnysqm=");
                        sbStr.Append("&sdtjchatiysqm=");
                        sbStr.Append("&sdtjnwlbdbysqm=");
                        sbStr.Append("&sdtjfzjcxxysqm=");
                        sbStr.Append("&sdtjfzjcdbqxysqm=");
                        sbStr.Append("&sdtjxdtysqm=");
                        sbStr.Append("&sdtjfbbcysqm=");
                        sbStr.Append("&sdtjfbbcqtysqm=");
                        sbStr.Append("&sdtjxczyjkwtysqm=");
                        sbStr.Append("&sdtjzyyyqkysqm=");
                        sbStr.Append("&sdtjjkpjysqm=");
                        sbStr.Append("&sdtjjkzdysqm=");
                        sbStr.Append("&sdfkrqz=");
                    }

                    #endregion

                    // 本人 手动签字
                    sbStr.Append("&sdfkqzbr=").Append(person.memberName);

                    // 签字板
                    sbStr.Append("&fkqzbr=");

                    // 家属 手动签字
                    sbStr.Append("&sdfkqzjs=");

                    // 签字板
                    sbStr.Append("&fkqzjs=");

                    // 反馈人 签字板
                    sbStr.Append("&fkrqz=");

                    // 反馈时间
                    sbStr.Append("&fktime=").Append(CommonExtensions.GetConvertDate(fkdate, "1"));

                    string createDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (!string.IsNullOrEmpty(CreateTimeSameTj) && CreateTimeSameTj == "1")
                    {
                        if (!string.IsNullOrEmpty(baseinfoRow["CheckDate"].ToString()))
                        {
                            createDate = Convert.ToDateTime(Convert.ToDateTime(baseinfoRow["CheckDate"].ToString()).ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH:mm:ss")).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                    sbStr.Append("&createtime=").Append(CommonExtensions.GetUrlEncodeVal(createDate));
                    sbStr.Append("&updatetime=").Append(CommonExtensions.GetUrlEncodeVal(createDate));

                    if (loginKey.Length == 16)
                    {
                        //所属机构
                        sbStr.Append("&pRgid=").Append(loginKey.Substring(0, 12));

                        //创建机构
                        sbStr.Append("&creatregion=").Append(loginKey.Substring(0, 12));
                    }
                    else
                    {
                        //所属机构
                        sbStr.Append("&pRgid=").Append(loginKey.Substring(0, 15));

                        //创建机构
                        sbStr.Append("&creatregion=").Append(loginKey.Substring(0, 15));
                    }

                    //创建人
                    sbStr.Append("&createuser=").Append(loginKey);

                    //更新人
                    sbStr.Append("&updateuser=").Append(loginKey);

                    returnString = web.PostHttp(baseUrl + "health/healthSave.action", sbStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

                    if (!baseUrl.Contains("sdcsm_new"))
                    {
                        // 非威海地区直接判断返回结果，威海地区查询列表判断是否成功
                        if (string.IsNullOrEmpty(returnString))
                        {
                            //throw new Exception("新增失败");
                            return "新增失败！";
                        }
                        if (returnString.Contains("操作成功"))
                        {
                            string post = HtmlHelper.GetTagValue(returnString, "$(\"#mainContent\",top.window.document).attr(\"src\",\"", "\")");
                            post = post.Replace("/sdcsm_new/", "").Replace("/sdcsm/", "");
                            string retString1 = web.GetHttp(baseUrl + post, "", SysCookieContainer);
                        }

                        HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                        if (doc == null || doc.DocumentNode.SelectSingleNode("//body/script[last()]") == null)
                        {
                            //throw new Exception("新增失败");
                            return "新增失败！";
                        }
                        else
                        {
                            var returnNode = doc.DocumentNode.SelectSingleNode("//body/script[last()]");

                            if (returnNode.InnerText.IndexOf("操作成功") == -1)
                            {
                                CommonExtensions.WriteLog(returnString);
                                //throw new Exception("新增失败");
                                return "新增失败！";
                            }
                        }

                        return "";
                    }
                    else
                    {
                        var sfList = GetCheckedDate(person);
                        sfList = sfList.Where(m => m.sfDate == tjDate).ToList();
                        if (sfList.Count > 0)
                        {
                            return "";
                        }
                        else
                        {
                            // call("EX-体检档案:身份证[" + person.idNumber + "]:体检新增失败" + DateTime.Now.ToString("HH:mm:ss") + "，重试中[" + (index + 1) + "]...");

                            if (index == 4)
                            {
                                return "新增失败！";
                            }
                            else
                            {
                                Thread.Sleep((10 * (index + 1)) * 1000);
                                CommonExtensions.WriteLog("[index:" + index + "]" + returnString);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    CommonExtensions.WriteLog("新增体检错误：" + ex.Message);
                    return "新增体检错误" + ex.Message;
                }
            }

            return "";
        }

        /// <summary>
        /// 更新体检信息
        /// </summary>
        /// <param name="person"></param>
        /// <param name="ds"></param>
        /// <param name="callback"></param>
        private string UpdateTj(PersonModel person, string key, DataSet ds, string fkdate, Action<string> callback)
        {
            string idcard = person.idNumber;
            WebHelper web = new WebHelper();

            string postData = "dGrdabh=" + person.pid + "&id=" + key + "&tz=2";
            string returnString = web.PostHttp(baseUrl + "/health/healthToUpdate.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);

            if (string.IsNullOrEmpty(returnString))
            {
                return "更新失败！请确保网路畅通。";
            }

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            if (doc != null)
            {
                // 获取修改前各栏位值
                DataRow baseinfoRow = ds.Tables["ARCHIVE_CUSTOMERBASEINFO"].Rows[0];
                object renQun = baseinfoRow["PopulationType"];
                StringBuilder sbStr = new StringBuilder();
                var node = doc.DocumentNode.SelectSingleNode("//input[@id='qdqxz']");
                sbStr.Append("id=").Append(key);
                sbStr.Append("&qdqxz=").Append("0");
                node = doc.DocumentNode.SelectSingleNode("//input[@id='wzd']");
                sbStr.Append("&wzd=").Append("100");
                sbStr.Append("&status=").Append("update");

                #region ARCHIVE_CUSTOMERBASEINFO

                //档案号
                sbStr.Append("&dGrdabh=").Append(person.pid);

                //生日
                sbStr.Append("&tjkInfo.dCsrq=").Append(baseinfoRow["Birthday"] == null ? "" : Convert.ToDateTime(baseinfoRow["Birthday"]).ToString("yyyy-MM-dd"));

                //体检时间
                string tjDate = baseinfoRow["CheckDate"] == null ? DateTime.Now.ToString("yyyy-MM-dd") : Convert.ToDateTime(baseinfoRow["CheckDate"]).ToString("yyyy-MM-dd");
                sbStr.Append("&happentime=").Append(tjDate);

                //责任医生
                sbStr.Append("&field2=").Append(HtmlHelper.GetUrlEncodeVal(baseinfoRow["Doctor"].ToString()));

                //症状
                var tem = baseinfoRow["Symptom"].ToString().Split(',');
                foreach (var s in tem)
                {
                    sbStr.Append("&gZhzh=").Append(s == "25" ? "99" : s);
                }
                sbStr.Append("&gZzqt=").Append(HtmlHelper.GetUrlEncodeVal(baseinfoRow["Other"].ToString()));

                // 症状签字板
                sbStr.Append("&tjzkysqm=");

                #endregion

                #region  ARCHIVE_GENERALCONDITION

                DataRow yibanDR = ds.Tables["ARCHIVE_GENERALCONDITION"].Rows[0];

                //体温
                sbStr.Append("&gTw=").Append(yibanDR["AnimalHeat"]);

                //脉率
                string pulseRate = yibanDR["PulseRate"].ToString();

                if (pulseRate != "")
                {
                    pulseRate = Math.Floor(double.Parse(pulseRate)).ToString();
                }

                sbStr.Append("&gMb=").Append(pulseRate);

                //呼吸频率
                string breathRate = yibanDR["BreathRate"].ToString();

                if (breathRate != "")
                {
                    breathRate = Math.Floor(double.Parse(breathRate)).ToString();
                }

                sbStr.Append("&gHx=").Append(breathRate);

                //左侧高
                sbStr.Append("&gXyzc1=").Append(yibanDR["LeftHeight"]);

                //左侧低
                sbStr.Append("&gXyzc2=").Append(yibanDR["LeftPre"]);

                //左侧：原因
                sbStr.Append("&zcyy=").Append(HtmlHelper.GetUrlEncodeVal(yibanDR["LeftReason"].ToString()));

                //右侧高
                sbStr.Append("&gXyyc1=").Append(yibanDR["RightHeight"]);

                //右侧低
                sbStr.Append("&gXyyc2=").Append(yibanDR["RightPre"]);

                //右：原因
                sbStr.Append("&ycyy=").Append(HtmlHelper.GetUrlEncodeVal(yibanDR["RightReason"].ToString()));

                //是否加入高血压管理
                sbStr.Append("&nrgxygl=").Append(renQun == null || renQun.ToString().Contains("6") ? "3" : "1");

                node = doc.DocumentNode.SelectSingleNode("//input[@name='nrgxyglold']");
                string strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                sbStr.Append("&nrgxyglold=").Append(strtmp);

                //身高
                sbStr.Append("&gSg=").Append(yibanDR["Height"]);

                //体重
                sbStr.Append("&gTzh=").Append(yibanDR["Weight"]);

                //腰围
                string yw = yibanDR["Waistline"].ToString();
                if (!string.IsNullOrEmpty(yw))
                {
                    yw = decimal.Parse(yw).ToString("0");
                }
                sbStr.Append("&gYw=").Append(yw);

                //体质指数
                sbStr.Append("&gTzhzh=").Append(yibanDR["BMI"]);

                //老年人健康状态
                sbStr.Append("&lnrzkpg=").Append(yibanDR["OldHealthStaus"]);

                //老年人生活自理能力
                sbStr.Append("&lnrzlpg=").Append(yibanDR["OldSelfCareability"]);

                //老年人认知能力
                sbStr.Append("&gLnrrz=").Append(yibanDR["OldRecognise"]);

                //老年人情感
                sbStr.Append("&gLnrqg=").Append(yibanDR["OldEmotion"]);

                //智力能力评分
                sbStr.Append("&gLnrrzfen=").Append(yibanDR["InterScore"]);

                //老年人抑郁评分
                sbStr.Append("&gLnrqgfen=").Append(yibanDR["GloomyScore"]);

                //签字板
                sbStr.Append("&tjybqkysqm=");

                #endregion

                #region ARCHIVE_LIFESTYLE

                string str = "";
                if (ds.Tables["ARCHIVE_LIFESTYLE"] != null && ds.Tables["ARCHIVE_LIFESTYLE"].Rows.Count > 0)
                {
                    DataRow lifeDR = ds.Tables["ARCHIVE_LIFESTYLE"].Rows[0];

                    //每次锻炼时间
                    sbStr.Append("&gMcdlsj=").Append(lifeDR["ExerciseTimes"]);

                    //坚持锻炼时间
                    sbStr.Append("&gJcdlsj=").Append(lifeDR["ExcisepersistTime"]);

                    //锻炼方式
                    str = lifeDR["ExerciseExistenseOther"].ToString();
                    strtmp = lifeDR["ExerciseExistense"].ToString();
                    sbStr.Append("&gDlfs=").Append(string.IsNullOrEmpty(strtmp) ? "" : HtmlHelper.GetUrlEncodeVal(strtmp.Replace("1", "散步").Replace("2", "跑步").Replace("3", "广场舞").Replace("4", "") + str));

                    //平均一天吸烟量
                    sbStr.Append("&gRxyl=").Append(lifeDR["SmokeDayNum"]);

                    //开始吸烟年龄
                    sbStr.Append("&gKsxynl=").Append(lifeDR["SmokeAgeStart"]);

                    //戒烟年龄
                    sbStr.Append("&gJynl=").Append(lifeDR["SmokeAgeForbiddon"]);

                    //日饮酒量
                    sbStr.Append("&gRyjl=").Append(lifeDR["DayDrinkVolume"]);

                    //戒酒年龄
                    sbStr.Append("&gJjnl=").Append(lifeDR["ForbiddonAge"]);

                    //开始饮酒年龄
                    sbStr.Append("&gKsyjnl=").Append(lifeDR["DrinkStartAge"]);

                    //是否已戒酒
                    sbStr.Append("&gSfjj=").Append(lifeDR["IsDrinkForbiddon"]);

                    //近一年是否醉酒
                    sbStr.Append("&gYnnsfyj=").Append(lifeDR["DrinkThisYear"]);

                    //饮酒种类
                    tem = lifeDR["DrinkType"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        sbStr.Append("&gYjzl=").Append(n == "5" ? "99" : n);
                    }

                    //饮酒种类其他
                    sbStr.Append("&gYjzlqt=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["DrinkTypeOther"].ToString()));

                    //饮食习惯
                    tem = lifeDR["DietaryHabit"].ToString().Split(',');
                    foreach (var n in tem)
                    {
                        if (string.IsNullOrEmpty(n)) continue;
                        sbStr.Append("&gYsxg=").Append(n);
                    }

                    //锻炼频率
                    sbStr.Append("&gDlpl=").Append(lifeDR["ExerciseRate"]);

                    //吸烟状况
                    sbStr.Append("&gXyzk=").Append(lifeDR["SmokeCondition"]);

                    //饮酒频率
                    sbStr.Append("&gYjpl=").Append(lifeDR["DrinkRate"]);

                    //职业危害史有无
                    sbStr.Append("&gYwzybl=").Append(lifeDR["CareerHarmFactorHistory"].ToString() == "2" ? "3" : "1");

                    //工种
                    sbStr.Append("&gJtzy=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["WorkType"].ToString()));

                    //工作时间
                    sbStr.Append("&gCysj=").Append(lifeDR["WorkTime"]);

                    //粉尘
                    sbStr.Append("&fenchen=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Dust"].ToString()));

                    //粉尘防护无有
                    sbStr.Append("&fchcs=").Append(lifeDR["DustProtect"] != null && lifeDR["DustProtect"].ToString() == "1" ? "1" : "3");
                    sbStr.Append("&fchy=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["DustProtectEx"].ToString()));

                    //放射
                    sbStr.Append("&gShexian=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Radiogen"].ToString()));

                    //放射防护无有
                    sbStr.Append("&gSxfhcs=").Append(lifeDR["RadiogenProtect"] != null && lifeDR["RadiogenProtect"].ToString() == "1" ? "1" : "3");
                    sbStr.Append("&gSxfhcsqt=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["RadiogenProtectEx"].ToString()));

                    //物理
                    sbStr.Append("&wuliyinsu=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Physical"].ToString()));

                    //物理防护无有
                    sbStr.Append("&wlcs=").Append(lifeDR["PhysicalProtect"] != null && lifeDR["PhysicalProtect"].ToString() == "1" ? "1" : "3");
                    sbStr.Append("&wly=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["PhysicalProtectEx"].ToString()));

                    //化学
                    sbStr.Append("&gHxp=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Chem"].ToString()));

                    //化学防护无有
                    sbStr.Append("&gHxpfhcs=").Append(lifeDR["ChemProtect"] != null && lifeDR["ChemProtect"].ToString() == "1" ? "1" : "3");
                    sbStr.Append("&gHxpfhcsjt=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["ChemProtectEx"].ToString()));

                    //其他危害
                    sbStr.Append("&blqita=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["Other"].ToString()));

                    //其他危害防护无有
                    sbStr.Append("&blqtcs=").Append(lifeDR["OtherProtect"] != null && lifeDR["OtherProtect"].ToString() == "1" ? "1" : "3");

                    //
                    sbStr.Append("&qty=").Append(HtmlHelper.GetUrlEncodeVal(lifeDR["OtherProtectEx"].ToString()));

                    //签字板
                    sbStr.Append("&tjshfsysqm=");
                }

                #endregion

                #region ARCHIVE_VISCERAFUNCTION

                if (ds.Tables["ARCHIVE_VISCERAFUNCTION"] != null && ds.Tables["ARCHIVE_VISCERAFUNCTION"].Rows.Count > 0)
                {
                    DataRow zangQi = ds.Tables["ARCHIVE_VISCERAFUNCTION"].Rows[0];

                    //口唇
                    sbStr.Append("&gKouchun=").Append(!string.IsNullOrEmpty(zangQi["Lips"].ToString()) ? zangQi["Lips"].ToString().Replace("0", "") : "");

                    //kchqt 口唇其他
                    sbStr.Append("&kchqt=").Append(HtmlHelper.GetUrlEncodeVal(zangQi["LipsEx"].ToString()));

                    //齿列
                    string chilie = zangQi["ToothResides"].ToString();

                    if (!string.IsNullOrEmpty(chilie))
                    {
                        var temp = chilie.Split(',');
                        foreach (var n in temp)
                        {
                            sbStr.Append("&gChilei=").Append(n);
                        }
                    }

                    //缺齿
                    if (chilie.Contains("2"))
                    {
                        string quechi = zangQi["HypodontiaEx"].ToString();
                        if (!string.IsNullOrEmpty(quechi) && quechi.Contains("#"))
                        {
                            var v = quechi.Split('#');
                            sbStr.Append("&quechi1=").Append(CommonExtensions.GetUrlEncodeVal(v[0]));

                            sbStr.Append("&quechi2=").Append(CommonExtensions.GetUrlEncodeVal(v[1]));

                            sbStr.Append("&quechi3=").Append(CommonExtensions.GetUrlEncodeVal(v[2]));

                            sbStr.Append("&quechi4=").Append(CommonExtensions.GetUrlEncodeVal(v[3]));
                        }
                    }

                    //龋齿
                    if (chilie.Contains("3"))
                    {
                        string quchi = zangQi["SaprodontiaEx"].ToString();
                        if (!string.IsNullOrEmpty(quchi) && quchi.Contains("#"))
                        {
                            var v = quchi.Split('#');
                            sbStr.Append("&quchi1=").Append(CommonExtensions.GetUrlEncodeVal(v[0]));

                            sbStr.Append("&quchi2=").Append(CommonExtensions.GetUrlEncodeVal(v[1]));

                            sbStr.Append("&quchi3=").Append(CommonExtensions.GetUrlEncodeVal(v[2]));

                            sbStr.Append("&quchi4=").Append(CommonExtensions.GetUrlEncodeVal(v[3]));
                        }
                    }

                    //龋齿
                    if (chilie.Contains("4"))
                    {
                        string yichi = zangQi["DentureEx"].ToString();
                        if (!string.IsNullOrEmpty(yichi) && yichi.Contains("#"))
                        {
                            var v = yichi.Split('#');
                            sbStr.Append("&yichi1=").Append(CommonExtensions.GetUrlEncodeVal(v[0]));

                            sbStr.Append("&yichi2=").Append(CommonExtensions.GetUrlEncodeVal(v[1]));

                            sbStr.Append("&yichi3=").Append(CommonExtensions.GetUrlEncodeVal(v[2]));

                            sbStr.Append("&yichi4=").Append(CommonExtensions.GetUrlEncodeVal(v[3]));
                        }
                    }

                    //齿列其他
                    sbStr.Append("&chlqt=").Append(HtmlHelper.GetUrlEncodeVal(zangQi["ToothResidesOther"].ToString()));

                    //PharyngealEx 咽部其他
                    sbStr.Append("&ybqt=").Append(HtmlHelper.GetUrlEncodeVal(zangQi["PharyngealEx"].ToString()));

                    //咽部
                    sbStr.Append("&gYanbu=").Append(!string.IsNullOrEmpty(zangQi["Pharyngeal"].ToString()) ? zangQi["Pharyngeal"].ToString().Replace("0", "") : "");

                    //左眼
                    sbStr.Append("&gZysl=").Append(zangQi["LeftView"]);

                    //右眼
                    sbStr.Append("&gYysl=").Append(zangQi["RightView"]);

                    //左眼矫正
                    sbStr.Append("&gZyjz=").Append(zangQi["LeftEyecorrect"]);

                    //右眼矫正
                    sbStr.Append("&gYyjz=").Append(zangQi["RightEyecorrect"]);

                    //听力
                    sbStr.Append("&gTl=").Append(zangQi["Listen"]);

                    //运动功能
                    sbStr.Append("&gYdgn=").Append(zangQi["SportFunction"]);
                }

                //签字板
                sbStr.Append("&tjzqgnysqm=");

                #endregion

                #region ARCHIVE_PHYSICALEXAM

                DataRow chatiDR = ds.Tables["ARCHIVE_PHYSICALEXAM"].Rows[0];

                //眼底
                sbStr.Append("&gYand=").Append(chatiDR["EyeRound"]);
                sbStr.Append("&gYandyi=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["EyeRoundEx"].ToString()));

                //眼底签字
                //手动签字
                sbStr.Append("&sdtjyandiysqm=");

                //签字板
                sbStr.Append("&tjyandiysqm=");

                //皮肤
                sbStr.Append("&gPfgm=").Append(!string.IsNullOrEmpty(chatiDR["Skin"].ToString()) ? chatiDR["Skin"].ToString().Replace("7", "99") : "");
                sbStr.Append("&gPfqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["SkinEx"].ToString()));

                //签字板
                sbStr.Append("&tjchatiysqm=");

                //巩膜
                sbStr.Append("&gGongmo=").Append(chatiDR["Sclere"] != null ? chatiDR["Sclere"].ToString().Replace("4", "99") : "");
                sbStr.Append("&gGmqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["SclereEx"].ToString()));

                //淋巴
                sbStr.Append("&gLbj=").Append(chatiDR["Lymph"]);
                sbStr.Append("&gLbjqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["LymphEx"].ToString()));

                //桶状胸
                sbStr.Append("&gTzx=").Append(chatiDR["BarrelChest"] != null && chatiDR["BarrelChest"].ToString() == "1" ? "2" : "1");

                //呼吸音
                sbStr.Append("&gHxy=").Append(chatiDR["BreathSounds"]);
                sbStr.Append("&gHxyyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["BreathSoundsEx"].ToString()));

                //罗音
                sbStr.Append("&gLy=").Append(chatiDR["Rale"]);
                sbStr.Append("&gLyyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["RaleEx"].ToString()));

                //杂音
                sbStr.Append("&gZayin=").Append(chatiDR["Noise"]);
                sbStr.Append("&gZayinyo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["NoiseEx"].ToString()));

                //压痛
                sbStr.Append("&gYato=").Append(chatiDR["PressPain"]);
                sbStr.Append("&gYatoyo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["PressPainEx"].ToString()));

                //包块
                sbStr.Append("&gBk=").Append(chatiDR["EnclosedMass"]);
                sbStr.Append("&gBkyo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["EnclosedMassEx"].ToString()));

                //肝大
                sbStr.Append("&gGanda=").Append(chatiDR["Liver"]);
                sbStr.Append("&gGandayo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["LiverEx"].ToString()));

                //脾大
                sbStr.Append("&gPida=").Append(chatiDR["Spleen"]);
                sbStr.Append("&gPidayo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["SpleenEx"].ToString()));

                //浊音
                sbStr.Append("&gZhuoyin=").Append(chatiDR["Voiced"]);
                sbStr.Append("&gZhuoyinyo=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["VoicedEx"].ToString()));

                //肛门指诊
                strtmp = chatiDR["Anus"].ToString();
                sbStr.Append("&gGmzhzh=").Append(string.IsNullOrEmpty(strtmp) ? "" : strtmp.Replace("5", "99"));

                //肛门指诊其他 
                sbStr.Append("&gGmzhzhyi=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["AnusEx"].ToString()));

                //肛门指诊签字
                //手动签字
                sbStr.Append("&sdtjgmzzysqm=");

                //签字板
                sbStr.Append("&tjgmzzysqm=");

                //心率
                decimal dec = 0;
                object obDBnull = DBNull.Value;
                chatiDR["HeartRate"] = Decimal.TryParse(chatiDR["HeartRate"].ToString(), out dec) ? dec.ToString("0") : obDBnull;
                sbStr.Append("&gXinlv=").Append(chatiDR["HeartRate"]);

                //心律
                sbStr.Append("&gXinlvci=").Append(chatiDR["HeartRhythm"]);

                //下肢水肿
                sbStr.Append("&gXzsz=").Append(GetWebEdemaCodeByPadCode(chatiDR["Edema"]));

                //足背，动脉搏动
                sbStr.Append("&gZbdmmd=").Append(chatiDR["FootBack"]);

                //查体其他
                sbStr.Append("&gCtqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["Other"].ToString()));

                //查体其他签字
                //手动签字
                sbStr.Append("&sdtjctqtysqm=");

                //签字板
                sbStr.Append("&tjctqtysqm=");

                //乳腺
                tem = chatiDR["Breast"].ToString().Split(',');
                foreach (var n in tem)
                {
                    sbStr.Append("&gRuxian=").Append(n == "5" ? "99" : n);
                }
                //乳腺异常
                sbStr.Append("&gRuxianqt=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["BreastEx"].ToString()));

                //乳腺签字
                //手动签字
                sbStr.Append("&sdtjrxysqm=");

                //签字板
                sbStr.Append("&tjrxysqm=");

                //外阴
                sbStr.Append("&gWaiyin=").Append(chatiDR["Vulva"]);

                //外阴异常
                sbStr.Append("&gWaiyinyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["VulvaEx"].ToString()));

                //阴道
                sbStr.Append("&gYindao=").Append(chatiDR["Vagina"]);
                sbStr.Append("&gYindaoyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["VaginaEx"].ToString()));

                //宫颈
                sbStr.Append("&gGongjing=").Append(chatiDR["CervixUteri"]);
                sbStr.Append("&gGongjingyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["CervixUteriEx"].ToString()));

                //宫体
                sbStr.Append("&gGongti=").Append(chatiDR["Corpus"]);
                sbStr.Append("&gGongtiyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["CorpusEx"].ToString()));

                //附件
                sbStr.Append("&gFujian=").Append(chatiDR["Attach"]);
                sbStr.Append("&gFujianyc=").Append(HtmlHelper.GetUrlEncodeVal(chatiDR["AttachEx"].ToString()));

                #endregion

                XmlNodeList xmlNodes = null;
                XmlDocument xmldoc = new XmlDocument();
                xmldoc.Load("D:\\QCSoft\\QCSoftPlatformV2.0\\SHValueRange.xml");
                if (xmldoc != null)
                {
                    xmlNodes = xmldoc.SelectNodes("//NewDataSet/Table1");
                }

                #region  ARCHIVE_ASSISTCHECK

                DataRow fuzhuDR = ds.Tables["ARCHIVE_ASSISTCHECK"].Rows[0];

                // 正常和异常字段的个数
                int zc = 0;
                int yc = 0;

                //血红蛋白
                string xhdb = fuzhuDR["HB"].ToString();
                sbStr.Append("&hb=").Append(xhdb);

                strtmp = GetZcOrYc(xmlNodes, "HB", xhdb);
                sbStr.Append("&xhdb=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //白细胞
                string bxb = fuzhuDR["WBC"].ToString();
                sbStr.Append("&wbc=").Append(bxb);

                strtmp = GetZcOrYc(xmlNodes, "WBC", bxb);
                sbStr.Append("&bxb=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //血小板
                string xxb = fuzhuDR["PLT"].ToString();
                sbStr.Append("&plt=").Append(xxb);

                strtmp = GetZcOrYc(xmlNodes, "PLT", xxb);
                sbStr.Append("&xxb=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                if (yc > 0)
                {
                    sbStr.Append("&xcg=").Append("2");
                }
                else if (zc > 0)
                {
                    sbStr.Append("&xcg=").Append("1");
                }

                //血常规其他
                string xcgqt = fuzhuDR["BloodOther"].ToString();
                sbStr.Append("&gXcgqt=").Append(HtmlHelper.GetUrlEncodeVal(xcgqt));

                zc = 0;
                yc = 0;
                strtmp = "";

                //尿蛋白
                string ndb = fuzhuDR["PRO"].ToString();
                sbStr.Append("&gNdb=").Append(HtmlHelper.GetUrlEncodeVal(ndb));

                strtmp = GetNcg(ndb);
                sbStr.Append("&ndb=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //尿糖
                string nt = fuzhuDR["GLU"].ToString();
                sbStr.Append("&gNt=").Append(HtmlHelper.GetUrlEncodeVal(nt));

                strtmp = GetNcg(nt);
                sbStr.Append("&nt=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //尿酮体
                string ntt = fuzhuDR["KET"].ToString();
                sbStr.Append("&gNtt=").Append(HtmlHelper.GetUrlEncodeVal(ntt));

                strtmp = GetNcg(ntt);
                sbStr.Append("&ntt=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //尿潜血
                string nqx = fuzhuDR["BLD"].ToString();
                sbStr.Append("&gNqx=").Append(HtmlHelper.GetUrlEncodeVal(nqx));

                strtmp = GetNcg(nqx);
                sbStr.Append("&nqx=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //尿常规其他
                string ncgqt = fuzhuDR["UrineOther"].ToString();
                sbStr.Append("&gNcgqt=").Append(HtmlHelper.GetUrlEncodeVal(ncgqt));

                if (yc > 0)
                {
                    sbStr.Append("&ncg=").Append("2");
                }
                else if (zc > 0)
                {
                    sbStr.Append("&ncg=").Append("1");
                }

                //是否为糖尿病
                sbStr.Append("&nrtnbgl=").Append(renQun.ToString().Contains("7") ? "3" : "1");

                node = doc.DocumentNode.SelectSingleNode("//input[@name='nrtnbglold']");
                strtmp = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                sbStr.Append("&nrtnbglold=").Append(strtmp);

                //空腹血糖
                string kfxt = fuzhuDR["FPGL"].ToString();
                sbStr.Append("&gKfxt=").Append(kfxt);

                strtmp = GetZcOrYc(xmlNodes, "FPGL", kfxt);
                sbStr.Append("&kfxt=").Append(strtmp);

                // 餐后2小时血糖
                string chxt = fuzhuDR["FPGDL"].ToString();
                sbStr.Append("&gChxt=").Append(chxt);

                //心电图
                string xdt = fuzhuDR["ECG"].ToString();

                if (!string.IsNullOrEmpty(xdt))
                {
                    var lst = xdt.Split(',');

                    foreach (var item in lst)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            sbStr.Append("&gXindt=").Append(GetWebECGByPadCode(item));
                        }
                    }
                }
                else
                {
                    sbStr.Append("&gXindt=");
                }

                sbStr.Append("&gXindtyi=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["ECGEx"].ToString()));

                // 签字板
                sbStr.Append("&tjxdtysqm=");

                //尿微量白蛋白
                string nwlbdb = fuzhuDR["ALBUMIN"].ToString();
                sbStr.Append("&nwlbdb=").Append(nwlbdb);

                // 签字板
                sbStr.Append("&tjnwlbdbysqm=");

                //大便潜血
                string dbqx = fuzhuDR["FOB"].ToString();
                sbStr.Append("&gDbqx=").Append(dbqx);

                // 签字板
                sbStr.Append("&tjfzjcdbqxysqm=");

                //糖化血红蛋白
                string thxhdb = fuzhuDR["HBALC"].ToString();
                sbStr.Append("&gThxhdb=").Append(thxhdb);

                //乙型肝炎表面抗原
                string yxgy = fuzhuDR["HBSAG"].ToString();
                sbStr.Append("&hbsag=").Append(yxgy);

                zc = 0;
                yc = 0;

                //血清谷丙转氨酶
                string xqgb = fuzhuDR["SGPT"].ToString();
                sbStr.Append("&alt=").Append(xqgb);

                strtmp = GetZcOrYc(xmlNodes, "SGPT", xqgb);
                sbStr.Append("&gbzam=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //血清谷草转氨酶
                string xqgc = fuzhuDR["GOT"].ToString();
                sbStr.Append("&ast=").Append(xqgc);

                strtmp = GetZcOrYc(xmlNodes, "GOT", xqgc);
                sbStr.Append("&gczam=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //白蛋白
                string bdb = fuzhuDR["BP"].ToString();
                sbStr.Append("&alb=").Append(bdb);

                strtmp = GetZcOrYc(xmlNodes, "BP", bdb);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //总胆红素
                string zdhs = fuzhuDR["TBIL"].ToString();
                sbStr.Append("&tbil=").Append(zdhs);

                strtmp = GetZcOrYc(xmlNodes, "TBIL", zdhs);
                sbStr.Append("&zdhs=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //结合胆红素
                string jhdhs = fuzhuDR["CB"].ToString();
                sbStr.Append("&dbil=").Append(jhdhs);

                strtmp = GetZcOrYc(xmlNodes, "CB", jhdhs);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                if (yc > 0)
                {
                    sbStr.Append("&ggn=").Append("2");
                }
                else if (zc > 0)
                {
                    sbStr.Append("&ggn=").Append("1");
                }

                zc = 0;
                yc = 0;

                //血清肌酐
                string xqjg = fuzhuDR["SCR"].ToString();
                sbStr.Append("&scr=").Append(xqjg);

                strtmp = GetZcOrYc(xmlNodes, "SCR", xqjg);
                sbStr.Append("&xqjg=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //血尿素氮
                string xnsd = fuzhuDR["BUN"].ToString();
                sbStr.Append("&bun=").Append(xnsd);

                strtmp = GetZcOrYc(xmlNodes, "BUN", xnsd);
                sbStr.Append("&nsd=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //血钾浓度
                string xjnd = fuzhuDR["PC"].ToString();
                sbStr.Append("&gSgnxjnd=").Append(xjnd);

                strtmp = GetZcOrYc(xmlNodes, "PC", xjnd);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //血钠浓度
                string xnnd = fuzhuDR["HYPE"].ToString();
                sbStr.Append("&gSgnxnnd=").Append(xnnd);

                strtmp = GetZcOrYc(xmlNodes, "HYPE", xnnd);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                if (yc > 0)
                {
                    sbStr.Append("&sgn=").Append("2");
                }
                else if (zc > 0)
                {
                    sbStr.Append("&sgn=").Append("1");
                }

                zc = 0;
                yc = 0;

                //总胆固醇
                string zdgc = fuzhuDR["TC"].ToString();
                sbStr.Append("&cho=").Append(zdgc);

                strtmp = GetZcOrYc(xmlNodes, "TC", zdgc);
                sbStr.Append("&zdgc=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //甘油三酯
                string gysz = fuzhuDR["TG"].ToString();
                sbStr.Append("&tg=").Append(gysz);

                strtmp = GetZcOrYc(xmlNodes, "TG", gysz);
                sbStr.Append("&gysz=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //血清低密度脂蛋白胆固醇
                string xqdmd = fuzhuDR["LowCho"].ToString();
                sbStr.Append("&ldlc=").Append(xqdmd);

                strtmp = GetZcOrYc(xmlNodes, "LowCho", xqdmd);
                sbStr.Append("&dmd=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                //血清高密度脂蛋白胆固醇
                string xqgmd = fuzhuDR["HeiCho"].ToString();
                sbStr.Append("&hdlc=").Append(xqgmd);

                strtmp = GetZcOrYc(xmlNodes, "HeiCho", xqgmd);
                sbStr.Append("&gmd=").Append(strtmp);
                if (strtmp == "1")
                {
                    zc++;
                }
                else if (strtmp == "2")
                {
                    yc++;
                }

                if (yc > 0)
                {
                    sbStr.Append("&xz=").Append("2");
                }
                else if (zc > 0)
                {
                    sbStr.Append("&xz=").Append("1");
                }

                //胸部X片
                sbStr.Append("&gXiongp=").Append(fuzhuDR["CHESTX"]);
                sbStr.Append("&gXiongpyc=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["CHESTXEx"].ToString()));

                // 胸部X线片手动签字
                sbStr.Append("&sdtjxbxxpysqm=");

                // 签字板
                sbStr.Append("&tjxbxxpysqm=");

                // 腹部B超
                string fbBc = fuzhuDR["BCHAO"].ToString();
                sbStr.Append("&gBchao=").Append(fbBc);
                sbStr.Append("&gBchaoyi=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["BCHAOEx"].ToString()));

                // 腹部B超签字板
                sbStr.Append("&tjfbbcysqm=");

                //宫颈涂片
                sbStr.Append("&gGjtp=").Append(fuzhuDR["CERVIX"]);
                sbStr.Append("&gGjtpyc=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["CERVIXEx"].ToString()));

                //其他
                sbStr.Append("&gFuzhuqt=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["Other"].ToString()));

                // 辅助检查其他 手动签字
                sbStr.Append("&sdtjfzjcqtysqm=");

                // 签字板
                sbStr.Append("&tjfzjcqtysqm=");

                #region 2017-10-20添加新字段

                //血型
                string xx = fuzhuDR["BloodType"].ToString();
                sbStr.Append("&xxABO=").Append(GetWebBloodTypeByPadCode(xx));

                string rh = fuzhuDR["RH"].ToString();
                sbStr.Append("&xxRh=").Append(HtmlHelper.GetUrlEncodeVal(GetWebRHByPadCode(rh)));

                //签字板
                sbStr.Append("&tjfzjcxxysqm=");

                //同型半胱氨酸
                string txbg = fuzhuDR["HCY"].ToString();
                sbStr.Append("&txbgas=").Append(txbg);

                // 其他B超
                string qtBc = fuzhuDR["BCHAOther"].ToString();
                sbStr.Append("&gBchaoqt=").Append(qtBc);
                sbStr.Append("&gBchaoyiqt=").Append(HtmlHelper.GetUrlEncodeVal(fuzhuDR["BCHAOtherEx"].ToString()));

                // 签字板
                sbStr.Append("&tjfbbcqtysqm=");

                #endregion

                #endregion

                #region ARCHIVE_MEDI_PHYS_DIST

                if (ds.Tables["ARCHIVE_MEDI_PHYS_DIST"] != null && ds.Tables["ARCHIVE_MEDI_PHYS_DIST"].Rows.Count > 0)
                {
                    DataRow zhongyiDR = ds.Tables["ARCHIVE_MEDI_PHYS_DIST"].Rows[0];

                    //平和质
                    sbStr.Append("&gPhz=").Append(zhongyiDR["Mild"]);

                    //气虚质
                    sbStr.Append("&gQxz=").Append(zhongyiDR["Faint"]);

                    //阳虚质
                    sbStr.Append("&gYangxz=").Append(zhongyiDR["Yang"]);

                    //阴虚质
                    sbStr.Append("&gYinxz=").Append(zhongyiDR["Yin"]);

                    //痰湿质
                    sbStr.Append("&gTsz=").Append(zhongyiDR["PhlegmDamp"]);

                    //湿热质
                    sbStr.Append("&gSrz=").Append(zhongyiDR["Muggy"]);

                    //血瘀质
                    sbStr.Append("&gXyz=").Append(zhongyiDR["BloodStasis"]);

                    //气郁质
                    sbStr.Append("&gQyz=").Append(zhongyiDR["QiConstraint"]);

                    //特禀质
                    sbStr.Append("&gTbz=").Append(zhongyiDR["Characteristic"]);
                }

                #endregion

                #region ARCHIVE_HEALTHQUESTION

                DataRow jiankangDR = ds.Tables["ARCHIVE_HEALTHQUESTION"].Rows[0];

                //脑血管
                tem = jiankangDR["BrainDis"].ToString().Split(',');
                foreach (var n in tem)
                {
                    sbStr.Append("&gNxgjb=").Append(n == "6" ? "99" : n);
                }
                if (tem != null && tem.Count() > 0)
                {
                    sbStr.Append("&gNxgjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["BrainOther"].ToString()));
                }

                //肾脏
                tem = jiankangDR["RenalDis"].ToString().Split(',');
                foreach (var n in tem)
                {
                    sbStr.Append("&gSzjb=").Append(n == "6" ? "99" : n);
                }
                if (tem != null && tem.Count() > 0)
                {
                    sbStr.Append("&gSzjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["RenalOther"].ToString()));
                }

                //心血管疾病
                tem = jiankangDR["HeartDis"].ToString().Split(',');
                foreach (var n in tem)
                {
                    sbStr.Append("&gXzjb=").Append(n == "10" ? "99" : n);
                }
                if (tem != null && tem.Count() > 0)
                {
                    sbStr.Append("&gXzjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["HeartOther"].ToString()));
                }

                //眼部
                tem = jiankangDR["EyeDis"].ToString().Split(',');
                foreach (var n in tem)
                {
                    sbStr.Append("&gYbjb=").Append(n == "5" ? "99" : n);
                }
                if (tem != null && tem.Count() > 0)
                {
                    sbStr.Append("&gYbjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["EyeOther"].ToString()));
                }

                //神经系统
                tem = jiankangDR["NerveDis"].ToString().Split(',');
                foreach (var n in tem)
                {
                    sbStr.Append("&gSjxtjb=").Append(GetWebNerveDisByPadCode(n));
                }
                if (tem != null && tem.Count() > 0)
                {
                    //神经系统其他
                    sbStr.Append("&gSjxtjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["NerveOther"].ToString()));
                }

                //其他
                tem = jiankangDR["ElseDis"].ToString().Split(',');
                foreach (var n in tem)
                {
                    sbStr.Append("&gQtxtjb=").Append(GetWebElseDisByPadCode(n));
                }
                if (tem != null && tem.Count() > 0)
                {
                    //其他-其他
                    sbStr.Append("&gQtxtjbqt=").Append(HtmlHelper.GetUrlEncodeVal(jiankangDR["ElseOther"].ToString()));
                }

                // 签字板
                sbStr.Append("&tjxczyjkwtysqm=");

                #endregion

                #region ARCHIVE_HOSPITALHISTORY

                DataTable zhuyuanDT = ds.Tables["ARCHIVE_HOSPITALHISTORY"];
                if (zhuyuanDT == null || zhuyuanDT.Rows.Count == 0)
                {
                    sbStr.Append("&zyzlqkyw=").Append("2");
                }
                else
                {
                    int i = 0;
                    foreach (DataRow dr in zhuyuanDT.Rows)
                    {
                        if (!string.IsNullOrEmpty(dr["InHospitalDate"].ToString()))
                        {
                            i++;

                            //入院日期
                            sbStr.Append("&zRyjcrq=").Append(dr["InHospitalDate"].ToString() != "" ? Convert.ToDateTime(dr["InHospitalDate"]).ToString("yyyy-MM-dd") : "");

                            //出院日期
                            sbStr.Append("&zCyccrq=").Append(dr["OutHospitalDate"].ToString() != "" ? Convert.ToDateTime(dr["OutHospitalDate"]).ToString("yyyy-MM-dd") : "");

                            //原因
                            sbStr.Append("&zYuanyin=").Append(HtmlHelper.GetUrlEncodeVal(dr["Reason"].ToString()));

                            //机构
                            sbStr.Append("&zYljgmc=").Append(HtmlHelper.GetUrlEncodeVal(dr["HospitalName"].ToString()));

                            //编号
                            sbStr.Append("&zBingah=").Append(HtmlHelper.GetUrlEncodeVal(dr["IllcaseNum"].ToString()));

                            //疑似：区分住院史和家庭病床史
                            sbStr.Append("&zType=").Append("1");
                        }
                    }
                    if (i == 0)
                    {
                        sbStr.Append("&zyzlqkyw=").Append("2");
                    }
                    else
                    {
                        sbStr.Append("&zyzlqkyw=").Append("1");
                    }
                }

                #endregion

                #region ARCHIVE_FAMILYBEDHISTORY

                DataTable familyDT = ds.Tables["ARCHIVE_FAMILYBEDHISTORY"];
                if (familyDT == null || familyDT.Rows.Count == 0)
                {
                    sbStr.Append("&jzbcsyw=").Append("2");
                }
                else
                {
                    int i = 0;
                    foreach (DataRow familyDR in familyDT.Rows)
                    {
                        if (!string.IsNullOrEmpty(familyDR["InHospitalDate"].ToString()))
                        {
                            i++;

                            //建床日期
                            sbStr.Append("&zRyjcrq=").Append(familyDR["InHospitalDate"].ToString() != "" ? Convert.ToDateTime(familyDR["InHospitalDate"]).ToString("yyyy-MM-dd") : "");

                            //撤床日期
                            sbStr.Append("&zCyccrq=").Append(familyDR["OutHospitalDate"].ToString() != "" ? Convert.ToDateTime(familyDR["OutHospitalDate"]).ToString("yyyy-MM-dd") : "");

                            //原因
                            sbStr.Append("&zYuanyin=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["Reasons"].ToString()));

                            //医疗机构
                            sbStr.Append("&zYljgmc=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["HospitalName"].ToString()));

                            //病案号
                            sbStr.Append("&zBingah=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["IllcaseNums"].ToString()));

                            //疑似：区分住院史和家庭病床史
                            sbStr.Append("&zType=").Append("2");
                        }
                    }
                    if (i == 0)
                    {
                        sbStr.Append("&jzbcsyw=").Append("2");
                    }
                    else
                    {
                        sbStr.Append("&jzbcsyw=").Append("1");
                    }
                }

                #endregion

                #region ARCHIVE_MEDICATION

                DataTable yongYaoDT = ds.Tables["ARCHIVE_MEDICATION"];
                if (yongYaoDT == null || yongYaoDT.Rows.Count == 0)
                {
                    sbStr.Append("&zyyyqkyw=").Append("2");
                }
                else
                {
                    int i = 0;
                    foreach (DataRow yaoDR in yongYaoDT.Rows)
                    {
                        if (!string.IsNullOrEmpty(yaoDR["MedicinalName"].ToString()))
                        {
                            i++;

                            //药物名称
                            sbStr.Append("&yYwmc=").Append(HtmlHelper.GetUrlEncodeVal(yaoDR["MedicinalName"].ToString()));

                            //用法
                            sbStr.Append("&yYongfa=").Append(HtmlHelper.GetUrlEncodeVal(yaoDR["UseAge"].ToString()));

                            //用量
                            strtmp = yaoDR["UseNum"].ToString();
                            sbStr.Append("&yYongl=").Append(string.IsNullOrEmpty(strtmp) ? "" : HtmlHelper.GetUrlEncodeVal(strtmp.Replace(",", "，")));

                            //用药时间
                            sbStr.Append("&yYysj=").Append(HtmlHelper.GetUrlEncodeVal(yaoDR["StartTime"].ToString()));

                            //服药依从性
                            sbStr.Append("&yFyycx=").Append(GetWebYongYaoQingKuang(yaoDR["PillDependence"].ToString()));
                        }
                    }
                    if (i == 0)
                    {
                        sbStr.Append("&zyyyqkyw=").Append("2");
                    }
                    else
                    {
                        sbStr.Append("&zyyyqkyw=").Append("1");
                    }
                }

                // 签字板
                sbStr.Append("&tjzyyyqkysqm=");

                #endregion

                #region ARCHIVE_INOCULATIONHISTORY

                DataTable yiCongDT = ds.Tables["ARCHIVE_INOCULATIONHISTORY"];
                if (yiCongDT == null || yiCongDT.Rows.Count == 0)
                {
                    sbStr.Append("&fmyghyfyw=").Append("2");
                }
                else
                {
                    int i = 0;
                    foreach (DataRow dr in yiCongDT.Rows)
                    {
                        if (!string.IsNullOrEmpty(dr["PillName"].ToString()))
                        {
                            i++;

                            //名称
                            sbStr.Append("&fJzmc=").Append(HtmlHelper.GetUrlEncodeVal(dr["PillName"].ToString()));

                            //时间
                            sbStr.Append("&fJzrq=").Append(dr["InoculationDate"] == null ? "" : Convert.ToDateTime(dr["InoculationDate"]).ToString("yyyy-MM-dd"));

                            //机构
                            sbStr.Append("&fJzjg=").Append(HtmlHelper.GetUrlEncodeVal(dr["InoculationHistory"].ToString()));
                        }
                    }

                    if (i == 0)
                    {
                        sbStr.Append("&fmyghyfyw=").Append("2");
                    }
                    else
                    {
                        sbStr.Append("&fmyghyfyw=").Append("1");
                    }
                }

                #endregion

                #region ARCHIVE_ASSESSMENTGUIDE

                DataTable pingjiaDT = ds.Tables["ARCHIVE_ASSESSMENTGUIDE"];
                if (pingjiaDT == null || pingjiaDT.Rows.Count == 0)
                {
                    sbStr.Append("&gJkpj=").Append("1");
                }
                else
                {
                    DataRow pingjiaDR = pingjiaDT.Rows[0];

                    if (pingjiaDR["IsNormal"].ToString() == "2")
                    {
                        sbStr.Append("&gJkpj=").Append("2");
                        string yc1 = pingjiaDR["Exception1"].ToString();
                        string yc2 = pingjiaDR["Exception2"].ToString();
                        string yc3 = pingjiaDR["Exception3"].ToString();
                        string yc4 = pingjiaDR["Exception4"].ToString();
                        string strTemp = "";

                        //异常信息超过150个字节，自动换到下行
                        int i = CommonExtensions.Getlenght(yc1);
                        while (i > 150)
                        {
                            if (yc1.Contains(";"))
                            {
                                strTemp = yc1.Substring(yc1.LastIndexOf(';') + 1);
                                yc1 = yc1.Substring(0, yc1.LastIndexOf(';'));
                                yc2 = (strTemp + ";" + yc2).TrimEnd(';');
                                i = CommonExtensions.Getlenght(yc1);
                            }
                            else
                            {
                                break;
                            }
                        }

                        i = CommonExtensions.Getlenght(yc2);
                        while (i > 150)
                        {
                            if (yc2.Contains(";"))
                            {
                                strTemp = yc2.Substring(yc2.LastIndexOf(';') + 1);
                                yc2 = yc2.Substring(0, yc2.LastIndexOf(';'));
                                yc3 = (strTemp + ";" + yc3).TrimEnd(';');
                                i = CommonExtensions.Getlenght(yc2);
                            }
                            else
                            {
                                break;
                            }
                        }

                        i = CommonExtensions.Getlenght(yc3);
                        while (i > 150)
                        {
                            if (yc3.Contains(";"))
                            {
                                strTemp = yc3.Substring(yc3.LastIndexOf(';') + 1);
                                yc3 = yc3.Substring(0, yc3.LastIndexOf(';'));
                                yc4 = (strTemp + ";" + yc4).TrimEnd(';');
                                i = CommonExtensions.Getlenght(yc3);
                            }
                            else
                            {
                                break;
                            }
                        }
                        i = CommonExtensions.Getlenght(yc4);
                        if (i > 150)
                        {
                            yc4 = CommonExtensions.cutSubstring(yc4, 150);
                        }

                        sbStr.Append("&gJkpjyc1=").Append(HtmlHelper.GetUrlEncodeVal(yc1));

                        sbStr.Append("&gJkpjyc2=").Append(HtmlHelper.GetUrlEncodeVal(yc2));

                        sbStr.Append("&gJkpjyc3=").Append(HtmlHelper.GetUrlEncodeVal(yc3));

                        sbStr.Append("&gJkpjyc4=").Append(HtmlHelper.GetUrlEncodeVal(yc4));
                    }
                    else
                    {
                        sbStr.Append("&gJkpj=").Append("1");
                    }

                    var zhidao = pingjiaDR["HealthGuide"];

                    int jkzdcount = 0;
                    var nodes = doc.DocumentNode.SelectNodes("//input[@name='gJkzd']");
                    if (nodes != null)
                    {
                        jkzdcount = nodes.Count;
                    }
                    foreach (var s in zhidao.ToString().Split(','))
                    {
                        if (jkzdcount == 3)
                        {
                            if (s == "4")
                            {
                                continue;
                            }
                        }
                        sbStr.Append("&gJkzd=").Append(GetHealthGuideCodeForWeb(s));
                    }
                    var temStr = pingjiaDR["DangerControl"];
                    foreach (var t in temStr.ToString().Split(','))
                    {
                        sbStr.Append("&gWxyskz=").Append(GetDangerControlCodeByPadCode(t));
                    }

                    //减体重
                    string arm = !string.IsNullOrEmpty(pingjiaDR["Arm"].ToString()) ? pingjiaDR["Arm"].ToString().ToLower().Replace("kg", "") : "";
                    sbStr.Append("&gWxystz=").Append(HtmlHelper.GetUrlEncodeVal(arm));

                    //疫苗
                    sbStr.Append("&gWsysym=").Append(HtmlHelper.GetUrlEncodeVal(pingjiaDR["VaccineAdvice"].ToString()));

                    //其他
                    sbStr.Append("&gWxysqt=").Append(HtmlHelper.GetUrlEncodeVal(pingjiaDR["Other"].ToString()));

                    //威海地区有减腹围选项
                    if (baseUrl.Contains("sdcsm_new"))
                    {
                        //减腹围
                        sbStr.Append("&gWxysjfw=").Append(HtmlHelper.GetUrlEncodeVal(pingjiaDR["WaistlineArm"].ToString()));
                    }
                }

                // 健康评价签字板
                sbStr.Append("&tjjkpjysqm=");

                // 健康指导签字板
                sbStr.Append("&tjjkzdysqm=");

                #endregion

                #region 手动签字

                // 签字维护
                DataTable dtSign = cDao.GetTjSignData();

                if (dtSign != null && dtSign.Rows.Count > 0)
                {
                    DataRow drSign = dtSign.Rows[0];

                    // 症状
                    sbStr.Append("&sdtjzkysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["SymptomSn"].ToString()));

                    // 一般情况
                    sbStr.Append("&sdtjybqkysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["GeneralConditionSn"].ToString()));

                    // 生活方式
                    sbStr.Append("&sdtjshfsysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["LifeStyleSn"].ToString()));

                    // 脏器功能
                    sbStr.Append("&sdtjzqgnysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["OrgansFunctionSn"].ToString()));

                    // 查体
                    sbStr.Append("&sdtjchatiysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["PSkinSn"].ToString()));

                    // 辅助检查 尿微量白蛋白
                    if (!string.IsNullOrEmpty(nwlbdb))
                    {
                        sbStr.Append("&sdtjnwlbdbysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AssistQtSn"].ToString()));
                    }
                    else
                    {
                        sbStr.Append("&sdtjnwlbdbysqm=");
                    }

                    // 血型
                    if (!string.IsNullOrEmpty(xx) || !string.IsNullOrEmpty(rh) || !string.IsNullOrEmpty(xhdb) || !string.IsNullOrEmpty(bxb) || !string.IsNullOrEmpty(xxb) || !string.IsNullOrEmpty(xcgqt) ||
                        !string.IsNullOrEmpty(ndb) || !string.IsNullOrEmpty(nt) || !string.IsNullOrEmpty(ntt) || !string.IsNullOrEmpty(nqx) || !string.IsNullOrEmpty(ncgqt) || !string.IsNullOrEmpty(kfxt) ||
                        !string.IsNullOrEmpty(chxt) || !string.IsNullOrEmpty(txbg))
                    {
                        sbStr.Append("&sdtjfzjcxxysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AssistQtSn"].ToString()));
                    }
                    else
                    {
                        sbStr.Append("&sdtjfzjcxxysqm=");
                    }

                    // 大便潜血
                    if (!string.IsNullOrEmpty(dbqx) || !string.IsNullOrEmpty(thxhdb) || !string.IsNullOrEmpty(yxgy) || !string.IsNullOrEmpty(xqgb) || !string.IsNullOrEmpty(xqgc) || !string.IsNullOrEmpty(bdb) ||
                        !string.IsNullOrEmpty(zdhs) || !string.IsNullOrEmpty(jhdhs) || !string.IsNullOrEmpty(xqjg) || !string.IsNullOrEmpty(xnsd) || !string.IsNullOrEmpty(xjnd) || !string.IsNullOrEmpty(xnnd) ||
                        !string.IsNullOrEmpty(zdgc) || !string.IsNullOrEmpty(gysz) || !string.IsNullOrEmpty(xqdmd) || !string.IsNullOrEmpty(xqgmd))
                    {
                        sbStr.Append("&sdtjfzjcdbqxysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AssistQtSn"].ToString()));
                    }
                    else
                    {
                        sbStr.Append("&sdtjfzjcdbqxysqm=");
                    }

                    // 心电图
                    if (!string.IsNullOrEmpty(xdt))
                    {
                        sbStr.Append("&sdtjxdtysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AECGSn"].ToString()));
                    }
                    else
                    {
                        sbStr.Append("&sdtjxdtysqm=");
                    }

                    // 腹部B超
                    if (!string.IsNullOrEmpty(fbBc))
                    {
                        sbStr.Append("&sdtjfbbcysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["ABtypeUltrasonicSn"].ToString()));
                    }
                    else
                    {
                        sbStr.Append("&sdtjfbbcysqm=");
                    }

                    // 其他B超
                    if (!string.IsNullOrEmpty(qtBc))
                    {
                        sbStr.Append("&sdtjfbbcqtysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["ABtypeQtSn"].ToString()));
                    }
                    else
                    {
                        sbStr.Append("&sdtjfbbcqtysqm=");
                    }

                    // 现存主要健康问题
                    sbStr.Append("&sdtjxczyjkwtysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["PhysicalQtSn"].ToString()));

                    // 主要用药情况
                    sbStr.Append("&sdtjzyyyqkysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["AMAUSn"].ToString()));

                    // 健康评价
                    sbStr.Append("&sdtjjkpjysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["HealthAssessmentSn"].ToString()));

                    // 健康指导
                    sbStr.Append("&sdtjjkzdysqm=").Append(HtmlHelper.GetUrlEncodeVal(drSign["HealthGuidanceSn"].ToString()));

                    // 反馈人
                    sbStr.Append("&sdfkrqz=").Append(HtmlHelper.GetUrlEncodeVal(drSign["PersonalFb"].ToString()));
                }
                else
                {
                    sbStr.Append("&sdtjzkysqm=");
                    sbStr.Append("&sdtjybqkysqm=");
                    sbStr.Append("&sdtjshfsysqm=");
                    sbStr.Append("&sdtjzqgnysqm=");
                    sbStr.Append("&sdtjchatiysqm=");
                    sbStr.Append("&sdtjnwlbdbysqm=");
                    sbStr.Append("&sdtjfzjcxxysqm=");
                    sbStr.Append("&sdtjfzjcdbqxysqm=");
                    sbStr.Append("&sdtjxdtysqm=");
                    sbStr.Append("&sdtjfbbcysqm=");
                    sbStr.Append("&sdtjfbbcqtysqm=");
                    sbStr.Append("&sdtjxczyjkwtysqm=");
                    sbStr.Append("&sdtjzyyyqkysqm=");
                    sbStr.Append("&sdtjjkpjysqm=");
                    sbStr.Append("&sdtjjkzdysqm=");
                    sbStr.Append("&sdfkrqz=");
                }

                #endregion

                // 本人 手动签字
                sbStr.Append("&sdfkqzbr=").Append(person.memberName);

                // 签字板
                sbStr.Append("&fkqzbr=");

                // 家属 手动签字
                sbStr.Append("&sdfkqzjs=");

                // 签字板
                sbStr.Append("&fkqzjs=");

                // 反馈人 签字板
                sbStr.Append("&fkrqz=");

                // 反馈时间
                sbStr.Append("&fktime=").Append(CommonExtensions.GetConvertDate(fkdate, "1"));

                node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
                string createDate = node == null || !node.Attributes.Contains("value") ? DateTime.Now.ToString() : node.Attributes["value"].Value;
                //if (!string.IsNullOrEmpty(CreateTimeSameTj) && CreateTimeSameTj == "1")
                //{
                //    if (!string.IsNullOrEmpty(baseinfoRow["CheckDate"].ToString()))
                //    {
                //        createDate = Convert.ToDateTime(Convert.ToDateTime(baseinfoRow["CheckDate"].ToString()).ToString("yyyy-MM-dd") + " " + DateTime.Now.ToString("HH:mm:ss")).ToString("yyyy-MM-dd HH:mm:ss");
                //    }
                //}

                sbStr.Append("&createtime=").Append(createDate);
                sbStr.Append("&updatetime=").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                //所属机构
                node = doc.DocumentNode.SelectSingleNode("//input[@name='pRgid']");
                strtmp = node == null || !node.Attributes.Contains("value") ? DateTime.Now.ToString() : node.Attributes["value"].Value;
                sbStr.Append("&pRgid=").Append(strtmp);

                //创建机构
                node = doc.DocumentNode.SelectSingleNode("//input[@name='creatregion']");
                strtmp = node == null || !node.Attributes.Contains("value") ? DateTime.Now.ToString() : node.Attributes["value"].Value;
                sbStr.Append("&creatregion=").Append(strtmp);

                //创建人
                node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
                strtmp = node == null || !node.Attributes.Contains("value") ? DateTime.Now.ToString() : node.Attributes["value"].Value;
                sbStr.Append("&createuser=").Append(strtmp);

                //更新人
                sbStr.Append("&updateuser=").Append(loginKey);

                returnString = web.PostHttp(baseUrl + "/health/healthupdatesave.action", sbStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

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
                        CommonExtensions.WriteLog(returnString);
                        return "更新失败！";
                    }
                }
            }
            else
            {
                CommonExtensions.WriteLog(returnString);
            }

            return "";
        }

        /// <summary>
        /// 上传签名
        /// </summary>
        /// <param name="person"></param>
        /// <param name="key"></param>
        /// <param name="checkDate"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private string UploadSign(PersonModel person, string key, string checkDate, string fkdate, Action<string> callback)
        {
            WebHelper web = new WebHelper();

            bool issucc = true;
            string postData = "dGrdabh=" + person.pid + "&id=" + key + "&tz=2";
            string returnString1 = web.PostHttp(baseUrl + "/health/healthToUpdate.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString1);
            if (doc != null)
            {
                var node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
                string createDate = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGrdabh']");
                string dah = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                if (!string.IsNullOrEmpty(createDate))
                {
                    string configfilepath = Config.GetValue("SignPath");
                    string filepath = string.IsNullOrEmpty(configfilepath) ? "D:\\QCSoft\\QCSoftPlatformV2.0\\Sign" : configfilepath;

                    string prgid = "";
                    if (loginKey.Length == 16)
                    {
                        prgid = loginKey.Substring(0, 12);
                    }
                    else
                    {
                        prgid = loginKey.Substring(0, 15);
                    }

                    #region 体检项目签名
                    //症状签名
                    string signfile = filepath + "\\Year\\_Doctor.png";
                    string picdata = "";
                    string xmldata = "";
                    string returnString = "";
                    if (File.Exists(signfile))
                    {
                        var nodes = doc.DocumentNode.SelectNodes("//input[@name='gZhzh'][@checked]");
                        if (nodes != null && nodes.Count > 0)
                        {
                            //禹城走接口上传签字
                            if (!string.IsNullOrEmpty(qzUrl))
                            {
                                picdata = CommonExtensions.ImageToBase64(signfile);
                                xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_zz", createDate, picdata);
                                returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }
                            else
                            {
                                postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_zz&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                                returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                            }
                        }
                    }

                    //一般情况签名
                    signfile = filepath + "\\Year\\_Doctor1.png";
                    if (File.Exists(signfile))
                    {
                        //禹城走接口上传签字
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_ybqk", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_ybqk&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //生活方式签名
                    signfile = filepath + "\\Year\\_Doctor3.png";
                    if (File.Exists(signfile))
                    {

                        //禹城走接口上传签字
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_shfs", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_shfs&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }

                    }

                    //脏器功能签名
                    signfile = filepath + "\\Year\\_Doctor4.png";
                    if (File.Exists(signfile))
                    {
                        //禹城走接口上传签字
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_zqgn", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_zqgn&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //查体签名
                    signfile = filepath + "\\Year\\_Doctor5.png";
                    if (File.Exists(signfile))
                    {
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_ct_cg", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_ct_cg&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //辅助检查签名
                    signfile = filepath + "\\Year\\_Doctor6.png";
                    if (File.Exists(signfile))
                    {
                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gAbo']");
                        string gAbo = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gRh']");
                        string gRh = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='hb']");
                        string hb = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='wbc']");
                        string wbc = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='plt']");
                        string plt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gXcgqt']");
                        string gXcgqt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gNdb']");
                        string gNdb = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gNt']");
                        string gNt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gNtt']");
                        string gNtt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gNqx']");
                        string gNqx = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gNcgqt']");
                        string gNcgqt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gKfxt']");
                        string gKfxt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gChxt']");
                        string gChxt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        //6 to 12
                        node = doc.DocumentNode.SelectSingleNode("//input[@id='nwlbdb']");
                        string nwlbdb = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@name='gDbqx'][@chekced]");
                        string gDbqx = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gThxhdb']");
                        string gThxhdb = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@name='hbsag'][@checked]");
                        string hbsag = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='alt']");
                        string alt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='ast']");
                        string ast = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='alb']");
                        string alb = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='tbil']");
                        string tbil = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='dbil']");
                        string dbil = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='scr']");
                        string scr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='bun']");
                        string bun = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gSgnxjnd']");
                        string gSgnxjnd = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gSgnxnnd']");
                        string gSgnxnnd = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='cho']");
                        string cho = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='tg']");
                        string tg = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='ldlc']");
                        string ldlc = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='hdlc']");
                        string hdlc = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            if (!string.IsNullOrEmpty(gAbo) || !string.IsNullOrEmpty(gRh) || !string.IsNullOrEmpty(hb) || !string.IsNullOrEmpty(wbc)
                                || !string.IsNullOrEmpty(plt) || !string.IsNullOrEmpty(gXcgqt) || !string.IsNullOrEmpty(gNdb) || !string.IsNullOrEmpty(gNt)
                                || !string.IsNullOrEmpty(gNtt) || !string.IsNullOrEmpty(gNqx) || !string.IsNullOrEmpty(gNcgqt) || !string.IsNullOrEmpty(gKfxt) || !string.IsNullOrEmpty(gChxt))
                            {
                                xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_fzjc_1to5", createDate, picdata);
                                returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }

                            if (!string.IsNullOrEmpty(nwlbdb) || !string.IsNullOrEmpty(gDbqx) || !string.IsNullOrEmpty(gThxhdb) || !string.IsNullOrEmpty(hbsag)
                                || !string.IsNullOrEmpty(alt) || !string.IsNullOrEmpty(ast) || !string.IsNullOrEmpty(alb) || !string.IsNullOrEmpty(tbil)
                                || !string.IsNullOrEmpty(dbil) || !string.IsNullOrEmpty(scr) || !string.IsNullOrEmpty(bun) || !string.IsNullOrEmpty(gSgnxjnd) || !string.IsNullOrEmpty(gSgnxnnd)
                                || !string.IsNullOrEmpty(cho) || !string.IsNullOrEmpty(tg) || !string.IsNullOrEmpty(ldlc) || !string.IsNullOrEmpty(hdlc))
                            {
                                xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_fzjc_6to12", createDate, picdata);
                                returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(gAbo) || !string.IsNullOrEmpty(gRh) || !string.IsNullOrEmpty(hb) || !string.IsNullOrEmpty(wbc)
                                || !string.IsNullOrEmpty(plt) || !string.IsNullOrEmpty(gXcgqt) || !string.IsNullOrEmpty(gNdb) || !string.IsNullOrEmpty(gNt)
                                || !string.IsNullOrEmpty(gNtt) || !string.IsNullOrEmpty(gNqx) || !string.IsNullOrEmpty(gNcgqt) || !string.IsNullOrEmpty(gKfxt) || !string.IsNullOrEmpty(gChxt))
                            {
                                postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_fzjc_1to5&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                                returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }

                            if (!string.IsNullOrEmpty(nwlbdb) || !string.IsNullOrEmpty(gDbqx) || !string.IsNullOrEmpty(gThxhdb) || !string.IsNullOrEmpty(hbsag)
                                || !string.IsNullOrEmpty(alt) || !string.IsNullOrEmpty(ast) || !string.IsNullOrEmpty(alb) || !string.IsNullOrEmpty(tbil)
                                || !string.IsNullOrEmpty(dbil) || !string.IsNullOrEmpty(scr) || !string.IsNullOrEmpty(bun) || !string.IsNullOrEmpty(gSgnxjnd) || !string.IsNullOrEmpty(gSgnxnnd)
                                || !string.IsNullOrEmpty(cho) || !string.IsNullOrEmpty(tg) || !string.IsNullOrEmpty(ldlc) || !string.IsNullOrEmpty(hdlc))
                            {
                                postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_fzjc_6to12&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                                returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                            }
                        }
                    }

                    //心电图签名
                    signfile = filepath + "\\Year\\_Doctor16.png";
                    if (File.Exists(signfile))
                    {
                        var nodes = doc.DocumentNode.SelectNodes("//input[@id='gXindt'][@checked]");

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gXindtyi']");
                        string gXindtyi = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        if ((nodes != null && nodes.Count > 0) || !string.IsNullOrEmpty(gXindtyi))
                        {
                            if (!string.IsNullOrEmpty(qzUrl))
                            {
                                picdata = CommonExtensions.ImageToBase64(signfile);

                                xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_fzjc_xdt", createDate, picdata);
                                returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }
                            else
                            {
                                postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_fzjc_xdt&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                                returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                            }
                        }
                    }

                    //B超签名
                    signfile = filepath + "\\Year\\_Doctor17.png";
                    if (File.Exists(signfile))
                    {
                        node = doc.DocumentNode.SelectSingleNode("//input[@name='gBchao'][@checked]");
                        string gFbbc = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gBchaoyi']");
                        string gFbbcyc = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        if (!string.IsNullOrEmpty(gFbbc) || !string.IsNullOrEmpty(gFbbcyc))
                        {
                            if (!string.IsNullOrEmpty(qzUrl))
                            {
                                picdata = CommonExtensions.ImageToBase64(signfile);

                                xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_fzjc_bc_fbbc", createDate, picdata);
                                returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }
                            else
                            {
                                postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_fzjc_bc_fbbc&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                                returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                            }
                        }
                    }

                    //其他B超签名
                    signfile = filepath + "\\Year\\_Doctor19.png";
                    if (File.Exists(signfile))
                    {
                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gQt'][@checked]");
                        string gQt = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        node = doc.DocumentNode.SelectSingleNode("//input[@id='gQtyc']");
                        string gQtyc = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                        if (!string.IsNullOrEmpty(gQt) || !string.IsNullOrEmpty(gQtyc))
                        {
                            if (!string.IsNullOrEmpty(qzUrl))
                            {
                                picdata = CommonExtensions.ImageToBase64(signfile);

                                xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_fzjc_bc_qt", createDate, picdata);
                                returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }
                            else
                            {
                                postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_fzjc_bc_qt&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                                returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                            }
                        }
                    }

                    //现存主要健康问题签名
                    signfile = filepath + "\\Year\\_Doctor7.png";
                    if (File.Exists(signfile))
                    {
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_jkwtzyzl", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_jkwtzyzl&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //用药签名
                    signfile = filepath + "\\Year\\_Doctor9.png";
                    if (File.Exists(signfile))
                    {
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_zyyyqk", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_zyyyqk&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //健康评价签名
                    signfile = filepath + "\\Year\\_Doctor10.png";
                    if (File.Exists(signfile))
                    {
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_jkpj", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_jkpj&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //健康指导签名
                    signfile = filepath + "\\Year\\_Doctor11.png";
                    if (File.Exists(signfile))
                    {
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "ysqm_jkzd", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=ysqm_jkzd&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //本人签名
                    signfile = filepath + "\\" + person.idNumber + "_" + checkDate.Replace("-", "") + "_B.png";
                    if (!string.IsNullOrEmpty(fkdate))
                    {
                        fkdate = DateTime.Parse(fkdate).ToString("yyyy-MM-dd");
                        signfile = filepath + "\\" + person.idNumber + "_" + fkdate.Replace("-", "") + "_B.png";
                    }
                    if (File.Exists(signfile))
                    {
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "jgfk_brqz", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=jgfk_brqz&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //家属签名
                    signfile = filepath + "\\" + person.idNumber + "_" + checkDate.Replace("-", "") + "_J.png";
                    if (!string.IsNullOrEmpty(fkdate))
                    {
                        fkdate = DateTime.Parse(fkdate).ToString("yyyy-MM-dd");
                        signfile = filepath + "\\" + person.idNumber + "_" + fkdate.Replace("-", "") + "_J.png";
                    }
                    if (File.Exists(signfile))
                    {
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "jgfk_jsqz", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=jgfk_jsqz&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }

                    //反馈人签名
                    signfile = filepath + "\\" + person.idNumber + "_" + checkDate.Replace("-", "") + "_F.png";
                    if (!string.IsNullOrEmpty(fkdate))
                    {
                        fkdate = DateTime.Parse(fkdate).ToString("yyyy-MM-dd");
                        signfile = filepath + "\\" + person.idNumber + "_" + fkdate.Replace("-", "") + "_F.png";
                    }
                    if (!File.Exists(signfile))
                    {
                        signfile = filepath + "\\Year\\_Doctor13.png";
                    }
                    if (File.Exists(signfile))
                    {
                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            picdata = CommonExtensions.ImageToBase64(signfile);

                            xmldata = GetXML(prgid, person.idNumber, "2", "jgfk_fkrqz", createDate, picdata);
                            returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                            if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                            {
                                issucc = false;
                            }
                        }
                        else
                        {
                            postData = "dGrdabh=" + dah + "&tablename=T_JK_JKTJ&signcolumn=jgfk_fkrqz&tablecreatetime=" + CommonExtensions.GetUrlEncodeVal(createDate) + "&pRgid=&loadurl=null&pic=" + CommonExtensions.GetUrlEncodeVal(CommonExtensions.ImageToBase64(signfile));
                            returnString = web.PostHttp(baseUrl + "qianzi/savesign.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                        }
                    }
                    #endregion
                }
            }
            if (!issucc)
            {
                return "签名上传失败，请重新上传";
            }
            return "";
        }

        private string UploadPic(PersonModel pm, string key, string checkDate, Action<string> callback)
        {
            bool issucc = true;
            WebHelper web = new WebHelper();
            string postData = "dGrdabh=" + pm.pid + "&id=" + key + "&tz=2";
            string returnString1 = web.PostHttp(baseUrl + "/health/healthToUpdate.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString1);
            if (doc != null)
            {
                var node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
                string createDate = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGrdabh']");
                string dah = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                if (!string.IsNullOrEmpty(createDate))
                {
                    //b超、心电路径
                    string Bchao = Config.GetValue("Bchao");
                    string Xindian = Config.GetValue("Xindian");

                    Bchao = string.IsNullOrEmpty(Bchao) ? "D:\\QCSoft\\TypeB" : Bchao;
                    Xindian = string.IsNullOrEmpty(Xindian) ? "D:\\QCSoft\\ECGPDF\\outFile" : Xindian;

                    string bcfilename = checkDate + "_" + pm.idNumber;//B超文件名
                    string xdfilename = "";//心电文件名

                    string bchaopath = Bchao + "\\" + bcfilename + ".jpg";
                    string xdpath = "";

                    //查询数据库中保存的心电文件名
                    DataTable dt = new CommonBusiness.CommonDAOBusiness().GetEcgFile(pm.idNumber, checkDate);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        xdfilename = dt.Rows[0]["MID"].ToString();
                    }
                    xdpath = string.IsNullOrEmpty(xdfilename) ? "" : Xindian + "\\" + xdfilename + ".png";

                    string bcbase64 = "";
                    string xdbase64 = "";
                    //存在b超和心电图片
                    if (File.Exists(bchaopath))
                    {
                        bcbase64 = CommonExtensions.ImageToBase64(bchaopath, "jpg");
                    }
                    if (!string.IsNullOrEmpty(xdfilename) && File.Exists(xdpath))
                    {
                        xdbase64 = CommonExtensions.ImageToBase64(xdpath, "jpg");
                    }

                    //存在图片时再上传
                    if (!string.IsNullOrEmpty(bcbase64) || !string.IsNullOrEmpty(xdbase64))
                    {
                        string prgid = "";
                        if (loginKey.Length == 16)
                        {
                            prgid = loginKey.Substring(0, 12);
                        }
                        else
                        {
                            prgid = loginKey.Substring(0, 15);
                        }

                        if (!string.IsNullOrEmpty(qzUrl))
                        {
                            string xmldata = "";
                            string returnString = "";
                            if (!string.IsNullOrEmpty(bcbase64))
                            {
                                //B超图片上传   
                                xmldata = GetXML(prgid, pm.idNumber, "2", "jktj_bc", createDate, bcbase64);
                                returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }

                            if (!string.IsNullOrEmpty(xdbase64))
                            {
                                Thread.Sleep(3000);
                                //心电图片上传   
                                xmldata = GetXML(prgid, pm.idNumber, "2", "jktj_xdt", createDate, xdbase64);
                                returnString = web.PostHttpSOAP(qzUrl, xmldata, "text/xml; charset=utf-8", SysCookieContainer);
                                if (string.IsNullOrEmpty(returnString) || !returnString.Contains("保存成功"))
                                {
                                    issucc = false;
                                }
                            }

                        }
                    }
                }
            }
            else
            {
                issucc = false;
            }

            if (issucc == false)
            {
                return "B超或心电报告上传失败，请重新上传。";
            }
            return "";
        }
        /// <summary>
        /// 肛门指针code,根据padCode
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebGangMenBYPadCode(string padCode)
        {
            string tem = "";
            switch (padCode)
            {
                case "2":
                    tem = "1";
                    break;
                case "3":
                    tem = "2";
                    break;
                case "4":
                    tem = "3";
                    break;
                case "5":
                    tem = "4";
                    break;

                case "6":
                case "7":
                    tem = "99";
                    break;
            }
            return tem;
        }
        /// <summary>
        /// 根据pad主要用药“服药依从性”，获取web String
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetWebYongYaoQingKuang(string code)
        {
            string tem = "规律";
            switch (code)
            {
                case "1":
                    tem = "规律";
                    break;
                case "2":
                    tem = "间断";
                    break;
                case "3":
                    tem = "不服药";
                    break;
            }
            return CommonExtensions.GetUrlEncodeVal(tem);
        }
        /// <summary>
        /// 根据pad 危险因素code 获取Web Code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetDangerControlCodeByPadCode(string code)
        {
            var tem = "99";
            switch (code)
            {
                case "1":
                    tem = "1";
                    break;
                case "2":
                    tem = "2";
                    break;
                case "3":
                    tem = "3";
                    break;
                case "4":
                    tem = "4";
                    break;
                case "8":
                    tem = "96";
                    break;
                case "5":
                    tem = "97";
                    break;
                case "6":
                    tem = "98";
                    break;
                case "7":
                    tem = "99";
                    break;
            }
            return tem;
        }
        /// <summary>
        /// 根据pad 下肢水肿code ,获取web Code
        ///  
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebEdemaCodeByPadCode(object padCode)
        {
            string tem = "1";
            switch (padCode + "")
            {
                case "1":
                    tem = "1";
                    break;
                case "2":
                    tem = "2";
                    break;
                case "3":
                    tem = "4";
                    break;
                case "4":
                    tem = "5";
                    break;
            }
            return tem;
        }

        /// <summary>
        /// 根据pad 血型code ,获取web Code
        ///  
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebBloodTypeByPadCode(object padCode)
        {
            string tem = "";
            switch (padCode + "")
            {
                case "1":
                    tem = "A";
                    break;
                case "2":
                    tem = "B";
                    break;
                case "3":
                    tem = "O";
                    break;
                case "4":
                    tem = "AB";
                    break;
            }
            return tem;
        }

        /// <summary>
        /// 根据web 血型code ,获取pad Code
        ///  
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebBloodTypeByWebCode(object padCode)
        {
            string tem = "";
            if (padCode.ToString().ToUpper().Contains("A"))
            {
                tem = "1";
            }
            if (padCode.ToString().ToUpper().Contains("B"))
            {
                tem = "2";
            }
            if (padCode.ToString().ToUpper().Contains("O"))
            {
                tem = "3";
            }
            if (padCode.ToString().ToUpper().Contains("AB"))
            {
                tem = "4";
            }
            return tem;
        }

        /// <summary>
        /// 根据pad 血型code ,获取web Code
        ///  
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebRHByPadCode(object padCode)
        {
            string tem = "";
            switch (padCode + "")
            {
                case "1":
                    tem = "阳性";
                    break;
                case "2":
                    tem = "阴性";
                    break;
                case "3":
                    tem = "不详";
                    break;
            }
            return tem;
        }

        /// <summary>
        /// 根据web RH code ,获取Pad Code
        ///  
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebRHByWebCode(object padCode)
        {
            string tem = "";
            if (padCode.ToString().ToUpper().Contains("否") || padCode.ToString().ToUpper().Contains("阳") || padCode.ToString().ToUpper().Contains("+"))
            {
                tem = "1";
            }
            else if (padCode.ToString().ToUpper().Contains("是") || padCode.ToString().ToUpper().Contains("阴") || padCode.ToString().ToUpper().Contains("-"))
            {
                tem = "2";
            }
            else
            {
                tem = "3";
            }

            return tem;
        }

        ///<summary>
        /// 根据pad 心电图code ,获取web Code
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebECGByPadCode(object padCode)
        {
            string tem = "";
            switch (padCode + "")
            {
                case "9":
                    tem = "99";
                    break;
                default:
                    tem = padCode.ToString();
                    break;
            }

            return tem;
        }

        ///<summary>
        /// 根据pad 视神经系统code ,获取web Code
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebNerveDisByPadCode(object padCode)
        {
            string tem = "";
            switch (padCode + "")
            {
                case "1":
                case "2":
                case "3":
                    tem = padCode.ToString();
                    break;
                case "4":
                    tem = "99";
                    break;
            }
            return tem;
        }

        ///<summary>
        /// 根据web 视神经系统code ,获取pad Code
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebNerveDisByWebCode(object padCode)
        {
            string tem = "";
            switch (padCode + "")
            {
                case "1":
                case "2":
                case "3":
                    tem = padCode.ToString();
                    break;
                case "99":
                    tem = "4";
                    break;
            }
            return tem;
        }

        ///<summary>
        /// 根据pad 其他系统code ,获取web Code
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebElseDisByPadCode(object padCode)
        {
            string tem = "";
            switch (padCode + "")
            {
                case "7":
                    tem = "99";
                    break;
                default:
                    tem = padCode.ToString();
                    break;
            }
            return tem;
        }

        ///<summary>
        /// 根据web 其他系统code ,获取pad Code
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebElseDisByWebCode(object padCode)
        {
            string tem = "";
            switch (padCode + "")
            {
                case "99":
                    tem = "7";
                    break;
                default:
                    tem = padCode.ToString();
                    break;
            }
            return tem;
        }

        /// <summary>
        /// 检验体检日期信息是否存在
        /// </summary>
        /// <param name="person"></param>
        /// <param name="checkDate"></param>
        /// <param name="tjKey"></param>
        /// <returns></returns>
        private bool GetCheckedDate(PersonModel person, string checkDate, out string tjKey)
        {
            tjKey = "";

            bool bo = false;
            WebHelper web = new WebHelper();
            string postData = "grdabh=" + person.pid;
            string returnString = web.PostHttp(baseUrl + "/health/showTjkjktjSkip.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            List<SFClass> sflist = new List<SFClass>();
            var node = doc.DocumentNode.SelectSingleNode("//div[@id='yearContainer']");
            if (node != null)
            {
                var nodes = node.SelectSingleNode("div").SelectNodes("a");
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (Convert.ToDateTime(checkDate).ToString("yyyy-MM-dd") == Convert.ToDateTime(n.InnerText).ToString("yyyy-MM-dd"))
                        {
                            tjKey = n.Id.Substring(1);
                            bo = true;
                            break;
                        }
                    }
                }

                //node = node.SelectSingleNode("div").SelectNodes("a")[0];
                //string key = node.Id;
                //key = key.Substring(1);

            }

            return bo;
        }

        /// <summary>
        /// 检验体检日期信息是否存在
        /// </summary>
        /// <param name="person"></param>
        /// <param name="tjKey"></param>
        /// <returns></returns>
        private List<SFClass> GetCheckedDate(PersonModel person)
        {
            WebHelper web = new WebHelper();
            string postData = "grdabh=" + person.pid;
            string returnString = web.PostHttp(baseUrl + "/health/showTjkjktjSkip.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            List<SFClass> sflist = new List<SFClass>();
            var node = doc.DocumentNode.SelectSingleNode("//div[@id='yearContainer']");
            if (node != null)
            {
                var nodes = node.SelectNodes("div");
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        var subNodes = n.SelectNodes("a");
                        if (subNodes != null)
                        {
                            foreach (var subNode in subNodes)
                            {
                                SFClass sf = new SFClass();
                                sf.key = subNode.Id.Substring(1);
                                sf.sfDate = !string.IsNullOrEmpty(subNode.InnerText.Trim()) ? DateTime.Parse(subNode.InnerText.Trim()).ToString("yyyy-MM-dd") : "";
                                sflist.Add(sf);
                            }
                        }
                    }
                }
            }

            return sflist;
        }

        /// <summary>
        /// 删除体检
        /// </summary>
        /// <param name="pm"></param>
        /// <param name="tjKey"></param>
        /// <returns></returns>
        private bool DeleteTjxx(PersonModel pm, string tjKey)
        {
            WebHelper web = new WebHelper();
            //http://20.1.1.78:9081/sdcsm/health/deletehealthdel.action?id=1205902&dGrdabh=371426080100002402
            string postData = "id=" + tjKey + "&dGrdabh=" + pm.pid;
            string returnString = web.GetHttp(baseUrl + "/health/deletehealthdel.action?" + postData, "", SysCookieContainer);
            if (!string.IsNullOrEmpty(returnString))
            {
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                if (doc == null || doc.DocumentNode.SelectSingleNode("//body/script[last()]") == null)
                {
                    return false;
                }
                else
                {
                    var returnNode = doc.DocumentNode.SelectSingleNode("//body/script[last()]");

                    if (returnNode.InnerText.IndexOf("操作成功") == -1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region DownLoad
        /// <summary>
        /// 发生异常，重复尝试
        /// </summary>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        //private void TryDownLoadTj(int tryCount, Action<string> callback)
        //{
        //    try
        //    {
        //        GetTjKeyAndInfo(callback);
        //    }
        //    catch (Exception ex)
        //    {
        //        if (ex.Message.IndexOf("登录超时") > -1)
        //        {
        //            callback("EX-“登录超时”、“该用户在别处登录”或者“当前用户信息被上级用户修改”导致用户无法操作,请您重新登录!");

        //            throw;
        //        }

        //        CommonExtensions.WriteLog(ex.Message);
        //        CommonExtensions.WriteLog(ex.StackTrace);

        //        if (tryCount < MaxtryCount)
        //        {
        //            System.Threading.Thread.Sleep(SleepMilliseconds);

        //            tryCount++;
        //            TryDownLoadTj(tryCount, callback);
        //        }
        //        else
        //        {
        //            callback("EX-个人档案:获取体检档案信息失败。请确保网路畅通。");
        //        }
        //    }
        //}

        /// <summary>
        ///  根据患者信息，获取体检信息
        /// </summary>
        /// <param name="callback"></param>
        //private void GetTjKeyAndInfo(Action<string> callback)
        //{
        //    if (lstPerson.Count == 0)
        //    {
        //        GrdaBusiness.GrdaBusiness grda = new GrdaBusiness.GrdaBusiness();
        //        grda.SysCookieContainer = SysCookieContainer;
        //        grda.loginKey = loginKey;
        //        grda.querylist = querylist;
        //        int pageSum = 0;
        //        List<PersonModel> listPerson = grda.GetGrdaFirstKeyAndInfo(callback, out pageSum);
        //        totalRows = grda.totalRows;
        //        GetTjInfoByPersonModels(listPerson, callback);

        //        for (var i = 2; i < pageSum + 1; i++)
        //        {
        //            listPerson = grda.GetPageInfo(i, callback);
        //            GetTjInfoByPersonModels(listPerson, callback);
        //        }
        //    }
        //    else
        //    {
        //        totalRows = lstPerson.Count;
        //        GetTjInfoByPersonModels(lstPerson, callback);
        //    }
        //}

        /// <summary>
        /// 根据信息集合，遍历、获取体检信息
        /// </summary>
        /// <param name="personList"></param>
        /// <param name="callback"></param>
        private void GetTjInfoByPersonModels(List<PersonModel> personList, Action<string> callback)
        {
            foreach (PersonModel pm in personList)
            {
                callback("下载-体检信息档案..." + currentIndex + "/" + totalRows);

                GetTjInfoByPersonModel(pm, 1, callback);

                currentIndex++;
            }
        }

        /// <summary>
        /// 根据信息，获取体检信息Key
        /// </summary>
        /// <param name="person"></param>
        /// <param name="callback"></param>
        private void GetTjInfoByPersonModel(PersonModel person, int tryCount, Action<string> callback)
        {
            string idcard = person.idNumber;
            try
            {
                WebHelper web = new WebHelper();
                string postData = "grdabh=" + person.pid;
                string returnString = web.PostHttp(baseUrl + "/health/showTjkjktjSkip.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
                var node = doc.DocumentNode.SelectSingleNode("//div[@id='yearContainer']");
                if (node != null)
                {
                    node = node.SelectSingleNode("div").SelectNodes("a")[0];
                    string key = node.Id;

                    key = key.Substring(1);
                    GetTjDataInfoByKey(key, person);
                }
            }
            catch (Exception ex)
            {

                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);

                if (tryCount < MaxtryCount)
                {
                    callback("EX-体检档案:身份证[" + idcard + "],姓名[" + person.memberName + "]:下载信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    GetTjInfoByPersonModel(person, tryCount, callback);
                }
                else
                {
                    callback("EX-体检档案:身份证[" + idcard + "],姓名[" + person.memberName + "]:下载信息失败。请确保网路畅通。");
                }
            }
        }
        /// <summary>
        /// 真。 下载
        /// </summary>
        /// <param name="key"></param>
        /// <param name="person"></param>
        private void GetTjDataInfoByKey(string key, PersonModel person)
        {
            string idcard = person.idNumber;

            WebHelper web = new WebHelper();
            string postData = "dGrdabh=" + person.pid + "&id=" + key + "&tz=2";
            string returnString = web.PostHttp(baseUrl + "/health/healthToUpdate.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            if (string.IsNullOrEmpty(returnString))
            {
                return;
            }

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            if (doc != null)
            {
                //解析页面信息
                DataSet dataDS = DataSetTmp.TjDataSet;
                DataSet saveDS = new DataSet();//
                DataTable baseinfoDT = dataDS.Tables["ARCHIVE_CUSTOMERBASEINFO"].Clone();

                #region ARCHIVE_CUSTOMERBASEINFO

                DataRow dr = baseinfoDT.NewRow();
                dr["IDCardNo"] = idcard;

                //体检日期
                var node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                dr["CheckDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@id='field2']");
                string doctor = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //更新责任医生 2017-03-06
                dr["Doctor"] = doctor;
                if (!string.IsNullOrEmpty(doctor))
                {
                    new CommonBusiness.CommonDAOBusiness().UpdateDocter(doctor, idcard);
                }

                // 症状
                var nodes = doc.DocumentNode.SelectNodes("//input[@name='gZhzh'][@checked]");
                string temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + (n.Attributes["value"].Value == "99" ? "25" : n.Attributes["value"].Value);
                        }
                    }
                }
                dr["Symptom"] = temStr.TrimStart(',');
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZzqt']");
                dr["Other"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                dr["PhysicalID"] = key;
                //node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                //dr["CreateBy"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
                dr["CreateDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                string tjrq = Convert.ToDateTime(dr["CheckDate"].ToString()).ToString("yyyy-MM-dd");
                baseinfoDT.Rows.Add(dr);
                int outkey = cDao.SaveMainTable(baseinfoDT, idcard, tjrq);

                #endregion
                // saveDS.Tables.Add(baseinfoDT);

                Decimal dec = 0;

                DataTable genDT = dataDS.Tables["ARCHIVE_GENERALCONDITION"].Clone();

                #region ARCHIVE_GENERALCONDITION  一般情况

                DataRow genDR = genDT.NewRow();

                //左侧血压
                node = doc.DocumentNode.SelectSingleNode("//input[@id='zcyy']");
                genDR["LeftReason"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //右侧血压
                node = doc.DocumentNode.SelectSingleNode("//input[@id='ycyy']");
                genDR["RightReason"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                genDR["PhysicalID"] = key;
                genDR["OutKey"] = outkey.ToString();
                genDR["IDCardNo"] = idcard;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gTw']");
                genDR["AnimalHeat"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gHx']");
                genDR["BreathRate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYw']");
                genDR["Waistline"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSg']");
                genDR["Height"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //老年人认知
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gLnrrz'][@checked]");
                genDR["OldRecognise"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //老年人情感
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gLnrqg'][@checked]");
                genDR["OldEmotion"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gMb']");
                genDR["PulseRate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gTzh']");
                genDR["Weight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gTzhzh']");
                genDR["BMI"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //简易智力状态
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gLnrrzfen']");
                genDR["InterScore"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //老年人抑郁症
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gLnrqgfen']");
                genDR["GloomyScore"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXyzc2']");
                genDR["LeftPre"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXyyc2']");
                genDR["RightPre"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //老年人情感 LeftPre
                //node = doc.DocumentNode.SelectSingleNode("//input[@id='']");
                //genDR["WaistIp"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXyzc1']");
                genDR["LeftHeight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXyyc1']");
                genDR["RightHeight"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //老年人健康状态
                node = doc.DocumentNode.SelectSingleNode("//input[@name='lnrzkpg'][@checked]");
                genDR["OldHealthStaus"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //node = doc.DocumentNode.SelectSingleNode("//input[@id='']");
                //体温
                //genDR["Tem"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //老年人生活自理
                node = doc.DocumentNode.SelectSingleNode("//input[@name='lnrzlpg'][@checked]");
                genDR["OldSelfCareability"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //node = doc.DocumentNode.SelectSingleNode("//select[@name='lNrgfgl']/option[@selected]");
                //genDR["OldMange"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //健康体检_一般状况
                genDR["AnimalHeat"] = Decimal.TryParse(genDR["AnimalHeat"].ToString(), out dec) ? dec.ToString() : "";
                genDR["BreathRate"] = Decimal.TryParse(genDR["BreathRate"].ToString(), out dec) ? dec.ToString() : "";
                genDR["Waistline"] = Decimal.TryParse(genDR["Waistline"].ToString(), out dec) ? dec.ToString() : "";
                genDR["Height"] = Decimal.TryParse(genDR["Height"].ToString(), out dec) ? dec.ToString() : "";
                genDR["PulseRate"] = Decimal.TryParse(genDR["PulseRate"].ToString(), out dec) ? dec.ToString() : "";
                genDR["Weight"] = Decimal.TryParse(genDR["Weight"].ToString(), out dec) ? dec.ToString() : "";
                genDR["BMI"] = Decimal.TryParse(genDR["BMI"].ToString(), out dec) ? dec.ToString() : "";
                genDR["InterScore"] = Decimal.TryParse(genDR["InterScore"].ToString(), out dec) ? dec.ToString() : "";
                genDR["GloomyScore"] = Decimal.TryParse(genDR["GloomyScore"].ToString(), out dec) ? dec.ToString() : "";
                genDR["LeftPre"] = Decimal.TryParse(genDR["LeftPre"].ToString(), out dec) ? dec.ToString() : "";
                genDR["RightPre"] = Decimal.TryParse(genDR["RightPre"].ToString(), out dec) ? dec.ToString() : "";
                genDR["WaistIp"] = Decimal.TryParse(genDR["WaistIp"].ToString(), out dec) ? dec.ToString() : "";
                genDR["LeftHeight"] = Decimal.TryParse(genDR["LeftHeight"].ToString(), out dec) ? dec.ToString() : "";
                genDR["RightHeight"] = Decimal.TryParse(genDR["RightHeight"].ToString(), out dec) ? dec.ToString() : "";

                genDT.Rows.Add(genDR);
                #endregion

                saveDS.Tables.Add(genDT);

                DataTable lifeDT = dataDS.Tables["ARCHIVE_LIFESTYLE"].Clone();
                #region ARCHIVE_LIFESTYLE  生活方式
                DataRow lifeDR = lifeDT.NewRow();
                lifeDR["PhysicalID"] = key;
                lifeDR["OutKey"] = outkey.ToString();
                lifeDR["IDCardNo"] = idcard;
                //日吸烟数量
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gRxyl']");
                lifeDR["SmokeDayNum"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //开始吸烟年龄
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gKsxynl']");
                lifeDR["SmokeAgeStart"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //戒烟年龄
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJynl']");
                lifeDR["SmokeAgeForbiddon"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //锻炼频率
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gDlpl'][@checked]");
                lifeDR["ExerciseRate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //每次锻炼时间
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gMcdlsj']");
                lifeDR["ExerciseTimes"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gYsxg'][@checked]");

                //饮食习惯
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + n.Attributes["value"].Value;
                        }
                    }
                }
                lifeDR["DietaryHabit"] = temStr.TrimStart(',');
                //锻炼方式
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gDlfs']");

                string strTmpE = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //lifeDR["ExerciseExistense"] = strTmpE.Replace("散步", "1").Replace("跑步", "2").Replace("广场舞", "3");

                lifeDR["ExerciseExistense"] = string.IsNullOrEmpty(strTmpE) ? "" : "4";
                lifeDR["ExerciseExistenseOther"] = strTmpE;

                //坚持锻炼时间
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJcdlsj']");
                lifeDR["ExcisepersistTime"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //吸烟状况
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXyzk'][@checked]");
                lifeDR["SmokeCondition"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //饮酒频率
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYjpl'][@checked]");
                lifeDR["DrinkRate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //日饮酒量
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gRyjl']");
                lifeDR["DayDrinkVolume"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //是否已戒酒
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSfjj'][@checked]");
                lifeDR["IsDrinkForbiddon"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //戒酒年龄
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJjnl']");
                lifeDR["ForbiddonAge"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //开始饮酒年龄
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gKsyjnl']");
                lifeDR["DrinkStartAge"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //近一年是否有醉酒
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYnnsfyj'][@checked]");
                lifeDR["DrinkThisYear"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //饮酒类型
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gYjzl'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + (n.Attributes["value"].Value == "99" ? "5" : n.Attributes["value"].Value);
                        }
                    }
                }
                lifeDR["DrinkType"] = temStr.TrimStart(',');
                //饮酒类型：其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYjzlqt']");
                lifeDR["DrinkTypeOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //职业病危害接触式有无
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYwzybl'][@checked]");
                lifeDR["CareerHarmFactorHistory"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value == "1" ? "1" : "2";
                //粉尘
                node = doc.DocumentNode.SelectSingleNode("//input[@id='fenchen']");
                lifeDR["Dust"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //粉尘防护：无有
                node = doc.DocumentNode.SelectSingleNode("//input[@name='fchcs'][@checked]");
                lifeDR["DustProtect"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value == "1" ? "1" : "2";
                //放射
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gShexian']");
                lifeDR["Radiogen"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //放射防护：无有
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSxfhcs'][@checked]");
                lifeDR["RadiogenProtect"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value == "1" ? "1" : "2";
                //物理防护
                node = doc.DocumentNode.SelectSingleNode("//input[@id='wuliyinsu']");
                lifeDR["Physical"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //物理防护：无有
                node = doc.DocumentNode.SelectSingleNode("//input[@name='wlcs'][@checked]");
                lifeDR["PhysicalProtect"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value == "1" ? "1" : "2";
                //化学
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gHxp']");
                lifeDR["Chem"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //化学防护：无有
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gHxpfhcs'][@checked]");
                lifeDR["ChemProtect"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value == "1" ? "1" : "2";
                //其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='blqita']");
                lifeDR["Other"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //其他防护：无有
                node = doc.DocumentNode.SelectSingleNode("//input[@name='blqtcs'][@checked]");
                lifeDR["OtherProtect"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value == "1" ? "1" : "2";
                //工种
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJtzy']");
                lifeDR["WorkType"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //从业时间
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gCysj']");
                lifeDR["WorkTime"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //粉尘防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='fchy']");
                lifeDR["DustProtectEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //放射防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSxfhcsqt']");
                lifeDR["RadiogenProtectEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //物理防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='wly']");
                lifeDR["PhysicalProtectEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //化学防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gHxpfhcsjt']");
                lifeDR["ChemProtectEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //其他防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='qty']");
                lifeDR["OtherProtectEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //健康体检_生活方式
                lifeDR["SmokeDayNum"] = Decimal.TryParse(lifeDR["SmokeDayNum"].ToString(), out dec) ? dec.ToString() : "";
                lifeDR["SmokeAgeStart"] = Decimal.TryParse(lifeDR["SmokeAgeStart"].ToString(), out dec) ? dec.ToString() : "";
                lifeDR["SmokeAgeForbiddon"] = Decimal.TryParse(lifeDR["SmokeAgeForbiddon"].ToString(), out dec) ? dec.ToString() : "";
                lifeDR["ExerciseTimes"] = Decimal.TryParse(lifeDR["ExerciseTimes"].ToString(), out dec) ? dec.ToString() : "";
                lifeDR["ExcisepersistTime"] = Decimal.TryParse(lifeDR["ExcisepersistTime"].ToString(), out dec) ? dec.ToString() : "";
                lifeDR["DayDrinkVolume"] = Decimal.TryParse(lifeDR["DayDrinkVolume"].ToString(), out dec) ? dec.ToString() : "";
                lifeDR["ForbiddonAge"] = Decimal.TryParse(lifeDR["ForbiddonAge"].ToString(), out dec) ? dec.ToString() : "";
                lifeDR["DrinkStartAge"] = Decimal.TryParse(lifeDR["DrinkStartAge"].ToString(), out dec) ? dec.ToString() : "";
                lifeDR["WorkTime"] = Decimal.TryParse(lifeDR["WorkTime"].ToString(), out dec) ? dec.ToString() : "";


                lifeDT.Rows.Add(lifeDR);
                #endregion
                saveDS.Tables.Add(lifeDT);

                DataTable viseraDT = dataDS.Tables["ARCHIVE_VISCERAFUNCTION"].Clone();
                #region ARCHIVE_VISCERAFUNCTION  脏器功能
                DataRow viseraDR = viseraDT.NewRow();
                viseraDR["PhysicalID"] = key;
                viseraDR["OutKey"] = outkey.ToString();
                viseraDR["IDCardNo"] = idcard;

                //口唇
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gKouchun'][@checked]");
                temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                viseraDR["Lips"] = string.IsNullOrEmpty(temStr) ? "" : temStr.Replace("6", "");

                //齿列
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gChilei'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += n.Attributes["value"].Value + ",";
                        }
                    }
                }
                viseraDR["ToothResides"] = temStr.TrimEnd(',');
                //缺齿
                if (temStr.Contains("2"))
                {
                    string quechi = "";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quechi1']");
                    quechi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quechi2']");
                    quechi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quechi3']");
                    quechi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quechi4']");
                    quechi += node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    viseraDR["HypodontiaEx"] = quechi;
                }
                //龋齿
                if (temStr.Contains("3"))
                {
                    string quchi = "";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quchi1']");
                    quchi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quchi2']");
                    quchi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quchi3']");
                    quchi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quchi4']");
                    quchi += node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    viseraDR["SaprodontiaEx"] = quchi;
                }
                //义齿
                if (temStr.Contains("4"))
                {
                    string yichi = "";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='yichi1']");
                    yichi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='yichi2']");
                    yichi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='yichi3']");
                    yichi += node == null || !node.Attributes.Contains("value") ? "#" : node.Attributes["value"].Value + "#";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='yichi4']");
                    yichi += node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    viseraDR["DentureEx"] = yichi;
                }
                //齿列其他
                node = doc.DocumentNode.SelectSingleNode("//input[@name='chlqt']");
                viseraDR["ToothResidesOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //咽部
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYanbu'][@checked]");
                temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                viseraDR["Pharyngeal"] = string.IsNullOrEmpty(temStr) ? "" : temStr.Replace("4", "");

                //左眼视力
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZysl']");
                viseraDR["LeftView"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //听力
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gTl'][@checked]");
                viseraDR["Listen"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //右眼视力
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYysl']");
                viseraDR["RightView"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //运动功能
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYdgn'][@checked]");
                viseraDR["SportFunction"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //左眼矫正
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZyjz']");
                viseraDR["LeftEyecorrect"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //右眼矫正
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYyjz']");
                viseraDR["RightEyecorrect"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //健康体检_脏器功能
                viseraDR["LeftView"] = Decimal.TryParse(viseraDR["LeftView"].ToString(), out dec) ? dec.ToString() : "";
                viseraDR["RightView"] = Decimal.TryParse(viseraDR["RightView"].ToString(), out dec) ? dec.ToString() : "";
                viseraDR["LeftEyecorrect"] = Decimal.TryParse(viseraDR["LeftEyecorrect"].ToString(), out dec) ? dec.ToString() : "";
                viseraDR["RightEyecorrect"] = Decimal.TryParse(viseraDR["RightEyecorrect"].ToString(), out dec) ? dec.ToString() : "";

                viseraDT.Rows.Add(viseraDR);
                #endregion
                saveDS.Tables.Add(viseraDT);

                DataTable phyDT = dataDS.Tables["ARCHIVE_PHYSICALEXAM"].Clone();

                #region ARCHIVE_PHYSICALEXAM  查体

                DataRow phyDR = phyDT.NewRow();
                phyDR["PhysicalID"] = key;
                phyDR["OutKey"] = outkey.ToString();
                phyDR["IDCardNo"] = idcard;

                //皮肤
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gPfgm'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr = (n.Attributes["value"].Value == "99" ? "7" : n.Attributes["value"].Value);
                            break;
                        }
                    }
                }
                phyDR["Skin"] = temStr;

                //皮肤异常
                if (temStr == "7")
                {
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='gPfqt']");
                    phyDR["SkinEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                }

                //巩膜
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gGongmo'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr = (n.Attributes["value"].Value == "99" ? "4" : n.Attributes["value"].Value);
                            break;
                        }
                    }
                }
                phyDR["Sclere"] = temStr;

                //巩膜异常gGmqt
                if (temStr == "4")
                {
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='gGmqt']");
                    phyDR["SclereEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                }

                //淋巴结
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gLbj'][@checked]");
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr = n.Attributes["value"].Value;
                            break;
                        }
                    }
                }
                phyDR["Lymph"] = temStr;

                //淋巴结异常
                if (temStr == "4")
                {
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='gLbjqt']");
                    phyDR["LymphEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                }

                //桶状胸
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gTzx'][@checked]");
                phyDR["BarrelChest"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value == "1" ? "2" : "1";
                //呼吸音
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gHxy'][@checked]");
                phyDR["BreathSounds"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //罗音
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gLy'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr = n.Attributes["value"].Value;
                            break;
                        }
                    }
                }
                phyDR["Rale"] = temStr;

                //罗音异常
                if (temStr == "4")
                {
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='gLyyc']");
                    phyDR["RaleEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                }

                //心率
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXinlv']");
                phyDR["HeartRate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //心律
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXinlvci'][@checked]");
                phyDR["HeartRhythm"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //杂音
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gZayin'][@checked]");
                phyDR["Noise"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //包块
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gBk'][@checked]");
                phyDR["EnclosedMass"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //下肢浮肿
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXzsz'][@checked]");
                temStr = "";
                if (node != null)
                {
                    if (node.Attributes.Contains("value"))
                    {
                        var n = node.Attributes["value"].Value;
                        temStr = this.GetEnclosedMassCodeByWebCode(n);
                    }

                }
                phyDR["Edema"] = temStr;
                //足背动脉搏动足背动脉搏动
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gZbdmmd'][@checked]");
                phyDR["FootBack"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //肛门指诊
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gGmzhzh'][@checked]");
                //nodes = doc.DocumentNode.SelectNodes("//input[@name='gGmzhzh'][@checked]");
                //temStr = "";
                //if (nodes != null)
                //{
                //    foreach (var n in nodes)
                //    {
                //        if (n.Attributes.Contains("value"))
                //        {
                //            temStr += "," + (n.Attributes["value"].Value == "99" ? "5" : n.Attributes["value"].Value);
                //            break;
                //        }
                //    }
                //}
                phyDR["Anus"] = node == null || !node.Attributes.Contains("value") ? "" : GetAnusForPad(node.Attributes["value"].Value.ToString());

                //肛门指诊
                if (phyDR["Anus"].ToString() == "5")
                {
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='gGmzhzhyi']");
                    phyDR["AnusEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                }

                //乳腺
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gRuxian'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr = (n.Attributes["value"].Value == "99" ? "5" : n.Attributes["value"].Value);
                            break;
                        }
                    }
                }
                phyDR["Breast"] = temStr;

                //外阴
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gWaiyin'][@checked]");
                phyDR["Vulva"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //阴道
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYindao'][@checked]");
                phyDR["Vagina"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //宫颈
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gGongjing'][@checked]");
                phyDR["CervixUteri"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //宫体
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gGongti'][@checked]");
                phyDR["Corpus"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //附件
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gFujian'][@checked]");
                phyDR["Attach"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gCtqt']");
                phyDR["Other"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //压痛
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYato'][@checked]");
                phyDR["PressPain"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //肝大
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gGanda'][@checked]");
                phyDR["Liver"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //脾大
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gPida'][@checked]");
                phyDR["Spleen"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //移动性浊音
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gZhuoyin'][@checked]");
                phyDR["Voiced"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //乳腺异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gRuxianqt']");
                phyDR["BreastEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //呼吸音异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gHxyyc']");
                phyDR["BreathSoundsEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //杂音异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZayinyo']");
                phyDR["NoiseEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //宫颈异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGongjingyc']");
                phyDR["CervixUteriEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //宫体异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGongtiyc']");
                phyDR["CorpusEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //附件异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gFujianyc']");
                phyDR["AttachEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //外阴异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gWaiyinyc']");
                phyDR["VulvaEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //阴道异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYindaoyc']");
                phyDR["VaginaEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                //压痛有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYatoyo']");
                phyDR["PressPainEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //肝大有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGandayo']");
                phyDR["LiverEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //脾大有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gPidayo']");
                phyDR["SpleenEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //移动性浊音，有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZhuoyinyo']");
                phyDR["VoicedEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //包块有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gBkyo']");
                phyDR["EnclosedMassEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //眼底，1正常、2异常
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYand'][@checked]");
                phyDR["EyeRound"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //眼底异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYandyi']");
                phyDR["EyeRoundEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                phyDT.Rows.Add(phyDR);
                #endregion
                saveDS.Tables.Add(phyDT);

                DataTable assistDT = dataDS.Tables["ARCHIVE_ASSISTCHECK"].Clone();
                #region ARCHIVE_ASSISTCHECK 辅助检查
                DataRow assistDR = assistDT.NewRow();
                assistDR["PhysicalID"] = key;
                assistDR["OutKey"] = outkey;
                assistDR["IDCardNo"] = idcard;
                //血红蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='hb']");
                assistDR["HB"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //白细胞
                node = doc.DocumentNode.SelectSingleNode("//input[@id='wbc']");
                assistDR["WBC"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血小板
                node = doc.DocumentNode.SelectSingleNode("//input[@id='plt']");
                assistDR["PLT"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //尿蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNdb']");
                assistDR["PRO"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //尿糖
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNt']");
                assistDR["GLU"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //尿酮体
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNtt']");
                assistDR["KET"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //尿潜血
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNqx']");
                assistDR["BLD"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //空腹血糖L
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gKfxt']");
                assistDR["FPGL"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //心电图1:正常,2:异常
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gXindt'][@checked]");
                string xdt = "";
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (var no in nodes)
                    {
                        strTmpE = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;
                        xdt += GetECGByWebCode(strTmpE) + ",";
                    }
                }
                assistDR["ECG"] = xdt.TrimEnd(',');
                //尿微量蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='nwlbdb']");
                assistDR["ALBUMIN"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //大便潜血1:阴性,2:阳性
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gDbqx'][@checked]");
                assistDR["FOB"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //糖化血红蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gThxhdb']");
                assistDR["HBALC"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //乙型肝炎表面抗原1:阴性,2:阳性
                node = doc.DocumentNode.SelectSingleNode("//input[@name='hbsag'][@checked]");
                assistDR["HBSAG"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血清谷丙转氨酶
                node = doc.DocumentNode.SelectSingleNode("//input[@id='alt']");
                assistDR["SGPT"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血清谷草转氨酶
                node = doc.DocumentNode.SelectSingleNode("//input[@id='ast']");
                assistDR["GOT"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //白蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='alb']");
                assistDR["BP"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //总胆红素
                node = doc.DocumentNode.SelectSingleNode("//input[@id='tbil']");
                assistDR["TBIL"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //结合胆红素
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dbil']");
                assistDR["CB"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血清肌酐
                node = doc.DocumentNode.SelectSingleNode("//input[@id='scr']");
                assistDR["SCR"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血尿素氮
                node = doc.DocumentNode.SelectSingleNode("//input[@id='bun']");
                assistDR["BUN"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血钾浓度
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSgnxjnd']");
                assistDR["PC"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血钠浓度
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSgnxnnd']");
                assistDR["HYPE"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //总胆固醇
                node = doc.DocumentNode.SelectSingleNode("//input[@id='cho']");
                assistDR["TC"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //甘油三酯
                node = doc.DocumentNode.SelectSingleNode("//input[@id='tg']");
                assistDR["TG"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血清低密度脂蛋白胆固醇
                node = doc.DocumentNode.SelectSingleNode("//input[@id='ldlc']");
                assistDR["LowCho"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血清高密度脂蛋白胆固醇
                node = doc.DocumentNode.SelectSingleNode("//input[@id='hdlc']");
                assistDR["HeiCho"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //胸部X线片1:正常,2:异常
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXiongp'][@checked]");
                assistDR["CHESTX"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //B超1:正常,2:异常
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gBchao'][@checked]");
                assistDR["BCHAO"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血常规其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXcgqt']");
                assistDR["BloodOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //尿常规其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNcgqt']");
                assistDR["UrineOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gFuzhuqt']");
                assistDR["Other"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //宫颈涂片1:正常,2:异常
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gGjtp'][@checked]");
                assistDR["CERVIX"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                ////谷氨酰转肽酶
                //    node = doc.DocumentNode.SelectSingleNode("//input[@id='']");
                //assistDR["GT"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //心电图异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXindtyi']");
                assistDR["ECGEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //胸部X线片异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXiongpyc']");
                assistDR["CHESTXEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //B超异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gBchaoyi']");
                assistDR["BCHAOEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //宫颈涂片异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGjtpyc']");
                assistDR["CERVIXEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                // 餐后2H血糖
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gChxt']");
                assistDR["FPGDL"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                #region 2017-10-20添加

                //血型
                node = doc.DocumentNode.SelectSingleNode("//input[@id='xxABO']");
                assistDR["BloodType"] = node == null || !node.Attributes.Contains("value") ? "" : GetWebBloodTypeByWebCode(node.Attributes["value"].Value);

                node = doc.DocumentNode.SelectSingleNode("//input[@id='xxRh']");
                assistDR["RH"] = node == null || !node.Attributes.Contains("value") ? "" : GetWebRHByWebCode(node.Attributes["value"].Value);

                // 同型半胱氨酸
                node = doc.DocumentNode.SelectSingleNode("//input[@id='txbgas']");
                temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                assistDR["HCY"] = Decimal.TryParse(temStr, out dec) ? dec.ToString() : "";

                node = doc.DocumentNode.SelectSingleNode("//input[@id='gBchaoqt'][@checked]");
                assistDR["BCHAOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                node = doc.DocumentNode.SelectSingleNode("//input[@id='gBchaoyiqt']");
                assistDR["BCHAOtherEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                #endregion
                //健康体检_辅助检查
                assistDR["HB"] = Decimal.TryParse(assistDR["HB"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["WBC"] = Decimal.TryParse(assistDR["WBC"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["PLT"] = Decimal.TryParse(assistDR["PLT"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["FPGL"] = Decimal.TryParse(assistDR["FPGL"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["ALBUMIN"] = Decimal.TryParse(assistDR["ALBUMIN"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["HBALC"] = Decimal.TryParse(assistDR["HBALC"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["SGPT"] = Decimal.TryParse(assistDR["SGPT"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["GOT"] = Decimal.TryParse(assistDR["GOT"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["BP"] = Decimal.TryParse(assistDR["BP"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["TBIL"] = Decimal.TryParse(assistDR["TBIL"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["CB"] = Decimal.TryParse(assistDR["CB"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["SCR"] = Decimal.TryParse(assistDR["SCR"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["BUN"] = Decimal.TryParse(assistDR["BUN"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["PC"] = Decimal.TryParse(assistDR["PC"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["HYPE"] = Decimal.TryParse(assistDR["HYPE"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["TC"] = Decimal.TryParse(assistDR["TC"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["TG"] = Decimal.TryParse(assistDR["TG"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["LowCho"] = Decimal.TryParse(assistDR["LowCho"].ToString(), out dec) ? dec.ToString() : "";
                assistDR["HeiCho"] = Decimal.TryParse(assistDR["HeiCho"].ToString(), out dec) ? dec.ToString() : "";

                assistDT.Rows.Add(assistDR);

                #endregion
                saveDS.Tables.Add(assistDT);

                DataTable mediphyDT = dataDS.Tables["ARCHIVE_MEDI_PHYS_DIST"].Clone();
                #region ARCHIVE_MEDI_PHYS_DIST 中医体质
                DataRow amediphyDR = mediphyDT.NewRow();
                amediphyDR["PhysicalID"] = key;
                amediphyDR["OutKey"] = outkey.ToString();
                amediphyDR["IDCardNo"] = idcard;
                //平和质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gPhz'][@checked]");
                amediphyDR["Mild"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //气虚质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gQxz'][@checked]");
                amediphyDR["Faint"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //阳虚质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYangxz'][@checked]");
                amediphyDR["Yang"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //阴虚质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gYinxz'][@checked]");
                amediphyDR["Yin"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //痰湿质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gTsz'][@checked]");
                amediphyDR["PhlegmDamp"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //湿热质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSrz'][@checked]");
                amediphyDR["Muggy"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血瘀质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gXyz'][@checked]");
                amediphyDR["BloodStasis"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //气郁质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gQyz'][@checked]");
                amediphyDR["QiConstraint"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //特兼质1:是,2:基本是
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gTbz'][@checked]");
                amediphyDR["Characteristic"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


                mediphyDT.Rows.Add(amediphyDR);
                #endregion
                saveDS.Tables.Add(mediphyDT);

                DataTable healthDT = dataDS.Tables["ARCHIVE_HEALTHQUESTION"].Clone();
                #region  ARCHIVE_HEALTHQUESTION 现存主要健康问题
                DataRow healthDR = healthDT.NewRow();
                healthDR["PhysicalID"] = key;
                healthDR["OutKey"] = outkey;
                healthDR["IDCardNo"] = idcard;
                //脑血管疾病（以英文逗号分隔）1:未发现,2:缺血性卒中,3:脑出血,4:蛛网膜下腔出血,5:短暂性脑缺血发作,6:其他
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gNxgjb'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + (n.Attributes["value"].Value == "99" ? "6" : n.Attributes["value"].Value);
                        }
                    }
                }

                healthDR["BrainDis"] = temStr.TrimStart(',');

                //肾脏疾病肾脏疾病
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gSzjb'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + (n.Attributes["value"].Value == "99" ? "6" : n.Attributes["value"].Value);
                        }
                    }
                }
                healthDR["RenalDis"] = temStr.TrimStart(',');
                //心脏疾病
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gXzjb'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        string str = n == null || !n.Attributes.Contains("value") ? "" : n.Attributes["value"].Value.Replace("10", "");
                        if (!string.IsNullOrEmpty(str))
                        {
                            temStr += "," + (str.Replace("99", "10"));
                        }
                    }
                }

                healthDR["HeartDis"] = temStr.TrimStart(',');
                ////血管疾病
                //nodes = doc.DocumentNode.SelectNodes("//input[@name='gXgjb'][@checked]");
                //temStr = "";
                //if (nodes != null)
                //{
                //    foreach (var n in nodes)
                //    {
                //        if (n.Attributes.Contains("value"))
                //        {
                //            temStr += "," + (n.Attributes["value"].Value == "99" ? "4" : n.Attributes["value"].Value);
                //        }
                //    }
                //}

                //healthDR["VesselDis"] = temStr.TrimStart(',');
                //眼部疾病
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gYbjb'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + (n.Attributes["value"].Value == "99" ? "5" : n.Attributes["value"].Value);
                        }
                    }
                }

                healthDR["EyeDis"] = temStr.TrimStart(',');

                //神经系统疾病
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gSjxtjb'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + (GetWebNerveDisByWebCode(n.Attributes["value"].Value));
                        }
                    }
                }

                healthDR["NerveDis"] = temStr.TrimStart(',');
                //其他系统疾病
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gQtxtjb'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + (GetWebElseDisByWebCode(n.Attributes["value"].Value));
                        }
                    }
                }
                healthDR["ElseDis"] = temStr.TrimStart(',');
                //脑血管疾病其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNxgjbqt']");
                healthDR["BrainOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //肾脏疾病其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSzjbqt']");
                healthDR["RenalOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //心脏疾病其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXzjbqt']");
                healthDR["HeartOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //血管其他疾病
                //node = doc.DocumentNode.SelectSingleNode("//input[@id='gXgjbqt']");
                //healthDR["VesselOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //眼部其他疾病
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYbjbqt']");
                healthDR["EyeOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //神经系统疾病其它
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSjxtjbqt']");
                healthDR["NerveOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //其他系统疾病其它
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gQtxtjbqt']");
                healthDR["ElseOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;


                healthDT.Rows.Add(healthDR);
                #endregion
                saveDS.Tables.Add(healthDT);

                DataTable hospitalDT = dataDS.Tables["ARCHIVE_HOSPITALHISTORY"].Clone();
                #region ARCHIVE_HOSPITALHISTORY 住院史

                node = doc.DocumentNode.SelectSingleNode("//input[@name='zyzlqkyw'][@checked]");
                if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                {
                    nodes = doc.DocumentNode.SelectSingleNode("//table[@id='zyzlqk']").SelectNodes("tr");
                    if (nodes != null && nodes.Count > 1)
                    {
                        for (var i = 1; i < nodes.Count; i++)
                        {
                            node = nodes[i];
                            DataRow hospitalDR = hospitalDT.NewRow();
                            hospitalDR["PhysicalID"] = key;
                            hospitalDR["IDCardNo"] = idcard;
                            hospitalDR["OutKey"] = outkey.ToString();
                            var nodet = node.SelectSingleNode("td/input[@name='zRyjcrq']");
                            hospitalDR["InHospitalDate"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            nodet = node.SelectSingleNode("td/input[@name='zYuanyin']");
                            hospitalDR["Reason"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            nodet = node.SelectSingleNode("td/input[@name='zBingah']");
                            hospitalDR["IllcaseNum"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            nodet = node.SelectSingleNode("td/input[@name='zYljgmc']");
                            hospitalDR["HospitalName"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            nodet = node.SelectSingleNode("td/input[@name='zCyccrq']");
                            hospitalDR["OutHospitalDate"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;


                            hospitalDT.Rows.Add(hospitalDR);
                        }
                    }
                }
                #endregion
                saveDS.Tables.Add(hospitalDT);

                DataTable familyDT = dataDS.Tables["ARCHIVE_FAMILYBEDHISTORY"].Clone();
                #region ARCHIVE_FAMILYBEDHISTORY  家族病床史

                node = doc.DocumentNode.SelectSingleNode("//input[@name='jzbcsyw'][@checked]");
                if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                {
                    nodes = doc.DocumentNode.SelectSingleNode("//table[@id='jzbcs']").SelectNodes("tr");
                    if (nodes != null && nodes.Count > 1)
                    {
                        for (var i = 1; i < nodes.Count; i++)
                        {
                            node = nodes[i];
                            DataRow familyDR = familyDT.NewRow();
                            familyDR["PhysicalID"] = key;
                            familyDR["OutKey"] = outkey.ToString();
                            familyDR["IDCardNo"] = idcard;
                            //入院时间
                            var nodet = node.SelectSingleNode("td/input[@name='zRyjcrq']");
                            familyDR["InHospitalDate"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            //原因
                            nodet = node.SelectSingleNode("td/input[@name='zYuanyin']");
                            familyDR["Reasons"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            //病案号
                            nodet = node.SelectSingleNode("td/input[@name='zBingah']");
                            familyDR["IllcaseNums"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            //医院名称
                            nodet = node.SelectSingleNode("td/input[@name='zYljgmc']");
                            familyDR["HospitalName"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            //出院时间
                            nodet = node.SelectSingleNode("td/input[@name='zCyccrq']");
                            familyDR["OutHospitalDate"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;

                            familyDT.Rows.Add(familyDR);
                        }
                    }

                }

                #endregion
                saveDS.Tables.Add(familyDT);

                DataTable medicationDT = dataDS.Tables["ARCHIVE_MEDICATION"].Clone();
                #region ARCHIVE_MEDICATION 主要用药情况

                node = doc.DocumentNode.SelectSingleNode("//input[@name='zyyyqkyw'][@checked]");
                if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                {

                    nodes = doc.DocumentNode.SelectNodes("//table[@id='zyyyqk']/tr");
                    if (nodes != null && nodes.Count > 1)
                    {
                        nodes.Remove(0);
                        foreach (var nodd in nodes)
                        {
                            var nod = nodd.SelectNodes("td");
                            DataRow medicationDR = medicationDT.NewRow();
                            medicationDR["PhysicalID"] = key;
                            medicationDR["IDCardNo"] = idcard;
                            medicationDR["Outkey"] = outkey;
                            //用法
                            var no = nod[1].SelectSingleNode("input");
                            medicationDR["UseAge"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;
                            //用量
                            no = nod[2].SelectSingleNode("input");
                            medicationDR["UseNum"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;
                            //用药时间
                            no = nod[3].SelectSingleNode("input");
                            medicationDR["StartTime"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;
                            //结束时间
                            //node =node.SelectSingleNode("//input[@id='']");
                            //medicationDR["EndTime"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                            //用药依从性
                            no = nod[4].SelectSingleNode("select/option[@selected]");
                            medicationDR["PillDependence"] = no == null || !no.Attributes.Contains("value") ? "" : GetYongYaoYiCongByWebCode(no.Attributes["value"].Value);
                            //药名
                            no = nod[0].SelectSingleNode("input");
                            medicationDR["MedicinalName"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                            medicationDT.Rows.Add(medicationDR);
                        }
                    }
                }


                #endregion
                saveDS.Tables.Add(medicationDT);

                DataTable inoculationDT = dataDS.Tables["ARCHIVE_INOCULATIONHISTORY"].Clone();
                #region ARCHIVE_INOCULATIONHISTORY  非免疫规划预防接种史表fmyghyfyw
                //var node12 = doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[2]/div[2]");
                //HtmlDocument doc2 = HtmlHelper.GetHtmlDocument(node12.InnerHtml);
                //node = doc2.DocumentNode.SelectSingleNode("//table[1]/tr[11]/td[2]/input[@name='fmyghyfyw']");
                //   var tempnode = node12.SelectSingleNode("//table[1]");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='fmyghyfyw'][@checked]");
                if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                {
                    nodes = doc.DocumentNode.SelectNodes("//table[@id='fmyghyf']/tr");
                    if (nodes != null && nodes.Count > 1)
                    {
                        for (var i = 1; i < nodes.Count; i++)
                        {
                            node = nodes[i];
                            DataRow inoculationDR = inoculationDT.NewRow();
                            inoculationDR["PhysicalID"] = key;
                            inoculationDR["IDCardNo"] = idcard;
                            inoculationDR["OutKey"] = outkey.ToString();
                            //名称
                            var nodet = node.SelectSingleNode("td/input[@name='fJzmc']");
                            inoculationDR["PillName"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;
                            //日期
                            nodet = node.SelectSingleNode("td/input[@name='fJzrq']");
                            inoculationDR["InoculationDate"] = CommonExtensions.GetConvertDate(nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value);
                            //接种机构
                            nodet = node.SelectSingleNode("td/input[@name='fJzjg']");
                            inoculationDR["InoculationHistory"] = nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value;

                            inoculationDT.Rows.Add(inoculationDR);
                        }
                    }
                }
                #endregion
                saveDS.Tables.Add(inoculationDT);

                DataTable assessmenDT = dataDS.Tables["ARCHIVE_ASSESSMENTGUIDE"].Clone();
                #region ARCHIVE_ASSESSMENTGUIDE 健康评价与指导

                //威海地区之外的移除减腰围字段
                if (!baseUrl.Contains("sdcsm_new"))
                {
                    assessmenDT.Columns.Remove("WaistlineArm");
                }
                DataRow assessmenDR = assessmenDT.NewRow();
                assessmenDR["PhysicalID"] = key;
                assessmenDR["IDCardNo"] = idcard;
                assessmenDR["OutKey"] = outkey.ToString();
                //健康评价，是否正常
                node = doc.DocumentNode.SelectSingleNode("//input[@checked][@name='gJkpj']");
                assessmenDR["IsNormal"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //健康指导
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gJkzd'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + GetHealthGuideCodeByWebCode(n.Attributes["value"].Value);
                        }
                    }
                }
                assessmenDR["HealthGuide"] = temStr.TrimStart(',');
                //危险因素控制
                nodes = doc.DocumentNode.SelectNodes("//input[@name='gWxyskz'][@checked]");
                temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr += "," + GetDangerControlCodeByWebCode(n.Attributes["value"].Value);
                        }
                    }
                }
                assessmenDR["DangerControl"] = temStr.TrimStart(',');
                //异常1
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJkpjyc1']");
                assessmenDR["Exception1"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJkpjyc2']");
                assessmenDR["Exception2"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJkpjyc3']");
                assessmenDR["Exception3"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJkpjyc4']");
                assessmenDR["Exception4"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //减体重目标
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gWxystz']");
                assessmenDR["Arm"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value.ToLower().Replace("kg", "");
                //建议疫苗接种
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gWsysym']");
                assessmenDR["VaccineAdvice"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                //其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gWxysqt']");
                assessmenDR["Other"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                if (baseUrl.Contains("sdcsm_new"))
                {
                    //减腹围
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='gWxysjfw']");
                    assessmenDR["WaistlineArm"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                }

                assessmenDT.Rows.Add(assessmenDR);
                #endregion
                saveDS.Tables.Add(assessmenDT);

                cDao.SaveDataSet(saveDS, idcard, "", outkey.ToString());
                saveDS.Tables.Clear();
            }
        }

        /// <summary>
        ///  修改获取 txtvalue_old
        /// </summary>
        /// <param name="key"></param>
        /// <param name="person"></param>
        private string GetTxt_old(string returnString)
        {
            //string idcard = person.idNumber;
            WebHelper web = new WebHelper();
            //string postData = "dGrdabh=" + person.pid + "&id=" + key + "&tz=2";
            //string returnString = web.PostHttp(baseUrl + "/health/healthToUpdate.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            StringBuilder sbtxt = new StringBuilder();
            if (doc != null)
            {
                //解析页面信息

                #region ARCHIVE_CUSTOMERBASEINFO

                //体检日期
                var node = doc.DocumentNode.SelectSingleNode("//input[@id='happentime']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //更新责任医生 2017-03-06
                node = doc.DocumentNode.SelectSingleNode("//input[@id='field2']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");


                //症状其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZzqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");


                #endregion

                #region ARCHIVE_GENERALCONDITION  一般情况

                // 体温
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gTw']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //脉率
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gMb']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //呼吸频率
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gHx']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");


                //左侧高
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXyzc1']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //左侧低
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXyzc2']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //左侧：原因
                node = doc.DocumentNode.SelectSingleNode("//input[@id='zcyy']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //右侧高
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXyyc1']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //右侧低
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXyyc2']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //右：原因
                node = doc.DocumentNode.SelectSingleNode("//input[@id='ycyy']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //身高 (保留两位小数)
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSg']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //体重
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gTzh']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //腰围(保留两位小数)
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYw']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //体质指数
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gTzhzh']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //  string ss2 = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value + ";";
                //简易智力状态
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gLnrrzfen']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //老年人抑郁症
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gLnrqgfen']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                #endregion

                #region ARCHIVE_LIFESTYLE  生活方式

                //每次锻炼时间
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gMcdlsj']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //坚持锻炼时间
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJcdlsj']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //锻炼方式
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gDlfs']");
                //  string strTmpE = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value + ";");
                //lifeDR["ExerciseExistense"] = strTmpE.Replace("散步", "1").Replace("跑步", "2").Replace("广场舞", "3");
                // sbtxt.Append(  string.IsNullOrEmpty(strTmpE) ? "" : "4";

                //日吸烟数量
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gRxyl']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //开始吸烟年龄
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gKsxynl']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //戒烟年龄
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJynl']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //日饮酒量
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gRyjl']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //戒酒年龄
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJjnl']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //开始饮酒年龄
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gKsyjnl']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //饮酒类型：其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYjzlqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //粉尘
                node = doc.DocumentNode.SelectSingleNode("//input[@id='fenchen']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //放射
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gShexian']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //物理防护
                node = doc.DocumentNode.SelectSingleNode("//input[@id='wuliyinsu']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //化学
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gHxp']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='blqita']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //工种
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gJtzy']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");
                //从业时间
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gCysj']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");
                //粉尘防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='fchy']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");
                //放射防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSxfhcsqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");
                //物理防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='wly']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");
                //化学防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gHxpfhcsjt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");
                //其他防护措施
                node = doc.DocumentNode.SelectSingleNode("//input[@id='qty']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                #endregion

                #region ARCHIVE_VISCERAFUNCTION  脏器功能


                //口唇
                //node = doc.DocumentNode.SelectSingleNode("//input[@name='gKouchun'][@checked]");
                //viseraDR["Lips"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value+";");
                //齿列
                var nodes = doc.DocumentNode.SelectNodes("//input[@name='gChilei'][@checked]");
                string temStr = "";
                if (nodes != null)
                {
                    foreach (var n in nodes)
                    {
                        if (n.Attributes.Contains("value"))
                        {
                            temStr = n.Attributes["value"].Value;
                            break;
                        }
                    }
                }

                //缺齿
                if (temStr.Contains("2"))
                {
                    // string quechi = "";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quechi1']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quechi2']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quechi3']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quechi4']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    //  viseraDR["HypodontiaEx"] = quechi;
                }
                //龋齿
                if (temStr.Contains("3"))
                {
                    //  string quchi = "";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quchi1']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quchi2']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quchi3']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='quchi4']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    //    viseraDR["SaprodontiaEx"] = quchi;
                }
                //义齿
                if (temStr.Contains("4"))
                {
                    string yichi = "";
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='yichi1']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='yichi2']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='yichi3']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='yichi4']");
                    sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                    // viseraDR["DentureEx"] = yichi;
                }
                //齿列其他
                node = doc.DocumentNode.SelectSingleNode("//input[@name='chlqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //咽部
                //node = doc.DocumentNode.SelectSingleNode("//input[@name='gYanbu'][@checked]");
                //viseraDR["Pharyngeal"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value+";");
                //左眼视力
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZysl']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //右眼视力
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYysl']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //左眼矫正
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZyjz']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //右眼矫正
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYyjz']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                #endregion

                #region ARCHIVE_PHYSICALEXAM  查体


                //眼底异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYandyi']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //皮肤异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gPfqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //巩膜异常gGmqt
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGmqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //淋巴结异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gLbjqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //呼吸音异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gHxyyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //罗音异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gLyyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //杂音异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZayinyo']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //压痛有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYatoyo']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //肝大有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGandayo']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //脾大有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gPidayo']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //移动性浊音，有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gZhuoyinyo']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //包块有
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gBkyo']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //肛门指诊
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGmzhzhyi']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //乳腺异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gRuxianqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //外阴异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gWaiyinyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //阴道异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYindaoyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //宫颈异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGongjingyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //宫体异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGongtiyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //附件异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gFujianyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gCtqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                #endregion

                #region ARCHIVE_ASSISTCHECK 辅助检查

                //血红蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='hb']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //白细胞
                node = doc.DocumentNode.SelectSingleNode("//input[@id='wbc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //血小板
                node = doc.DocumentNode.SelectSingleNode("//input[@id='plt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //血常规其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXcgqt']");
                sbtxt.Append(node != null && !node.Attributes.Contains("value") && !string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";" : "");

                //尿蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNdb']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //尿糖
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //尿酮体
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNtt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //尿潜血
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNqx']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //尿常规其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNcgqt']");
                sbtxt.Append(node != null && !node.Attributes.Contains("value") && !string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";" : "");

                //空腹血糖L
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gKfxt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //空腹血糖DL 饭后俩小时
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gChxt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //心电图异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXindtyi']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //尿微量蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='nwlbdb']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //糖化血红蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gThxhdb']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //乙型肝炎表面抗原1:阴性,2:阳性
                node = doc.DocumentNode.SelectSingleNode("//input[@name='hbsag'][@checked]");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //血清谷丙转氨酶
                node = doc.DocumentNode.SelectSingleNode("//input[@id='alt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //血清谷草转氨酶
                node = doc.DocumentNode.SelectSingleNode("//input[@id='ast']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //白蛋白
                node = doc.DocumentNode.SelectSingleNode("//input[@id='alb']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //总胆红素
                node = doc.DocumentNode.SelectSingleNode("//input[@id='tbil']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //结合胆红素
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dbil']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //血清肌酐
                node = doc.DocumentNode.SelectSingleNode("//input[@id='scr']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //血尿素氮
                node = doc.DocumentNode.SelectSingleNode("//input[@id='bun']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //血钾浓度
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSgnxjnd']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //血钠浓度
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSgnxnnd']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //总胆固醇
                node = doc.DocumentNode.SelectSingleNode("//input[@id='cho']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //甘油三酯
                node = doc.DocumentNode.SelectSingleNode("//input[@id='tg']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //血清低密度脂蛋白胆固醇
                node = doc.DocumentNode.SelectSingleNode("//input[@id='ldlc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //血清高密度脂蛋白胆固醇
                node = doc.DocumentNode.SelectSingleNode("//input[@id='hdlc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");

                //胸部X线片异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXiongpyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //B超异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gBchaoyi']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");
                //宫颈涂片异常
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gGjtpyc']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                //其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gFuzhuqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";");

                #endregion

                #region ARCHIVE_MEDI_PHYS_DIST 中医体质
                #endregion

                #region  ARCHIVE_HEALTHQUESTION 现存主要健康问题

                //脑血管疾病其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gNxgjbqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //肾脏疾病其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gSzjbqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //心脏疾病其他
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXzjbqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //血管其他疾病
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gXgjbqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //眼部其他疾病
                node = doc.DocumentNode.SelectSingleNode("//input[@id='gYbjbqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //神经系统疾病其它
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gSjxtjbqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                //其他系统疾病其它
                node = doc.DocumentNode.SelectSingleNode("//input[@name='gQtxtjbqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                #endregion

                #region ARCHIVE_HOSPITALHISTORY 住院史

                node = doc.DocumentNode.SelectSingleNode("//input[@name='zyzlqkyw'][@checked]");
                if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                {
                    nodes = doc.DocumentNode.SelectSingleNode("//table[@id='zyzlqk']").SelectNodes("tr");
                    if (nodes != null && nodes.Count > 1)
                    {
                        for (var i = 1; i < nodes.Count; i++)
                        {
                            node = nodes[i];


                            //入院日期
                            var nodet = node.SelectSingleNode("td/input[@name='zRyjcrq']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value + ";");

                            //出院日期
                            nodet = node.SelectSingleNode("td/input[@name='zCyccrq']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value + ";");
                            //原因
                            nodet = node.SelectSingleNode("td/input[@name='zYuanyin']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(nodet.Attributes["value"].Value) + ";");
                            //医院名称
                            nodet = node.SelectSingleNode("td/input[@name='zYljgmc']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(nodet.Attributes["value"].Value) + ";");
                            //病案号 
                            nodet = node.SelectSingleNode("td/input[@name='zBingah']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value + ";");

                            //  hospitalDT.Rows.Add(hospitalDR);
                        }
                    }
                }
                #endregion

                #region ARCHIVE_FAMILYBEDHISTORY  家族病床史

                node = doc.DocumentNode.SelectSingleNode("//input[@name='jzbcsyw'][@checked]");
                if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                {
                    nodes = doc.DocumentNode.SelectSingleNode("//table[@id='jzbcs']").SelectNodes("tr");
                    if (nodes != null && nodes.Count > 1)
                    {
                        for (var i = 1; i < nodes.Count; i++)
                        {
                            node = nodes[i];


                            //入院时间
                            var nodet = node.SelectSingleNode("td/input[@name='zRyjcrq']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value + ";"); ;
                            //出院时间
                            nodet = node.SelectSingleNode("td/input[@name='zCyccrq']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value + ";"); ;
                            //原因
                            nodet = node.SelectSingleNode("td/input[@name='zYuanyin']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(nodet.Attributes["value"].Value) + ";"); ;
                            //医院名称
                            nodet = node.SelectSingleNode("td/input[@name='zYljgmc']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(nodet.Attributes["value"].Value) + ";"); ;
                            //病案号
                            nodet = node.SelectSingleNode("td/input[@name='zBingah']");
                            sbtxt.Append(nodet == null || !nodet.Attributes.Contains("value") ? "" : nodet.Attributes["value"].Value + ";"); ;
                        }
                    }

                }

                #endregion

                #region ARCHIVE_MEDICATION 主要用药情况

                node = doc.DocumentNode.SelectSingleNode("//input[@name='zyyyqkyw'][@checked]");
                if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                {

                    nodes = doc.DocumentNode.SelectNodes("//table[@id='zyyyqk']/tr");
                    if (nodes != null && nodes.Count > 1)
                    {
                        nodes.Remove(0);
                        foreach (var nodd in nodes)
                        {
                            var nod = nodd.SelectNodes("td");

                            //药名
                            var no = nod[0].SelectSingleNode("input");
                            sbtxt.Append(no == null || !no.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(no.Attributes["value"].Value) + ";");

                            //用法
                            no = nod[1].SelectSingleNode("input");
                            sbtxt.Append(no == null || !no.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(no.Attributes["value"].Value) + ";");
                            //用量
                            no = nod[2].SelectSingleNode("input");
                            sbtxt.Append(no == null || !no.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(no.Attributes["value"].Value) + ";");
                            //用药时间
                            no = nod[3].SelectSingleNode("input");
                            sbtxt.Append(no == null || !no.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(no.Attributes["value"].Value) + ";");
                        }
                    }
                }


                #endregion


                #region ARCHIVE_INOCULATIONHISTORY  非免疫规划预防接种史表fmyghyfyw

                node = doc.DocumentNode.SelectSingleNode("//input[@name='fmyghyfyw'][@checked]");

                if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                {
                    nodes = doc.DocumentNode.SelectSingleNode("//table[@id='fmyghyf']").SelectNodes("tr");
                    if (nodes != null && nodes.Count > 1)
                    {
                        for (var i = 1; i < nodes.Count; i++)
                        {
                            node = nodes[i];

                            //名称
                            var nodet = node.SelectSingleNode("td/input[@name='fJzmc']");
                            sbtxt.Append(nodet == null && !nodet.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(nodet.Attributes["value"].Value) + ";");
                            //日期
                            nodet = node.SelectSingleNode("td/input[@name='fJzrq']");
                            sbtxt.Append(nodet == null && !nodet.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(nodet.Attributes["value"].Value) + ";");
                            //接种机构
                            nodet = node.SelectSingleNode("td/input[@name='fJzjg']");
                            sbtxt.Append(nodet == null && !nodet.Attributes.Contains("value") ? "" : HtmlHelper.GetUrlEncodeVal(nodet.Attributes["value"].Value) + ";");
                        }
                    }
                }
                #endregion


                #region ARCHIVE_ASSESSMENTGUIDE 健康评价与指导

                //   assessmenDR["DangerControl"] = temStr.TrimStart(',');
                //异常1
                node = doc.DocumentNode.SelectSingleNode("//div[@id='page3']//table[@id='table2']/tr/td//input[@id='gJkpjyc1']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //异常2
                node = doc.DocumentNode.SelectSingleNode("//div[@id='page3']//table[@id='table2']/tr/td//input[@id='gJkpjyc2']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //异常3
                node = doc.DocumentNode.SelectSingleNode("//div[@id='page3']//table[@id='table2']/tr/td//input[@id='gJkpjyc3']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //异常4
                node = doc.DocumentNode.SelectSingleNode("//div[@id='page3']//table[@id='table2']/tr/td//input[@id='gJkpjyc4']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //减体重目标
                node = doc.DocumentNode.SelectSingleNode("//div[@id='page3']//table[@id='table2']/tr/td//input[@id='gWxystz']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : node.Attributes["value"].Value + ";");
                //建议疫苗接种
                node = doc.DocumentNode.SelectSingleNode("//div[@id='page3']//table[@id='table2']/tr/td//input[@id='gWsysym']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;
                //其他
                node = doc.DocumentNode.SelectSingleNode("//div[@id='page3']//table[@id='table2']/tr/td//input[@id='gWxysqt']");
                sbtxt.Append(node == null || !node.Attributes.Contains("value") || string.IsNullOrWhiteSpace(node.Attributes["value"].Value) ? "" : HtmlHelper.GetUrlEncodeVal(node.Attributes["value"].Value) + ";"); ;

                #endregion

                return sbtxt.ToString();
            }
            return sbtxt.ToString();
        }

        /// <summary>
        /// 根据页面下肢水肿code,获取padCode
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetEnclosedMassCodeByWebCode(string code)
        {
            var temString = "1";
            switch (code)
            {
                case "1":
                    temString = "1";
                    break;
                case "2":
                    temString = "2";
                    break;
                case "3":
                    temString = "2";
                    break;
                case "4":
                    temString = "3";
                    break;
                case "5":
                    temString = "4";
                    break;
            }
            return temString;
        }

        /// <summary>
        /// 根据文字，获取对应pad 用药情况code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetYongYaoYiCongByWebCode(string code)
        {
            string tem = "1";
            switch (code)
            {
                case "规律":
                    tem = "1";
                    break;
                case "间断":
                    tem = "2";
                    break;
                case "不服药":
                    tem = "3";
                    break;
            }
            return tem;
        }

        /// <summary>
        /// 根据Web 健康指导Code,获取哦pad Code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetHealthGuideCodeByWebCode(string code)
        {
            var tem = "";
            if (baseUrl.Contains("sdcsm_new"))
            {
                if (baseUrl.Contains("10.61.64.53"))
                {
                    switch (code)
                    {
                        case "1":
                            tem = "2";
                            break;
                        case "2":
                            tem = "3";
                            break;
                        case "3":
                            tem = "4";
                            break;
                        case "4":
                            tem = "1";
                            break;
                    }
                }
                else
                {
                    return code;
                }
            }
            switch (code)
            {
                case "1":
                    tem = "4";
                    break;
                case "2":
                    tem = "1";
                    break;
                case "3":
                    tem = "2";
                    break;
                case "4":
                    tem = "3";
                    break;
            }
            return tem;
        }

        /// <summary>
        /// 根据Web 健康指导Code,获取哦pad Code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetHealthGuideCodeForWeb(string code)
        {
            var tem = "";
            ////威海乳山
            if (baseUrl.Contains("10.61.64.50"))
            {
                return code;
            }
            switch (code)
            {
                case "1":
                    tem = "2";
                    break;
                case "2":
                    tem = "3";
                    break;
                case "3":
                    tem = "4";
                    break;
                case "4":
                    tem = "1";
                    break;
            }
            return tem;
        }

        private string GetAnusForPad(string code)
        {
            var tem = code;
            switch (code)
            {
                case "99":
                    tem = "5";
                    break;
            }
            return tem;
        }

        /// <summary>
        /// 根据web危险因素控制Code ,获取 PAD code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetDangerControlCodeByWebCode(string code)
        {
            var tem = "7";
            switch (code)
            {
                case "1":
                    tem = "1";
                    break;
                case "2":
                    tem = "2";
                    break;
                case "3":
                    tem = "3";
                    break;
                case "4":
                    tem = "4";
                    break;
                case "97":
                    tem = "5";
                    break;
                case "98":
                    tem = "6";
                    break;
                case "99":
                    tem = "7";
                    break;
                case "96":
                    tem = "8";
                    break;
            }
            return tem;
        }

        ///<summary>
        /// 根据pad 心电图code ,获取web Code
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetECGByWebCode(object code)
        {
            string tem = "";
            switch (code + "")
            {
                case "99":
                    tem = "9";
                    break;
                default:
                    tem = code.ToString();
                    break;
            }

            return tem;
        }
        #endregion

        /// <summary>
        /// 获取验证码图片
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private Bitmap getImage(string url)
        {
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream st = response.GetResponseStream();
            Bitmap bitmap = (Bitmap)Bitmap.FromStream(st);
            return bitmap;
        }

        /// <summary>
        /// 获取验证码转换字符
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetRandomCode(string url)
        {
            try
            {
                Bitmap bitmap1 = getImage(url);

                Bitmap bitmap = (Bitmap)bitmap1.Clone();
                ImageHelper ud = new ImageHelper(bitmap);
                bitmap = ud.GrayByPixels();

                ud.ClearNoise(100, 3);

                bitmap = ud.ReSetBitMap();

                tessnet2.Tesseract ocr = new tessnet2.Tesseract();
                ocr.SetVariable("tessedit_char_whitelist", "0123456789");
                ocr.Init(serverPath + @"\\tmpe", "eng", true);
                List<tessnet2.Word> result = ocr.DoOCR(bitmap, Rectangle.Empty);
                string code = result[0].Text;
                return code;
            }
            catch (Exception ex)
            {
            }
            return "";
        }

        private string GetXML(string prgid, string idcardno, string tablename, string signcolumn, string tablecreatetime, string picbase64)
        {
            string xml = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                              "<s:Body>" +
                                "<DataCenterWebservice xmlns=\"http://webservice.jiekou.com\">" +
                                  "<in0>" + key + "</in0>" +
                                  "<in1>" + operate + "</in1>" +
                                  "<in2><![CDATA[<?xml version=\"1.0\" encoding=\"UTF-8\"?><XMLTOPERSONS return=\"TRUE\" value=\"0\" username=\"" + loginKey + "\" prgid=\"" + prgid + "\"><row name=\"T_DA_JKDA_RKXZL\"><field name=\"DSfzh\">" + idcardno + "</field><subrow name=\"PICFILE\"><field name=\"tablename\">" + tablename + "</field><field name=\"signcolumn\">" + signcolumn + "</field><field name=\"tablecreatetime\">" + tablecreatetime + "</field><field name=\"picture\">" + picbase64 + "</field></subrow></row></XMLTOPERSONS>]]></in2>" +
                                "</DataCenterWebservice>" +
                              "</s:Body>" +
                            "</s:Envelope>";
            return xml;
        }

        private string GetBCXml(string prgid, PersonModel pm, string tjDate, string bcfilename, string xdfilename, string bcbase64, string xdbase64)
        {
            string xml = "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">" +
                            "<s:Body>" +
                                "<DataCenterWebservice xmlns=\"http://webservice.jiekou.com\">" +
                                 "<in0>" + key + "</in0>" +
                                 "<in1>" + operate + "</in1>" +
                                  "<in2><![CDATA[<?xml version=\"1.0\" encoding=\"UTF-8\"?> " +
                                        "<XMLTOPERSONS return=\"TRUE\" biaoshi=\"2\" value=\"0\" username=\"" + loginKey + "\"  prgid=\"" + prgid + "\">" +
                                            "<row name=\"T_DA_JKDA_RKXZL\">" +
                                            "<field name=\"DSfzh\"><![CDATA[" + pm.idNumber + "]]></field>" +
                                            "<field name=\"jkbs\"><![CDATA[1]]></field>" +
                                            "<subrow name=\"T_JK_JKTJ\">" +
                                                "<field name=\"happentime\"><![CDATA[" + tjDate + "]]></field>" +
                                                "<subrow name=\"T_PW_PICFILE\"> " +
                                                    "<field name=\"DGrdabh\"><![CDATA[" + pm.pid + "]]></field>" +
                                                    "<field name=\"filename\"><![CDATA[" + bcfilename + "]]></field>" +
                                                    "<field name=\"tablename\"><![CDATA[T_JK_JKTJ]]></field>" +
                                                    "<field name=\"signcolumn\"><![CDATA[bctp]]></field>" +
                                                    "<field name=\"picture\"><![CDATA[" + bcbase64 + "]]></field>" +
                                                "</subrow>" +
                                                "<subrow name=\"T_PW_PICFILE\"> " +
                                                    "<field name=\"DGrdabh\"><![CDATA[" + pm.pid + "]]></field>" +
                                                    "<field name=\"filename\"><![CDATA[" + xdfilename + "]]></field>" +
                                                    "<field name=\"tablename\"><![CDATA[T_JK_JKTJ]]></field>" +
                                                    "<field name=\"signcolumn\"><![CDATA[xdttp]]></field>" +
                                                    "<field name=\"picture\"><![CDATA[" + xdbase64 + "]]></field>" +
                                                "</subrow>" +
                                            "</subrow>" +
                                            "</row>" +
                                        "</XMLTOPERSONS>]]>" +
                                  "</in2>" +
                                  "</DataCenterWebservice>" +
                              "</s:Body>" +
                            "</s:Envelope>";
            return xml;
        }

        /// <summary>
        /// 根据配置文件中的设定值，判断正常或异常
        /// </summary>
        /// <param name="xmlNodes"></param>
        /// <param name="nodeName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GetZcOrYc(XmlNodeList xmlNodes, string nodeName, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (xmlNodes == null || xmlNodes.Count == 0)
            {
                return "1";
            }

            string str = "1";
            string minval = "";
            string maxval = "";

            foreach (XmlNode item in xmlNodes)
            {
                if (nodeName == "BUN" || nodeName == "PC" || nodeName == "HYPE")
                {
                    if (item.SelectSingleNode("name").InnerText == nodeName)
                    {
                        minval = item.SelectSingleNode("minvalue").InnerText;
                        maxval = item.SelectSingleNode("maxvalue").InnerText;
                        break;
                    }
                }
                else
                {
                    if (item.SelectSingleNode("code").InnerText == nodeName)
                    {
                        minval = item.SelectSingleNode("minvalue").InnerText;
                        maxval = item.SelectSingleNode("maxvalue").InnerText;
                        break;
                    }
                }
            }

            decimal oDec = 0;
            if (decimal.TryParse(value, out oDec))
            {
                if (!string.IsNullOrEmpty(minval))
                {
                    if (oDec < decimal.Parse(minval))
                    {
                        str = "2";
                    }
                }

                if (!string.IsNullOrEmpty(maxval))
                {
                    if (oDec > decimal.Parse(maxval))
                    {
                        str = "2";
                    }
                }
            }

            return str;
        }

        /// <summary>
        /// 上传判断尿常规是否正常
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GetNcg(string value)
        {
            string str = "";

            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            if (value == "-")
            {
                str = "1";
            }
            else
            {
                str = "2";
            }

            return str;
        }

        private string getInputName(string flag, string html, string index = "1", string endflag = "<input", string className = "")
        {
            string tmp = HtmlHelper.GetTagValue(html, flag, endflag);
            var doc = HtmlHelper.GetHtmlDocument(tmp);

            HtmlNode node = null;

            if (!string.IsNullOrEmpty(className))
            {
                var nodes = doc.DocumentNode.SelectNodes("//input[@class='" + className + "']");
                if (nodes != null && nodes.Count > 0)
                {
                    try
                    {
                        node = nodes[int.Parse(index) - 1];
                    }
                    catch (Exception ex)
                    {
                    }

                }
            }
            else
            {
                node = doc.DocumentNode.SelectSingleNode("//input[@onblur][" + index + "]");
            }

            string text = node == null || !node.Attributes.Contains("name") ? "" : node.Attributes["name"].Value;

            return text;
        }

        private string getRadioName(string flag, string html, string index = "1", string endflag = "</tr>")
        {
            string tmp = HtmlHelper.GetTagValue(html, flag, endflag);
            var doc = HtmlHelper.GetHtmlDocument(tmp);
            var node = doc.DocumentNode.SelectSingleNode("//input[@type='radio'][" + index + "]");
            string id = node == null || !node.Attributes.Contains("name") ? "" : node.Attributes["name"].Value;

            return id;
        }

        private string getCheckBoxName(string flag, string html, string index = "1", string endflag = "</tr>")
        {
            string tmp = HtmlHelper.GetTagValue(html, flag, endflag);
            var doc = HtmlHelper.GetHtmlDocument(tmp);
            var node = doc.DocumentNode.SelectSingleNode("//input[@type='checkbox'][" + index + "]");
            string id = node == null || !node.Attributes.Contains("name") ? "" : node.Attributes["name"].Value;

            return id;
        }
    }
}
