using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using BaseBusiness;
using FluorineFx.Messaging.Messages;
using System;
using FluorineFx.IO;
using Utilities.Common;
using System.Linq;
using System.Net;
using Model.JsonModel;
using Newtonsoft.Json;
using HtmlAgilityPack;
using Model.InfoModel;
using DAL;

namespace CommonBusiness
{
    public class CommonBusiness : BaseBusinessRule
    {
        string baseUrl = Config.GetValue("baseUrl");

        /// <summary>
        /// 根据身份证获取信息
        /// </summary>
        /// <param name="strIDCardNo"></param>
        /// <returns></returns>
        public PersonModel GetGrdaByIDCardNo(string strIDCardNo, string loginKey, CookieContainer strSysCookieContainer)
        {
            PersonModel pm = GetGrdaByIDCardNoUpperOrLower(strIDCardNo.ToUpper(), loginKey, strSysCookieContainer);
            return pm == null ? GetGrdaByIDCardNoUpperOrLower(strIDCardNo.ToLower(), loginKey, strSysCookieContainer) : pm;
        }

        private PersonModel GetGrdaByIDCardNoUpperOrLower(string strIDCardNo, string loginKey, CookieContainer strSysCookieContainer)
        {
            WebHelper web = new WebHelper();

            StringBuilder postStr = new StringBuilder();
            #region
            postStr.Append("_cxgxb=").Append("on");
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
            postStr.Append("&dDazt=").Append("1");
            postStr.Append("&dGrdabh=").Append("");
            postStr.Append("&dHyzk=").Append("");
            postStr.Append("&dJd=").Append("");
            postStr.Append("&dJwh=").Append("");
            postStr.Append("&dJzzk=").Append("");
            postStr.Append("&dMz=").Append("");
            postStr.Append("&dSfrhyx=").Append("");
            postStr.Append("&dSfzh=").Append(strIDCardNo);
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
            postStr.Append("&enddate=").Append("");
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
            postStr.Append("&startdate=").Append("");
            postStr.Append("&startdc=").Append("");
            postStr.Append("&startgx=").Append("");
            postStr.Append("&startlr=").Append("");
            postStr.Append("&updateuser=").Append("");
            postStr.Append("&val888").Append("1");

            #endregion

            string returnString = "";

            returnString = web.PostHttp(baseUrl + "/grjbxxsearch.action", postStr.ToString(), "application/x-www-form-urlencoded", strSysCookieContainer);


            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            if (doc == null)
            {
                return null;
            }

            var nodes = doc.DocumentNode.SelectNodes("//tr");
            PersonModel person = null;

            if (nodes != null)
            {
                person = new PersonModel();

                for (var i = 1; i < nodes.Count; i++)
                {
                    var node = nodes[i].SelectNodes("td");
                    if (node.Count == 1)
                    {
                        person = null;
                        break;
                    }
                    if (string.IsNullOrEmpty(node[6].InnerText.Trim()))
                    {
                        continue;
                    }

                    var anode = node[1].SelectSingleNode("a[1]");
                    person.fid = anode == null || !anode.Attributes.Contains("onclick") ? "" : anode.Attributes["onclick"].Value.Replace("jtgx('", "").Replace("')", "").Trim();
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
                    person.memberArchiveCode = node[2].InnerText;
                    person.memberName = node[3].InnerText;
                    break;
                }
            }

            return person;
        }

        /// <summary>
        /// 获取当前的村委会清单
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="responseNo"></param>
        /// <returns></returns>
        public DataSet GetSysCommittee(string cookie, string responseNo)
        {
            DataSet dtUploadSetRow = new DataSet();

            ManagerBase mb = new ManagerBase(SendType.Query);

            byte[] byteSend = GetNode(responseNo);

            string sendHeader = CommonExtensions.GetSendHeader(cookie, byteSend.Length).ToString();

            mb.SendMessage(cookie, byteSend, sendHeader);

            AMFDeserializer ad = new AMFDeserializer(new MemoryStream(mb.socketReceiveData));
            AMFMessage timeMessage = ad.ReadAMFMessage();

            mb.socketReceiveData = null;

            if (timeMessage.BodyCount == 0 || timeMessage.Bodies.FirstOrDefault().Content is FluorineFx.Messaging.Messages.ErrorMessage)
            {
                CommonExtensions.WriteLog("不存在");

                return dtUploadSetRow;
            }

            var content = (AcknowledgeMessage)timeMessage.Bodies.FirstOrDefault().Content;

            // node 字符串
            /*
            <node id='3064' name='中亚南路街道办事处' level='4' levelCN='乡镇' code='650104014' isLoad='true' isBranch='true'>
                <node id='3718' name='团结新村社区居委会' level='5' levelCN='村' code='650104014007' isLoad='false' ></node>
                <node id='3721' name='铁路花园社区居委会' level='5' levelCN='村' code='650104014010' isLoad='false' ></node>
            </node>
             */
            var strNodes = content.body.ToString();


            using (StringReader xmlSR = new StringReader(strNodes))
            {
                dtUploadSetRow.ReadXml(xmlSR);
            }

            return dtUploadSetRow;
        }

        /// <summary>
        /// 获取上传人员信息
        /// </summary>
        /// <returns></returns>
        public IList<DataSet> GetUploadDataSet()
        {
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.Append(@"SELECT * FROM UPLOADED");

            base.Parameter.Clear();

            DataTable dtSearch = base.Search(sbQuery.ToString());

            List<DataSet> lstDataSet = new List<DataSet>();

            foreach (DataRow row in dtSearch.Rows)
            {
                DataSet dtUploadSetRow = new DataSet();

                using (StringReader xmlSR = new StringReader(row["datapacket"].ToString()))
                {
                    dtUploadSetRow.ReadXml(xmlSR);

                    if (dtUploadSetRow.Tables.Count > 0)
                    {
                        lstDataSet.Add(dtUploadSetRow);
                    }
                }
            }


            return lstDataSet;
        }

        /// <summary>
        /// 根据编号获取当前系统的村委会ID
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public Committee GetCommitteeKeyByCode(string code, DataSet dtUploadSetRow)
        {
            Committee returnCommittee = new Committee();

            returnCommittee.code = code;

            if (dtUploadSetRow.Tables.Count > 0 && dtUploadSetRow.Tables[0].Rows.Count > 0)
            {
                DataTable dt = dtUploadSetRow.Tables[0];

                DataRow[] drs = dt.Select("code =" + code);

                if (drs.Length > 0)
                {
                    returnCommittee.id = drs[0]["id"].ToString();
                    returnCommittee.name = drs[0]["name"].ToString();
                }
            }

            return returnCommittee;
        }

        /// <summary>
        /// 根据编号获取当前系统的村委会ID
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public Committee GetCommitteeCodeByKey(string id, DataSet dtUploadSetRow)
        {
            Committee returnCommittee = new Committee();

            returnCommittee.id = id;

            if (dtUploadSetRow.Tables.Count > 0 && dtUploadSetRow.Tables[0].Rows.Count > 0)
            {
                DataTable dt = dtUploadSetRow.Tables[0];

                DataRow[] drs = dt.Select("id =" + id);

                if (drs.Length > 0)
                {
                    returnCommittee.code = drs[0]["code"].ToString();
                    returnCommittee.name = drs[0]["name"].ToString();
                }
            }

            return returnCommittee;
        }

        /// <summary>
        /// 获取全部行政区域
        /// </summary>
        /// <param name="SysCookieContainer"></param>
        /// <returns></returns>
        public List<OrganizesData> GetOrganizes(CookieContainer SysCookieContainer)
        {
            WebHelper web = new WebHelper();

            string returnString = web.PostHttp(baseUrl + "/server/platform/org/orgunit/selectAdminOrganizes.do", "", "application/x-www-form-urlencoded", SysCookieContainer);

            List<OrganizesData> lstOrganizes = new List<OrganizesData>();

            OrganizesJson organizesAdmin = JsonConvert.DeserializeObject<OrganizesJson>(returnString);

            if (organizesAdmin.body != null && organizesAdmin.body.d1 != null && organizesAdmin.body.d1.data != null && organizesAdmin.body.d1.data.Count > 0)
            {
                OrganizesData root = organizesAdmin.body.d1.data[0];

                string postdata = "s.tid=" + root.t_id + "&sunit.state.u_status=" + root.u_status + "&s.treeid=" + root.u_id;
                returnString = web.PostHttp(baseUrl + "/server/platform/org/orgunit/selectOrgMTree.do", postdata, "application/x-www-form-urlencoded", SysCookieContainer);

                OrganizesJson organizesAll = JsonConvert.DeserializeObject<OrganizesJson>(returnString);

                if (organizesAll.body != null && organizesAll.body.d1 != null && organizesAll.body.d1.data != null && organizesAll.body.d1.data.Count > 0)
                { lstOrganizes.AddRange(organizesAll.body.d1.data); }
            }

            return lstOrganizes;
        }

        public List<Doctor> GetDoctors(CookieContainer SysCookieContainer)
        {
            WebHelper web = new WebHelper();

            string returnString = web.PostHttp(baseUrl + "/lookPerson.action", "", "application/x-www-form-urlencoded", SysCookieContainer);

            HtmlDocument doc = new HtmlDocument();

            returnString = returnString.Replace("\t", "").Replace("/>", " />");

            using (TextReader reader = new StringReader(returnString))
            {
                doc.Load(reader);
            }

            List<Doctor> lstDoctor = new List<Doctor>();
            var nodes = doc.DocumentNode.SelectNodes("//select[@id='createuser']/option");

            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    Doctor d = new Doctor();

                    d.id = n.Attributes["value"].Value;
                    d.name = n.NextSibling.InnerText.Trim();

                    lstDoctor.Add(d);
                }
            }

            return lstDoctor;
        }

        /// <summary>
        /// 登陆账户的标示
        /// </summary>
        /// <param name="SysCookieContainer"></param>
        /// <returns></returns>
        public string GetLoginUser(CookieContainer SysCookieContainer)
        {
            WebHelper web = new WebHelper();
            string key = null;
            string returnString = web.PostHttp(baseUrl + "/xitongshezhi/xtggzy/toupdateUserInfo.action", "", "application/x-www-form-urlencoded", SysCookieContainer);

            HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
            var node = doc.DocumentNode.SelectSingleNode("//input[@name='pUserid']");
            if (node != null)
            {
                key = node.Attributes.Contains("value") ? node.Attributes["value"].Value : null;
            }
            return key;
        }

        /// <summary>
        /// 获取乡镇-村委会
        /// </summary>
        /// <param name="logieKey"></param>
        /// <param name="SysCookieContainer"></param>
        /// <returns></returns>
        public List<Town> GetTownList(string loginKey, CookieContainer SysCookieContainer)
        {

            //http://20.1.1.38:9080/sdcsm/healthArchives/addArchivesHz.action?addSign=2&dJtdabh=&tz=2
            List<Town> list = new List<Town>();
            try
            {
                WebHelper web = new WebHelper();
                string returnString = web.PostHttp(baseUrl + "/healthArchives/addArchivesHz.action?addSign=2&dJtdabh=&tz=2", "", "application/x-www-form-urlencoded", SysCookieContainer);
                HtmlDocument doc = HtmlHelper.GetHtmlDocument(returnString);
                if (doc != null)
                {
                    var nodes = doc.DocumentNode.SelectNodes("//select[@id='street1']/option");
                    foreach (var node in nodes)
                    {
                        if (!node.Attributes.Contains("value") || string.IsNullOrEmpty(node.Attributes["value"].Value))
                        {
                            continue;
                        }
                        Town town = new Town();
                        town.code = node.Attributes["value"].Value;
                        town.text = node.NextSibling.InnerText.Trim();
                        //town.villageList = GetVillageList(town.code.ToString(), loginKey, SysCookieContainer);
                        list.Add(town);
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                //CommonExtensions.WriteLog("获取镇失败：" + ex);
            }
            return list;
        }

        /// <summary>
        /// 获取乡镇下的村委会
        /// </summary>
        /// <param name="SysCookieContainer"></param>
        /// <returns></returns>
        public List<Village> GetVillageList(string code, string loginKey, CookieContainer SysCookieContainer)
        {

            List<Village> villageList = new List<Village>();
            try
            {
                WebHelper web = new WebHelper();
                StringBuilder postData = new StringBuilder();
                postData.Append("callCount=").Append("1").Append("\r\n");
                postData.Append("nextReverseAjaxIndex=").Append("0").Append("\r\n");
                postData.Append("c0-scriptName=").Append("getCommon").Append("\r\n");
                postData.Append("c0-methodName=").Append("getJwhByJdRgid").Append("\r\n");
                postData.Append("c0-id=").Append("0").Append("\r\n");
                postData.Append("c0-param0=").Append("string:" + loginKey.Substring(0, 15)).Append("\r\n");
                postData.Append("c0-param1=").Append("string:" + code).Append("\r\n");
                postData.Append("batchId=").Append("1").Append("\r\n");
                postData.Append("instanceId=").Append("0").Append("\r\n");
                postData.Append("page=").Append("%2Fsdcsm%2FhealthArchives%2FaddArchivesHz.action%3FaddSign%3D2%26dJtdabh%3D%26tz%3D2").Append("\r\n");
                postData.Append("scriptSessionId=").Append("!9gaPMFURR3T497mlL6V5HFDJlJANwx6Hon/NEyxHon-i6aBqlI8p");
                //string data = HtmlHelper.GetUrlEncodeVal(postData.ToString(),"UTF-8");
                string data = postData.ToString();
                //http://20.1.1.38:9080/sdcsm/dwr/call/plaincall/getCommon.getJwhByJdRgid.dwr
                string returnString = web.PostHttpNoCookie(baseUrl + "dwr/call/plaincall/getCommon.getJwhByJdRgid.dwr", data, "text/plain");

                string functionStr = HtmlHelper.GetTagValue(returnString,"dwr.engine.remote.handleCallback(",");");
                functionStr = HtmlHelper.GetTagValue(functionStr, "[{", "}]");
                if (!string.IsNullOrEmpty(functionStr))
                {
                    functionStr = "[{" + functionStr + "}]";
                }
                if (functionStr.IndexOf('[') > -1)
                {
                    List<VillageJson> villages = JsonConvert.DeserializeObject<List<VillageJson>>(functionStr);
                    foreach (var d in villages)
                    {
                        Village vill = new Village();
                        vill.code = d.b_rgid;
                        vill.text = d.b_name;
                        villageList.Add(vill);
                    }
                }
            }
            catch (Exception ex)
            {
                //CommonExtensions.WriteLog("获取村委会失败：" + ex);
            }
            return villageList;
        }

        private void GetVillageListTest(string code, string loginKey, CookieContainer SysCookieContainer)
        {
            //string strTitle = TextBox1.Text;
            //string strDesc = TextBox2.Text;

            //Encoding encoding = Encoding.GetEncoding("GB2312");

            //string postData = "Title=" + strTitle;
            //string strUrl = "http://xxx/java.action";
            //postData += ("&Desc=" + strDesc);
            //byte[] data = encoding.GetBytes(postData);

            //// 准备请求...
            //HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(strUrl);
            //myRequest.Method = "POST";
            //myRequest.ContentType = "application/x-www-form-urlencoded";
            //myRequest.ContentLength = data.Length;
            //Stream newStream = myRequest.GetRequestStream();
            //// 发送数据
            //newStream.Write(data, 0, data.Length);
            //newStream.Close();
        }

        #region Private

        /// <summary>
        /// 获取全部村委会amf
        /// </summary>
        /// <returns></returns>
        private byte[] GetNode(string responseNo)
        {
            RemotingMessage rtMsg = new RemotingMessage();

            rtMsg.source = null;
            rtMsg.operation = "findLazyLoadDistrictInitializeForCombo";
            rtMsg.clientId = Guid.NewGuid().ToString().ToUpper();
            rtMsg.messageId = Guid.NewGuid().ToString().ToUpper();
            rtMsg.destination = "districtRO";
            rtMsg.timestamp = 0;
            rtMsg.timeToLive = 0;
            rtMsg.headers.Add(RemotingMessage.FlexClientIdHeader, Guid.NewGuid().ToString().ToUpper());
            rtMsg.headers.Add(RemotingMessage.EndpointHeader, "my-amf");

            List<object> bodys = new List<object>();
            bodys.Add("0");

            rtMsg.body = bodys.ToArray();

            AMFMessage _amf3 = new AMFMessage(3);// 创建 AMF

            List<RemotingMessage> lstR = new List<RemotingMessage>();
            lstR.Add(rtMsg);

            AMFBody _amfbody = new AMFBody("null", "/" + responseNo, lstR.ToArray());
            _amf3.AddBody(_amfbody);

            MemoryStream _Memory = new MemoryStream();//内存流
            AMFSerializer _Serializer = new AMFSerializer(_Memory);//序列化

            _Serializer.WriteMessage(_amf3);//将消息写入

            return _Memory.ToArray();
        }

        /// <summary>
        /// 用户认证
        /// </summary>
        /// <returns></returns>
        private byte[] AuthenticateUser(string userID, string psd, string responseNo)
        {
            RemotingMessage rtMsg = new RemotingMessage();

            rtMsg.source = null;
            rtMsg.operation = "authenticateUser";
            rtMsg.clientId = Guid.NewGuid().ToString().ToUpper();
            rtMsg.messageId = Guid.NewGuid().ToString().ToUpper();
            rtMsg.destination = "userRO";
            rtMsg.timestamp = 0;
            rtMsg.timeToLive = 0;
            rtMsg.headers.Add(RemotingMessage.FlexClientIdHeader, Guid.NewGuid().ToString().ToUpper());
            rtMsg.headers.Add(RemotingMessage.EndpointHeader, "my-amf");

            List<object> bodys = new List<object>();

            bodys.Add(userID);
            bodys.Add(psd);

            rtMsg.body = bodys.ToArray();

            AMFMessage _amf3 = new AMFMessage(3);// 创建 AMF

            List<RemotingMessage> lstR = new List<RemotingMessage>();
            lstR.Add(rtMsg);

            AMFBody _amfbody = new AMFBody("null", "/" + responseNo, lstR.ToArray());
            _amf3.AddBody(_amfbody);

            MemoryStream _Memory = new MemoryStream();//内存流
            AMFSerializer _Serializer = new AMFSerializer(_Memory);//序列化

            _Serializer.WriteMessage(_amf3);//将消息写入

            return _Memory.ToArray();
        }

        #endregion
    }

    /// <summary>
    /// 村委会实体
    /// </summary>
    public class Committee
    {
        public string id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
    }

    public class Doctor
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}

