using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Newtonsoft.Json;
using Model.JsonModel;
using Utilities.Common;
using System.ComponentModel;
using System.Threading.Tasks;
using DAL;

namespace InterfaceForm
{
    public partial class FrmLogin : Form
    {
        public FrmLogin()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        int requestNo = 1;

        DataSet dsCommittee;

        /// <summary>
        /// 登陆者的用户名
        /// </summary>
        public string LoginDoctorName;

        CookieContainer cookies;

        string cookiesStr;

        string baseUrl = Config.GetValue("baseUrl");

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtUser.Text))
                {
                    MessageBox.Show("请输入用户名！");
                    return;
                }
                if (string.IsNullOrEmpty(txtPsd.Text))
                {
                    MessageBox.Show("请输入密码！");
                    return;
                }
                lblmsg.Visible = true;
                BackgroundWorker bgwDownType1 = new BackgroundWorker();
                bgwDownType1.WorkerReportsProgress = true;

                WebHelper webhelper = new WebHelper();
                CommonExtensions.Cookies = new CookieContainer();
                CommonExtensions.Userid = txtUser.Text.Trim();
                CommonExtensions.Password = txtPsd.Text.Trim();
                string postData = "macAddress=&loginname=" + CommonExtensions.GetUrlEncodeVal(txtUser.Text.Trim()) + "&password=" + CommonExtensions.GetUrlEncodeVal(txtPsd.Text.Trim()) + "&loginType=1";

                string webCookie = "";
                HtmlAgilityPack.HtmlDocument doc = null;
                cookies = new CookieContainer();

                bgwDownType1.DoWork += (bsend, be) =>
                {
                    string retString = webhelper.PostHttp(baseUrl + "login.action", postData, "application/x-www-form-urlencoded", cookies);

                    doc = HtmlHelper.GetHtmlDocument(retString);
                    webCookie = webhelper.CookieInfo;
                };

                bgwDownType1.RunWorkerCompleted += (bsend, be) =>
                {
                    string title = "";

                    var node = doc != null ? doc.DocumentNode.SelectSingleNode("//title") : null;
                    if (node != null)
                    {
                        title = node.InnerText;
                    }

                    if (title.Contains("首页"))
                    {
                        FrmMain ft = new FrmMain();
                        ft.SysCookie = cookiesStr;

                        ft.SysCookieContainer = cookies;

                        ft.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("登陆失败！");
                        return;
                    }

                };

                bgwDownType1.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void FrmLogin_Load(object sender, EventArgs e)
        {
            //WebHelper webhelper = new WebHelper();
            //CommonExtensions.Cookies = new CookieContainer();

            //string retString = webhelper.GetCookie(baseUrl + "login.action", "", cookies);

            //string a = "血脂异常；血清低密度脂蛋白胆固醇:3.71mmol/L ↑;血清高密度脂蛋白胆固醇:1.94mmol/L ↑;";
            //string b = CommonExtensions.cutSubstring(a,50);
            //txtUser.Text = "szy";
            //  txtUser.Text = "tht";
            //  txtPsd.Text = "123456";

            //txtUser.Text = "zhangshu";
            //txtPsd.Text = "654321";

            //txtUser.Text = "371427B100100026";
            //txtPsd.Text = "123123";
            //txtUser.Text = "fdt";
            //txtPsd.Text = "123456";
            //txtUser.Text = "sqwsfwzx4";
            //txtPsd.Text = "sqwsfwzx123456";

            //txtUser.Text = "szbwsyyq";
            //txtPsd.Text = "yq1997";
        }
    }
}
