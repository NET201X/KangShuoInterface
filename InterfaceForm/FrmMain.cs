using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CommonBusiness;
using System.Net;
using Model.JsonModel;
using Model.InfoModel;
using System.IO;

//using net.greatsoft.chss.dto.privilege;
//using net.greatsoft.chss.domain.organization.model;

namespace InterfaceForm
{
    public partial class FrmMain : Form
    {
        int pageSize = 10;     //每页显示行数
        int nMax = 0;         //总记录数
        int pageCount = 0;    //页数＝总记录数/每页显示行数
        int pageCurrent = 0;   //当前页号
        int nCurrent = 0;      //当前记录行

        DataTable dtInfo = new DataTable();

        /// <summary>
        /// 登录用户的信息
        /// </summary>
        //public UserDTO SysUser { get; set; }

        /// <summary>
        /// 村委会信息
        /// </summary>
        public DataSet DSCommittee { get; set; }

        /// <summary>
        /// 系统cookie
        /// </summary>
        public string SysCookie { get; set; }

        /// <summary>
        /// 系统cookie
        /// </summary>
        public CookieContainer SysCookieContainer { get; set; }

        /// <summary>
        /// 请求编号
        /// </summary>
        public int requestNo { get; set; }

        /// <summary>
        /// 登录医生
        /// </summary>
        public string LoginDoctorName = "";

        /// <summary>
        /// 对应的其他系统中的医生id
        /// </summary>
        string LoginDoctorIDForSys = "";

        List<OrganizesData> lstOrganizesData;
        /// <summary>
        /// 乡镇街道-村委会
        /// </summary>
        List<Town> townListData;

        List<Town> QtownListData;

        string serverPath = "";

        public FrmMain()
        {
            InitializeComponent();
        }

        public JsonData LoginData { get; set; }

        public string loginKey { set; get; }

        CommonBusiness.CommonBusiness cb = new CommonBusiness.CommonBusiness();

        private void FrmUADS_Load(object sender, EventArgs e)
        {
            //获取运行路径
            serverPath = Application.StartupPath;

            chkDate.Checked = true;

            dtPickerS.Enabled = chkDate.Checked;
            dtPickerE.Enabled = chkDate.Checked;

            dtSbrith.Enabled = chkBrith.Checked;
            dtEBrith.Enabled = chkBrith.Checked;

            dtPickerS.Text = DateTime.Now.ToString("yyyy/MM/dd");
            dtPickerE.Text = DateTime.Now.ToString("yyyy/MM/dd");

            chkVist.Checked = false;
            dtVistS.Enabled = chkVist.Checked;
            dtVistE.Enabled = chkVist.Checked;
            dtVistS.Text = DateTime.Now.ToString("yyyy/MM/dd");
            dtVistE.Text = DateTime.Now.ToString("yyyy/MM/dd");

            chkJd.Checked = false;
            dtJdSdate.Enabled = chkJd.Checked;
            dtJdEdate.Enabled = chkJd.Checked;
            dtJdSdate.Text = DateTime.Now.ToString("yyyy/MM/dd");
            dtJdEdate.Text = DateTime.Now.ToString("yyyy/MM/dd");

            //绑定下拉框
            CommonDAOBusiness bs = new CommonDAOBusiness();

            DataTable doctor = bs.GetDoctorName();
            //int doctornum = doctor.Rows.Count;

            //cbdoctor.Items.Add("");

            foreach (DataRow dr in doctor.Rows)
            {
                if (dr["CreateMenName"] != null && dr["CreateMenName"].ToString() != "")
                {
                    cmbBoxAllDoctor.Items.Add(dr["CreateMenName"]);
                }
            }

            if (doctor.Rows.Count == 0)
            {
                cmbBoxAllDoctor.Items.Add("");
            }

            //绑定村委
            DataTable dtV = bs.GetVillageName();

            cmbQVillageID.Items.Add("请选择");
            cmbQVillageID.DataSource = dtV;
            cmbQVillageID.ValueMember = "VillageName";
            cmbQVillageID.DisplayMember = "VillageName";

            lblDoctorName.Text = LoginDoctorName;

            loginKey = cb.GetLoginUser(SysCookieContainer);

            townListData = cb.GetTownList(loginKey, SysCookieContainer);

            QtownListData = townListData;

            List<Village> townList = new List<Village>();
            List<Village> townList1 = new List<Village>();
            var i = 0;
            foreach (var item in townListData)
            {
                if (i == 0)
                {
                    Village town1 = new Village();
                    town1.code = "";
                    town1.text = "请选择";

                    townList.Add(town1);
                    i++;
                }
                Village town = new Village();
                town.code = item.code;
                town.text = item.text;

                townList.Add(town);
            }


            street1.DataSource = townList;
            street1.ValueMember = "code";
            street1.DisplayMember = "text";
            i = 0;
            foreach (var item in QtownListData)
            {
                if (i == 0)
                {
                    Village town1 = new Village();
                    town1.code = "";
                    town1.text = "请选择";

                    townList1.Add(town1);
                    i++;
                }
                Village town = new Village();
                town.code = item.code;
                town.text = item.text;

                townList1.Add(town);
            }

            cmbTown.DataSource = townList1;
            cmbTown.ValueMember = "code";
            cmbTown.DisplayMember = "text";

            cbdoctor.DataSource = cb.GetDoctors(SysCookieContainer);
            cbdoctor.ValueMember = "id";
            cbdoctor.DisplayMember = "name";

            cmbBoxAllDoctor.SelectedIndex = 0;
            cbdoctor.SelectedIndex = 0;

            // 查询
            nameselect_Click(sender, e);

            // 显示数据
            // LoadData();

            // 增加勾选框
            AddOutOfOfficeColumn();
        }

        /// <summary>
        /// 添加多选框
        /// </summary>
        private void AddOutOfOfficeColumn()
        {
            DataGridViewCheckBoxColumn column = new DataGridViewCheckBoxColumn();
            {
                column.HeaderText = "选择";
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                column.FlatStyle = FlatStyle.Standard;

                column.CellTemplate = new DataGridViewCheckBoxCell();
                column.CellTemplate.Style.BackColor = Color.Beige;
            }

            dgvBaseInfo.Columns.Insert(0, column);
        }

        /// <summary>
        /// 查询人员
        /// </summary>
        private void LoadData()
        {
            int nStartPos = 0;   //当前页面开始记录行
            int nEndPos = 0;     //当前页面结束记录行
            DataTable dtTemp = dtInfo.Clone();   //克隆DataTable结构框架
            if (pageCurrent == pageCount)
                nEndPos = nMax;
            else
                nEndPos = pageSize * pageCurrent;

            nStartPos = nCurrent;
            labNpage.Text = Convert.ToString(pageCurrent);

            if (nEndPos > dtInfo.Rows.Count)
            {
                nEndPos = dtInfo.Rows.Count;
            }

            // 从元数据源复制记录行
            for (int i = nStartPos; i < nEndPos; i++)
            {
                dtTemp.ImportRow(dtInfo.Rows[i]);
                nCurrent++;
            }

            dgvBaseInfo.DataSource = dtTemp;
        }

        #region 分页

        /// <summary>
        ///  上一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpage_Click(object sender, EventArgs e)
        {
            int count = Convert.ToInt32(labNpage.Text.ToString());
            if (Convert.ToInt32(labNpage.Text) <= 1)
            {
                MessageBox.Show("当前是首页。");
            }
            else
            {
                count--;
                labNpage.Text = Convert.ToString(count);
                pageCurrent = count;
                nCurrent = pageSize * (pageCurrent - 1);
                LoadData();
            }
            ckbAllSelect.CheckState = CheckState.Unchecked;
        }

        /// <summary>
        /// 下一页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDpage_Click(object sender, EventArgs e) //下一页
        {
            int count = Convert.ToInt32(labNpage.Text.ToString());
            if (Convert.ToInt32(labNpage.Text) >= Convert.ToInt32(labSumpage.Text))
            {
                MessageBox.Show("当前是尾页。");
            }
            else
            {
                count++;
                labNpage.Text = Convert.ToString(count);
                pageCurrent = count;
                LoadData();
            }
            ckbAllSelect.CheckState = CheckState.Unchecked;
        }

        /// <summary>
        /// GO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnsure_Click(object sender, EventArgs e)
        {
            System.Text.RegularExpressions.Regex reg1 = new System.Text.RegularExpressions.Regex(@"^[0-9]*$");//只能输入数字          
            bool ismatch1 = reg1.IsMatch(txtTpage.Text.ToString());
            if (ismatch1)
            {
                int sum = Convert.ToInt32(labSumpage.Text.ToString());
                pageCurrent = Convert.ToInt32(txtTpage.Text.ToString());
                if (pageCurrent > sum || pageCurrent < 1)
                {
                    MessageBox.Show("超出查询页码的范围。");
                }
                else
                {
                    nCurrent = pageSize * (pageCurrent - 1);
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("请输入有效的整数！");
                txtTpage.Text = "";
            }

            ckbAllSelect.CheckState = CheckState.Unchecked;
        }

        #endregion

        /// <summary>
        /// 全选
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ckbAllSelect_CheckedChanged(object sender, EventArgs e)
        {
            int count = dgvBaseInfo.RowCount;//数值为5；RowCount为显示的行；
            if (ckbAllSelect.CheckState == CheckState.Checked)
            {
                for (int i = 0; i < dgvBaseInfo.RowCount; i++)
                {
                    DataGridViewCheckBoxCell checkBox = (DataGridViewCheckBoxCell)this.dgvBaseInfo.Rows[i].Cells[0];
                    checkBox.Value = 1;
                }
            }
            if (ckbAllSelect.CheckState == CheckState.Unchecked)
            {
                for (int i = 0; i < dgvBaseInfo.RowCount; i++)
                {
                    DataGridViewCheckBoxCell checkBox = (DataGridViewCheckBoxCell)this.dgvBaseInfo.Rows[i].Cells[0];
                    checkBox.Value = 0;
                }
            }
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nameselect_Click(object sender, EventArgs e)
        {
            if (chkDate.Checked && (dtPickerS.Text != "" && dtPickerE.Text != "" && Convert.ToDateTime(dtPickerS.Text) > Convert.ToDateTime(dtPickerE.Text)))
            {
                MessageBox.Show("体检日期起不能大于体检日期止");

                return;
            }

            if (chkVist.Checked && (dtVistS.Text != "" && dtVistE.Text != "" && Convert.ToDateTime(dtVistS.Text) > Convert.ToDateTime(dtVistE.Text)))
            {
                MessageBox.Show("随访日期起不能大于随访日期止");

                return;
            }

            if (chkJd.Checked && (dtJdSdate.Text != "" && dtJdEdate.Text != "" && Convert.ToDateTime(dtJdSdate.Text) > Convert.ToDateTime(dtJdEdate.Text)))
            {
                MessageBox.Show("更新日期起不能大于更新日期止");

                return;
            }

            string strdateS = chkDate.Checked ? dtPickerS.Text : "";
            string strdateE = chkDate.Checked ? dtPickerE.Text : "";

            string strvistS = chkVist.Checked ? dtVistS.Text : "";
            string strvistE = chkVist.Checked ? dtVistE.Text : "";

            string strjdS = chkJd.Checked ? dtJdSdate.Text : "";
            string strjdE = chkJd.Checked ? dtJdEdate.Text : "";

            string VillageName = cmbQVillageID.Text.ToString();

            bool clqb = chkclall.Checked;

            CommonDAOBusiness bs = new CommonDAOBusiness();
            DataTable dt = new DataTable();

            if (chkVist.Checked)
            {
                dt = bs.GetPersonNameIDByVistDate(tbnameSelect.Text.Trim(), tbIDCard.Text.Trim(), cmbBoxAllDoctor.Text, VillageName, strvistS, strvistE, strjdS, strjdE);
            }
            else
            {
                dt = bs.GetPersonNameID(tbnameSelect.Text.Trim(), tbIDCard.Text.Trim(), cmbBoxAllDoctor.Text, VillageName, strdateS, strdateE, strjdS, strjdE,clqb);
            }

            dgvBaseInfo.DataSource = dt;
            dgvBaseInfo.AllowUserToAddRows = false;
            dtInfo = dt;
            dgvBaseInfo.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            nMax = dtInfo.Rows.Count;
            pageCount = nMax / pageSize;

            if ((nMax % pageSize) > 0)
            {
                pageCount += 1;
            }

            labCount.Text = nMax.ToString();

            labSumpage.Text = pageCount.ToString();
            labNpage.Text = "1";
            pageCurrent = Convert.ToInt32(labNpage.Text);
            nCurrent = 0;
            LoadData();
        }

        /// <summary>
        /// 清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, EventArgs e)
        {
            cmbBoxAllDoctor.SelectedIndex = 0;
            tbnameSelect.Text = "";
            tbIDCard.Text = "";

            chkDate.Checked = true;
            dtPickerS.Enabled = chkDate.Checked;
            dtPickerE.Enabled = chkDate.Checked;
        }

        /// <summary>
        /// //将在数据中选中的行复制给一个新建的datatable中；
        /// </summary>
        /// <returns></returns>
        private DataTable GetSelectedInfo()
        {
            DataTable dt = new DataTable();
            DataColumn col0 = new DataColumn("身份证号码", typeof(string));
            dt.Columns.Add(col0);
            DataColumn col1 = new DataColumn("姓名", typeof(string));
            dt.Columns.Add(col1);
            DataColumn col3 = new DataColumn("医生", typeof(string));
            dt.Columns.Add(col3);
            DataColumn col4 = new DataColumn("更新日期");
            dt.Columns.Add(col4);

            for (int i = 0; i < dgvBaseInfo.RowCount; i++) //显示的行
            {
                string s = dgvBaseInfo.Rows[i].Cells[1].ToString();
                if (Convert.ToInt32(dgvBaseInfo.Rows[i].Cells[0].Value) == 1)
                {
                    DataRow drq = dt.NewRow();
                    drq[0] = dgvBaseInfo.Rows[i].Cells[1].Value;
                    drq[1] = dgvBaseInfo.Rows[i].Cells[2].Value;
                    drq[2] = dgvBaseInfo.Rows[i].Cells[3].Value;
                    drq[3] = dgvBaseInfo.Rows[i].Cells[3].Value;
                    dt.Rows.Add(drq);
                }
            }

            return dt;
        }

        /// <summary>
        /// 上传单笔
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectedUpload_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            dt = GetSelectedInfo();

            if (dt.Rows.Count > 0)
            {
                string ids = "";

                foreach (DataRow row in dt.Rows)
                {
                    ids += "," + row["身份证号码"].ToString();
                }

                ids = ids.TrimStart(',');

                UploadMain(ids);
            }
            else
            {
                MessageBox.Show("请选择需要上传的人员！");
            }
        }

        /// <summary>
        /// 上传全部
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAllUpload_Click(object sender, EventArgs e)
        {
            string ids = "";

            for (int i = 0; i < dtInfo.Rows.Count; i++) //显示的行
            {
                ids += "," + dtInfo.Rows[i]["身份证号码"];
            }

            if (string.IsNullOrEmpty(ids))
            {
                MessageBox.Show("请先查询");

                return;
            }

            UploadMain(ids.TrimStart(','));
        }

        /// <summary>
        /// 上传
        /// </summary>
        /// <param name="ids"></param>
        private void UploadMain(string ids)
        {
            if (MessageBox.Show("确定要上传吗?", "提示信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                //LoginDoctorIDForSys = cbdoctor.SelectedValue == null ? "" : cbdoctor.SelectedValue.ToString();

                //if (string.IsNullOrEmpty(LoginDoctorIDForSys))
                //{
                //    MessageBox.Show("请选择医生");
                //    return;
                //}

                ClearText();

                string strLoginDoctorName = cmbBoxAllDoctor.Text;

                string strDoctorName = cbdoctor.Text;

                string villageVal = village1.SelectedValue.ToString();
                string streetVal = street1.SelectedValue.ToString();

                bool bgrd = chkGrd.Checked;
                bool btj = chkTJ.Checked;
                bool bjt = chkJT.Checked;
                bool blnr = chkLNR.Checked;
                bool bgxy = chkGxy.Checked;
                bool btnb = chkTnb.Checked;
                bool bgxb = chkGxb.Checked;
                bool bncz = chkNcz.Checked;
                bool onyTel = chkTel.Checked;
                bool delSameTj = chkDelSameTj.Checked;
                bool upsign = chkUpsign.Checked;
                bool uppic = chkUploadPic.Checked;

                CommonDAOBusiness cb = new CommonDAOBusiness();

                BackgroundWorker bgwDownType1 = new BackgroundWorker();
                bgwDownType1.WorkerReportsProgress = true;

                bgwDownType1.DoWork += (send, e) =>
                {
                    bgwDownType1.ReportProgress(1, "个人档案上传开始");
                    GrdaBusiness.GrdaBusiness gb = new GrdaBusiness.GrdaBusiness();

                    gb.DefultDoctor = LoginDoctorIDForSys;
                    gb.DefultDoctorName = strDoctorName;

                    gb.loginKey = loginKey;
                    gb.townList = townListData;
                    gb.Village1 = villageVal;
                    gb.Street1 = streetVal;
                    gb.SysCookieContainer = SysCookieContainer;
                    gb.onyEditTel = onyTel;

                    if (bgrd)
                    {
                        gb.lstUploadData = cb.GetGrdaDataSet(strLoginDoctorName, ids);
                        gb.SaveGrda((s) =>
                        {
                            bgwDownType1.ReportProgress(1, s);
                        });
                    }

                    bgwDownType1.ReportProgress(1, "个人档案上传完成");

                    bgwDownType1.ReportProgress(2, "健康体检上传开始");

                    TjBusiness.TjBusiness tj = new TjBusiness.TjBusiness();
                    tj.SysCookieContainer = SysCookieContainer;
                    tj.loginKey = loginKey;
                    tj.serverPath = serverPath;
                    tj.delSameTj = delSameTj;
                    tj.uploadSign = upsign;
                    tj.uploadTj = btj;
                    tj.uploadPic = uppic;

                    if (btj || upsign || blnr || uppic)
                    {
                        tj.lstUploadData = cb.GetTjMainDataSet(strLoginDoctorName, ids);

                        tj.SaveTJ((s) =>
                        {
                            bgwDownType1.ReportProgress(2, s);
                        });
                    }

                    bgwDownType1.ReportProgress(2, "健康体检上传完成");




                    //bgwDownType1.ReportProgress(11, "家庭信息上传开始");

                    //JtBusiness.JtBusiness jt = new JtBusiness.JtBusiness();
                    //jt.SysCookieContainer = SysCookieContainer;
                    //jt.loginKey = loginKey;
                    //jt.lstUploadData = cb.GetFamilyDataSet(strLoginDoctorName, ids);

                    //jt.SaveFamily((s) =>
                    //{
                    //    bgwDownType1.ReportProgress(11, s);
                    //});

                    //bgwDownType1.ReportProgress(11, "家庭信息上传完成");

                    bgwDownType1.ReportProgress(3, "老年人信息上传开始");
                    LnrBusiness.LnrBusiness lnr = new LnrBusiness.LnrBusiness();

                    lnr.DefultDoctor = LoginDoctorIDForSys;
                    lnr.DefultDoctorName = strDoctorName;

                    lnr.loginkey = loginKey;
                    lnr.SysCookieContainer = SysCookieContainer;

                    if (blnr)
                    {
                        lnr.lstUploadData = cb.GetLnrDataSet(strLoginDoctorName, ids);

                        lnr.SaveLnr((s) =>
                        {
                            bgwDownType1.ReportProgress(3, s);
                        });
                    }

                    bgwDownType1.ReportProgress(3, "老年人信息上传完成");

                    bgwDownType1.ReportProgress(4, "高血压信息上传开始");
                    GxyBusiness.GxyBusiness gxy = new GxyBusiness.GxyBusiness();

                    gxy.DefultDoctor = LoginDoctorIDForSys;
                    gxy.DefultDoctorName = strDoctorName;
                    gxy.loginkey = loginKey;
                    gxy.SysCookieContainer = SysCookieContainer;

                    if (bgxy)
                    {
                        gxy.lstUploadData = cb.GetGxyDataSet(strLoginDoctorName, ids);

                        gxy.SaveGxy((s) =>
                        {
                            bgwDownType1.ReportProgress(4, s);
                        });
                    }

                    bgwDownType1.ReportProgress(4, "高血压信息上传完成");
                    bgwDownType1.ReportProgress(5, "糖尿病信息上传开始");
                    TnbBusiness.TnbBusiness tnb = new TnbBusiness.TnbBusiness();

                    tnb.DefultDoctor = LoginDoctorIDForSys;
                    tnb.DefultDoctorName = strDoctorName;
                    tnb.loginkey = loginKey;
                    tnb.SysCookieContainer = SysCookieContainer;

                    if (btnb)
                    {
                        tnb.lstUploadData = cb.GetTnbDataSet(strLoginDoctorName, ids);

                        tnb.SaveTnb((s) =>
                        {
                            bgwDownType1.ReportProgress(5, s);
                        });
                    }

                    bgwDownType1.ReportProgress(5, "糖尿病信息上传完成");

                    bgwDownType1.ReportProgress(6, "脑卒中信息上传开始");
                    NczBusiness.NczBusiness ncz = new NczBusiness.NczBusiness();

                    ncz.DefultDoctor = LoginDoctorIDForSys;
                    ncz.DefultDoctorName = strDoctorName;
                    ncz.loginkey = loginKey;
                    ncz.SysCookieContainer = SysCookieContainer;

                    if (bncz)
                    {
                        ncz.lstUploadData = cb.GetNczDataSet(strLoginDoctorName, ids);

                        ncz.SaveInfo((s) =>
                        {
                            bgwDownType1.ReportProgress(6, s);
                        });
                    }

                    bgwDownType1.ReportProgress(6, "脑卒中信息上传完成");

                    bgwDownType1.ReportProgress(8, "冠心病信息上传开始");
                    GxbBusiness.GxbBusiness gxb = new GxbBusiness.GxbBusiness();

                    gxb.DefultDoctor = LoginDoctorIDForSys;
                    gxb.DefultDoctorName = strDoctorName;
                    gxb.loginkey = loginKey;
                    gxb.SysCookieContainer = SysCookieContainer;

                    if (bgxb)
                    {
                        gxb.lstUploadData = cb.GetGxbDataSet(strLoginDoctorName, ids);

                        gxb.SaveInfo((s) =>
                        {
                            bgwDownType1.ReportProgress(8, s);
                        });
                    }

                    bgwDownType1.ReportProgress(8, "冠心病信息上传完成");
                };

                bgwDownType1.ProgressChanged += (send, e) =>
                {
                    string showVal = e.UserState.ToString();

                    if (showVal.IndexOf("EX-") != -1)
                    {
                        // 异常信息
                        lstBoxEx.Items.Add(showVal);
                        return;
                    }

                    int step = e.ProgressPercentage;
                    switch (step)
                    {
                        case 1:
                            lblShowType1.Text = showVal;
                            break;
                        case 2:
                            lblShowType2.Text = showVal;
                            break;
                        case 3:
                            lblShowType3.Text = showVal;
                            break;
                        case 4:
                            lblShowType4.Text = showVal;
                            break;
                        case 5:
                            lblShowType5.Text = showVal;
                            break;
                        case 11:
                            lblShowType11.Text = showVal;
                            break;
                        case 6:
                            lblShowType6.Text = showVal;
                            break;
                        case 8:
                            lblShowType8.Text = showVal;
                            break;
                    }
                };

                bgwDownType1.RunWorkerAsync();
            }
        }

        /// <summary>
        /// 关闭程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmUADS_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// 下载单个
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDownSelected_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            dt = GetSelectedInfo();

            if (dt.Rows.Count > 0)
            {
                string ids = "";

                foreach (DataRow row in dt.Rows)
                {
                    ids += "," + row["身份证号码"].ToString();
                }

                ids = ids.TrimStart(',');

                DownloadMain(ids);
            }
            else if (down1.Text.Trim() != "")
            {
                DownloadMain(down1.Text.Trim());
            }
            else
            {
                MessageBox.Show("请选择需要下载的人员！");
            }
        }

        /// <summary>
        /// 下载全部
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDownAll_Click(object sender, EventArgs e)
        {
            DownloadMain("");
        }

        /// <summary>
        /// 下载
        /// </summary>
        private void DownloadMain(string ids)
        {
            if (MessageBox.Show("确定要下载吗?", "提示信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                if (txtStart.Text.Trim() != "" && txtEnd.Text.Trim() != "")
                {
                    if (Convert.ToInt32(txtStart.Text.Trim()) > Convert.ToInt32(txtEnd.Text.Trim()))
                    {
                        MessageBox.Show("请输入正确的下载起止笔数！");

                        return;
                    }
                }
                ClearText();
                BackgroundWorker bgwDownType1 = new BackgroundWorker();
                bgwDownType1.WorkerReportsProgress = true;
                QueryList querylist = new QueryList();

                querylist.qTown = cmbTown.SelectedValue != null ? cmbTown.SelectedValue.ToString() : "";
                querylist.qVill = cmbVill.SelectedValue != null ? cmbVill.SelectedValue.ToString() : "";
                querylist.qGxy = chkQgxy.Checked;
                querylist.qLnr = chkQlnr.Checked;
                querylist.qTnb = chkQtnb.Checked;
                querylist.qGxb = chkQgxb.Checked;
                querylist.qNcz = chkQncz.Checked;
                querylist.qZl = chkQzl.Checked;
                querylist.qMzf = chkQmzf.Checked;
                querylist.qJsb = chkQjsb.Checked;

                bool onlyGr = chkOnlyGr.Checked;
                bool onlydah = chkDownID.Checked;
                string sdate = "";
                string edate = "";
                if (chkBrith.Checked)
                {
                    sdate = DateTime.Parse(dtSbrith.Text).ToString("yyyy-MM-dd");
                    edate = DateTime.Parse(dtEBrith.Text).ToString("yyyy-MM-dd"); 
                }

                bgwDownType1.DoWork += (send, e) =>
                {
                    bgwDownType1.ReportProgress(1, "个人信息下载开始...");
                    bgwDownType1.ReportProgress(2, "健康体检下载开始...");
                    GrdaBusiness.GrdaBusiness gb = new GrdaBusiness.GrdaBusiness();
                    gb.SysCookieContainer = SysCookieContainer;
                    gb.loginKey = loginKey;
                    gb.querylist = querylist;
                    gb.onlyGr = onlyGr;
                    gb.onlydah = onlydah;
                    gb.sdate = sdate;
                    gb.edate = edate;

                    Action<string>[] callbackAll = new Action<string>[]
                    {
                        (s) =>
                        {
                            bgwDownType1.ReportProgress(1, s);
                        }
                        ,(s) =>
                        {
                            bgwDownType1.ReportProgress(2, s);
                        }
                        ,(s) =>
                        {
                            bgwDownType1.ReportProgress(3, s);
                        }
                         ,(s) =>
                        {
                            bgwDownType1.ReportProgress(4, s);
                        }
                        ,(s) =>
                        {
                            bgwDownType1.ReportProgress(5, s);
                        }
                        ,(s) =>
                        {
                            bgwDownType1.ReportProgress(6, s);
                        }
                        ,(s) =>
                        {
                            bgwDownType1.ReportProgress(7, s);
                        }
                        //,(s) =>
                        //{
                        //    bgwDownType1.ReportProgress(11, s);
                        //}
                     };

                    //个人档案，下载
                    if (string.IsNullOrEmpty(ids))
                    {
                        gb.StartIndex = txtStart.Text.Trim();
                        gb.EndIndex = txtEnd.Text.Trim();
                        gb.DownGrda(callbackAll);
                    }
                    else
                    {
                        gb.DownGrda(ids, callbackAll);
                    }

                    //JtBusiness.JtBusiness jt = new JtBusiness.JtBusiness();
                    //jt.SysCookieContainer = SysCookieContainer;
                    //jt.loginKey = loginKey;

                    //if (string.IsNullOrEmpty(ids))
                    //{
                    //    jt.DownJT((s) =>
                    //    {
                    //        bgwDownType1.ReportProgress(11, s);
                    //    });
                    //}
                    //else
                    //{
                    //    jt.DownJTByIds(ids, (s) =>
                    //    {
                    //        bgwDownType1.ReportProgress(11, s);
                    //    });
                    //}
                   

                    bgwDownType1.ReportProgress(1, "个人档案下载完成");
                    bgwDownType1.ReportProgress(2, "健康体检下载完成");
                    bgwDownType1.ReportProgress(3, "老年人信息下载完成");
                    bgwDownType1.ReportProgress(4, "高血压信息下载完成");
                    bgwDownType1.ReportProgress(5, "糖尿病信息下载完成");
                    bgwDownType1.ReportProgress(6, "脑卒中信息下载完成");
                    bgwDownType1.ReportProgress(7, "冠心病信息下载完成");
                    //bgwDownType1.ReportProgress(11, "家庭信息下载完成");
                };

                bgwDownType1.ProgressChanged += (send, e) =>
                {
                    string showVal = e.UserState.ToString();

                    if (showVal.IndexOf("EX-") != -1)
                    {
                        // 异常信息
                        lstBoxEx.Items.Add(showVal);

                        return;
                    }

                    int step = e.ProgressPercentage;
                    switch (step)
                    {
                        case 1:
                            lblShowType1.Text = showVal;
                            break;
                        case 2:
                            lblShowType2.Text = showVal;
                            break;
                        case 3:
                            lblShowType3.Text = showVal;
                            break;
                        case 4:
                            lblShowType4.Text = showVal;
                            break;
                        case 5:
                            lblShowType5.Text = showVal;
                            break;
                        case 6:
                            lblShowType6.Text = showVal;
                            break;
                        case 7:
                            lblShowType8.Text = showVal;
                            break;
                        //case 11:
                        //    lblShowType11.Text = showVal;
                        //    break;
                    }
                };

                bgwDownType1.RunWorkerAsync();

                //bgwDownType1.DoWork += (send, e) =>
                //{
                //    bgwDownType1.ReportProgress(1, "个人档案下载开始...");

                //    GrdaBusiness.GrdaBusiness gb = new GrdaBusiness.GrdaBusiness();
                //    gb.SysCookieContainer = SysCookieContainer;
                //    gb.loginKey = loginKey;
                //    gb.querylist = querylist;

                //    if (string.IsNullOrEmpty(ids))
                //    {
                //        gb.DownGrda((s) =>
                //        {
                //            bgwDownType1.ReportProgress(1, s);
                //        });
                //    }
                //    else
                //    {
                //        gb.DownGrda(ids, (s) =>
                //        {
                //            bgwDownType1.ReportProgress(1, s);
                //        });
                //    }
                //    bgwDownType1.ReportProgress(1, "个人档案下载完成");
                //    bgwDownType1.ReportProgress(2, "健康体检下载开始...");
                //    TjBusiness.TjBusiness tj = new TjBusiness.TjBusiness();
                //    tj.SysCookieContainer = SysCookieContainer;
                //    tj.loginKey = loginKey;
                //    tj.lstPerson = gb.lstPerson;
                //    tj.querylist = querylist;
                //    if (string.IsNullOrEmpty(ids))
                //    {
                //        tj.DownLoadTj((s) =>
                //        {
                //            bgwDownType1.ReportProgress(2, s);
                //        });
                //    }
                //    else
                //    {
                //        tj.DownLoadTJByIds(ids, (s) =>
                //        {
                //            bgwDownType1.ReportProgress(2, s);
                //        });
                //    }

                //    bgwDownType1.ReportProgress(2, "健康体检下载完成");

                //    bgwDownType1.ReportProgress(11, "家庭信息下载开始...");
                //    JtBusiness.JtBusiness jt = new JtBusiness.JtBusiness();
                //    jt.SysCookieContainer = SysCookieContainer;
                //    jt.loginKey = loginKey;

                //    if (string.IsNullOrEmpty(ids))
                //    {
                //        jt.DownJT((s) =>
                //        {
                //            bgwDownType1.ReportProgress(11, s);
                //        });
                //    }
                //    else
                //    {
                //        jt.DownJTByIds(ids, (s) =>
                //        {
                //            bgwDownType1.ReportProgress(11, s);
                //        });
                //    }

                //    bgwDownType1.ReportProgress(11, "家庭信息下载完成");
                //};

                //bgwDownType1.ProgressChanged += (send, e) =>
                //{
                //    string showVal = e.UserState.ToString();

                //    if (showVal.IndexOf("EX-") != -1)
                //    {
                //        // 异常信息
                //        lstBoxEx.Items.Add(showVal);

                //        return;
                //    }

                //    int step = e.ProgressPercentage;
                //    switch (step)
                //    {
                //        case 1:
                //            lblShowType1.Text = showVal;
                //            break;
                //        case 2:
                //            lblShowType2.Text = showVal;
                //            break;
                //        case 11:
                //            lblShowType11.Text = showVal;
                //            break;
                //    }

                //};

                //bgwDownType1.RunWorkerAsync();

                //BackgroundWorker bgwDownType2 = new BackgroundWorker();
                //bgwDownType2.WorkerReportsProgress = true;

                //bgwDownType2.DoWork += (send, e) =>
                //{
                //    bgwDownType2.ReportProgress(3, "老年人信息下载开始...");

                //    LnrBusiness.LnrBusiness lnr = new LnrBusiness.LnrBusiness();
                //    lnr.SysCookieContainer = SysCookieContainer;
                //    lnr.loginkey = loginKey;

                //    if (string.IsNullOrEmpty(ids))
                //    {
                //        lnr.Download((s) =>
                //        {
                //            bgwDownType2.ReportProgress(3, s);
                //        });
                //    }
                //    else
                //    {
                //        lnr.DownLnrByIDs(ids, (s) =>
                //        {
                //            bgwDownType2.ReportProgress(3, s);
                //        });
                //    }

                //    bgwDownType2.ReportProgress(3, "老年人信息下载完成");
                //    bgwDownType2.ReportProgress(4, "高血压信息下载开始...");
                //    GxyBusiness.GxyBusiness gxy = new GxyBusiness.GxyBusiness();
                //    gxy.SysCookieContainer = SysCookieContainer;
                //    gxy.loginkey = loginKey;

                //    if (string.IsNullOrEmpty(ids))
                //    {
                //        gxy.DownGxy((s) =>
                //        {
                //            bgwDownType2.ReportProgress(4, s);
                //        });
                //    }
                //    else
                //    {
                //        gxy.DownGxyByIDs(ids, (s) =>
                //        {
                //            bgwDownType2.ReportProgress(4, s);
                //        });
                //    }

                //    bgwDownType2.ReportProgress(4, "高血压信息下载完成");
                //    bgwDownType2.ReportProgress(5, "糖尿病信息下载开始...");
                //    TnbBusiness.TnbBusiness tnb = new TnbBusiness.TnbBusiness();
                //    tnb.SysCookieContainer = SysCookieContainer;
                //    tnb.loginkey = loginKey;

                //    if (string.IsNullOrEmpty(ids))
                //    {
                //        tnb.DownTnb((s) =>
                //        {
                //            bgwDownType2.ReportProgress(5, s);
                //        });
                //    }
                //    else
                //    {
                //        tnb.DownTnbByIDs(ids, (s) =>
                //        {
                //            bgwDownType2.ReportProgress(5, s);
                //        });
                //    }

                //    bgwDownType2.ReportProgress(5, "糖尿病信息下载完成");
                //};

                //bgwDownType2.ProgressChanged += (send, e) =>
                //{
                //    string showVal = e.UserState.ToString();

                //    if (showVal.IndexOf("EX-") != -1)
                //    {
                //        //异常信息
                //        lstBoxEx.Items.Add(showVal);
                //        return;
                //    }

                //    int step = e.ProgressPercentage;
                //    switch (step)
                //    {
                //        case 3:
                //            lblShowType3.Text = showVal;
                //            break;
                //        case 4:
                //            lblShowType4.Text = showVal;
                //            break;
                //        case 5:
                //            lblShowType5.Text = showVal;
                //            break;
                //        case 11:
                //            lblShowType11.Text = showVal;
                //            break;
                //    }
                //};

                //bgwDownType2.RunWorkerAsync();

                //BackgroundWorker bgwDownType3 = new BackgroundWorker();
                //bgwDownType3.WorkerReportsProgress = true;

                //bgwDownType3.DoWork += (send, e) =>
                //{
                //    bgwDownType3.ReportProgress(6, "脑卒中信息下载开始...");

                //    NczBusiness.NczBusiness ncz = new NczBusiness.NczBusiness();
                //    ncz.SysCookieContainer = SysCookieContainer;
                //    ncz.loginkey = loginKey;

                //    if (string.IsNullOrEmpty(ids))
                //    {
                //        ncz.DownInfo((s) =>
                //        {
                //            bgwDownType3.ReportProgress(6, s);
                //        });
                //    }
                //    else
                //    {
                //        ncz.DownByIDs(ids, (s) =>
                //        {
                //            bgwDownType3.ReportProgress(6, s);
                //        });
                //    }

                //    bgwDownType3.ReportProgress(6, "脑卒中信息下载完成");

                //    bgwDownType3.ReportProgress(8, "冠心病信息下载开始...");

                //    GxbBusiness.GxbBusiness gxb = new GxbBusiness.GxbBusiness();
                //    gxb.SysCookieContainer = SysCookieContainer;
                //    gxb.loginkey = loginKey;

                //    if (string.IsNullOrEmpty(ids))
                //    {
                //        gxb.DownInfo((s) =>
                //        {
                //            bgwDownType3.ReportProgress(8, s);
                //        });
                //    }
                //    else
                //    {
                //        gxb.DownByIDs(ids, (s) =>
                //        {
                //            bgwDownType3.ReportProgress(8, s);
                //        });
                //    }

                //    bgwDownType3.ReportProgress(8, "冠心病信息下载完成");
                //};
                //bgwDownType3.ProgressChanged += (send, e) =>
                //{
                //    string showVal = e.UserState.ToString();

                //    if (showVal.IndexOf("EX-") != -1)
                //    {
                //        //异常信息
                //        lstBoxEx.Items.Add(showVal);
                //        return;
                //    }

                //    int step = e.ProgressPercentage;
                //    switch (step)
                //    {
                //        case 6:
                //            lblShowType6.Text = showVal;
                //            break;
                //        case 8:
                //            lblShowType8.Text = showVal;
                //            break;
                //    }
                //};
                //bgwDownType3.RunWorkerAsync();
            }
        }

        private void chkDate_Click(object sender, EventArgs e)
        {
            dtPickerS.Enabled = chkDate.Checked;
            dtPickerE.Enabled = chkDate.Checked;
            if (chkVist.Checked)
            {
                chkVist.Checked = false;
                dtVistS.Enabled = chkVist.Checked;
                dtVistE.Enabled = chkVist.Checked;
            }
        }

        private void chkVist_Click(object sender, EventArgs e)
        {
            dtVistS.Enabled = chkVist.Checked;
            dtVistE.Enabled = chkVist.Checked;
            if (chkDate.Checked)
            {
                chkDate.Checked = false;
                dtPickerS.Enabled = chkDate.Checked;
                dtPickerE.Enabled = chkDate.Checked;
            }
        }

        private void chkJd_Click(object sender, EventArgs e)
        {
            dtJdSdate.Enabled = chkJd.Checked;
            dtJdEdate.Enabled = chkJd.Checked;
        }

        private void ClearText()
        {
            lblShowType1.Text = "...";
            lblShowType2.Text = "...";
            lblShowType3.Text = "...";
            lblShowType4.Text = "...";
            lblShowType5.Text = "...";
            lblShowType11.Text = "...";
            lblShowType6.Text = "...";
            lblShowType8.Text = "...";

            lstBoxEx.Items.Clear();
        }
        private void InitVillage()
        {
            Village village = new Village();
            village.code = "";
            village.text = "请选择";

            List<Village> villageList = new List<Village>();
            villageList.Add(village);

            village1.DataSource = villageList;
            village1.ValueMember = "code";
            village1.DisplayMember = "text";
        }

        private void InitQVillage()
        {
            Village village = new Village();
            village.code = "";
            village.text = "请选择";

            List<Village> villageList = new List<Village>();
            villageList.Add(village);

            cmbVill.DataSource = villageList;
            cmbVill.ValueMember = "code";
            cmbVill.DisplayMember = "text";
        }
        private void street1_SelectedValueChanged(object sender, EventArgs e)
        {
            village1.DataSource = null;
            village1.Items.Clear();

            if (street1.SelectedItem == null)
            {
                return;
            }

            var newStreet = ((Village)street1.SelectedItem).code;

            if (newStreet.ToString() == "")
            {
                InitVillage();
            }
            else
            {
                var villagelist = cb.GetVillageList(newStreet.ToString(), loginKey, SysCookieContainer);

                village1.DataSource = villagelist;
                village1.ValueMember = "code";
                village1.DisplayMember = "text";

            }
        }

        private void chkAll_Click(object sender, EventArgs e)
        {
            bool checkvalue = chkAll.Checked;

            chkGrd.Checked = checkvalue;
            chkTJ.Checked = checkvalue;
            chkJT.Checked = checkvalue;
            chkTnb.Checked = checkvalue;
            chkLNR.Checked = checkvalue;
            chkGxy.Checked = checkvalue;
            chkGxb.Checked = checkvalue;
            chkNcz.Checked = checkvalue;
        }

        /// <summary>
        /// 绑定下载村委查询条件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbTown_SelectedValueChanged(object sender, EventArgs e)
        {
            cmbVill.DataSource = null;
            cmbVill.Items.Clear();

            if (cmbTown.SelectedItem == null)
            {
                return;
            }

            var newStreet = ((Village)cmbTown.SelectedItem).code;

            if (newStreet.ToString() == "")
            {
                InitQVillage();
            }
            else
            {
                var villagelist = cb.GetVillageList(newStreet.ToString(), loginKey, SysCookieContainer);

                Village village = new Village();
                village.code = "";
                village.text = "请选择";
                villagelist.Insert(0,village);

                cmbVill.DataSource = villagelist;
                cmbVill.ValueMember = "code";
                cmbVill.DisplayMember = "text";
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (lstBoxEx.Items.Count == 0)
            {
                MessageBox.Show("异常信息为空，不能导出");
                return;
            }
            SaveFileDialog savefiledialog1 = new SaveFileDialog();
            savefiledialog1.Filter = "文本文档(*.txt)|*.txt|所有文件(*.*)|*.*";
            string filepath = "";
            string totext = "";
            if (savefiledialog1.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < lstBoxEx.Items.Count; i++)
                {
                    totext += lstBoxEx.Items[i] + "\r\n";
                }
                filepath = savefiledialog1.FileName; ;
                StreamWriter sw = new StreamWriter(filepath);
                sw.Write(totext);
                sw.Flush();
                sw.Close();
                //MessageBox.Show(savefiledialog1.FileName);
            }
        }

        private void chkBrith_Click(object sender, EventArgs e)
        {
            dtSbrith.Enabled = chkBrith.Checked;
            dtEBrith.Enabled = chkBrith.Checked;
        }

        private void pictureBoxQuery_Click(object sender, EventArgs e)
        {
            if (chkDate.Checked && (dtPickerS.Text != "" && dtPickerE.Text != "" && Convert.ToDateTime(dtPickerS.Text) > Convert.ToDateTime(dtPickerE.Text)))
            {
                MessageBox.Show("体检日期起不能大于体检日期止");

                return;
            }

            if (chkVist.Checked && (dtVistS.Text != "" && dtVistE.Text != "" && Convert.ToDateTime(dtVistS.Text) > Convert.ToDateTime(dtVistE.Text)))
            {
                MessageBox.Show("随访日期起不能大于随访日期止");

                return;
            }

            if (chkJd.Checked && (dtJdSdate.Text != "" && dtJdEdate.Text != "" && Convert.ToDateTime(dtJdSdate.Text) > Convert.ToDateTime(dtJdEdate.Text)))
            {
                MessageBox.Show("更新日期起不能大于更新日期止");

                return;
            }

            string strdateS = chkDate.Checked ? dtPickerS.Text : "";
            string strdateE = chkDate.Checked ? dtPickerE.Text : "";

            string strvistS = chkVist.Checked ? dtVistS.Text : "";
            string strvistE = chkVist.Checked ? dtVistE.Text : "";

            string strjdS = chkJd.Checked ? dtJdSdate.Text : "";
            string strjdE = chkJd.Checked ? dtJdEdate.Text : "";

            string VillageName = cmbQVillageID.Text.ToString();

            bool clqb = chkclall.Checked;

            CommonDAOBusiness bs = new CommonDAOBusiness();
            DataTable dt = new DataTable();

            if (chkVist.Checked)
            {
                dt = bs.GetPersonNameIDByVistDate(tbnameSelect.Text.Trim(), tbIDCard.Text.Trim(), cmbBoxAllDoctor.Text, VillageName, strvistS, strvistE, strjdS, strjdE);
            }
            else
            {
                dt = bs.GetPersonNameID(tbnameSelect.Text.Trim(), tbIDCard.Text.Trim(), cmbBoxAllDoctor.Text, VillageName, strdateS, strdateE, strjdS, strjdE, clqb);
            }

            dgvBaseInfo.DataSource = dt;
            dgvBaseInfo.AllowUserToAddRows = false;
            dtInfo = dt;
            dgvBaseInfo.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            nMax = dtInfo.Rows.Count;
            pageCount = nMax / pageSize;

            if ((nMax % pageSize) > 0)
            {
                pageCount += 1;
            }

            labCount.Text = nMax.ToString();

            labSumpage.Text = pageCount.ToString();
            labNpage.Text = "1";
            pageCurrent = Convert.ToInt32(labNpage.Text);
            nCurrent = 0;
            LoadData();
        }

        private void pictureBoxClear_Click(object sender, EventArgs e)
        {
            cmbBoxAllDoctor.SelectedIndex = 0;
            tbnameSelect.Text = "";
            tbIDCard.Text = "";

            chkDate.Checked = true;
            dtPickerS.Enabled = chkDate.Checked;
            dtPickerE.Enabled = chkDate.Checked;
        }

        private void pictureBoxSelectedUpload_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            dt = GetSelectedInfo();

            if (dt.Rows.Count > 0)
            {
                string ids = "";

                foreach (DataRow row in dt.Rows)
                {
                    ids += "," + row["身份证号码"].ToString();
                }

                ids = ids.TrimStart(',');

                UploadMain(ids);
            }
            else
            {
                MessageBox.Show("请选择需要上传的人员！");
            }
        }

        private void pictureBoxAllUpload_Click(object sender, EventArgs e)
        {
            string ids = "";

            for (int i = 0; i < dtInfo.Rows.Count; i++) //显示的行
            {
                ids += "," + dtInfo.Rows[i]["身份证号码"];
            }

            if (string.IsNullOrEmpty(ids))
            {
                MessageBox.Show("请先查询");

                return;
            }

            UploadMain(ids.TrimStart(','));
        }

        private void pictureBoxDownSelected_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            dt = GetSelectedInfo();

            if (dt.Rows.Count > 0)
            {
                string ids = "";

                foreach (DataRow row in dt.Rows)
                {
                    ids += "," + row["身份证号码"].ToString();
                }

                ids = ids.TrimStart(',');

                DownloadMain(ids);
            }
            else if (down1.Text.Trim() != "")
            {
                DownloadMain(down1.Text.Trim());
            }
            else
            {
                MessageBox.Show("请选择需要下载的人员！");
            }
        }

        private void pictureBoxDownAll_Click(object sender, EventArgs e)
        {
            DownloadMain("");
        }

        private void linkLabelExport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (lstBoxEx.Items.Count == 0)
            {
                MessageBox.Show("异常信息为空，不能导出");
                return;
            }
            SaveFileDialog savefiledialog1 = new SaveFileDialog();
            savefiledialog1.Filter = "文本文档(*.txt)|*.txt|所有文件(*.*)|*.*";
            string filepath = "";
            string totext = "";
            if (savefiledialog1.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < lstBoxEx.Items.Count; i++)
                {
                    totext += lstBoxEx.Items[i] + "\r\n";
                }
                filepath = savefiledialog1.FileName; ;
                StreamWriter sw = new StreamWriter(filepath);
                sw.Write(totext);
                sw.Flush();
                sw.Close();
                //MessageBox.Show(savefiledialog1.FileName);
            }
        }
    }

    public class ComBoxInfo
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}
