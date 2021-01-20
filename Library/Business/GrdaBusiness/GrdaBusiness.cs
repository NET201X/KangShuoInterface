using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Data;
using Model.InfoModel;
using DAL;
using Utilities.Common;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Web;
using System.Collections;
using System.Text.RegularExpressions;

namespace GrdaBusiness
{
    public class GrdaBusiness
    {
        #region 系统参数
        string baseUrl = Config.GetValue("baseUrl");
        string isUpdate = Config.GetValue("isUpdate");
        string isUpdateHzsfz = Config.GetValue("UpdateHzsfz");
        private int MaxtryCount = Convert.ToInt32(Config.GetValue("errorTryCount"));

        private int SleepMilliseconds = Convert.ToInt32(Config.GetValue("sleepMilliseconds"));

        public string Street1 = "";
        public string Village1 = "";
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
        int currentIndex = 1;

        /// <summary>
        /// 每次下载笔数
        /// </summary>
        int pageSize = 15;

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

        #endregion

        public List<Town> townList { set; get; }

        public string loginKey { set; get; }

        public string StartIndex { set; get; }
        public string EndIndex { set; get; }

        #region 人群分类下载条件 2017-05-03添加
        public QueryList querylist { set; get; }
        #endregion

        public bool onlyGr { get; set; }
        public bool onlydah { get; set; }

        public string sdate { get; set; }
        public string edate { get; set; }

        public bool onyEditTel { get; set; }

        string CreateTimeSameTj = Config.GetValue("CreateTimeSameTj");

        /// <summary>
        /// 个人信息下载入口
        /// </summary>
        /// <param name="callback"></param>
        public void DownGrda(params Action<string>[] callbackAll)
        {
            currentIndex = 1;
            TryDownGrda(1, callbackAll);
            GC.Collect();
        }

        /// <summary>
        /// 个人信息下载入口，根据身份证号
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="callback"></param>
        public void DownGrda(string ids, params Action<string>[] callbackAll)
        {
            // 个人回执
            Action<string> callback = callbackAll[0];
            try
            {
                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();

                var idsa = ids.Split(',');
                currentIndex = 1;
                totalRows = idsa.Length;

                foreach (var id in idsa)
                {
                    PersonModel person = cb.GetGrdaByIDCardNo(id, loginKey, SysCookieContainer);

                    if (person == null)
                    {
                        callback("下载-个人基本信息档案..." + currentIndex + "/" + idsa.Length);
                        currentIndex++;
                        continue;
                    }

                    GetGrda(person, 1, callbackAll);

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

        public void SaveGrda(Action<string> callback)
        {
            if (lstUploadData == null)
            {
                callback("EX-无个人信息上传。");
                return;
            }
            currentIndex = 1;
            foreach (DataSet ds in lstUploadData)
            {
                DataTable dt = ds.Tables["ARCHIVE_BASEINFO"];

                if (dt != null && dt.Rows.Count > 0)
                {
                    TrySaveGrda(ds, 1, callback);
                }

                callback("上传-个人基本信息档案..." + currentIndex + "/" + lstUploadData.Count);
                currentIndex++;

                if (baseUrl.Contains("sdcsm_new"))
                {
                    System.Threading.Thread.Sleep((1) * 1000);
                }
            }
        }

        private void TrySaveGrda(DataSet ds, int tryCount, Action<string> callback)
        {
            DataTable dt = ds.Tables["ARCHIVE_BASEINFO"];
            string idcard = dt.Rows[0]["IDCardNo"].ToString();
            string name = dt.Rows[0]["CustomerName"].ToString();
            try
            {
                CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                PersonModel person = cb.GetGrdaByIDCardNo(idcard, loginKey, SysCookieContainer);

                if (person == null)
                {
                    if (!CheckIdcard(idcard))
                    {
                        //add
                        AddGrda(ds, callback);
                    }
                    else
                    {
                        callback("EX-个人档案:身份证[" + idcard + "],姓名[" + name + "]:此身份证号已在其他社区建立档案，不能重复建档！");
                    }
                }
                else
                {
                    if (isUpdate == "1")
                    {
                        //如勾选了只更新电话号码，则更新电话号码，其他栏位不覆盖
                        if (onyEditTel)
                        {
                            UpdateGrdaTel(person, ds, callback);
                        }
                        else
                        {
                            //update
                            UpdateGrda(person, ds, callback);
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
                    callback("EX-个人档案:身份证[" + idcard + "]:上传个人档案信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    TrySaveGrda(ds, tryCount, callback);
                }
                else
                {
                    callback("EX-个人档案:身份证[" + idcard + "],姓名[" + name + "]:上传个人档案信息失败。请确保网路畅通。");
                }
            }
        }

        /// <summary>
        /// 验证身份证号是否重复
        /// </summary>
        /// <param name="idcardno"></param>
        /// <returns></returns>
        private bool CheckIdcard(string idcardno)
        {
            //http://192.168.1.2:9080/sdcsm/dwr/call/plaincall/getCommon.CheckSfzh.dwr
            WebHelper web = new WebHelper();
            StringBuilder postData = new StringBuilder();
            postData.Append("callCount=1\r\n");
            postData.Append("nextReverseAjaxIndex=0\r\n");
            postData.Append("c0-scriptName=getCommon\r\n");
            postData.Append("c0-methodName=CheckSfzh\r\n");
            postData.Append("c0-id=0\r\n");
            postData.Append("c0-param0=string:" + idcardno + "\r\n");
            postData.Append("batchId=1\r\n");
            postData.Append("instanceId=0\r\n");
            postData.Append("page=%2Fsdcsm%2FhealthArchives%2FaddArchivesHz.action%3FaddSign%3D2%26dJtdabh%3D%26tz%3D2\r\n");
            postData.Append("scriptSessionId=UMq$Kga3dXPYlNBJ9aC2if2o70m/Qe7u70m-sq0l9Peab\r\n");

            string returnString = web.PostHttpNoCookie(baseUrl + "dwr/call/plaincall/getCommon.CheckSfzh.dwr", postData.ToString(), "text/plain");

            //            throw 'allowScriptTagRemoting is false.';
            //(function(){
            //var r=window.dwr._[0];
            ////#DWR-INSERT
            ////#DWR-REPLY
            //r.handleCallback("2","0",0);
            //})();
            if (!string.IsNullOrEmpty(returnString))
            {
                string val = HtmlHelper.GetTagValue(returnString, "handleCallback(", ");");
                if (!string.IsNullOrEmpty(val))
                {
                    var lst = val.Split(',');
                    if (lst[2] != "0")
                    {
                        return true;
                    }
                }
            }
            return false;

        }
        /// <summary>
        /// 新增个人档案
        /// </summary>
        /// <param name="ds"></param>
        private void AddGrda(DataSet ds, Action<string> callback)
        {
            DataRow baseInfo = ds.Tables["ARCHIVE_BASEINFO"].Rows[0];
            string idcard = baseInfo["IDCardNo"].ToString();
            string name = baseInfo["CustomerName"].ToString();
            WebHelper web = new WebHelper();
            StringBuilder postData = new StringBuilder();

            //添加请求，获取系统参数
            #region 2017-04-26 添加
            //http://222.132.49.202:9080/sdcsm/healthArchives/addArchivesHz.action?addSign=2&dJtdabh=&tz=2
            string post = "/healthArchives/addArchivesHz.action?addSign=2&dJtdabh=&tz=2";
            string returnString = web.GetHttp(baseUrl + post, "", SysCookieContainer);
            string submitoken = "";

            if (!string.IsNullOrEmpty(returnString))
            {
                var doc = HtmlHelper.GetHtmlDocument(returnString);

                if (doc != null)
                {
                    var node = doc.DocumentNode.SelectSingleNode("//input[@name='submitoken']");

                    submitoken = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                }
            }
            #endregion

            #region

            //postData.Append("etjbxx.fcyyid=").Append("");
            //postData.Append("&shi=").Append("");
            //postData.Append("&xian=").Append("");
            //postData.Append("&yiyuan=").Append("");
            //postData.Append("&etjbxx.eFqxm=").Append("");
            //postData.Append("&etjbxx.eFqgzdw=").Append("");
            //postData.Append("&etjbxx.eFqlxdh=").Append("");
            //postData.Append("&etjbxx.eFqcsrq=").Append("");
            //postData.Append("&etjbxx.eMqxm=").Append("");
            //postData.Append("&etjbxx.eMqgzdw=").Append("");
            //postData.Append("&etjbxx.eMqlxdh=").Append("");
            //postData.Append("&etjbxx.eMqcsrq=").Append("");
            //postData.Append("&etjbxx.eYzbm=").Append("");
            //postData.Append("&etjbxx.eMqsfzh=").Append("");
            //postData.Append("&etjbxx.eJddw=").Append("");
            //postData.Append("&etjbxx.eJddwdh=").Append("");
            //postData.Append("&etjbxx.eTrjg=").Append("");
            //postData.Append("&etjbxx.eTrjgdh=").Append("");
            //postData.Append("&yuninfo.fcyyid=").Append("");
            //postData.Append("&yuninfoshi=").Append("");
            //postData.Append("&yuninfoxian=").Append("");
            //postData.Append("&yuninfoyiyuan=").Append("");
            //postData.Append("&yuninfo.hkszd=").Append("");
            //postData.Append("&yuninfo.hklxdh=").Append("");
            //postData.Append("&yuninfo.chxyd=").Append("");
            //postData.Append("&yuninfo.chphone=").Append("");
            //postData.Append("&yuninfo.workphone=").Append("");
            //postData.Append("&yuninfo.hubName=").Append("");
            //postData.Append("&yuninfo.hubAge=").Append("");
            //postData.Append("&yuninfo.hubWhcd=").Append("");
            //postData.Append("&yuninfo.hubMz=").Append("");
            //postData.Append("&yuninfo.hubGzdw=").Append("");
            //postData.Append("&yuninfo.hubPhone=").Append("");
            //postData.Append("&yuninfo.hubZy=").Append("");

            postData.Append("&dWzd=").Append("");//完整度
            postData.Append("&qdqxz=").Append("");//缺项

            postData.Append("&dJtdabh=").Append("");//家庭编号？

            postData.Append("&dYhzgx=").Append("1");//与户主关系
            postData.Append("&dDazt=").Append("1");//档案状态
            postData.Append("&dDaztyy=").Append("");
            postData.Append("&dXm=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["CustomerName"] + "")); //姓名
            postData.Append("&dXb=").Append(baseInfo["Sex"]);//性别
            postData.Append("&dZjlx=").Append("1");//证件类型
            postData.Append("&dSfzh=").Append(idcard);//身份证号
            postData.Append("&dZjhqt=").Append("");
            postData.Append("&dCsrq=").Append(baseInfo["Birthday"] == null || baseInfo["Birthday"].ToString() == "" ? "" : Convert.ToDateTime(baseInfo["Birthday"]).ToString("yyyy-MM-dd"));//出生日期
            postData.Append("&dLxdh=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Phone"] + ""));//本人电话
            postData.Append("&dGzdw=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["WorkUnit"] + "")); //工作单位
            postData.Append("&dLxrdh=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["ContactPhone"] + ""));//联系人电话
            postData.Append("&dLxrxm=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["ContactName"] + ""));//联系人姓名
            object tem = baseInfo["LiveType"];
            postData.Append("&dJzzk=").Append(tem == null || tem.ToString() == "2" ? "4" : tem); //常住类型
            //民族
            tem = baseInfo["Minority"];
            postData.Append("&dMz=").Append(GetWebNationCodeByPadNationName1(tem == null ? "" : tem.ToString()));//民族
            //血型
            tem = baseInfo["BloodType"];
            postData.Append("&jkzk.dXx=").Append(GetWebBooldTypeCodeByPadCode(tem == null ? "" : tem.ToString()));
            //RH 
            postData.Append("&jkzk.dSfrhyx=").Append(baseInfo["RH"]);
            //职业
            tem = baseInfo["Job"];
            postData.Append("&dZy=").Append(GetWebJobCodeByPadCode(tem == null ? "" : tem.ToString()));
            //文化程度
            tem = baseInfo["Culture"];
            postData.Append("&dWhcd=").Append(GetWebCultureCodeByPadCode(tem == null ? "" : tem.ToString()));
            postData.Append("&shxg.dLdqd=").Append("");//劳动强度

            //婚姻状况
            tem = baseInfo["MaritalStatus"];
            postData.Append("&dHyzk=").Append(GetWebMaritalCodeByPadCode(tem == null ? "" : tem.ToString()));

            //医疗费用支付方式
            tem = baseInfo["MedicalPayType"];

            string czybk = baseInfo["TownMedicalCard"].ToString();
            string jmybk = baseInfo["ResidentMedicalCard"].ToString();
            string pkybk = baseInfo["PovertyReliefMedicalCard"].ToString();
            if (tem != null)
            {
                var Strs = tem.ToString().Split(',');
                foreach (var code in Strs)
                {
                    postData.Append("&dYlfzflx=").Append(GetWebMedicalPayTypeCodesByPadCode(code));
                }
            }

            //城镇医保卡号
            postData.Append("&dYlbxh=").Append(czybk);//医疗保险号

            //居民医保卡号
            postData.Append("&dXnhh=").Append(jmybk);//新农合号

            //贫困救助卡号
            postData.Append("&pkjzkh=").Append(pkybk);

            postData.Append("&dYlfzflxqt=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["MedicalPayTypeOther"] + ""));//其他支付方式

            postData.Append("&dSheng=").Append(loginKey.Substring(0, 2));//省######
            postData.Append("&dShi=").Append(loginKey.Substring(0, 4));//市#####
            postData.Append("&dQu=").Append(loginKey.Substring(0, 6));//区###

            if (Village1 != "" && Street1 != "")
            {
                postData.Append("&dJd=").Append(Street1);//乡镇街道
                postData.Append("&dJwh=").Append(Village1);//村委会            
            }
            else
            {
                var townName = baseInfo["TownName"].ToString();
                var villName = baseInfo["VillageName"].ToString();

                Town town = townList[0];
                List<Village> villageList = null;

                if (townName != "")
                {
                    town = (Town)townList.Where(c => c.text.ToString().Contains(townName.ToString().Substring(0, 2))).FirstOrDefault();
                }

                Village village = null;

                if (town != null)
                {
                    postData.Append("&dJd=").Append(town.code);//乡镇街道

                    CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();
                    villageList = cb.GetVillageList(town.code.ToString(), loginKey, SysCookieContainer);

                    if (villageList != null && villageList.Count > 0)
                    {
                        village = (from vi in villageList
                                   where vi.text.ToString().Contains(villName.ToString().Substring(0, 2))
                                   select vi).FirstOrDefault();

                        if (village != null)
                        {
                            postData.Append("&dJwh=").Append(village.code);//村委会
                        }
                        else
                        {
                            postData.Append("&dJwh=").Append(villageList[0].code);//村委会
                        }
                    }
                }
            }

            postData.Append("&dXxdz=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Address"] + ""));//详细地址
            postData.Append("&dSspq=").Append("");//所属片区
            postData.Append("&dDalb=").Append("2");//档案类别：1城镇 ；2：农村##

            //药物过敏史
            tem = baseInfo["DrugAllergic"];
            string temStr = "1";
            if (tem != null)
            {
                var ss = tem.ToString().Split(',');
                if (ss.Count() > 0 && ss.Contains("1"))
                {
                    temStr = "2";
                }
            }
            //过敏史选项
            //postData.Append("&jkzk.dGms=").Append("2");

            postData.Append("&jkzk.dGms=").Append(temStr);
            if (temStr != "2")
            {
                var strS = tem.ToString().Split(',');
                foreach (var s in strS)
                {
                    if (s == "1") continue;
                    postData.Append("&jkzk.dYgms=").Append(GetWebDrugAllergic(s));
                }
                if (strS.Contains("5"))
                {
                    postData.Append("&jkzk.dGmsqt=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["DrugAllergicOther"] + ""));

                }
            }

            postData.Append("&jkzk.dYwjws=").Append("1");//？1
            //？ jb0

            DataTable jiwangDT = ds.Tables["ARCHIVE_ILLNESSHISTORYINFO"];

            DataRow[] temRows = jiwangDT.Select("IllnessType=1");
            //postData.Append("&isjb=").Append("2");

            string strJbs = "2";

            if (temRows != null && temRows.Count() > 0)
            {
                int i = 0;
                string strjb = "";
                foreach (var row in temRows)
                {
                    if (row["IllnessName"].ToString().Contains(",") || row["IllnessName"].ToString().Trim() == "1")
                    {
                        continue;
                    }
                    strJbs = "1";

                    strjb += ",jb" + i;

                    //既往史疾病
                    postData.Append("&jb").Append(i).Append("=").Append(row["IllnessName"].ToString());
                    //肿瘤  
                    postData.Append("&exzl=").Append(HtmlHelper.GetUrlEncodeVal(row["Therioma"].ToString()));
                    //职业病
                    postData.Append("&zybqt=").Append(HtmlHelper.GetUrlEncodeVal(row["JobIllness"].ToString()));
                    //其他
                    postData.Append("&jbqt=").Append(HtmlHelper.GetUrlEncodeVal(row["IllnessOther"].ToString()));
                    string stmp = row["DiagnoseTime"].ToString() == "" ? "" : Convert.ToDateTime(row["DiagnoseTime"]).ToString("yyyy-MM");
                    postData.Append("&zdrq=").Append(stmp);

                    i++;
                }

                postData.Append("&jbgrhidden=").Append(HtmlHelper.GetUrlEncodeVal(strjb.TrimStart(',')));
            }
            else
            {
                postData.Append("&jbgrhidden=").Append("jb0");
                postData.Append("&exzl=");//肿瘤
                postData.Append("&zybqt=");//职业病
                postData.Append("&jbqt=");//疾病其他
            }

            postData.Append("&isjb=").Append(strJbs);//既往史疾病，有无

            temRows = jiwangDT.Select("IllnessType=2");
            //既往史手术有无
            // postData.Append("&isshsh=").Append("1");

            postData.Append("&isshsh=").Append(temRows != null && temRows.Count() > 0 ? "2" : "1");
            if (temRows != null && temRows.Count() > 0)
            {
                foreach (var r in temRows)
                {
                    postData.Append("&ssmc=").Append(HtmlHelper.GetUrlEncodeVal(r["IllnessNameOther"] + ""));
                    postData.Append("&ssrq=").Append(r["DiagnoseTime"] == null ? "" : Convert.ToDateTime(r["DiagnoseTime"]).ToString("yyy-MM-dd"));
                }
            }

            //外伤
            temRows = jiwangDT.Select("IllnessType=3");
            // postData.Append("&iswsh=").Append("1");

            postData.Append("&iswsh=").Append(temRows != null && temRows.Count() > 0 ? "2" : "1");
            if (temRows != null && temRows.Count() > 0)
            {
                foreach (var r in temRows)
                {
                    postData.Append("&wsmc=").Append(HtmlHelper.GetUrlEncodeVal(r["IllnessNameOther"] + ""));
                    postData.Append("&wsrq=").Append(r["DiagnoseTime"] == null ? "" : Convert.ToDateTime(r["DiagnoseTime"]).ToString("yyy-MM-dd"));
                }
            }

            //输血
            temRows = jiwangDT.Select("IllnessType=4");
            // postData.Append("&isshx=").Append("1");

            postData.Append("&isshx=").Append(temRows != null && temRows.Count() > 0 ? "2" : "1");
            if (temRows != null && temRows.Count() > 0)
            {
                foreach (var r in temRows)
                {
                    postData.Append("&sxmc=").Append(HtmlHelper.GetUrlEncodeVal(r["IllnessNameOther"] + ""));
                    postData.Append("&sxrq=").Append(r["DiagnoseTime"] == null ? "" : Convert.ToDateTime(r["DiagnoseTime"]).ToString("yyy-MM-dd"));
                }
            }

            DataTable familyDT = ds.Tables["ARCHIVE_FAMILYHISTORYINFO"];
            string str2 = "";
            if (familyDT != null && familyDT.Rows.Count > 0)
            {
                string strtmpDT = "";

                int index = 0;
                DataRow familyDR = familyDT.Rows[0];
                tem = familyDR["FatherHistory"];
                if (!string.IsNullOrEmpty(tem.ToString().Trim(',')) && tem.ToString() != "1")
                {
                    foreach (var n in tem.ToString().Split(','))
                    {
                        postData.Append("&jzsjbmc" + index + "=").Append(n != "12" ? (Convert.ToInt32(n) - 1).ToString() : "100");//其他

                    }
                    str2 += "jzsjbmc" + index.ToString() + ",";
                    postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["FatherHistoryOther"] + ""));//其他
                    postData.Append("&jzsjzcy=").Append("3");//关系 
                    index++;

                    strtmpDT = "1";
                }
                tem = familyDR["MotherHistory"];
                if (!string.IsNullOrEmpty(tem.ToString().Trim(',')) && tem.ToString() != "1")
                {
                    foreach (var n in tem.ToString().Split(','))
                    {
                        postData.Append("&jzsjbmc" + index + "=").Append(n != "12" ? (Convert.ToInt32(n) - 1).ToString() : "100");//其他

                    }
                    str2 += "jzsjbmc" + index.ToString() + ",";
                    postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["MotherHistoryOther"] + ""));//其他
                    postData.Append("&jzsjzcy=").Append("4");//关系 
                    index++;

                    strtmpDT = "1";
                }
                tem = familyDR["BrotherSisterHistory"];
                if (!string.IsNullOrEmpty(tem.ToString().Trim(',')) && tem.ToString() != "1")
                {

                    foreach (var n in tem.ToString().Split(','))
                    {
                        postData.Append("&jzsjbmc" + index + "=").Append(n != "12" ? (Convert.ToInt32(n) - 1).ToString() : "100");//其他

                    }
                    str2 += "jzsjbmc" + index.ToString() + ",";
                    postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["BrotherSisterHistoryOther"] + ""));//其他
                    postData.Append("&jzsjzcy=").Append("5");//关系 

                    index++;

                    strtmpDT = "1";
                }
                tem = familyDR["ChildrenHistory"];
                if (!string.IsNullOrEmpty(tem.ToString().Trim(',')) && tem.ToString() != "1")
                {

                    foreach (var n in tem.ToString().Split(','))
                    {
                        postData.Append("&jzsjbmc" + index + "=").Append(n != "12" ? (Convert.ToInt32(n) - 1).ToString() : "100");//其他

                    }
                    str2 += "jzsjbmc" + index.ToString() + ",";
                    postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["ChildrenHistoryOther"] + ""));//其他
                    postData.Append("&jzsjzcy=").Append("6");//关系 

                    index++;

                    strtmpDT = "1";
                }

                if (strtmpDT == "")
                {
                    postData.Append("&jkzkjzsqt=");
                    postData.Append("&Jkzk.dYwjb=").Append("2");//家族史有无
                }
                else
                {
                    postData.Append("&Jkzk.dYwjb=").Append("1");
                }
            }
            else
            {
                postData.Append("&jkzkjzsqt=");
                postData.Append("&Jkzk.dYwjb=").Append("2");//家族史有无
            }

            // postData.Append("&grhidden=").Append("jzsjbmc0");//?
            postData.Append("&grhidden=").Append(str2.TrimEnd(','));//20170206  hry 
            postData.Append("&rowNumber=").Append("");

            //暴露史有无
            tem = baseInfo["Exposure"];
            temStr = "2";
            if (!string.IsNullOrEmpty(tem.ToString()) && !tem.ToString().Contains("1"))
            {
                temStr = "1";
            }
            postData.Append("&jkzk.dBls=").Append(temStr);
            //化学品
            postData.Append("&jkzk.dBlshxp=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Chemical"].ToString()));
            //毒物
            postData.Append("&jkzk.dBlsdw=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Poison"].ToString()));
            //射线
            postData.Append("&jkzk.dBlssx=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Radial"].ToString()));

            //遗传病史
            tem = baseInfo["Disease"];
            postData.Append("&jkzk.dYcbs=").Append(!string.IsNullOrEmpty(tem.ToString()) && tem.ToString() == "1" ? "2" : "1");
            //遗传病史其他
            if (!string.IsNullOrEmpty(tem.ToString()) && !tem.ToString().Contains("1"))
            {
                postData.Append("&jkzk.dYcbsjb=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["DiseaseEx"] + ""));
            }
            //残疾情况
            tem = baseInfo["DiseasEndition"];
            // postData.Append("&jkzk.dYwcj=").Append("2");

            postData.Append("&jkzk.dYwcj=").Append(string.IsNullOrEmpty(tem.ToString()) || tem.ToString().Contains("1") ? "2" : "1");
            //残疾选项
            if (!string.IsNullOrEmpty(tem.ToString()) && !tem.ToString().Contains("1"))
            {
                var strS = tem.ToString().Split(',');
                foreach (var n in strS)
                {
                    postData.Append("&jkzk.dCjmz=").Append(GetWebDiseasenditionCodeByPadCode(n));
                }
                if (strS.Contains("8"))
                {
                    postData.Append("&jkzk.dCjqt=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["DiseasenditionEx"] + ""));
                }
            }
            //生活环境
            DataTable huanjingDT = ds.Tables["ARCHIVE_BASEINFOARCHIVE_ENVIRONMENT"];

            if (huanjingDT != null && huanjingDT.Rows.Count > 0)
            {
                DataRow hjDR = huanjingDT.Rows[0];
                //cfpqsb 厨房
                postData.Append("&cfpqsb=").Append(hjDR["BlowMeasure"]);
                //rllx  燃料
                postData.Append("&rllx=").Append(hjDR["FuelType"]);
                //ys 饮水
                postData.Append("&ys=").Append(hjDR["DrinkWater"]);
                //cs 厕所
                postData.Append("&cs=").Append(hjDR["Toilet"]);
                //qxl 禽畜
                string LiveStockRail = hjDR["LiveStockRail"].ToString();
                postData.Append("&qxl=").Append(getQcl(LiveStockRail));
            }

            //签字
            postData.Append("&fkqzbr=");
            postData.Append("&fkqzjs=");
            postData.Append("&sdfkqzbr=").Append(CommonExtensions.GetUrlEncodeVal(baseInfo["CustomerName"].ToString()));
            postData.Append("&sdfkqzjs=").Append("");
            postData.Append("&fktime=").Append(CommonExtensions.GetUrlEncodeVal(DateTime.Now.ToString("yyyy年MM月dd日")));

            //调查时间
            postData.Append("&happentime=").Append(Convert.ToDateTime(baseInfo["CreateDate"]).ToString("yyyy-MM-dd"));

            string createtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (!string.IsNullOrEmpty(CreateTimeSameTj) && CreateTimeSameTj == "1")
            {
                createtime = Convert.ToDateTime(Convert.ToDateTime(baseInfo["CreateDate"].ToString()).ToString("yyyy-MM-dd") + " " + DateTime.Now.AddMinutes(-15).ToString("HH:mm:ss")).ToString("yyyy-MM-dd HH:mm:ss");
            }
            postData.Append("&createtime=").Append(createtime);
            postData.Append("&createuser=").Append(loginKey);
            postData.Append("&updatetime=").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            postData.Append("&updateuser=").Append(loginKey);

            if (loginKey.Length == 16)
            {
                postData.Append("&pRgid=").Append(loginKey.Substring(0, 12));
            }
            else
            {
                postData.Append("&pRgid=").Append(loginKey.Substring(0, 15));
            }
            postData.Append("&dzrys=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Doctor"].ToString()));
            #endregion

            #region 2017-04-26 添加
            postData.Append("&dZzbh=");
            //postData.Append("&dSfzdz=");
            postData.Append("&submitoken=").Append(CommonExtensions.GetUrlEncodeVal(submitoken));
            #endregion

            #region 2020-12-03 修改 家庭情况和健康卡信息
            //户主姓名
            postData.Append("&hzxm=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["HouseName"].ToString()));
            postData.Append("&hzsfzh=").Append(baseInfo["FamilyIDCardNo"].ToString());
            postData.Append("&jtrks=").Append(baseInfo["FamilyNum"].ToString());
            postData.Append("&jtjg=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["FamilyStructure"].ToString()));

            string live = baseInfo["LiveCondition"].ToString();

            postData.Append("&tgrjbxjzqk=").Append(live.Replace("0", ""));

            //孕产情况
            string PreSituation = baseInfo["PreSituation"].ToString();
            string PreNum = baseInfo["PreNum"].ToString();
            string YieldNum = baseInfo["YieldNum"].ToString();
            if (!string.IsNullOrEmpty(PreSituation))
            {
                postData.Append("&lHyqk=").Append(CommonExtensions.GetUrlEncodeVal(GetPreSituationByPadCode(PreSituation)));
                postData.Append("&lYc=").Append(PreNum);
                postData.Append("&lCc=").Append(YieldNum);
            }
            //户籍地址
            string HouseHoldAddress = baseInfo["HouseHoldAddress"].ToString();

            postData.Append("&dSfzdz=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["HouseHoldAddress"].ToString()));

            //居民健康档案信息卡
            DataTable dt = ds.Tables["archive_health_info"];
            string Prevalence = "";
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow health = ds.Tables["archive_health_info"].Rows[0];
                Prevalence = health["Prevalence"].ToString();
                if (!string.IsNullOrEmpty(Prevalence))
                {
                    var tmp = Prevalence.Split(',');
                    foreach (var item in tmp)
                    {
                        postData.Append("&hbqk=").Append(item);
                    }
                }
                postData.Append("&hbqkqt=").Append(HtmlHelper.GetUrlEncodeVal(health["PrevalenceOther"].ToString()));
                postData.Append("&jdjglxdh=").Append(health["OrgTelphone"].ToString());
                postData.Append("&jtzrys=").Append(HtmlHelper.GetUrlEncodeVal(health["FamilyDoctor"].ToString()));
                postData.Append("&jtzryslxdh=").Append(health["FamilyDoctorTel"].ToString());
                postData.Append("&sqzrhs=").Append(HtmlHelper.GetUrlEncodeVal(health["Nurses"].ToString()));
                postData.Append("&sqzrhslxdh=").Append(health["NursesTel"].ToString());
                postData.Append("&ggwsry=").Append(HtmlHelper.GetUrlEncodeVal(health["HealthPersonnel"].ToString()));
                postData.Append("&ggwsrylxdh=").Append(health["HealthPersonnelTel"].ToString());
                postData.Append("&qtsm=").Append(HtmlHelper.GetUrlEncodeVal(health["Others"].ToString()));
            }
            else
            {
                postData.Append("&hbqk=");
                postData.Append("&hbqkqt=");
                postData.Append("&jdjglxdh=");
                postData.Append("&jtzrys=");
                postData.Append("&jtzryslxdh=");
                postData.Append("&sqzrhs=");
                postData.Append("&sqzrhslxdh=");
                postData.Append("&ggwsry=");
                postData.Append("&ggwsrylxdh=");
                postData.Append("&qtsm=");
            }

            #endregion

            returnString = web.PostHttp(baseUrl + "/healthArchives/addition.action", postData.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            HtmlDocument d = HtmlHelper.GetHtmlDocument(returnString);
            if (d != null && d.DocumentNode.SelectSingleNode("//title") != null
                && d.DocumentNode.SelectSingleNode("//title").InnerText.Contains("500")
                )
            {
                CommonExtensions.WriteLog(returnString);
                callback("EX-个人档案信息：身份证号[ " + idcard + "],姓名[" + name + "] 个人档案新增失败，服务器错误！");
            }
        }

        /// <summary>
        /// 更新个人档案
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userKey"></param>
        private void UpdateGrda(PersonModel person, DataSet ds, Action<string> callback)
        {
            WebHelper web = new WebHelper();
            string idcard = person.idNumber;
            //string post = "dah=" + person.pid + "&tz=2";
            //string returnString = web.PostHttp(baseUrl + "/healthArchives/updateArchives.action", post, "application/x-www-form-urlencoded", SysCookieContainer);

            //http://20.1.1.73:9081/sdcsm/healthArchives/updateArchives.action?dah=371482040010060701&tz=1&zdgllx=
            string returnString = web.GetHttp(baseUrl + "/healthArchives/updateArchives.action?dah=" + person.pid + "&tz=1&zdgllx=", "", SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc != null)
            {
                DataRow baseInfo = ds.Tables["ARCHIVE_BASEINFO"].Rows[0];
                StringBuilder postData = new StringBuilder();

                #region

                postData.Append("dGrdabh=").Append(person.pid);
                var node = doc.DocumentNode.SelectSingleNode("//input[@name='dJtdabh']");
                postData.Append("&dJtdabh=").Append(node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGrdabhshow']");
                postData.Append("&dGrdabhshow=").Append(node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGrdabh17']");
                postData.Append("&dGrdabh17=").Append(node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dPyjm']");
                postData.Append("&dPyjm=").Append(node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dWzd']");
                postData.Append("&dWzd=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "100");
                node = doc.DocumentNode.SelectSingleNode("//input[@id='qdqxz']");
                postData.Append("&qdqxz=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "0");//？
                //与户主关系  待家庭成员信息修改时，更新
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dYhzgx']/option[@selected]");
                postData.Append("&dYhzgx=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value.Replace("0", "1") : "1");
                //档案状态
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dDazt'][@checked]");
                //postData.Append("&dDazt=").Append("2");
                postData.Append("&dDazt=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //档案状态-缘由
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dDaztyy']/option[@selected]");
                postData.Append("&dDaztyy=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//input[@id='dJlrq']");
                postData.Append("&dJlrq=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                postData.Append("&dJlrqs=").Append("");

                node = doc.DocumentNode.SelectSingleNode("//input[@id='dFhdyy']");
                postData.Append("&dFhdyy=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//input[@id='dFhdyys']");
                postData.Append("&dFhdyys=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                postData.Append("&dXm=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["CustomerName"] + "")); //姓名
                postData.Append("&dXb=").Append(baseInfo["Sex"]);//性别
                postData.Append("&dZjlx=").Append("1");//证件类型
                postData.Append("&dSfzh=").Append(idcard);//身份证号
                //证据编号
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dZjhqt']");
                postData.Append("&dZjhqt=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //出生日期
                postData.Append("&dCsrq=").Append(baseInfo["Birthday"] == null ? "" : Convert.ToDateTime(baseInfo["Birthday"]).ToString("yyyy-MM-dd"));//出生日期
                postData.Append("&dLxdh=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Phone"].ToString()));//本人电话
                postData.Append("&dGzdw=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["WorkUnit"] + "")); //工作单位
                postData.Append("&dLxrdh=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["ContactPhone"] + ""));//联系人电话
                postData.Append("&dLxrxm=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["ContactName"] + ""));//联系人姓名
                object tem = baseInfo["LiveType"];
                postData.Append("&dJzzk=").Append(tem == null || tem.ToString() == "2" ? "4" : tem); //常住类型
                //民族
                tem = baseInfo["Minority"];
                postData.Append("&dMz=").Append(GetWebNationCodeByPadNationName(tem == null ? "" : tem.ToString()));//民族
                //血型
                tem = baseInfo["BloodType"];
                postData.Append("&jkzk.dXx=").Append(GetWebBooldTypeCodeByPadCode(tem == null ? "" : tem.ToString()));
                //RH 
                postData.Append("&jkzk.dSfrhyx=").Append(baseInfo["RH"].ToString());

                //备用电话
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dbydh']");
                postData.Append("&dbydh=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //职业
                tem = baseInfo["Job"];
                postData.Append("&dZy=").Append(GetWebJobCodeByPadCode(tem == null ? "" : tem.ToString()));
                //文化程度
                tem = baseInfo["Culture"];
                postData.Append("&dWhcd=").Append(GetWebCultureCodeByPadCode(tem == null ? "" : tem.ToString()));
                //劳动强度
                node = doc.DocumentNode.SelectSingleNode("//select[@id='shxg.dLdqd']/option[@selected]");
                postData.Append("&shxg.dLdqd=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//劳动强度

                //婚姻状况
                tem = baseInfo["MaritalStatus"];
                postData.Append("&dHyzk=").Append(GetWebMaritalCodeByPadCode(tem == null ? "" : tem.ToString()));
                //医疗费用支付方式
                tem = baseInfo["MedicalPayType"];

                string czybk = baseInfo["TownMedicalCard"].ToString();
                string jmybk = baseInfo["ResidentMedicalCard"].ToString();
                string pkybk = baseInfo["PovertyReliefMedicalCard"].ToString();
                if (tem != null)
                {
                    var Strs = tem.ToString().Split(',');
                    foreach (var code in Strs)
                    {
                        postData.Append("&dYlfzflx=").Append(GetWebMedicalPayTypeCodesByPadCode(code));
                    }
                }

                //城镇医保卡号
                postData.Append("&dYlbxh=").Append(czybk);

                //居民医保卡号
                postData.Append("&dXnhh=").Append(jmybk);

                //贫困救助卡号
                postData.Append("&pkjzkh=").Append(pkybk);

                postData.Append("&dYlfzflxqt=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["MedicalPayTypeOther"] + ""));//其他支付方式


                postData.Append("&dSheng=").Append(loginKey.Substring(0, 2));//省######
                postData.Append("&dShi=").Append(loginKey.Substring(0, 4));//市#####

                node = doc.DocumentNode.SelectSingleNode("//select[@name='dQu']/option[@selected]");
                postData.Append("&dQu=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//区###

                node = doc.DocumentNode.SelectSingleNode("//select[@name='dJd']/option[@selected]");
                postData.Append("&dJd=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//select[@name='dJwh']/option[@selected]");
                postData.Append("&dJwh=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                //if (Village1 != "" && Street1 != "")
                //{
                //    postData.Append("&dJd=").Append(Street1);//乡镇街道
                //    postData.Append("&dJwh=").Append(Village1);//村委会            
                //}
                //else
                //{
                //    var townName = baseInfo["TownName"].ToString();
                //    var villName = baseInfo["VillageName"].ToString();

                //    Town town = townList[0];


                //    if (townName != "")
                //    {
                //        town = (Town)townList.Where(c => c.text.ToString().Contains(townName.ToString().Substring(0, 2))).FirstOrDefault();
                //    }

                //    Village village = null;

                //    if (town != null && town.villageList.Count > 0)
                //    {
                //        village = (from vi in town.villageList
                //                   where vi.text.ToString().Contains(villName.ToString().Substring(0, 2))
                //                   select vi).FirstOrDefault();
                //    }

                //    if (town != null)
                //    {
                //        postData.Append("&dJd=").Append(town.code);//乡镇街道
                //    }

                //    if (village != null)
                //    {
                //        postData.Append("&dJwh=").Append(village.code);//村委会
                //    }
                //    else
                //    {
                //        if (town != null && town.villageList != null)
                //        {
                //            postData.Append("&dJwh=").Append(town.villageList[0].code);//村委会
                //        }
                //    }
                //}

                postData.Append("&dXxdz=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Address"] + ""));//详细地址
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dSspq']/option[@selected]");
                //postData.Append("&dSspq=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//所属片区

                node = doc.DocumentNode.SelectSingleNode("//input[@id='dZzbh']");
                postData.Append("&dZzbh=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//select[@id='dDalb']/option[@selected]");
                postData.Append("&dDalb=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//档案类别：1城镇 ；2：农村##


                //药物过敏史
                tem = baseInfo["DrugAllergic"];
                string temStr = "1";
                if (tem != null)
                {
                    var ss = tem.ToString().Split(',');
                    if (ss.Count() > 0 && ss.Contains("1"))
                    {
                        temStr = "2";
                    }
                }
                //过敏史选项
                // postData.Append("&jkzk.dGms=").Append(temStr);

                postData.Append("&jkzk.dGms=").Append(temStr);
                if (temStr != "2")
                {
                    var strS = tem.ToString().Split(',');
                    foreach (var s in strS)
                    {
                        if (s == "1") continue;
                        postData.Append("&jkzk.dYgms=").Append(GetWebDrugAllergic(s));
                    }
                    if (strS.Contains("5"))
                    {
                        postData.Append("&jkzk.dGmsqt=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["DrugAllergicOther"] + ""));

                    }
                }
                //既往史
                node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dYwjws']");
                postData.Append("&jkzk.dYwjws=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//？1
                node = doc.DocumentNode.SelectSingleNode("//input[@id='jbhiddenyjgjzs']");
                postData.Append("&jbhiddenyjgjzs=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//？ jb0

                DataTable jiwangDT = ds.Tables["ARCHIVE_ILLNESSHISTORYINFO"];

                DataRow[] temRows = jiwangDT.Select("IllnessType=1");
                // postData.Append("&isjb=").Append("1");

                string strJbs = "2";
                string strjb = "";
                if (temRows != null && temRows.Count() > 0)
                {
                    int i = 0;

                    foreach (var row in temRows)
                    {
                        if (row["IllnessName"].ToString().Contains(",") || row["IllnessName"].ToString().Trim() == "1")
                        {
                            continue;
                        }
                        strJbs = "1";

                        strjb += ",jb" + i;

                        //既往史疾病
                        postData.Append("&jb").Append(i).Append("=").Append(row["IllnessName"].ToString());
                        //肿瘤  
                        postData.Append("&exzl=").Append(HtmlHelper.GetUrlEncodeVal(row["Therioma"].ToString()));
                        //职业病
                        postData.Append("&zybqt=").Append(HtmlHelper.GetUrlEncodeVal(row["JobIllness"].ToString()));
                        //其他
                        postData.Append("&jbqt=").Append(HtmlHelper.GetUrlEncodeVal(row["IllnessOther"].ToString()));
                        string stmp = row["DiagnoseTime"].ToString() == "" ? "" : Convert.ToDateTime(row["DiagnoseTime"]).ToString("yyyy-MM");
                        postData.Append("&zdrq=").Append(stmp);

                        i++;
                    }

                    postData.Append("&jbgrhidden=").Append(HtmlHelper.GetUrlEncodeVal(strjb.TrimStart(',')));
                }
                if (string.IsNullOrEmpty(strjb.TrimStart(',')))
                {
                    postData.Append("&jbgrhidden=").Append("jb0");
                    postData.Append("&exzl=");//肿瘤
                    postData.Append("&zybqt=");//职业病
                    postData.Append("&jbqt=");//疾病其他
                }
                //else
                //{
                //    postData.Append("&jbgrhidden=").Append("jb0");
                //    postData.Append("&exzl=");//肿瘤
                //    postData.Append("&zybqt=");//职业病
                //    postData.Append("&jbqt=");//疾病其他
                //}

                postData.Append("&isjb=").Append(strJbs);//既往史疾病，有无

                temRows = jiwangDT.Select("IllnessType=2");
                //既往史手术有无
                // postData.Append("&isshsh=").Append("1");

                postData.Append("&isshsh=").Append(temRows != null && temRows.Count() > 0 ? "2" : "1");
                if (temRows != null && temRows.Count() > 0)
                {
                    foreach (var r in temRows)
                    {
                        postData.Append("&ssmc=").Append(HtmlHelper.GetUrlEncodeVal(r["IllnessNameOther"] + ""));
                        postData.Append("&ssrq=").Append(r["DiagnoseTime"] == null ? "" : Convert.ToDateTime(r["DiagnoseTime"]).ToString("yyy-MM-dd"));
                    }
                }

                //外伤
                temRows = jiwangDT.Select("IllnessType=3");
                //postData.Append("&iswsh=").Append("1");

                postData.Append("&iswsh=").Append(temRows != null && temRows.Count() > 0 ? "2" : "1");
                if (temRows != null && temRows.Count() > 0)
                {
                    foreach (var r in temRows)
                    {
                        postData.Append("&wsmc=").Append(HtmlHelper.GetUrlEncodeVal(r["IllnessNameOther"] + ""));
                        postData.Append("&wsrq=").Append(r["DiagnoseTime"] == null ? "" : Convert.ToDateTime(r["DiagnoseTime"]).ToString("yyy-MM-dd"));
                    }
                }

                //输血
                temRows = jiwangDT.Select("IllnessType=4");
                //postData.Append("&isshx=").Append("1");

                postData.Append("&isshx=").Append(temRows != null && temRows.Count() > 0 ? "2" : "1");
                if (temRows != null && temRows.Count() > 0)
                {
                    foreach (var r in temRows)
                    {
                        postData.Append("&sxmc=").Append(HtmlHelper.GetUrlEncodeVal(r["IllnessNameOther"] + ""));
                        postData.Append("&sxrq=").Append(r["DiagnoseTime"] == null ? "" : Convert.ToDateTime(r["DiagnoseTime"]).ToString("yyy-MM-dd"));
                    }
                }


                DataTable familyDT = ds.Tables["ARCHIVE_FAMILYHISTORYINFO"];

                if (familyDT != null && familyDT.Rows.Count > 0)
                {
                    string strtmpDT = "";

                    int index = 0;
                    DataRow familyDR = familyDT.Rows[0];
                    tem = familyDR["FatherHistory"];
                    if (!string.IsNullOrEmpty(tem.ToString().Trim(',')) && tem.ToString() != "1")
                    {
                        foreach (var n in tem.ToString().Split(','))
                        {
                            postData.Append("&jzsjbmc" + index + "=").Append(n != "12" ? (Convert.ToInt32(n) - 1).ToString() : "100");//其他
                        }

                        postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["FatherHistoryOther"] + ""));//其他
                        postData.Append("&jzsjzcy=").Append("3");//关系 
                        index++;

                        strtmpDT = "1";
                    }
                    tem = familyDR["MotherHistory"];
                    if (!string.IsNullOrEmpty(tem.ToString().Trim(',')) && tem.ToString() != "1")
                    {
                        foreach (var n in tem.ToString().Split(','))
                        {
                            postData.Append("&jzsjbmc" + index + "=").Append(n != "12" ? (Convert.ToInt32(n) - 1).ToString() : "100");//其他
                        }
                        postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["MotherHistoryOther"] + ""));//其他
                        postData.Append("&jzsjzcy=").Append("4");//关系 
                        index++;

                        strtmpDT = "1";
                    }
                    tem = familyDR["BrotherSisterHistory"];
                    if (!string.IsNullOrEmpty(tem.ToString().Trim(',')) && tem.ToString() != "1")
                    {

                        foreach (var n in tem.ToString().Split(','))
                        {
                            postData.Append("&jzsjbmc" + index + "=").Append(n != "12" ? (Convert.ToInt32(n) - 1).ToString() : "100");//其他
                        }
                        postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["BrotherSisterHistoryOther"] + ""));//其他
                        postData.Append("&jzsjzcy=").Append("5");//关系 

                        index++;

                        strtmpDT = "1";
                    }
                    tem = familyDR["ChildrenHistory"];
                    if (!string.IsNullOrEmpty(tem.ToString().Trim(',')) && tem.ToString() != "1")
                    {

                        foreach (var n in tem.ToString().Split(','))
                        {
                            postData.Append("&jzsjbmc" + index + "=").Append(n != "12" ? (Convert.ToInt32(n) - 1).ToString() : "100");//其他
                        }
                        postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(familyDR["ChildrenHistoryOther"] + ""));//其他
                        postData.Append("&jzsjzcy=").Append("6");//关系 

                        index++;

                        strtmpDT = "1";
                    }

                    if (strtmpDT == "")
                    {
                        postData.Append("&jkzkjzsqt=");
                        postData.Append("&Jkzk.dYwjb=").Append("2");//家族史有无
                    }
                    else
                    {
                        postData.Append("&Jkzk.dYwjb=").Append("1");
                    }
                }
                else
                {
                    postData.Append("&jkzkjzsqt=");
                    postData.Append("&Jkzk.dYwjb=").Append("2");//家族史有无
                }


                postData.Append("&grhidden=").Append("jzsjbmc0");//?
                postData.Append("&rowNumber=").Append("");


                //暴露史有无
                tem = baseInfo["Exposure"];
                temStr = "2";
                if (!string.IsNullOrEmpty(tem.ToString()) && !tem.ToString().Contains("1"))
                {
                    temStr = "1";
                }
                postData.Append("&jkzk.dBls=").Append(temStr);
                //化学品
                postData.Append("&jkzk.dBlshxp=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Chemical"].ToString()));
                //毒物
                postData.Append("&jkzk.dBlsdw=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Poison"].ToString()));
                //射线
                postData.Append("&jkzk.dBlssx=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Radial"].ToString()));


                //遗传病史
                tem = baseInfo["Disease"];
                postData.Append("&jkzk.dYcbs=").Append(!string.IsNullOrEmpty(tem.ToString()) && tem.ToString() == "1" ? "2" : "1");
                //遗传病史其他
                if (!string.IsNullOrEmpty(tem.ToString()) && !tem.ToString().Contains("1"))
                {
                    postData.Append("&jkzk.dYcbsjb=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["DiseaseEx"] + ""));
                }
                //残疾情况
                tem = baseInfo["DiseasEndition"];
                // postData.Append("&jkzk.dYwcj=").Append("2");

                postData.Append("&jkzk.dYwcj=").Append(string.IsNullOrEmpty(tem.ToString()) || tem.ToString().Contains("1") ? "2" : "1");
                //残疾选项
                if (!string.IsNullOrEmpty(tem.ToString()) && !tem.ToString().Contains("1"))
                {
                    var strS = tem.ToString().Split(',');
                    foreach (var n in strS)
                    {
                        postData.Append("&jkzk.dCjmz=").Append(GetWebDiseasenditionCodeByPadCode(n));
                    }
                    if (strS.Contains("8"))
                    {
                        postData.Append("&jkzk.dCjqt=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["DiseasenditionEx"] + ""));
                    }
                }
                //生活环境
                DataTable huanjingDT = ds.Tables["ARCHIVE_BASEINFOARCHIVE_ENVIRONMENT"];

                if (huanjingDT != null && huanjingDT.Rows.Count > 0)
                {
                    DataRow hjDR = huanjingDT.Rows[0];
                    //cfpqsb 厨房
                    postData.Append("&cfpqsb=").Append(hjDR["BlowMeasure"]);
                    //rllx  燃料
                    postData.Append("&rllx=").Append(hjDR["FuelType"]);
                    //ys 饮水
                    postData.Append("&ys=").Append(hjDR["DrinkWater"]);
                    //cs 厕所
                    postData.Append("&cs=").Append(hjDR["Toilet"]);
                    //qxl 禽畜
                    postData.Append("&qxl=").Append(getQcl(hjDR["LiveStockRail"].ToString()));
                }

                //签字
                node = doc.DocumentNode.SelectSingleNode("//input[@name='fkqzbr']");
                postData.Append("&fkqzbr=").Append(CommonExtensions.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));

                node = doc.DocumentNode.SelectSingleNode("//input[@name='fkqzjs']");
                postData.Append("&fkqzjs=").Append(CommonExtensions.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));

                postData.Append("&sdfkqzbr=").Append(CommonExtensions.GetUrlEncodeVal(baseInfo["CustomerName"].ToString()));

                node = doc.DocumentNode.SelectSingleNode("//input[@name='sdfkqzjs']");
                postData.Append("&sdfkqzjs=").Append(CommonExtensions.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));

                node = doc.DocumentNode.SelectSingleNode("//input[@name='fktime']");
                postData.Append("&fktime=").Append(CommonExtensions.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));

                //调查时间
                node = doc.DocumentNode.SelectSingleNode("//input[@id='d11']");
                string happentime = node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "";

                node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
                string createtime = node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "";

                if (!string.IsNullOrEmpty(CreateTimeSameTj) && CreateTimeSameTj == "1")
                {
                    if (!string.IsNullOrEmpty(baseInfo["CreateDate"].ToString()))
                    {
                        if (!string.IsNullOrEmpty(createtime))
                        {
                            //录入时间大于2018-7-1日的才改时间
                            if (Convert.ToDateTime(createtime) >= Convert.ToDateTime("2018-07-01"))
                            {
                                createtime = Convert.ToDateTime(Convert.ToDateTime(baseInfo["CreateDate"].ToString()).ToString("yyyy-MM-dd") + " " + DateTime.Now.AddMinutes(-15).ToString("HH:mm:ss")).ToString("yyyy-MM-dd HH:mm:ss");
                            }
                        }
                    }

                    happentime = Convert.ToDateTime(createtime).ToString("yyyy-MM-dd");
                }

                postData.Append("&happentime=").Append(happentime);
                postData.Append("&createtime=").Append(createtime);

                node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
                postData.Append("&createuser=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='updatetime']");
                postData.Append("&updatetime=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='updateuser']");
                postData.Append("&updateuser=").Append(loginKey);
                node = doc.DocumentNode.SelectSingleNode("//input[@id='pRgid']");

                node = doc.DocumentNode.SelectSingleNode("//input[@name='creatregion']");
                postData.Append("&creatregion=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //postData.Append("&pRgid=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                if (loginKey.Length == 16)
                {
                    postData.Append("&pRgid=").Append(loginKey.Substring(0, 12));
                }
                else
                {
                    postData.Append("&pRgid=").Append(loginKey.Substring(0, 15));
                }
                postData.Append("&dzrys=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["Doctor"].ToString()));

                #region 2017-10-19 家庭情况和健康卡信息
                //户主姓名
                postData.Append("&dhzxm=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["HouseName"].ToString()));

                node = doc.DocumentNode.SelectSingleNode("//input[@id='dhzsfzh']");
                string hzsfzh = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                if (!string.IsNullOrEmpty(isUpdateHzsfz) && isUpdateHzsfz == "1")
                {
                    postData.Append("&dhzsfzh=").Append(baseInfo["FamilyIDCardNo"].ToString());
                }
                else
                {
                    postData.Append("&dhzsfzh=").Append(hzsfzh);
                }

                //因为原始档案户主身份证号没下载下来，所以先不更新平台户主身份证号

                postData.Append("&jtrks=").Append(baseInfo["FamilyNum"].ToString());
                postData.Append("&jtjg=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["FamilyStructure"].ToString()));

                string live = baseInfo["LiveCondition"].ToString();

                postData.Append("&tgrjbxjzqk=").Append(live.Replace("0", ""));

                //孕产情况
                string PreSituation = baseInfo["PreSituation"].ToString();
                string PreNum = baseInfo["PreNum"].ToString();
                string YieldNum = baseInfo["YieldNum"].ToString();
                if (!string.IsNullOrEmpty(PreSituation))
                {
                    postData.Append("&lHyqk=").Append(CommonExtensions.GetUrlEncodeVal(GetPreSituationByPadCode(PreSituation)));
                    postData.Append("&lYc=").Append(PreNum);
                    postData.Append("&lCc=").Append(YieldNum);
                }
                //户籍地址
                postData.Append("&dSfzdz=").Append(HtmlHelper.GetUrlEncodeVal(baseInfo["HouseHoldAddress"].ToString()));

                //居民健康档案信息卡
                DataTable dt = ds.Tables["archive_health_info"];
                string Prevalence = "";
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow health = ds.Tables["archive_health_info"].Rows[0];
                    Prevalence = health["Prevalence"].ToString();
                    if (!string.IsNullOrEmpty(Prevalence))
                    {
                        var tmp = Prevalence.Split(',');
                        foreach (var item in tmp)
                        {
                            postData.Append("&hbqk=").Append(item);
                        }
                    }
                    postData.Append("&hbqkqt=").Append(HtmlHelper.GetUrlEncodeVal(health["PrevalenceOther"].ToString()));
                    postData.Append("&jdjglxdh=").Append(health["OrgTelphone"].ToString());
                    postData.Append("&jtzrys=").Append(HtmlHelper.GetUrlEncodeVal(health["FamilyDoctor"].ToString()));
                    postData.Append("&jtzryslxdh=").Append(health["FamilyDoctorTel"].ToString());
                    postData.Append("&sqzrhs=").Append(HtmlHelper.GetUrlEncodeVal(health["Nurses"].ToString()));
                    postData.Append("&sqzrhslxdh=").Append(health["NursesTel"].ToString());
                    postData.Append("&ggwsry=").Append(HtmlHelper.GetUrlEncodeVal(health["HealthPersonnel"].ToString()));
                    postData.Append("&ggwsrylxdh=").Append(health["HealthPersonnelTel"].ToString());
                    postData.Append("&qtsm=").Append(HtmlHelper.GetUrlEncodeVal(health["Others"].ToString()));
                }
                else
                {
                    postData.Append("&hbqk=");
                    postData.Append("&hbqkqt=");
                    postData.Append("&jdjglxdh=");
                    postData.Append("&jtzrys=");
                    postData.Append("&jtzryslxdh=");
                    postData.Append("&sqzrhs=");
                    postData.Append("&sqzrhslxdh=");
                    postData.Append("&ggwsry=");
                    postData.Append("&ggwsrylxdh=");
                    postData.Append("&qtsm=");
                }

                #endregion

                #endregion

                returnString = web.PostHttp(baseUrl + "/healthArchives/saveUpdate.action", postData.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

                HtmlDocument d = HtmlHelper.GetHtmlDocument(returnString);
                if (d != null && d.DocumentNode.SelectSingleNode("//title") != null
                    && d.DocumentNode.SelectSingleNode("//title").InnerText.Contains("500")
                    )
                {
                    CommonExtensions.WriteLog(returnString);
                    callback("EX-个人档案信息：身份证号[" + idcard + "],姓名[" + person.memberName + "] 个人档案更新失败，服务器错误！");
                }
            }
        }

        /// <summary>
        /// 只更新电话
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="userKey"></param>
        private void UpdateGrdaTel(PersonModel person, DataSet ds, Action<string> callback)
        {
            WebHelper web = new WebHelper();
            string idcard = person.idNumber;
            string post = "dah=" + person.pid + "&tz=2";
            string returnString = web.PostHttp(baseUrl + "/healthArchives/updateArchives.action", post, "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc != null)
            {
                DataRow baseInfo = ds.Tables["ARCHIVE_BASEINFO"].Rows[0];
                StringBuilder postData = new StringBuilder();

                #region

                postData.Append("dGrdabh=").Append(person.pid);
                var node = doc.DocumentNode.SelectSingleNode("//input[@name='dJtdabh']");
                postData.Append("&dJtdabh=").Append(node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGrdabhshow']");
                postData.Append("&dGrdabhshow=").Append(node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGrdabh17']");
                postData.Append("&dGrdabh17=").Append(node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dPyjm']");
                postData.Append("&dPyjm=").Append(node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dWzd']");
                postData.Append("&dWzd=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "100");
                node = doc.DocumentNode.SelectSingleNode("//input[@id='qdqxz']");
                postData.Append("&qdqxz=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "0");//？
                //与户主关系  待家庭成员信息修改时，更新
                node = doc.DocumentNode.SelectSingleNode("//select[@name='dYhzgx']/option[@selected]");
                postData.Append("&dYhzgx=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value.Replace("0", "1") : "1");
                //档案状态
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dDazt'][@checked]");
                //postData.Append("&dDazt=").Append("2");
                postData.Append("&dDazt=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //档案状态-缘由
                node = doc.DocumentNode.SelectSingleNode("//select[@name='dDaztyy']/option[@selected]");
                postData.Append("&dDaztyy=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dXm']");
                postData.Append("&dXm=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "")); //姓名

                node = doc.DocumentNode.SelectSingleNode("//select[@name='dXb']/option[@selected]");
                postData.Append("&dXb=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : baseInfo["Sex"]);//性别

                node = doc.DocumentNode.SelectSingleNode("//select[@name='dZjlx']/option[@selected]");
                postData.Append("&dZjlx=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "1");//证件类型
                postData.Append("&dSfzh=").Append(idcard);//身份证号
                //证据编号
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dZjhqt']");
                postData.Append("&dZjhqt=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //出生日期
                node = doc.DocumentNode.SelectSingleNode("//input[@name='dCsrq']");
                postData.Append("&dCsrq=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : baseInfo["Birthday"] == null ? "" : Convert.ToDateTime(baseInfo["Birthday"]).ToString("yyyy-MM-dd"));//出生日期
                postData.Append("&dLxdh=").Append(baseInfo["Phone"]);//本人电话

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dGzdw']");
                postData.Append("&dGzdw=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : baseInfo["WorkUnit"] + "")); //工作单位

                postData.Append("&dLxrdh=").Append(baseInfo["ContactPhone"]);//联系人电话

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dLxrxm']");
                postData.Append("&dLxrxm=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : baseInfo["ContactName"] + ""));//联系人姓名

                node = doc.DocumentNode.SelectSingleNode("//select[@name='dJzzk']/option[@selected]");
                postData.Append("&dJzzk=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""); //常住类型
                //民族
                node = doc.DocumentNode.SelectSingleNode("//select[@name='dMz']/option[@selected]");
                postData.Append("&dMz=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//民族

                node = doc.DocumentNode.SelectSingleNode("//input[@name='ssmz']");
                postData.Append("&ssmz=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//民族

                //血型
                node = doc.DocumentNode.SelectSingleNode("//select[@name='jkzk.dXx']/option[@selected]");
                postData.Append("&jkzk.dXx=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //RH 
                node = doc.DocumentNode.SelectSingleNode("//select[@id='jkzk.dSfrhyx']/option[@selected]");
                if (node == null)
                {
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dSfrhyx'][@checked]");
                }
                postData.Append("&jkzk.dSfrhyx=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //职业
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dZy'][@checked]");
                postData.Append("&dZy=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //文化程度
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dWhcd']/option[@selected]");
                postData.Append("&dWhcd=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                //劳动强度
                //node = doc.DocumentNode.SelectSingleNode("//select[@id='shxg.dLdqd']/option[@selected]");
                //postData.Append("&shxg.dLdqd=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//劳动强度

                //婚姻状况
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dHyzk']/option[@selected]");
                postData.Append("&dHyzk=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                //医疗费用支付方式
                var nodes = doc.DocumentNode.SelectNodes("//input[@name='dYlfzflx'][@checked]");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (var nod in nodes)
                    {
                        postData.Append("&dYlfzflx=").Append(nod != null && nod.Attributes.Contains("value") ? nod.Attributes["value"].Value : "");
                    }
                }
                else
                {
                    postData.Append("&dYlfzflx=");
                }

                //城镇医保卡号
                string czybk = HtmlHelper.GetTagValue(HtmlHelper.GetTagValue(returnString, "<input type='text' id='dczhszjbylbxkh' name='dczhszjbylbxkh'", "/>").Replace("\\", ""), "value=\"", "\"");
                if (!string.IsNullOrEmpty(czybk))
                {
                    postData.Append("&dczhszjbylbxkh=").Append(czybk);
                }
                //居民医保卡号
                string jmybk = HtmlHelper.GetTagValue(HtmlHelper.GetTagValue(returnString, "<input type='text' id='djmjbylbxkh' name='djmjbylbxkh'", "/>").Replace("\\", ""), "value=\"", "\"");
                if (!string.IsNullOrEmpty(jmybk))
                {
                    postData.Append("&djmjbylbxkh=").Append(jmybk);
                }
                //贫困救助卡号
                string pkybk = HtmlHelper.GetTagValue(HtmlHelper.GetTagValue(returnString, "<input type='text' id='dpkjzkh1' name='dpkjzkh1'", "/>").Replace("\\", ""), "value=\"", "\"");
                if (!string.IsNullOrEmpty(pkybk))
                {
                    postData.Append("&dpkjzkh1=").Append(pkybk);
                }
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dYlfzflxqt']");
                postData.Append("&dYlfzflxqt=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));//其他支付方式

                node = doc.DocumentNode.SelectSingleNode("//input[@id='dYlbxh']");
                postData.Append("&dYlbxh=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//医疗保险号
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dXnhh']");
                postData.Append("&dXnhh=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//新农合号
                postData.Append("&dSheng=").Append(loginKey.Substring(0, 2));//省######
                postData.Append("&dShi=").Append(loginKey.Substring(0, 4));//市#####
                postData.Append("&dQu=").Append(loginKey.Substring(0, 6));//区###

                node = doc.DocumentNode.SelectSingleNode("//select[@name='dJd']/option[@selected]");
                postData.Append("&dJd=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//select[@name='dJwh']/option[@selected]");
                postData.Append("&dJwh=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//input[@id='dXxdz']");
                postData.Append("&dXxdz=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));//详细地址
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dSspq']/option[@selected]");
                //postData.Append("&dSspq=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//所属片区
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dDalb']/option[@selected]");
                postData.Append("&dDalb=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//档案类别：1城镇 ；2：农村##

                //药物过敏史
                node = doc.DocumentNode.SelectSingleNode("//select[@id='dSspq']/option[@selected]");
                postData.Append("&jkzk.dGms=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                nodes = doc.DocumentNode.SelectNodes("//input[@name='jkzk.dYgms'][@checked]");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (var nod in nodes)
                    {
                        postData.Append("&jkzk.dYgms=").Append(nod != null && nod.Attributes.Contains("value") ? nod.Attributes["value"].Value : "");
                    }
                }
                else
                {
                    postData.Append("&jkzk.dYgms=");
                }

                node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dGmsqt']");
                postData.Append("&jkzk.dGmsqt=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));

                node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dYwjws']");
                postData.Append("&jkzk.dYwjws=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//？1
                //node = doc.DocumentNode.SelectSingleNode("//input[@id='jbgrhidden']");
                //postData.Append("&jbgrhidden=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");//？ jb0

                //DataTable jiwangDT = ds.Tables["ARCHIVE_ILLNESSHISTORYINFO"];

                //DataRow[] temRows = jiwangDT.Select("IllnessType=1");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='isjb'][@checked]");
                postData.Append("&isjb=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                string strJbs = "2";
                string strjb = "";
                var jwsTds = doc.DocumentNode.SelectNodes("//td[@id='jbtdid']");

                if (jwsTds != null)
                {
                    foreach (var jws in jwsTds)
                    {
                        var chkBox = jws.SelectNodes("input[@checked]");
                        if (chkBox != null && chkBox.Count > 0)
                        {
                            foreach (var chk in chkBox)
                            {
                                postData.Append("&").Append(chk.Attributes["name"].Value).Append("=").Append(chk.Attributes["value"].Value);
                            }
                        }

                        //肿瘤  
                        var exzl = jws.SelectNodes("input[@name='exzl']");
                        if (exzl != null && exzl.Count > 0)
                        {
                            foreach (var nod in exzl)
                            {
                                postData.Append("&exzl=").Append(HtmlHelper.GetUrlEncodeVal(nod != null && nod.Attributes.Contains("value") ? nod.Attributes["value"].Value : ""));
                            }
                        }

                        //职业病
                        var zyb = jws.SelectNodes("input[@name='zybqt']");
                        if (zyb != null && zyb.Count > 0)
                        {
                            foreach (var nod in zyb)
                            {
                                postData.Append("&zybqt=").Append(HtmlHelper.GetUrlEncodeVal(nod != null && nod.Attributes.Contains("value") ? nod.Attributes["value"].Value : ""));
                            }
                        }

                        //其他
                        var qt = jws.SelectNodes("input[@name='jbqt']");
                        if (qt != null && qt.Count > 0)
                        {
                            foreach (var nod in qt)
                            {
                                postData.Append("&jbqt=").Append(HtmlHelper.GetUrlEncodeVal(nod != null && nod.Attributes.Contains("value") ? nod.Attributes["value"].Value : ""));
                            }
                        }

                        var qzrq = jws.SelectNodes("input[@name='zdrq']");
                        if (qzrq != null && qzrq.Count > 0)
                        {
                            foreach (var nod in qzrq)
                            {
                                postData.Append("&zdrq=").Append(HtmlHelper.GetUrlEncodeVal(nod != null && nod.Attributes.Contains("value") ? nod.Attributes["value"].Value : ""));
                            }
                        }
                    }
                }
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jbgrhidden']");
                postData.Append("&jbgrhidden=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));

                //既往史手术有无
                node = doc.DocumentNode.SelectSingleNode("//input[@name='isshsh'][@checked]");
                postData.Append("&isshsh=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//table[@id='tabss']");
                nodes = node.SelectNodes("tr[position()>2]");
                if (nodes != null)
                {
                    foreach (var nn in nodes)
                    {
                        node = nn.SelectNodes("td")[0].SelectSingleNode("input");
                        postData.Append("&ssmc=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                        node = nn.SelectNodes("td")[1].SelectSingleNode("input");
                        postData.Append("&ssrq=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                    }
                }

                //外伤
                node = doc.DocumentNode.SelectSingleNode("//input[@name='iswsh'][@checked]");
                postData.Append("&iswsh=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);

                node = doc.DocumentNode.SelectSingleNode("//table[@id='tabws']");

                nodes = node.SelectNodes("tr[position()>2]");
                if (nodes != null)
                {
                    foreach (var no in nodes)
                    {
                        node = no.SelectNodes("td")[0].SelectSingleNode("input");
                        postData.Append("&wsmc=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                        node = no.SelectNodes("td")[1].SelectSingleNode("input");
                        postData.Append("&wsrq=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                    }
                }

                //输血
                node = doc.DocumentNode.SelectSingleNode("//input[@name='isshx'][@checked]");
                postData.Append("&isshx=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);

                node = doc.DocumentNode.SelectSingleNode("//table[@id='tabsx']");
                nodes = node.SelectNodes("tr[position()>2]");
                if (nodes != null)
                {
                    foreach (var no in nodes)
                    {
                        node = no.SelectNodes("td")[0].SelectSingleNode("input");
                        postData.Append("&sxmc=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                        node = no.SelectNodes("td")[1].SelectSingleNode("input");
                        postData.Append("&sxrq=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                    }
                }

                //家族史
                node = doc.DocumentNode.SelectSingleNode("//select[@id='Jkzk.dYwjb']/option[@selected]");
                postData.Append("&Jkzk.dYwjb=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);

                nodes = doc.DocumentNode.SelectSingleNode("//table[@id='addjz']").SelectNodes("tr");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (var no in nodes)
                    {
                        var tds = no.SelectNodes("td");
                        var n = tds[3].SelectSingleNode("div/select[@name='jzsjzcy']/option[@selected]");
                        postData.Append("&jzsjzcy=").Append(n == null || !n.Attributes.Contains("value") ? "" : n.Attributes["value"].Value);

                        var ns = tds[1].SelectNodes("input[@checked]");
                        if (ns != null && ns.Count > 0)
                        {
                            foreach (var item in ns)
                            {
                                postData.Append("&").Append(item.Attributes["name"].Value).Append("=").Append(item == null || !item.Attributes.Contains("value") ? "" : item.Attributes["value"].Value);
                            }
                        }
                        ns = tds[1].SelectNodes("input[@name='jkzkjzsqt']");
                        if (ns != null && ns.Count > 0)
                        {
                            foreach (var item in ns)
                            {
                                postData.Append("&jkzkjzsqt=").Append(HtmlHelper.GetUrlEncodeVal(item == null || !item.Attributes.Contains("value") ? "" : item.Attributes["value"].Value));
                            }
                        }
                    }
                }

                node = doc.DocumentNode.SelectSingleNode("//input[@id='grhidden']");
                postData.Append("&grhidden=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);//?
                node = doc.DocumentNode.SelectSingleNode("//input[@id='rowNumber']");
                postData.Append("&rowNumber=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);

                //暴露史有无
                node = doc.DocumentNode.SelectSingleNode("//select[@name='jkzk.dBls']/option[@selected]");
                postData.Append("&jkzk.dBls=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);

                //化学品
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jkzk.dBlshxp']");
                postData.Append("&jkzk.dBlshxp=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                //毒物
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jkzk.dBlsdw']");
                postData.Append("&jkzk.dBlsdw=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                //射线
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jkzk.dBlssx']");
                postData.Append("&jkzk.dBlssx=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));

                //遗传病史
                node = doc.DocumentNode.SelectSingleNode("//select[@name='jkzk.dYcbs']/option[@selected]");
                postData.Append("&jkzk.dYcbs=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                //遗传病史其他
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jkzk.dYcbsjb']");
                postData.Append("&jkzk.dYcbsjb=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));

                //残疾情况
                node = doc.DocumentNode.SelectSingleNode("//select[@name='jkzk.dYwcj']/option[@selected]");
                postData.Append("&jkzk.dYwcj=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);

                //残疾选项
                nodes = doc.DocumentNode.SelectNodes("//input[@name='jkzk.dCjmz'][@checked]");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (var item in nodes)
                    {
                        postData.Append("&jkzk.dCjmz=").Append(item.Attributes["value"].Value);
                    }
                }
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jkzk.dYcbsjb']");
                postData.Append("&jkzk.dCjqt=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));

                //生活环境

                //cfpqsb 厨房
                node = doc.DocumentNode.SelectSingleNode("//input[@name='cfpqsb'][@checked]");
                postData.Append("&cfpqsb=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                //rllx  燃料
                node = doc.DocumentNode.SelectSingleNode("//input[@name='rllx'][@checked]");
                postData.Append("&rllx=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                //ys 饮水
                node = doc.DocumentNode.SelectSingleNode("//input[@name='ys'][@checked]");
                postData.Append("&ys=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                //cs 厕所
                node = doc.DocumentNode.SelectSingleNode("//input[@name='cs'][@checked]");
                postData.Append("&cs=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                //qxl 禽畜
                node = doc.DocumentNode.SelectSingleNode("//input[@name='qxl'][@checked]");
                postData.Append("&qxl=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);


                //调查时间
                node = doc.DocumentNode.SelectSingleNode("//input[@name='happentime']");
                string happentime = node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "";

                node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
                string createtime = node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "";
                postData.Append("&happentime=").Append(happentime);
                postData.Append("&createtime=").Append(createtime);

                node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
                postData.Append("&createuser=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='updatetime']");
                postData.Append("&updatetime=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                node = doc.DocumentNode.SelectSingleNode("//input[@name='updateuser']");
                postData.Append("&updateuser=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");
                node = doc.DocumentNode.SelectSingleNode("//input[@id='pRgid']");
                postData.Append("&pRgid=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//input[@name='creatregion']");
                postData.Append("&creatregion=").Append(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : "");

                node = doc.DocumentNode.SelectSingleNode("//input[@name='dzrys']");
                postData.Append("&dzrys=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));

                #region 2017-10-19 家庭情况和健康卡信息
                //户主姓名
                node = doc.DocumentNode.SelectSingleNode("//input[@name='hzxm']");
                postData.Append("&hzxm=").Append(HtmlHelper.GetUrlEncodeVal(node != null && node.Attributes.Contains("value") ? node.Attributes["value"].Value : ""));

                node = doc.DocumentNode.SelectSingleNode("//input[@id='hzsfzh']");
                string hzsfzh = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                postData.Append("&hzsfzh=").Append(hzsfzh);

                //因为原始档案户主身份证号没下载下来，所以先不更新平台户主身份证号
                node = doc.DocumentNode.SelectSingleNode("//input[@id='jtrks']");
                postData.Append("&jtrks=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//input[@id='jtjg']");
                postData.Append("&jtjg=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));

                node = doc.DocumentNode.SelectSingleNode("//input[@name='tgrjbxjzqk'][@checked]");
                postData.Append("&tgrjbxjzqk=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);

                //孕产情况
                node = doc.DocumentNode.SelectSingleNode("//input[@id='lHyqk'][@checked]");
                postData.Append("&lHyqk=").Append(CommonExtensions.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//select[@name='lYc']/option[@selected]");
                postData.Append("&lYc=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);
                node = doc.DocumentNode.SelectSingleNode("//select[@name='lCc']/option[@selected]");
                postData.Append("&lCc=").Append(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value);

                //户籍地址
                node = doc.DocumentNode.SelectSingleNode("//input[@id='dSfzdz']");
                postData.Append("&dSfzdz=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));

                //居民健康档案信息卡
                nodes = doc.DocumentNode.SelectNodes("//input[@name='hbqk'][@checked]");
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (var item in nodes)
                    {
                        postData.Append("&hbqk=").Append(item.Attributes["value"].Value);
                    }
                }
                node = doc.DocumentNode.SelectSingleNode("//input[@name='hbqkqt']");
                postData.Append("&hbqkqt=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jdjglxdh']");
                postData.Append("&jdjglxdh=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jtzrys']");
                postData.Append("&jtzrys=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='jtzryslxdh']");
                postData.Append("&jtzryslxdh=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='sqzrhs']");
                postData.Append("&sqzrhs=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='sqzrhslxdh']");
                postData.Append("&sqzrhslxdh=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='ggwsry']");
                postData.Append("&ggwsry=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='ggwsrylxdh']");
                postData.Append("&ggwsrylxdh=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='qtsm']");
                postData.Append("&qtsm=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));

                //签字
                node = doc.DocumentNode.SelectSingleNode("//input[@name='fkqzbr']");
                postData.Append("&fkqzbr=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                node = doc.DocumentNode.SelectSingleNode("//input[@name='fkqzjs']");
                postData.Append("&fkqzjs=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));

                postData.Append("&sdfkqzbr=").Append(CommonExtensions.GetUrlEncodeVal(baseInfo["CustomerName"].ToString()));

                node = doc.DocumentNode.SelectSingleNode("//input[@name='sdfkqzjs']");
                postData.Append("&sdfkqzjs=").Append(HtmlHelper.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));

                node = doc.DocumentNode.SelectSingleNode("//input[@name='fktime']");
                postData.Append("&fktime=").Append(CommonExtensions.GetUrlEncodeVal(node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value));
                #endregion

                #endregion
                returnString = web.PostHttp(baseUrl + "/healthArchives/saveUpdate.action", postData.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

                HtmlDocument d = HtmlHelper.GetHtmlDocument(returnString);
                if (d != null && d.DocumentNode.SelectSingleNode("//title") != null
                    && d.DocumentNode.SelectSingleNode("//title").InnerText.Contains("500")
                    )
                {
                    CommonExtensions.WriteLog(returnString);
                    callback("EX-个人档案信息：身份证号[" + idcard + "],姓名[" + person.memberName + "] 个人档案更新失败，服务器错误！");
                }
            }
        }

        /// <summary>
        /// 根据名族名称，获取webCode
        /// </summary>
        /// <param name="nationName"></param>
        /// <returns></returns>
        private string GetWebNationCodeByPadNationName(string nationName)
        {
            string code = "1";
            switch (nationName.Trim())
            {
                case "汉族":
                    code = "1"; break;
                case "蒙古族":
                    code = "2"; break;
                case "回族":
                    code = "3"; break;
                case "藏族":
                    code = "4"; break;
                case "维吾尔族":
                    code = "5"; break;
                case "苗族":
                    code = "6"; break;
                case "彝族":
                    code = "7"; break;
                case "壮族":
                    code = "8"; break;
                case "布依族":
                    code = "9"; break;
                case "朝鲜族":
                    code = "10"; break;
                case "满族":
                    code = "11"; break;
                case "侗族":
                    code = "12"; break;
                case "瑶族":
                    code = "13"; break;
                case "白族":
                    code = "14"; break;
                case "土家族":
                    code = "15"; break;
                case "哈尼族":
                    code = "16"; break;
                case "哈萨克族":
                    code = "17"; break;
                case "傣族":
                    code = "18"; break;
                case "黎族":
                    code = "19"; break;
                case "傈僳族":
                    code = "20"; break;
                case "佤族":
                    code = "21"; break;
                case "畲族":
                    code = "22"; break;
                case "高山族":
                    code = "23"; break;
                case "拉祜族":
                    code = "24"; break;
                case "水族":
                    code = "25"; break;
                case "东乡族":
                    code = "26"; break;
                case "纳西族":
                    code = "27"; break;
                case "景颇族":
                    code = "28"; break;
                case "柯尔克孜族 ":
                    code = "29"; break;
                case "土族":
                    code = "30"; break;
                case "达斡尔族":
                    code = "31"; break;
                case "仫佬族":
                    code = "32"; break;
                case "羌族":
                    code = "33"; break;
                case "布朗族":
                    code = "34"; break;
                case "撒拉族":
                    code = "35"; break;
                case "毛难族":
                    code = "36"; break;
                case "仡佬族":
                    code = "37"; break;
                case "锡伯族":
                    code = "38"; break;
                case "阿昌族":
                    code = "39"; break;
                case "普米族":
                    code = "40"; break;
                case "塔吉克族":
                    code = "41"; break;
                case "怒族":
                    code = "42"; break;
                case "乌孜别克族":
                    code = "43"; break;
                case "俄罗斯族":
                    code = "44"; break;
                case "鄂温克族":
                    code = "45"; break;
                case "德昂族":
                    code = "46"; break;
                case "保安族":
                    code = "47"; break;
                case "裕固族":
                    code = "48"; break;
                case "京族":
                    code = "49"; break;
                case "塔塔尔族 ":
                    code = "50"; break;
                case "独龙族":
                    code = "51"; break;
                case "鄂伦春族":
                    code = "52"; break;
                case "赫哲族":
                    code = "53"; break;
                case "门巴族":
                    code = "54"; break;
                case "珞巴族":
                    code = "55"; break;
                case "基诺族":
                    code = "56"; break;
            }
            return code;
        }

        private string GetWebNationCodeByPadNationName1(string nationName)
        {
            string code = "1";
            switch (nationName.Trim())
            {
                case "汉族":
                    code = "1"; break;
                default:
                    code = "99"; break;
            }
            return code;
        }
        /// <summary>
        /// 根据pad血型code，获取web血型code
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebBooldTypeCodeByPadCode(string padCode)
        {
            string tem = "5";
            if (baseUrl.Contains("sdcsm_new"))
            {
                return padCode;
            }
            else
            {
                switch (padCode)
                {
                    case "1":
                        tem = "1";
                        break;
                    case "4":
                        tem = "2";
                        break;
                    case "2":
                        tem = "3";
                        break;
                    case "3":
                        tem = "4";
                        break;
                    case "5":
                        tem = "5";
                        break;
                }
            }
            return tem;
        }
        /// <summary>
        /// 根据padJOB Code，获取WebCode
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebJobCodeByPadCode(string padCode)
        {
            string job = "";
            if (baseUrl.Contains("sdcsm_new"))
            {
                switch (padCode)
                {
                    case "1":
                        job = "0";
                        break;
                    case "2":
                        job = "1";
                        break;
                    case "3":
                        job = "2";
                        break;
                    case "4":
                        job = "3";
                        break;
                    case "5":
                        job = "4";
                        break;
                    case "6":
                        job = "5";
                        break;
                    case "7":
                        job = "6";
                        break;
                    case "8":
                        job = "7";
                        break;
                    case "9":
                        job = "8";
                        break;
                }
            }
            else
            {
                switch (padCode)
                {
                    case "1":
                        job = "6";
                        break;
                    case "2":
                        job = "3";
                        break;
                    case "3":
                        job = "4";
                        break;
                    case "4":
                        job = "5";
                        break;
                    case "5":
                        job = "1";
                        break;
                    case "6":
                        job = "2";
                        break;
                    case "7":
                        job = "12";
                        break;
                    case "8":
                        job = "99";
                        break;
                    case "9":
                        job = "9";
                        break;
                }
            }

            return job;
        }
        /// <summary>
        /// 根据pad 文化程度Code ,获取WebCode
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebCultureCodeByPadCode(string padCode)
        {
            string cultureName = "";
            switch (padCode)
            {
                case "1":
                    cultureName = "10";
                    break;
                case "2":
                    cultureName = "20";
                    break;
                case "3":
                    cultureName = "30";
                    break;
                case "4":
                    cultureName = "40";
                    break;
                case "5":
                    cultureName = "50";
                    break;
                case "6":
                    cultureName = "60";
                    break;
                case "7":
                    cultureName = "70";
                    break;
                case "8":
                    cultureName = "80";
                    break;
                case "9":
                    cultureName = "90";
                    break;
            }
            return cultureName;
        }
        /// <summary>
        /// 根据pad 婚姻状况code,获取WebCode
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebMaritalCodeByPadCode(string padCode)
        {
            string marital = "5";
            switch (padCode)
            {
                case "1":
                    marital = "10";
                    break;
                case "2":
                    marital = "20";
                    break;
                case "3":
                    marital = "30";
                    break;
                case "4":
                    marital = "40";
                    break;
                case "5":
                    marital = "90";
                    break;
            }
            return marital;
        }
        /// <summary>
        /// 根据 pad字符方式code,获取WebCode
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebMedicalPayTypeCodesByPadCode(string padCode)
        {
            string temStr = "";
            if (baseUrl.Contains("sdcsm_new"))
            {
                return padCode;
            }
            else
            {
                switch (padCode)
                {
                    case "1":
                        temStr = "1";
                        break;
                    case "2":
                        temStr = "2";
                        break;
                    case "4":
                        temStr = "3";
                        break;
                    case "5":
                        temStr = "4";
                        break;
                    case "6":
                        temStr = "5";
                        break;
                    case "7":
                        temStr = "6";
                        break;
                    case "8":
                        temStr = "99";
                        break;
                }
            }

            return temStr;
        }
        /// <summary>
        /// 根据pad 药物过敏史code,获取WebCode
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebDrugAllergic(string padCode)
        {
            string str = "99";
            switch (padCode)
            {
                case "2":
                    str = "1";
                    break;
                case "3":
                    str = "2";
                    break;
                case "4":
                    str = "4";
                    break;
                case "5":
                    str = "99";
                    break;
                default:
                    break;
            }
            return str;
        }
        /// <summary>
        /// 根据pad 残疾code ,获取Web Code
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetWebDiseasenditionCodeByPadCode(string padCode)
        {
            string str = "";
            if (baseUrl.Contains("sdcsm_new"))
            {
                return padCode;
            }
            else
            {
                switch (padCode)
                {
                    case "3":
                        str = "2";
                        break;
                    case "4":
                        str = "3";
                        break;
                    case "5":
                        str = "4";
                        break;
                    case "6":
                        str = "5";
                        break;
                    case "2":
                        str = "6";
                        break;
                    case "7":
                        str = "7";
                        break;
                    case "8":
                        str = "9";
                        break;
                }
            }
            return str;
        }

        /// <summary>
        /// 孕产情况
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetPreSituationByPadCode(string padCode)
        {
            string code = "";
            switch (padCode)
            {
                case "1":
                    code = "未孕";
                    break;
                case "2":
                    code = "已孕未生产";
                    break;
                case "3":
                    code = "已生产随访期内";
                    break;
                case "4":
                    code = "已生产随访期外";
                    break;
                case "5":
                    code = "不详";
                    break;
                default:
                    break;
            }
            return code;
        }

        #region Donwload

        private void TryDownGrda(int tryCount, params Action<string>[] callbackAll)
        {
            //个人回执
            Action<string> callback = callbackAll[0];
            try
            {
                GetGrdaKeyAndInfo(callbackAll);
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
                    TryDownGrda(tryCount, callback);
                }
                else
                {
                    callback("EX-个人档案:获取个人档案信息失败。请确保网路畅通。");
                }
            }
        }

        int thispagecurrent = 1;
        /// <summary>
        /// 获取第一页数据
        /// </summary>
        /// <param name="callback"></param>
        public List<PersonModel> GetGrdaFirstKeyAndInfo(out int pageSum, params Action<string>[] callbackAll)
        {
            //个人回执
            Action<string> callback = callbackAll[0];

            pageSum = 0;
            WebHelper web = new WebHelper();
            StringBuilder postStr = new StringBuilder();
            #region
            postStr.Append("_cxgxb=").Append("on");

            #region 查询条件2017-05-03
            string town = "";
            string vill = "";
            if (querylist != null)
            {
                if (!string.IsNullOrEmpty(querylist.qTown))
                {
                    town = querylist.qTown;
                }
                if (!string.IsNullOrEmpty(querylist.qVill))
                {
                    vill = querylist.qVill;
                }
                if (querylist.qLnr)
                {
                    postStr.Append("&age=65");
                }
                if (querylist.qGxy)
                {
                    postStr.Append("&cxgxy=tzgxy");
                }
                if (querylist.qTnb)
                {
                    postStr.Append("&cxtnb=tztnb");
                }
                if (querylist.qGxb)
                {
                    postStr.Append("&cxgxb=tzgxb");
                }
                if (querylist.qNcz)
                {
                    postStr.Append("&cxnzz=tznzz");
                }
                if (querylist.qZl)
                {
                    postStr.Append("&cxzl=tzzl");
                }
                if (querylist.qMzf)
                {
                    postStr.Append("&cxmzf=tzmzf");
                }
                if (querylist.qJsb)
                {
                    postStr.Append("&cxjsb=tzjsb");
                }
            }
            #endregion

            postStr.Append("&_cxgxy=").Append("on");
            postStr.Append("&_cxjsb=").Append("on");
            postStr.Append("&_cxmzf=").Append("on");
            postStr.Append("&_cxnzz=").Append("on");
            postStr.Append("&_cxslc=").Append("on");
            postStr.Append("&_cxtlc=").Append("on");
            postStr.Append("&_cxtnb=").Append("on");
            postStr.Append("&_cxyyc=").Append("on");
            postStr.Append("&_cxzl=").Append("on");
            postStr.Append("&_cxzlc=").Append("on");
            postStr.Append("&_cxztc=").Append("on");
            postStr.Append("&_czqzxxfp=").Append("on");
            postStr.Append("&_dYhzgx=").Append("on");
            postStr.Append("&_fp=").Append("on");
            postStr.Append("&_kfxtss=").Append("on");
            postStr.Append("&_ljgxy=").Append("on");
            postStr.Append("&_tnlyc=").Append("on");
            postStr.Append("&_xzbysg=").Append("on");
            postStr.Append("&_zdxy=").Append("on");
            postStr.Append("&age1=").Append("");
            postStr.Append("&age2=").Append("");
            postStr.Append("&createuser=").Append("");
            postStr.Append("&dDalb=").Append("");
            postStr.Append("&dDazt=").Append("");
            postStr.Append("&dGrdabh=").Append("");
            postStr.Append("&dHyzk=").Append("");
            postStr.Append("&dJd=").Append(town);
            postStr.Append("&dJwh=").Append(vill);
            postStr.Append("&dJzzk=").Append("");
            postStr.Append("&dMz=").Append("");
            postStr.Append("&dSfrhyx=").Append("");
            postStr.Append("&dSfzh=").Append("");
            postStr.Append("&dSspq=").Append("");
            postStr.Append("&dWhcd=").Append("");
            postStr.Append("&dXb=").Append("");
            postStr.Append("&dXm=").Append("");
            postStr.Append("&dXnhh=").Append("");
            postStr.Append("&dXx=").Append("");
            postStr.Append("&dXxdz=").Append("");
            postStr.Append("&dYlbxh=").Append("");
            postStr.Append("&dZy=").Append("");
            postStr.Append("&dasfhg=").Append("");
            postStr.Append("&enddate=").Append(edate);
            postStr.Append("&enddc=").Append("");
            postStr.Append("&endgx=").Append("");
            postStr.Append("&endlr=").Append("");
            postStr.Append("&hxsjg=").Append("on");
            postStr.Append("&null=").Append("");

            if (loginKey.Length == 16)
            {
                postStr.Append("&pZcrgid=").Append(loginKey.Substring(0, 12));
            }
            else
            {
                postStr.Append("&pZcrgid=").Append(loginKey.Substring(0, 15));
            }

            postStr.Append("&pxbz=").Append("0");
            postStr.Append("&qxcx=").Append("");
            postStr.Append("&sign=").Append("false");
            postStr.Append("&startdate=").Append(sdate);
            postStr.Append("&startdc=").Append("");
            postStr.Append("&startgx=").Append("");
            postStr.Append("&startlr=").Append("");
            postStr.Append("&updateuser=").Append("");
            postStr.Append("&val888").Append("1");
            #endregion

            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/grjbxxsearch.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);

            if (returnString.Contains("对不起，没有数据记录！"))
            {
                callback("EX-对不起，没有数据记录！");
                return null;
            }

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

            if (doc == null)
            {
                callback("请求超时，稍后重试");
                return null;
            }

            var div = doc.DocumentNode.SelectSingleNode("//div[@class='page_and_btn']");//获取page的div
            var snode = div.SelectSingleNode("//input[@id='all']");//获取总页数
            var total = snode == null ? "0" : snode.Attributes["value"].Value.Trim();//获取总页数

            snode = div.SelectSingleNode("//ul/li[1]");//获取总笔数

            var totalCount = snode == null ? "0" : snode.InnerText.Replace("总数：", "").Trim();//获取总笔数

            int totalInt = 0;

            int.TryParse(total, out totalInt);//总页数
            int.TryParse(total, out pageSum);

            int.TryParse(totalCount, out totalRows);//总笔数

            if (!string.IsNullOrEmpty(EndIndex))
            {
                int endIndexInfo = Convert.ToInt32(EndIndex);
                if (endIndexInfo > totalRows)
                {
                    endIndexInfo = totalRows;
                }
                else
                {
                    totalRows = endIndexInfo;
                }
            }

            int startIndexInfo = 1;
            if (!string.IsNullOrEmpty(StartIndex))
            {
                startIndexInfo = Convert.ToInt32(StartIndex);
            }

            currentIndex = 1;
            List<PersonModel> listPerson = new List<PersonModel>();
            var nodes = doc.DocumentNode.SelectNodes("//tr");

            for (var i = 1; i < nodes.Count; i++)
            {
                var node = nodes[i].SelectNodes("td");

                // 按笔数下载
                if (currentIndex < startIndexInfo)
                {
                    currentIndex++;
                    thispagecurrent++;
                    continue;
                }

                if (thispagecurrent > totalRows)
                {
                    break;
                }

                if (string.IsNullOrEmpty(node[6].InnerText.Trim()))
                {
                    currentIndex++;
                    continue;
                }

                PersonModel person = new PersonModel();
                person.pid = node[2].InnerText;
                string pid = node[2].SelectSingleNode("a[1]").OuterHtml;
                if (!string.IsNullOrEmpty(pid))
                {
                    pid = HtmlHelper.GetLastTagValue(pid, "dGrdabh=", "\"");
                }
                if (!string.IsNullOrEmpty(pid))
                {
                    person.pid = pid;
                }

                person.idNumber = node[6].InnerText;

                person.memberName = node[3].InnerText;
                person.fid = node[1].SelectSingleNode("a[1]").Attributes.Contains("onclick") ? node[1].SelectSingleNode("a").Attributes["onclick"].Value.Replace("jtgx('", "").Replace("')", "") : "";
                listPerson.Add(person);

                thispagecurrent++;
            }

            lstPerson.AddRange(listPerson);

            return listPerson;
        }

        private void GetGrdaKeyAndInfo(params Action<string>[] callbackAll)
        {
            int pageSum = 0;
            List<PersonModel> listPerson = GetGrdaFirstKeyAndInfo(out pageSum, callbackAll);

            //开始获取当前页信息
            if (listPerson == null)
            {
                return;
            }
            GetGrdaInfo(listPerson, callbackAll);

            //按照笔数下载计算下载页
            int pstartPage = 2;

            int startIndexInfo = 1;

            if (!string.IsNullOrEmpty(StartIndex))
            {
                startIndexInfo = Convert.ToInt32(StartIndex);

                int ptmp = (startIndexInfo - 1) / pageSize + 1;

                if (ptmp > pstartPage)
                {
                    pstartPage = ptmp;

                    currentIndex = startIndexInfo;
                }
            }

            //翻页
            for (var i = pstartPage; i < pageSum + 1; i++)
            {
                if (thispagecurrent > totalRows)
                {
                    break;
                }

                //指定页的人员信息获取，并获取数据
                List<PersonModel> personS = GetPageInfo(i, callbackAll);
                GetGrdaInfo(personS, callbackAll);
            }
        }

        //根据页码，获取指定页信息
        public List<PersonModel> GetPageInfo(int pageNum, params Action<string>[] callbackAll)
        {
            //个人回执
            Action<string> callback = callbackAll[0];

            WebHelper web = new WebHelper();

            StringBuilder postStr = new StringBuilder();

            #region
            postStr.Append("_cxgxb=").Append("on");

            #region 查询条件2017-05-03
            string town = "";
            string vill = "";
            if (querylist != null)
            {
                if (!string.IsNullOrEmpty(querylist.qTown))
                {
                    town = querylist.qTown;
                }
                if (!string.IsNullOrEmpty(querylist.qVill))
                {
                    vill = querylist.qVill;
                }
                if (querylist.qLnr)
                {
                    postStr.Append("&age=65");
                }
                if (querylist.qGxy)
                {
                    postStr.Append("&cxgxy=tzgxy");
                }
                if (querylist.qTnb)
                {
                    postStr.Append("&cxtnb=tztnb");
                }
                if (querylist.qGxb)
                {
                    postStr.Append("&cxgxb=tzgxb");
                }
                if (querylist.qNcz)
                {
                    postStr.Append("&cxnzz=tznzz");
                }
                if (querylist.qZl)
                {
                    postStr.Append("&cxzl=tzzl");
                }
                if (querylist.qMzf)
                {
                    postStr.Append("&cxmzf=tzmzf");
                }
                if (querylist.qJsb)
                {
                    postStr.Append("&cxjsb=tzjsb");
                }
            }
            #endregion

            postStr.Append("&_cxgxy=").Append("on");
            postStr.Append("&_cxjsb=").Append("on");
            postStr.Append("&_cxmzf=").Append("on");
            postStr.Append("&_cxnzz=").Append("on");
            postStr.Append("&_cxslc=").Append("on");
            postStr.Append("&_cxtlc=").Append("on");
            postStr.Append("&_cxtnb=").Append("on");
            postStr.Append("&_cxyyc=").Append("on");
            postStr.Append("&_cxzl=").Append("on");
            postStr.Append("&_cxzlc=").Append("on");
            postStr.Append("&_cxztc=").Append("on");
            postStr.Append("&_czqzxxfp=").Append("on");
            postStr.Append("&_dYhzgx=").Append("on");
            postStr.Append("&_fp=").Append("on");
            postStr.Append("&_kfxtss=").Append("on");
            postStr.Append("&_ljgxy=").Append("on");
            postStr.Append("&_tnlyc=").Append("on");
            postStr.Append("&_xzbysg=").Append("on");
            postStr.Append("&_zdxy=").Append("on");
            postStr.Append("&age1=").Append("");
            postStr.Append("&age2=").Append("");
            postStr.Append("&createuser=").Append("");
            postStr.Append("&dDalb=").Append("");
            postStr.Append("&dDazt=").Append("");
            postStr.Append("&dGrdabh=").Append("");
            postStr.Append("&dHyzk=").Append("");
            postStr.Append("&dJd=").Append(town);
            postStr.Append("&dJwh=").Append(vill);
            postStr.Append("&dJzzk=").Append("");
            postStr.Append("&dMz=").Append("");
            postStr.Append("&dSfrhyx=").Append("");
            postStr.Append("&dSfzh=").Append("");
            postStr.Append("&dSspq=").Append("");
            postStr.Append("&dWhcd=").Append("");
            postStr.Append("&dXb=").Append("");
            postStr.Append("&dXm=").Append("");
            postStr.Append("&dXnhh=").Append("");
            postStr.Append("&dXx=").Append("");
            postStr.Append("&dXxdz=").Append("");
            postStr.Append("&dYlbxh=").Append("");
            postStr.Append("&dZy=").Append("");
            postStr.Append("&dasfhg=").Append("");
            postStr.Append("&enddate=").Append(edate);
            postStr.Append("&enddc=").Append("");
            postStr.Append("&endgx=").Append("");
            postStr.Append("&endlr=").Append("");
            postStr.Append("&hxsjg=").Append("on");
            postStr.Append("&null=").Append("");

            if (loginKey.Length == 16)
            {
                postStr.Append("&pZcrgid=").Append(loginKey.Substring(0, 12));
            }
            else
            {
                postStr.Append("&pZcrgid=").Append(loginKey.Substring(0, 15));
            }

            postStr.Append("&pxbz=").Append("0");
            postStr.Append("&qxcx=").Append("");
            postStr.Append("&sign=").Append("false");
            postStr.Append("&startdate=").Append(sdate);
            postStr.Append("&startdc=").Append("");
            postStr.Append("&startgx=").Append("");
            postStr.Append("&startlr=").Append("");
            postStr.Append("&updateuser=").Append("");
            postStr.Append("&val888").Append("1");

            postStr.Append("&status=").Append("ajax");
            postStr.Append("&page.currentPage=").Append(pageNum);
            #endregion

            //查询请求执行
            string returnString = web.PostHttp(baseUrl + "/grjbxxsearch.action", postStr.ToString(), "application/x-www-form-urlencoded", SysCookieContainer);
            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            if (doc == null)
            {
                callback("请求超时，稍后重试");
                return null;
            }

            int startIndexInfo = 1;
            if (!string.IsNullOrEmpty(StartIndex))
            {
                startIndexInfo = Convert.ToInt32(StartIndex);
            }

            thispagecurrent = pageSize * (pageNum - 1) + 1;

            List<PersonModel> listPerson = new List<PersonModel>();
            var nodes = doc.DocumentNode.SelectNodes("//tr");

            if (nodes != null)
            {
                for (var i = 1; i < nodes.Count; i++)
                {
                    var node = nodes[i].SelectNodes("td");

                    if (thispagecurrent < startIndexInfo)
                    {
                        thispagecurrent++;
                        continue;
                    }

                    if (thispagecurrent > totalRows)
                    {
                        break;
                    }

                    if (string.IsNullOrEmpty(node[6].InnerText.Trim()))
                    {
                        currentIndex++;
                        continue;
                    }

                    PersonModel person = new PersonModel();
                    person.pid = node[2].InnerText;
                    string pid = node[2].SelectSingleNode("a[1]").OuterHtml;
                    if (!string.IsNullOrEmpty(pid))
                    {
                        pid = HtmlHelper.GetLastTagValue(pid, "dGrdabh=", "\"");
                    }
                    if (!string.IsNullOrEmpty(pid))
                    {
                        person.pid = pid;
                    }

                    person.idNumber = node[6].InnerText;

                    person.memberName = node[3].InnerText;
                    person.fid = node[1].SelectSingleNode("a[1]").Attributes.Contains("onclick") ? node[1].SelectSingleNode("a").Attributes["onclick"].Value.Replace("jtgx('", "").Replace("')", "") : "";
                    listPerson.Add(person);

                    thispagecurrent++;
                }
            }

            lstPerson.AddRange(listPerson);
            return listPerson;
        }

        /// <summary>
        /// 根据信息，获取个人档案
        /// </summary>
        /// <param name="listPerson"></param>
        /// <param name="callback"></param>
        private void GetGrdaInfo(List<PersonModel> listPerson, params Action<string>[] callbackAll)
        {
            //个人回执
            Action<string> callback = callbackAll[0];

            foreach (PersonModel pm in listPerson)
            {
                GetGrda(pm, 1, callbackAll);

                callback("下载-个人基本信息档案..." + currentIndex + "/" + totalRows);

                currentIndex++;
            }
        }

        // 身份证下载 
        private void GetGrda(PersonModel pm, int tryCount, params Action<string>[] callbackAll)
        {
            string idcard = pm.idNumber.ToString();

            // 个人回执
            Action<string> callback = callbackAll[0];

            try
            {
                TryGetGrdaInfo(pm, tryCount, callback);
            }
            catch (Exception ex)
            {
                CommonExtensions.WriteLog("EX-身份证号:" + idcard + ",姓名：" + pm.memberName + ",个人档案下载数据异常:" + ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);

                if (tryCount < MaxtryCount)
                {
                    callback("EX-个人基本信息档案:身份证[" + idcard + "],姓名：" + pm.memberName + ",:下载信息失败。重新尝试获取第" + tryCount + "次...");

                    System.Threading.Thread.Sleep(SleepMilliseconds);

                    tryCount++;
                    GetGrda(pm, tryCount, callbackAll);
                }
                else
                {
                    callback("EX-个人基本信息档案:身份证[" + idcard + "],姓名：" + pm.memberName + ",:下载信息失败。请确保网路畅通。");
                }
            }

            callback("下载-个人基本信息档案..." + currentIndex + "/" + totalRows);

            // 当不勾选只下载个人档案时，在下载体检及随访信息
            if (onlyGr == false && onlydah == false)
            {
                // 体检回执
                Action<string> callback1 = callbackAll[1];
                // 老年人回执
                Action<string> callback2 = callbackAll[2];

                // 高血压回执
                Action<string> callback3 = callbackAll[3];

                // 糖尿病回执
                Action<string> callback4 = callbackAll[4];

                // 脑卒中回执
                Action<string> callback5 = callbackAll[5];

                // 冠心病回执
                Action<string> callback6 = callbackAll[6];

                ////家庭
                //Action<string> callback7 = callbackAll[7];

                Task task1 = Task.Factory.StartNew(() =>
                {
                    TjBusiness.TjBusiness tj = new TjBusiness.TjBusiness();
                    tj.SysCookieContainer = SysCookieContainer;
                    tj.loginKey = loginKey;
                    tj.totalRows = totalRows;

                    tj.currentIndex = currentIndex;
                    tj.DownInfoByPerson(pm, callback1);
                });

                Task task2 = Task.Factory.StartNew(() =>
                {
                    LnrBusiness.LnrBusiness lnr = new LnrBusiness.LnrBusiness();
                    lnr.SysCookieContainer = SysCookieContainer;
                    lnr.loginkey = loginKey;
                    lnr.totalRows = totalRows;

                    lnr.currentIndex = currentIndex;
                    lnr.DownInfoByPerson(pm, callback2);
                });

                Task task3 = Task.Factory.StartNew(() =>
                {
                    GxyBusiness.GxyBusiness gxy = new GxyBusiness.GxyBusiness();
                    gxy.SysCookieContainer = SysCookieContainer;
                    gxy.loginkey = loginKey;
                    gxy.totalRows = totalRows;

                    gxy.currentIndex = currentIndex;
                    gxy.DownInfoByPerson(pm, callback3);
                });

                Task task4 = Task.Factory.StartNew(() =>
                {
                    TnbBusiness.TnbBusiness tnb = new TnbBusiness.TnbBusiness();
                    tnb.SysCookieContainer = SysCookieContainer;
                    tnb.loginkey = loginKey;
                    tnb.totalRows = totalRows;

                    tnb.currentIndex = currentIndex;
                    tnb.DownInfoByPerson(pm, callback4);
                });

                Task task5 = Task.Factory.StartNew(() =>
                {
                    NczBusiness.NczBusiness ncz = new NczBusiness.NczBusiness();
                    ncz.SysCookieContainer = SysCookieContainer;
                    ncz.loginkey = loginKey;
                    ncz.totalRows = totalRows;

                    ncz.currentIndex = currentIndex;
                    ncz.DownInfoByPerson(pm, callback5);
                });

                Task task6 = Task.Factory.StartNew(() =>
                {
                    GxbBusiness.GxbBusiness gxb = new GxbBusiness.GxbBusiness();
                    gxb.SysCookieContainer = SysCookieContainer;
                    gxb.loginkey = loginKey;
                    gxb.totalRows = totalRows;

                    gxb.currentIndex = currentIndex;
                    gxb.DownInfoByPerson(pm, callback6);
                });

                //Task task7 = Task.Factory.StartNew(() =>
                //{
                //    JtBusiness.JtBusiness jt = new JtBusiness.JtBusiness();
                //    jt.SysCookieContainer = SysCookieContainer;
                //    jt.loginKey = loginKey;
                //    jt.totalRows = totalRows;

                //    jt.currentIndex = currentIndex;
                //    jt.DownJTByPerson(pm, callback7);
                //});


                Task.WaitAll(task1, task2, task3, task4, task5, task6);//, task7
            }
        }

        /// <summary>
        /// 获取基本信息
        /// </summary>
        /// <param name="person"></param>
        /// <param name="tryCount"></param>
        /// <param name="callback"></param>
        private void TryGetGrdaInfo(PersonModel person, int tryCount, Action<string> callback)
        {
            string idNumber = person.idNumber;

            try
            {
                WebHelper web = new WebHelper();

                string postData = "dah=" + person.pid + "&tz=2";
                string returnString = web.PostHttp(baseUrl + "/healthArchives/updateArchives.action", postData, "application/x-www-form-urlencoded", SysCookieContainer);
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);

                DataSet saveDS = new DataSet();
                if (onlydah)
                {
                    DataSet dsT = DataSetTmp.RecordIdDataSet;

                    DataTable dtbase = dsT.Tables["ARCHIVE_BASEINFO"].Clone();
                    DataRow dr = dtbase.NewRow();

                    dr["IDCardNo"] = idNumber;
                    dr["RecordID"] = person.pid;

                    //var node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
                    //string temStr = "";
                    //if (node != null)
                    //{
                    //    temStr = node.ParentNode.InnerText;
                    //}
                    //dr["CreateMenName"] = temStr.Trim().Replace("\r", "").Replace("\n", "");

                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='dhzsfzh']");
                    //dr["FamilyIDCardNo"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    dtbase.Rows.Add(dr);
                    saveDS.Tables.Add(dtbase);

                    CommonBusiness.CommonDAOBusiness dao = new CommonBusiness.CommonDAOBusiness();
                    dao.SaveDataSet(saveDS, idNumber, "", "", "", true);
                    saveDS.Tables.Clear();
                }
                else
                {
                    DataSet dsT = DataSetTmp.GrdaDataSet;

                    DataTable baseInfo = dsT.Tables["ARCHIVE_BASEINFO"].Clone();
                    DataRow dr = baseInfo.NewRow();
                    #region ARCHIVE_BASEINFO

                    // dr["RecordID"] = person.pid;

                    dr["IDCardNo"] = idNumber;
                    dr["RecordID"] = person.pid;

                    //var node = doc.DocumentNode.SelectSingleNode("//input[@id='123456']");
                    //dr["OrgProvinceID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='123456']");
                    //dr["OrgCityID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='123456']");
                    //dr["OrgDistrictID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='123456']");
                    //dr["OrgTownID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='123456']");
                    //dr["OrgVillageID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    dr["ProvinceID"] = "37";

                    var node = doc.DocumentNode.SelectSingleNode("//select[@name='dShi']/option[@selected]");
                    dr["CityID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//select[@name='dQu']/option[@selected]");
                    dr["DistrictID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//select[@name='dJd']/option[@selected]");
                    dr["TownID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["TownName"] = node == null ? "" : node.NextSibling.InnerText;

                    node = doc.DocumentNode.SelectSingleNode("//select[@name='dJwh']/option[@selected]");
                    dr["VillageID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["VillageName"] = node == null ? "" : node.NextSibling.InnerText;

                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dGzdw']");
                    dr["WorkUnit"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //常住类型
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='dJzzk']/option[@selected]"); //常住类型
                    string temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["LiveType"] = temStr == "" || temStr == "4" ? "2" : "1";

                    //民族
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='dMz']/option[@selected]");
                    temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["Nation"] = temStr == "" || temStr == "1" ? "1" : "2";
                    dr["Minority"] = GetNationNameByWebNum(temStr);

                    //RH
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='jkzk.dSfrhyx']/option[@selected]");
                    dr["RH"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //文化程度
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='dWhcd']/option[@selected]");
                    temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["Culture"] = GetCulturecodeByWebNum(temStr);

                    //职业
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dZy'][@checked]");
                    temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["Job"] = GetJobCodeByWebNum(temStr);
                    //婚姻状况
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='dHyzk']/option[@selected]");
                    temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["MaritalStatus"] = GetMaritalCodeByWebNum(temStr);
                    //支付方式
                    var nodes = doc.DocumentNode.SelectNodes("//input[@name='dYlfzflx'][@checked]");
                    dr["MedicalPayType"] = GetMedicalPayTypeCodesByWebNodes(nodes);

                    // 城镇或省直职工基本医疗保险-医疗保险号
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dYlbxh']");
                    dr["TownMedicalCard"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    // 居民基本医疗保险-医疗保险号
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dXnhh']");
                    dr["ResidentMedicalCard"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    // 贫困救助-卡号
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='pkjzkh']");
                    dr["PovertyReliefMedicalCard"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //药物过敏史
                    nodes = doc.DocumentNode.SelectNodes("//input[@name='jkzk.dYgms'][@checked]");
                    dr["DrugAllergic"] = GetDrugAllergicByWebNodes(nodes);
                    //遗传病史有无
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='jkzk.dYcbs']/option[@selected]");
                    temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["Disease"] = temStr == "" || temStr == "2" ? "1" : "2";
                    //残疾情况
                    nodes = doc.DocumentNode.SelectNodes("//input[@name='jkzk.dCjmz'][@checked]");
                    dr["DiseasEndition"] = GetDiseasCodeByWebNodes(nodes);
                    //姓名
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dXm']");
                    dr["CustomerName"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //责任医生
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='updateuser']");
                    temStr = "";
                    if (node != null)
                    {
                        temStr = node.ParentNode.InnerText;
                    }
                    dr["Doctor"] = temStr;
                    //sex
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='dXb']/option[@selected]");
                    dr["Sex"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //生日
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dCsrq']");
                    dr["Birthday"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    DateTime dtbirth = new DateTime();
                    if (!DateTime.TryParse(dr["Birthday"].ToString(), out dtbirth))
                    {
                        dr["Birthday"] = "";
                    }

                    //联系人
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dLxrxm']");
                    dr["ContactName"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //联系人Phone
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dLxrdh']");
                    dr["ContactPhone"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //血型
                    temStr = "";
                    nodes = doc.DocumentNode.SelectNodes("//select[@id='jkzk.dXx']/option[@selected]");
                    if (nodes != null && nodes.Count > 0)
                    {
                        foreach (var n in nodes)
                        {
                            temStr = n == null || !n.Attributes.Contains("value") ? "" : n.Attributes["value"].Value;
                            if (!string.IsNullOrEmpty(temStr))
                            {
                                break;
                            }
                        }
                    }
                    dr["BloodType"] = GetBooldTypeByWebNum(temStr);

                    //电话
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dLxdh']");
                    dr["Phone"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //字符方式其他
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='dYlfzflxqt']");
                    dr["MedicalPayTypeOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //药物过敏史其他
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dGmsqt']");
                    dr["DrugAllergicOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //遗传病史其他
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dYcbsjb']");
                    dr["DiseaseEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //残疾其他
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dCjqt']");
                    dr["DiseasenditionEx"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='123456']");
                    //dr["CustomerID"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //现住址

                    dr["Address"] = GetAddressByDocument(doc);
                    node = doc.DocumentNode.SelectSingleNode("//select[@name='dShi']/option[@selected]");
                    temStr = "";
                    if (node != null && node.Attributes.Contains("value") && !string.IsNullOrEmpty(node.Attributes["value"].Value))
                    {
                        temStr = node.NextSibling.InnerText;
                    }
                    //dr["HouseHoldAddress"] = temStr;
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='pRgid']");
                    temStr = "";
                    if (node != null)
                    {
                        temStr = node.ParentNode.InnerText;
                    }

                    dr["CreateUnitName"] = temStr.Trim();

                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='pRgid']");
                    //dr["CreateUnit"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    temStr = "";
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dBlshxp']");
                    if (node != null && node.Attributes.Contains("value") && !string.IsNullOrEmpty(node.Attributes["value"].Value))
                    {
                        temStr += ",2";
                    }
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dBlsdw']");
                    if (node != null && node.Attributes.Contains("value") && !string.IsNullOrEmpty(node.Attributes["value"].Value))
                    {
                        temStr += ",3";
                    }
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jkzk.dBlssx']");
                    if (node != null && node.Attributes.Contains("value") && !string.IsNullOrEmpty(node.Attributes["value"].Value))
                    {
                        temStr += ",4";
                    }
                    temStr = temStr.TrimStart(',');
                    dr["Exposure"] = temStr == "" ? "1" : temStr;
                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='123456']");
                    //dr["CreateBy"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='createtime']");
                    dr["CreateDate"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='createuser']");
                    temStr = "";
                    if (node != null)
                    {
                        temStr = node.ParentNode.InnerText;
                    }
                    dr["CreateMenName"] = temStr.Trim().Replace("\r", "").Replace("\n", "");


                    //最后修改时间 /更新日期  

                    dr["LastUpdateDate"] = DateTime.Now.ToString("yyyy-MM-dd");

                    /* nodes = node.SelectNodes("//input[@name='jb0'][@checked]");
                     temStr = "";

                     if (nodes != null)
                     {
                         foreach (var n in nodes)
                         {
                             if (n.Attributes.Contains("value"))
                             {
                                 string nval = n.Attributes["value"].Value;

                                 if (nval == "2")
                                 {
                                     temStr+=","+"";
                                 }
                             }
                         }
                     }

                     dr["PopulationType"] = temStr;*/
                    //人群
                    //node = doc.DocumentNode.SelectSingleNode("//input[@id='123456']");
                    //dr["PopulationType"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    //与户主关系
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='dYhzgx']/option[@selected]");
                    dr["HouseRelation"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    #region 2017-10-19修改
                    //家庭情况
                    node = doc.DocumentNode.SelectSingleNode("//input[@id='hzxm']");
                    dr["HouseName"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@id='hzsfzh']");
                    dr["FamilyIDCardNo"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jtrks']");
                    dr["FamilyNum"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@id='jtjg']");
                    dr["FamilyStructure"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//select[@id='djzqk']/option[@selected]");
                    if (node == null)
                    {
                        node = doc.DocumentNode.SelectSingleNode("//input[@name='tgrjbxjzqk'][@checked]");
                    }
                    temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["LiveCondition"] = temStr;

                    //孕产情况
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='lHyqk'][@checked]");
                    temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    dr["PreSituation"] = GetPreSituationByWeb(temStr);

                    node = doc.DocumentNode.SelectSingleNode("//select[@name='lYc']/option[@selected]");
                    dr["PreNum"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//select[@name='lCc']/option[@selected]");
                    dr["YieldNum"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //化学、毒物、射线
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='jkzk.dBlshxp']");
                    dr["Chemical"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='jkzk.dBlsdw']");
                    dr["Poison"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='jkzk.dBlssx']");
                    dr["Radial"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    //健康档案信息卡
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='dSfzdz']");
                    dr["HouseHoldAddress"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    DataTable healthDT = dsT.Tables["archive_health_info"].Clone();
                    DataRow drHel = healthDT.NewRow();

                    nodes = doc.DocumentNode.SelectNodes("//input[@name='hbqk'][@checked]");
                    temStr = "";
                    if (nodes != null && nodes.Count > 0)
                    {
                        foreach (var n in nodes)
                        {
                            temStr += n == null || !n.Attributes.Contains("value") ? "" : n.Attributes["value"].Value + ",";
                        }
                    }
                    drHel["IDCardNo"] = person.idNumber;
                    drHel["Prevalence"] = temStr.TrimEnd(',');

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='hbqkqt']");
                    drHel["PrevalenceOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='jdjglxdh']");
                    drHel["OrgTelphone"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='jtzrys']");
                    drHel["FamilyDoctor"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='jtzryslxdh']");
                    drHel["FamilyDoctorTel"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='sqzrhs']");
                    drHel["Nurses"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='sqzrhslxdh']");
                    drHel["NursesTel"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='ggwsry']");
                    drHel["HealthPersonnel"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='ggwsrylxdh']");
                    drHel["HealthPersonnelTel"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    node = doc.DocumentNode.SelectSingleNode("//input[@name='qtsm']");
                    drHel["Others"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;

                    healthDT.Rows.Add(drHel);
                    saveDS.Tables.Add(healthDT);
                    #endregion

                    #endregion

                    DataTable huanjingDT = dsT.Tables["ARCHIVE_BASEINFOARCHIVE_ENVIRONMENT"].Clone();
                    DataRow drH = huanjingDT.NewRow();
                    #region ARCHIVE_BASEINFOARCHIVE_ENVIRONMENT
                    //drH["RecordID"] = person.pid;

                    drH["IDCardNo"] = person.idNumber;
                    nodes = doc.DocumentNode.SelectNodes("//input[@name='cfpqsb'][@checked]");
                    temStr = "";
                    if (nodes != null)
                    {
                        foreach (var n in nodes)
                        {
                            if (n.Attributes.Contains("value"))
                            {
                                //pad 只能单选
                                //temStr += "," + n.Attributes["value"].Value;
                                temStr = n.Attributes["value"].Value;
                                break;
                            }
                        }
                    }
                    drH["BlowMeasure"] = temStr.TrimStart(',');
                    nodes = doc.DocumentNode.SelectNodes("//input[@name='rllx'][@checked]");
                    temStr = "";
                    if (nodes != null)
                    {
                        foreach (var n in nodes)
                        {
                            if (n.Attributes.Contains("value"))
                            {
                                //pad 只能单选
                                //temStr += "," + n.Attributes["value"].Value;
                                temStr = n.Attributes["value"].Value;
                                break;
                            }
                        }
                    }
                    drH["FuelType"] = temStr.TrimStart(',');
                    nodes = doc.DocumentNode.SelectNodes("//input[@name='ys'][@checked]");
                    temStr = "";
                    if (nodes != null)
                    {
                        foreach (var n in nodes)
                        {
                            if (n.Attributes.Contains("value"))
                            {
                                //pad 只能单选
                                //temStr += "," + n.Attributes["value"].Value;
                                temStr = n.Attributes["value"].Value;
                                break;
                            }
                        }
                    }
                    drH["DrinkWater"] = temStr.TrimStart(',');
                    nodes = doc.DocumentNode.SelectNodes("//input[@name='cs'][@checked]");
                    temStr = "";
                    if (nodes != null)
                    {
                        foreach (var n in nodes)
                        {
                            if (n.Attributes.Contains("value"))
                            {
                                //pad 只能单选
                                //temStr += "," + n.Attributes["value"].Value;
                                temStr = n.Attributes["value"].Value;
                                break;
                            }
                        }
                    }

                    drH["Toilet"] = temStr.TrimStart(',');
                    nodes = doc.DocumentNode.SelectNodes("//input[@name='qxl'][@checked]");
                    temStr = "";
                    if (nodes != null)
                    {
                        foreach (var n in nodes)
                        {
                            if (n.Attributes.Contains("value"))
                            {
                                //pad 只能单选
                                //temStr += "," + n.Attributes["value"].Value;
                                temStr = getQclForPad(n.Attributes["value"].Value);
                                break;
                            }
                        }
                    }
                    drH["LiveStockRail"] = temStr.TrimStart(',');

                    #endregion
                    huanjingDT.Rows.Add(drH);
                    saveDS.Tables.Add(huanjingDT);

                    DataTable familyHistroy = dsT.Tables["ARCHIVE_FAMILYHISTORYINFO"].Clone();

                    #region  ARCHIVE_FAMILYHISTORYINFO
                    //SelectSingleNode("//select[@id='dJzzk']/option[@selected]")
                    node = doc.DocumentNode.SelectSingleNode("//select[@id='Jkzk.dYwjb']/option[@selected]");
                    if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                    {
                        nodes = doc.DocumentNode.SelectSingleNode("//table[@id='addjz']").SelectNodes("tr");
                        DataRow drF = familyHistroy.NewRow();

                        //drF["RecordID"] = person.pid;
                        drF["IDCardNo"] = person.idNumber;
                        string m = "";
                        string f = "";
                        string s = "";
                        string c = "";
                        string mo = "";
                        string fo = "";
                        string so = "";
                        string co = "";
                        foreach (var no in nodes)
                        {
                            var tds = no.SelectNodes("td");
                            var n = tds[3].SelectSingleNode("div/select[@id='jkzkjzbs2']/option[@selected]");
                            string tem = "";
                            if (n != null && n.Attributes.Contains("value"))
                            {
                                tem = GetFamilyTypeByWebCode(n.Attributes["value"].Value);
                            }
                            var ns = tds[1].SelectNodes("input[@checked]");
                            string temsss = "";
                            if (ns != null)
                            {
                                foreach (var nn in ns)
                                {
                                    if (nn.Attributes.Contains("value"))
                                    {
                                        temsss += "," + GetFamily(nn.Attributes["value"].Value);
                                    }
                                }
                            }
                            string other = "";
                            n = tds[1].SelectSingleNode("input[@id='qtwt4']");
                            if (n != null && n.Attributes.Contains("value"))
                            {
                                other = n.Attributes["value"].Value;
                            }
                            switch (tem)
                            {
                                case "1":
                                    f += temsss;
                                    fo += other;
                                    break;
                                case "2":
                                    m += temsss;
                                    mo += other;
                                    break;
                                case "3":
                                    s += temsss;
                                    so += other;
                                    break;

                                case "4":
                                    c += temsss;
                                    co += other;
                                    break;
                            }

                        }

                        drF["FatherHistory"] = f.TrimStart(',');

                        drF["FatherHistoryOther"] = fo;

                        drF["MotherHistory"] = m.TrimStart(',');

                        drF["MotherHistoryOther"] = mo;

                        drF["BrotherSisterHistory"] = s.TrimStart(',');

                        drF["BrotherSisterHistoryOther"] = so;

                        drF["ChildrenHistory"] = c.TrimStart(',');

                        drF["ChildrenHistoryOther"] = co;

                        familyHistroy.Rows.Add(drF);
                    }

                    #endregion

                    saveDS.Tables.Add(familyHistroy);

                    List<string> strARQ = new List<string>();

                    DataTable jiwang = dsT.Tables["ARCHIVE_ILLNESSHISTORYINFO"].Clone();

                    #region   ARCHIVE_ILLNESSHISTORYINFO
                    //疾病
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='isjb'][@checked]");
                    if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "1")
                    {
                        var jwsTds = doc.DocumentNode.SelectNodes("//td[@id='jbtdid']");

                        if (jwsTds != null)
                        {
                            foreach (var jws in jwsTds)
                            {
                                var chkBox = jws.SelectNodes("input[@checked]");

                                if (chkBox != null && chkBox.Count == 1)
                                {
                                    var c = chkBox[0];

                                    var tdnode = c.ParentNode;
                                    DataRow drJ = jiwang.NewRow();

                                    drJ["IDCardNo"] = person.idNumber;
                                    drJ["IllnessType"] = "1";

                                    var no = tdnode.SelectSingleNode("input[@name='exzl']");
                                    drJ["Therioma"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                                    no = tdnode.SelectSingleNode("input[@name='zybqt']");
                                    drJ["JobIllness"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                                    no = tdnode.SelectSingleNode("input[@name='jbqt']");

                                    drJ["IllnessOther"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                                    drJ["IllnessName"] = !c.Attributes.Contains("value") ? "" : c.Attributes["value"].Value;

                                    string strtmpRQ = "";
                                    switch (drJ["IllnessName"].ToString())
                                    {
                                        case "8":
                                            strtmpRQ = "5";
                                            break;
                                        case "2":
                                            strtmpRQ = "6";
                                            break;
                                        case "3":
                                            strtmpRQ = "7";
                                            break;
                                        case "4":
                                            strtmpRQ = "8";
                                            break;
                                        case "7":
                                            strtmpRQ = "9";
                                            break;
                                    }

                                    if (!string.IsNullOrEmpty(strtmpRQ) && !strARQ.Contains(strtmpRQ))
                                    {
                                        strARQ.Add(strtmpRQ);
                                    }

                                    no = tdnode.SelectSingleNode("input[@name='zdrq']");
                                    temStr = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                                    drJ["DiagnoseTime"] = temStr == "" || temStr.Length < 7 ? "" : temStr.Substring(0, 7) + "-01";

                                    jiwang.Rows.Add(drJ);
                                }
                                else if (chkBox != null && chkBox.Count > 1)
                                {
                                    foreach (var c in chkBox)
                                    {
                                        var tdnode = c.ParentNode;

                                        DataRow drJ = jiwang.NewRow();

                                        drJ["IDCardNo"] = person.idNumber;
                                        drJ["IllnessType"] = "1";

                                        var no = tdnode.SelectSingleNode("input[@name='exzl']");
                                        drJ["Therioma"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                                        no = tdnode.SelectSingleNode("input[@name='zybqt']");
                                        drJ["JobIllness"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                                        no = tdnode.SelectSingleNode("input[@name='jbqt']");

                                        drJ["IllnessOther"] = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                                        drJ["IllnessName"] = !c.Attributes.Contains("value") ? "" : c.Attributes["value"].Value;

                                        string strtmpRQ = "";
                                        switch (drJ["IllnessName"].ToString())
                                        {
                                            case "8":
                                                strtmpRQ = "5";
                                                break;
                                            case "2":
                                                strtmpRQ = "6";
                                                break;
                                            case "3":
                                                strtmpRQ = "7";
                                                break;
                                            case "4":
                                                strtmpRQ = "8";
                                                break;
                                            case "7":
                                                strtmpRQ = "9";
                                                break;
                                        }

                                        if (!string.IsNullOrEmpty(strtmpRQ) && !strARQ.Contains(strtmpRQ))
                                        {
                                            strARQ.Add(strtmpRQ);
                                        }

                                        no = tdnode.SelectSingleNode("input[@name='zdrq']");
                                        temStr = no == null || !no.Attributes.Contains("value") ? "" : no.Attributes["value"].Value;

                                        drJ["DiagnoseTime"] = temStr == "" || temStr.Length < 7 ? "" : temStr.Substring(0, 7) + "-01";

                                        jiwang.Rows.Add(drJ);
                                    }
                                }
                            }
                        }
                    }

                    //手术
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='isshsh'][@checked]");
                    if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "2")
                    {
                        node = doc.DocumentNode.SelectSingleNode("//table[@id='tabss']");
                        nodes = node.SelectNodes("tr[position()>1]");
                        if (nodes != null)
                        {
                            foreach (var nn in nodes)
                            {
                                DataRow drS = jiwang.NewRow();
                                //drS["RecordID"] = person.pid;
                                drS["IDCardNo"] = person.idNumber;
                                drS["IllnessType"] = "2";
                                node = nn.SelectNodes("td")[0].SelectSingleNode("input");
                                drS["IllnessNameOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                                node = nn.SelectNodes("td")[1].SelectSingleNode("input");
                                temStr = "";
                                temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                                drS["DiagnoseTime"] = temStr == "" || temStr.Length < 7 ? "" : temStr.Substring(0, 7) + "-01";

                                jiwang.Rows.Add(drS);
                            }
                        }
                    }

                    //外伤
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='iswsh'][@checked]");
                    if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "2")
                    {
                        node = doc.DocumentNode.SelectSingleNode("//table[@id='tabws']");

                        nodes = node.SelectNodes("tr[position()>1]");
                        if (nodes != null)
                        {
                            foreach (var no in nodes)
                            {
                                DataRow drS = jiwang.NewRow();
                                //drS["RecordID"] = person.pid;
                                drS["IDCardNo"] = person.idNumber;
                                drS["IllnessType"] = "3";
                                node = no.SelectSingleNode("//input[@name='wsmc']");
                                drS["IllnessNameOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                                node = no.SelectSingleNode("//input[@name='wsrq']");

                                temStr = "";
                                temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                                drS["DiagnoseTime"] = temStr == "" || temStr.Length < 7 ? "" : temStr.Substring(0, 7) + "-01";

                                jiwang.Rows.Add(drS);
                            }
                        }
                    }

                    //输血
                    node = doc.DocumentNode.SelectSingleNode("//input[@name='isshx'][@checked]");
                    if (node != null && node.Attributes.Contains("value") && node.Attributes["value"].Value == "2")
                    {
                        node = doc.DocumentNode.SelectSingleNode("//table[@id='tabsx']");
                        nodes = node.SelectNodes("tr[position()>1]");
                        if (nodes != null)
                        {
                            foreach (var no in nodes)
                            {
                                DataRow drS = jiwang.NewRow();
                                //drS["RecordID"] = person.pid;
                                drS["IDCardNo"] = person.idNumber;
                                drS["IllnessType"] = "4";
                                node = no.SelectSingleNode("//input[@name='sxmc']");
                                drS["IllnessNameOther"] = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                                node = no.SelectSingleNode("//input[@name='sxrq']");

                                temStr = "";
                                temStr = node == null || !node.Attributes.Contains("value") ? "" : node.Attributes["value"].Value;
                                drS["DiagnoseTime"] = temStr == "" || temStr.Length < 7 ? "" : temStr.Substring(0, 7) + "-01";

                                jiwang.Rows.Add(drS);

                            }
                        }
                    }

                    #endregion

                    saveDS.Tables.Add(jiwang);

                    //儿童
                    DateTime dtbir1 = new DateTime();

                    if (DateTime.TryParse(dr["Birthday"].ToString(), out dtbir1))
                    {
                        DateTime dtNow = DateTime.Now;

                        int iNdtNow = Convert.ToInt32(dtNow.AddYears(-6).ToString("yyyyMMdd"));
                        int iNdtBir = Convert.ToInt32(dtbir1.ToString("yyyyMMdd"));

                        if (iNdtNow <= iNdtBir)
                        {
                            strARQ.Add("2");
                        }
                    }

                    //老年人
                    DateTime dtbir = new DateTime();

                    if (DateTime.TryParse(dr["Birthday"].ToString(), out dtbir))
                    {
                        DateTime dtNow = DateTime.Now;

                        int iNdtNow = Convert.ToInt32(dtNow.AddYears(-65).ToString("yyyyMMdd"));
                        int iNdtBir = Convert.ToInt32(dtbir.ToString("yyyyMMdd"));

                        if (iNdtNow >= iNdtBir)
                        {
                            strARQ.Add("4");
                        }
                    }

                    dr["PopulationType"] = string.Join(",", strARQ.ToArray());

                    baseInfo.Rows.Add(dr);
                    saveDS.Tables.Add(baseInfo);

                    CommonBusiness.CommonDAOBusiness dao = new CommonBusiness.CommonDAOBusiness();
                    dao.SaveDataSet(saveDS, idNumber);
                    saveDS.Tables.Clear();
                }

            }
            catch (Exception ex)
            {
                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);
            }
        }

        /// <summary>
        /// 孕产情况
        /// </summary>
        /// <param name="padCode"></param>
        /// <returns></returns>
        private string GetPreSituationByWeb(string padCode)
        {
            string code = "";
            switch (padCode)
            {
                case "未孕":
                    code = "1";
                    break;
                case "已孕未生产":
                    code = "2";
                    break;
                case "已生产随访期内":
                    code = "3";
                    break;
                case "已生产随访期外":
                    code = "4";
                    break;
                case "不详":
                    code = "5";
                    break;
                default:
                    break;
            }
            return code;
        }

        /// <summary>
        /// 根据web民族code 获取对应名族名称
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private string GetNationNameByWebNum(string num)
        {
            string nationName = "";
            switch (num)
            {
                case "1":
                    nationName = "汉族";
                    break;
                case "2":
                    nationName = "蒙古族";
                    break;
                case "3":
                    nationName = "回族";
                    break;
                case "4":
                    nationName = "藏族";
                    break;
                case "5":
                    nationName = "维吾尔族";
                    break;
                case "6":
                    nationName = "苗族";
                    break;
                case "7":
                    nationName = "彝族";
                    break;
                case "8":
                    nationName = "壮族";
                    break;
                case "9":
                    nationName = "布依族";
                    break;
                case "10":
                    nationName = "朝鲜族";
                    break;
                case "11":
                    nationName = "满族";
                    break;
                case "12":
                    nationName = "侗族";
                    break;
                case "13":
                    nationName = "瑶族";
                    break;
                case "14":
                    nationName = "白族";
                    break;
                case "15":
                    nationName = "土家族";
                    break;
                case "16":
                    nationName = "哈尼族";
                    break;
                case "17":
                    nationName = "哈萨克族";
                    break;
                case "18":
                    nationName = "傣族";
                    break;
                case "19":
                    nationName = "黎族";
                    break;
                case "20":
                    nationName = "僳僳族";
                    break;
                case "21":
                    nationName = "佤族";
                    break;
                case "22":
                    nationName = "畲族";
                    break;
                case "23":
                    nationName = "高山族";
                    break;
                case "24":
                    nationName = "拉祜族";
                    break;
                case "25":
                    nationName = "水族";
                    break;
                case "26":
                    nationName = "东乡族";
                    break;
                case "27":
                    nationName = "纳西族";
                    break;
                case "28":
                    nationName = "景颇族";
                    break;
                case "29":
                    nationName = "柯尔克孜族";
                    break;
                case "30":
                    nationName = "土族";
                    break;
                case "31":
                    nationName = "达斡尔族";
                    break;
                case "32":
                    nationName = "仫佬族";
                    break;
                case "33":
                    nationName = "羌族";
                    break;
                case "34":
                    nationName = "布朗族";
                    break;
                case "35":
                    nationName = "撒拉族";
                    break;
                case "36":
                    nationName = "毛南族";
                    break;
                case "37":
                    nationName = "仡佬族";
                    break;
                case "38":
                    nationName = "锡伯族";
                    break;
                case "39":
                    nationName = "阿昌族";
                    break;
                case "40":
                    nationName = "普米族";
                    break;
                case "41":
                    nationName = "塔吉克族";
                    break;
                case "42":
                    nationName = "怒族";
                    break;
                case "43":
                    nationName = "乌孜别克族";
                    break;
                case "44":
                    nationName = "俄罗斯族";
                    break;
                case "45":
                    nationName = "鄂温克族";
                    break;
                case "46":
                    nationName = "崩龙族";
                    break;
                case "47":
                    nationName = "保安族";
                    break;
                case "48":
                    nationName = "裕固族";
                    break;
                case "49":
                    nationName = "京族";
                    break;
                case "50":
                    nationName = "塔塔尔族";
                    break;
                case "51":
                    nationName = "独龙族";
                    break;
                case "52":
                    nationName = "鄂伦春族";
                    break;
                case "53":
                    nationName = "赫哲族";
                    break;
                case "54":
                    nationName = "门巴族";
                    break;
                case "55":
                    nationName = "珞巴族";
                    break;
                case "56":
                    nationName = "基诺族";
                    break;
                case "57":
                    nationName = "其他";
                    break;
                case "58":
                    nationName = "外国血统";
                    break;
                default:
                    nationName = "汉族";
                    break;


            }
            return nationName;
        }
        /// <summary>
        /// 根据web文化程度code 获取pad文化程度code
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private string GetCulturecodeByWebNum(string num)
        {
            string cultureName = "";
            switch (num)
            {
                case "10":
                    cultureName = "1";
                    break;
                case "20":
                    cultureName = "2";
                    break;
                case "30":
                    cultureName = "3";
                    break;
                case "40":
                    cultureName = "4";
                    break;
                case "50":
                    cultureName = "5";
                    break;
                case "60":
                    cultureName = "6";
                    break;
                case "70":
                    cultureName = "7";
                    break;
                case "80":
                    cultureName = "8";
                    break;
                case "90":
                    cultureName = "9";
                    break;
            }
            return cultureName;

        }
        /// <summary>
        /// 根据web职业code,获取pad 职业code
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private string GetJobCodeByWebNum(string num)
        {
            string job = "";
            if (baseUrl.Contains("sdcsm_new"))
            {
                switch (num)
                {
                    case "0":
                        job = "1";
                        break;
                    case "1":
                        job = "2";
                        break;
                    case "2":
                        job = "3";
                        break;
                    case "3":
                        job = "4";
                        break;
                    case "4":
                        job = "5";
                        break;
                    case "5":
                        job = "6";
                        break;
                    case "6":
                        job = "7";
                        break;
                    case "7":
                        job = "8";
                        break;
                    case "8":
                        job = "9";
                        break;
                }
            }
            else
            {
                switch (num)
                {
                    case "6":
                        job = "1";
                        break;
                    case "3":
                        job = "2";
                        break;
                    case "4":
                        job = "3";
                        break;
                    case "5":
                        job = "4";
                        break;
                    case "1":
                        job = "5";
                        break;
                    case "2":
                        job = "6";
                        break;
                    case "12":
                        job = "7";
                        break;
                    case "99":
                        job = "8";
                        break;
                    default:
                        job = "9";
                        break;
                }
            }
            return job;
        }
        /// <summary>
        /// 根据web婚姻状况，获取pad婚姻code
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private string GetMaritalCodeByWebNum(string num)
        {
            string marital = "5";
            switch (num)
            {
                case "10":
                    marital = "1";
                    break;
                case "20":
                case "21":
                case "22":
                case "23":
                    marital = "2";
                    break;
                case "30":
                    marital = "3";
                    break;
                case "40":
                    marital = "4";
                    break;
                case "90":
                    marital = "5";
                    break;
            }
            return marital;
        }
        /// <summary>
        /// 根据WebHtmlNodeCollection， 获取 pad支付方式字符串
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private string GetMedicalPayTypeCodesByWebNodes(HtmlNodeCollection nodes)
        {
            string medicalPay = "";
            if (nodes == null)
            {
                return "";
            }
            foreach (var node in nodes)
            {
                if (node.Attributes.Contains("value"))
                {
                    string temStr = node.Attributes["value"].Value;
                    if (!baseUrl.Contains("sdcsm_new"))
                    {
                        switch (temStr)
                        {
                            case "1":
                                temStr = "1";
                                break;
                            case "2":
                                temStr = "2";
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
                            case "99":
                                temStr = "8";
                                break;
                        }
                    }
                    medicalPay += "," + temStr;
                }
            }
            return medicalPay.TrimStart(',');
        }
        /// <summary>
        /// 根据WebHtmlNodeCollection ，获取pad 药物过敏史
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private string GetDrugAllergicByWebNodes(HtmlNodeCollection nodes)
        {
            if (nodes == null)
            {
                return "1";
            }
            string temStr = "";
            foreach (var node in nodes)
            {
                if (node.Attributes.Contains("value"))
                {
                    string str = node.Attributes["value"].Value;
                    switch (str)
                    {
                        case "1":
                            str = "2";
                            break;
                        case "2":
                            str = "3";
                            break;
                        case "4":
                            str = "4";
                            break;
                        case "99":
                            str = "5";
                            break;
                        default:
                            break;
                    }

                    temStr += "," + str;
                }
            }
            string ss = temStr.TrimStart(',');
            if (ss == "")
            {
                return "1";
            }
            else
            {
                return ss;
            }
        }
        /// <summary>
        /// 根据WebHtmlNodeCollection ，获取pad 残疾code字符串
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private string GetDiseasCodeByWebNodes(HtmlNodeCollection nodes)
        {
            if (nodes == null)
            {
                return "1";
            }
            string temStr = "";
            foreach (var node in nodes)
            {
                if (node.Attributes.Contains("value"))
                {
                    string str = node.Attributes["value"].Value;
                    if (!baseUrl.Contains("sdcsm_new"))
                    {
                        switch (str)
                        {
                            case "2":
                                str = "3";
                                break;
                            case "3":
                                str = "4";
                                break;
                            case "4":
                                str = "5";
                                break;
                            case "5":
                                str = "6";
                                break;
                            case "6":
                                str = "2";
                                break;
                            case "7":
                                str = "7";
                                break;
                            case "9":
                                str = "8";
                                break;
                        }
                    }
                    temStr += "," + str;
                }
            }
            string ss = temStr.TrimStart(',');
            if (ss == "")
            {
                return "1";
            }
            else
            {
                return ss;
            }
        }
        /// <summary>
        /// 根据Web血型code , 获取pad Boold code
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private string GetBooldTypeByWebNum(string num)
        {
            string tem = "5";
            if (baseUrl.Contains("sdcsm_new"))
            {
                return num;
            }
            else
            {
                switch (num)
                {
                    case "1":
                        tem = "1";
                        break;
                    case "2":
                        tem = "4";
                        break;
                    case "3":
                        tem = "2";
                        break;
                    case "4":
                        tem = "3";
                        break;
                    //case "5":
                    //    tem = "5";
                    //    break;
                }
            }
            return tem;
        }
        /// <summary>
        /// 获取详细住址
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private string GetAddressByDocument(HtmlDocument doc)
        {
            string address = "";
            /*var node = doc.DocumentNode.SelectSingleNode("//select[@name='dShi']/option[@selected]");
            if (node.Attributes.Contains("value") && !string.IsNullOrEmpty(node.Attributes["value"].Value))
            {
                address += node.NextSibling.InnerText;
            }
            node = doc.DocumentNode.SelectSingleNode("//select[@name='dQu']/option[@selected]");
            if (node.Attributes.Contains("value") && !string.IsNullOrEmpty(node.Attributes["value"].Value))
            {
                address += node.NextSibling.InnerText;
            }
            node = doc.DocumentNode.SelectSingleNode("//select[@id='street1']/option[@selected]");
            if (node.Attributes.Contains("value") && !string.IsNullOrEmpty(node.Attributes["value"].Value))
            {
                address += node.NextSibling.InnerText;
            }
            node = doc.DocumentNode.SelectSingleNode("//select[@id='village']/option[@selected]");
            if (node.Attributes.Contains("value") && !string.IsNullOrEmpty(node.Attributes["value"].Value))
            {
                address += node.NextSibling.InnerText;
            }*/
            var node = doc.DocumentNode.SelectSingleNode("//input[@id='dXxdz']");
            if (node != null && node.Attributes.Contains("value"))
            {
                address += node.Attributes["value"].Value;
            }
            return address;

        }
        /// <summary>
        /// 家庭成员类型（家族史）
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetFamilyTypeByWebCode(string code)
        {
            string tem = "1";
            switch (code)
            {
                case "1":
                case "2":
                case "3":
                    tem = "1";
                    break;
                case "4":
                    tem = "2";
                    break;
                case "5":
                    tem = "3";
                    break;
                case "6":
                    tem = "4";
                    break;

            }
            return tem;
        }

        /// <summary>
        /// （家族史）
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string GetFamily(string code)
        {
            code = code.Trim();

            if (code == "100")
            {
                return "12";
            }

            string returnVal = "";

            int intCode = 0;

            if (int.TryParse(code, out intCode))
            {
                returnVal = (intCode + 1).ToString();
            }

            return returnVal;
        }

        private string getQcl(string code)
        {
            string val = "";
            if (baseUrl.Contains("sdcsm_new"))
            {
                switch (code)
                {
                    case "1":
                        val = "0";
                        break;
                    case "2":
                        val = "1";
                        break;
                    case "3":
                        val = "2";
                        break;
                    case "4":
                        val = "3";
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (code)
                {
                    case "1":
                        val = "4";
                        break;
                    case "2":
                        val = "1";
                        break;
                    case "3":
                        val = "2";
                        break;
                    case "4":
                        val = "1";
                        break;
                    default:
                        break;
                }
            }
            return val;
        }

        private string getQclForPad(string code)
        {
            string val = "";
            if (baseUrl.Contains("sdcsm_new"))
            {
                switch (code)
                {
                    case "0":
                        val = "1";
                        break;
                    case "1":
                        val = "2";
                        break;
                    case "2":
                        val = "3";
                        break;
                    case "3":
                        val = "4";
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (code)
                {
                    case "1":
                        val = "2";
                        break;
                    case "2":
                        val = "3";
                        break;
                    case "3":
                        val = "4";
                        break;
                    case "4":
                        val = "1";
                        break;
                    default:
                        break;
                }
            }
            return val;
        }
        #endregion
    }
}
