using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseBusiness;
using System.Data;
using Utilities.Common;
using DAL;

namespace CommonBusiness
{
    public class CommonDAOBusiness : BaseBusinessRule
    {
        string baseUrl = Config.GetValue("baseUrl");

        /// <summary>
        /// 获取医生名单
        /// </summary>
        /// <returns></returns>
        public DataTable GetDoctorName()
        {
            string selectdoctor = "select distinct  CreateMenName from ARCHIVE_BASEINFO order by CreateMenName";

            return base.Search(selectdoctor);
        }

        /// <summary>
        /// 条件查询
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="doctor"></param>
        /// <returns></returns>
        public DataTable GetPersonNameID(string name, string id, string doctor, string VillageName, string dateS = "", string dateE = "", string jdDateS = "", string jdDateE = "", bool clqb = false)
        {
            StringBuilder selectName = new StringBuilder();

            selectName.Append(@"select
                                    baseinfo.IDCardNo as 身份证号码
                                    ,baseinfo.CustomerName as 姓名
                                    ,baseinfo.CreateMenName as 医生
                                    ,cusinfo.LastUpdateDate as 体检日期  ");
            if (!string.IsNullOrEmpty(jdDateS))
            {
                selectName.Append(" ,baseinfo.LastUpdateDate as 更新日期 ");
            }
            selectName.Append(@"  from 
                                    ARCHIVE_BASEINFO  baseinfo
                                LEFT JOIN
                                (
		                                select ARCHIVE_CUSTOMERBASEINFO.IDCardNo,max(CheckDate) as LastUpdateDate from 
				                                ARCHIVE_CUSTOMERBASEINFO ");
            if (clqb)
            {
                selectName.Append(@"  
	        
                                        inner join ARCHIVE_VISCERAFUNCTION ction on ARCHIVE_CUSTOMERBASEINFO.ID=ction.OutKey
                                        and (ction.HypodontiaEx like '%全部%' or ction.SaprodontiaEx like '%全部%' or ction.DentureEx like '%全部%') ");
            }
            selectName.Append(@"      GROUP BY ARCHIVE_CUSTOMERBASEINFO.IDCardNo
                                ) cusinfo
                                    ON baseinfo.IDCardNo = cusinfo.IDCardNo 
                                where ");

            selectName.Append("  baseinfo.CustomerName like '%" + name + "%' ");

            if (VillageName.Trim() != "" && !VillageName.Trim().Contains("请选择"))
            {
                selectName.Append("  and baseinfo.VillageName like '%" + VillageName + "%' ");
            }

            base.Parameter.Clear();

            if (doctor != "")
            {
                selectName.Append(" and baseinfo.CreateMenName = @CreateMenName");
                base.Parameter.Add("CreateMenName", doctor);
            }

            if (id != "")
            {
                selectName.Append(" and baseinfo.IDCardNo = @IDCardNo");
                base.Parameter.Add("IDCardNo", id);
            }

            if (!string.IsNullOrEmpty(dateS))
            {
                selectName.Append("  AND cusinfo.LastUpdateDate >= @dateS");
                base.Parameter.Add("dateS", dateS);
            }

            if (!string.IsNullOrEmpty(dateE))
            {
                selectName.Append("   AND cusinfo.LastUpdateDate <= @dateE");
                base.Parameter.Add("dateE", dateE);
            }

            if (!string.IsNullOrEmpty(jdDateS))
            {
                selectName.Append("   AND baseinfo.LastUpdateDate >= @jdDateS");
                base.Parameter.Add("jdDateS", jdDateS);
            }

            if (!string.IsNullOrEmpty(jdDateE))
            {
                selectName.Append("   AND baseinfo.LastUpdateDate <= @jdDateE");
                base.Parameter.Add("jdDateE", jdDateE);
            }

            return base.Search(selectName.ToString());
        }

        /// <summary>
        /// 根据随访日期查询
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="doctor"></param>
        /// <param name="VillageName"></param>
        /// <param name="dateS"></param>
        /// <param name="dateE"></param>
        /// <returns></returns>
        public DataTable GetPersonNameIDByVistDate(string name, string id, string doctor, string VillageName, string dateS = "", string dateE = "", string jdDateS = "", string jdDateE = "")
        {
            StringBuilder selectName = new StringBuilder();

            selectName.Append(@"select
                                    baseinfo.IDCardNo as 身份证号码
                                    ,baseinfo.CustomerName as 姓名
                                    ,baseinfo.CreateMenName as 医生
                                    ,cusinfo.LastUpdateDate as 随访日期 ");

            if (!string.IsNullOrEmpty(jdDateS))
            {
                selectName.Append(" ,baseinfo.LastUpdateDate as 更新日期 ");
            }

            selectName.Append(@"
                                from 
                                    ARCHIVE_BASEINFO  baseinfo
                                LEFT JOIN
                                (
		                                select DISTINCT IDCardNo,LastUpdateDate FROM
	                                    (
		                                    select IDCardNo,max(VisitDate) as LastUpdateDate from 
				                                    CD_DIABETESFOLLOWUP  
		                                    GROUP BY IDCardNo
		                                    UNION
		                                    select IDCardNo,max(FollowUpDate) as LastUpdateDate from 
				                                    CD_HYPERTENSIONFOLLOWUP 
		                                    GROUP BY IDCardNo
		                                    UNION
		                                    select IDCardNo,max(FollowUpDate) as LastUpdateDate from 
				                                    OLDER_SELFCAREABILITY 
		                                    GROUP BY IDCardNo
		                                    UNION
		                                    select IDCardNo,MAX(RecordDate) as LastUpdateDate 
		                                    FROM
			                                    OLD_MEDICINE_CN
		                                    GROUP BY IDCardNo
                                            UNION
                                            select IDCardNo,MAX(VisitDate) as LastUpdateDate 
		                                    FROM
			                                    CD_CHD_FOLLOWUP
		                                    GROUP BY IDCardNo
                                            UNION
                                            select IDCardNo,MAX(FollowupDate) as LastUpdateDate 
		                                    FROM
			                                    CD_STROKE_FOLLOWUP
                                            GROUP BY IDCardNo
                                            UNION
                                            select IDCardNo,MAX(FollowUpDate) as LastUpdateDate 
		                                    FROM
			                                    CD_MENTALDISEASE_FOLLOWUP
		                                    GROUP BY IDCardNo
	                                    ) as tbl 
                                ) cusinfo
                                    ON baseinfo.IDCardNo = cusinfo.IDCardNo 
                                where ");

            selectName.Append("  baseinfo.CustomerName like '%" + name + "%' ");

            if (VillageName.Trim() != "" && !VillageName.Trim().Contains("请选择"))
            {
                selectName.Append("  and baseinfo.VillageName like '%" + VillageName + "%' ");
            }

            base.Parameter.Clear();

            if (doctor != "")
            {
                selectName.Append(" and baseinfo.CreateMenName = @CreateMenName");
                base.Parameter.Add("CreateMenName", doctor);
            }

            if (id != "")
            {
                selectName.Append(" and baseinfo.IDCardNo = @IDCardNo");
                base.Parameter.Add("IDCardNo", id);
            }

            if (!string.IsNullOrEmpty(dateS))
            {
                selectName.Append("  AND cusinfo.LastUpdateDate >= @dateS");
                base.Parameter.Add("dateS", dateS);
            }

            if (!string.IsNullOrEmpty(dateE))
            {
                selectName.Append("   AND cusinfo.LastUpdateDate <= @dateE");
                base.Parameter.Add("dateE", dateE);
            }
            if (!string.IsNullOrEmpty(jdDateS))
            {
                selectName.Append("   AND baseinfo.LastUpdateDate >= @jdDateS");
                base.Parameter.Add("jdDateS", jdDateS);
            }

            if (!string.IsNullOrEmpty(jdDateE))
            {
                selectName.Append("   AND baseinfo.LastUpdateDate <= @jdDateE");
                base.Parameter.Add("jdDateE", jdDateE);
            }
            return base.Search(selectName.ToString());
        }

        /// <summary>
        /// 获取村委清档
        /// </summary>
        /// <returns></returns>
        public DataTable GetVillageName()
        {
            string sql = "select distinct  VillageName from ARCHIVE_BASEINFO order by VillageName";

            return base.Search(sql);
        }

        #region 个人档案

        /// <summary>
        /// 个人档案
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetGrdaDataSet(string doctor, string ids)
        {
            #region SQL
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"SELECT 
		                        baseinfo.CustomerName
		                        ,baseinfo.IDCardNo
		                        ,baseinfo.Birthday
		                        ,baseinfo.Sex
		                        ,baseinfo.Address
		                        ,baseinfo.HouseHoldAddress
		                        ,baseinfo.WorkUnit
		                        ,baseinfo.Phone
		                        ,baseinfo.ContactName
		                        ,baseinfo.ContactPhone
		                        ,baseinfo.LiveType
		                        ,'' as 户籍类型
		                        ,baseinfo.Nation
                                ,baseinfo.Minority
		                        ,baseinfo.BloodType
		                        ,baseinfo.RH
		                        ,baseinfo.Culture
		                        ,baseinfo.Job
		                        ,baseinfo.MaritalStatus
		                        ,baseinfo.MedicalPayType
		                        ,baseinfo.MedicalPayTypeOther
		                        ,baseinfo.DrugAllergic
		                        ,baseinfo.DrugAllergicOther
		                        ,baseinfo.Exposure
		                        ,baseinfo.Disease
		                        ,baseinfo.DiseaseEx
		                        ,baseinfo.DiseasEndition
		                        ,baseinfo.DiseasenditionEx
		                        ,baseinfo.HouseRelation
		                        ,baseinfo.CreateBy
		                        ,baseinfo.LastUpdateBy
		                        ,baseinfo.LastUpdateBy
		                        ,baseinfo.LastUpdateDate
		                        ,baseinfo.Doctor
		                        ,baseinfo.FamilyIDCardNo
		                        ,baseinfo.RecordID
		                        ,baseinfo.VillageID
                                ,baseinfo.CreateDate
                                ,baseinfo.VillageName
                                ,baseinfo.TownName

                                ,baseinfo.CreateUnitName
                                ,baseinfo.CreateMenName
                                ,baseinfo.HouseName
                                ,baseinfo.OrgName
                                ,baseinfo.FamilyNum
                                ,baseinfo.FamilyStructure
                                ,baseinfo.TownMedicalCard
                                ,baseinfo.ResidentMedicalCard
                                ,baseinfo.PovertyReliefMedicalCard
                                ,baseinfo.LiveCondition
                                ,baseinfo.PreSituation
                                ,baseinfo.PreNum
                                ,baseinfo.YieldNum
                                ,baseinfo.Chemical
                                ,baseinfo.Poison
                                ,baseinfo.Radial

                                ,env.ID as envID
		                        ,env.BlowMeasure
		                        ,env.FuelType
		                        ,env.DrinkWater
		                        ,env.Toilet
		                        ,env.LiveStockRail
                                ,fh.ID as fhID
                                ,fh.FamilyType
                                ,fh.FatherHistory
                                ,fh.FatherHistoryOther
                                ,fh.MotherHistory
                                ,fh.MotherHistoryOther
                                ,fh.BrotherSisterHistory
                                ,fh.BrotherSisterHistoryOther
                                ,fh.ChildrenHistory
                                ,fh.ChildrenHistoryOther
                                ,ill.ID as illID
                                ,ill.IllnessType
                                ,ill.IllnessName
                                ,ill.Therioma
                                ,ill.IllnessOther
                                ,ill.JobIllness
                                ,ill.IllnessNameOther
                                ,ill.DiagnoseTime

                                ,health.ID as hID
                                ,health.Prevalence
                                ,health.PrevalenceOther
                                ,health.OrgTelphone
                                ,health.FamilyDoctor
                                ,health.FamilyDoctorTel
                                ,health.Nurses
                                ,health.NursesTel
                                ,health.HealthPersonnel
                                ,health.HealthPersonnelTel
                                ,health.Others
                        FROM 
		                        ARCHIVE_BASEINFO baseinfo
                        LEFT JOIN 
                                ARCHIVE_BASEINFOARCHIVE_ENVIRONMENT env
                            ON baseinfo.IDCardNo = env.IDCardNo
                        LEFT JOIN 
                                ARCHIVE_FAMILYHISTORYINFO fh
                            ON baseinfo.IDCardNo = fh.IDCardNo
                        LEFT JOIN 
                                ARCHIVE_ILLNESSHISTORYINFO ill
                            ON baseinfo.IDCardNo = ill.IDCardNo
                        LEFT JOIN 
                                archive_health_info health
                            ON baseinfo.IDCardNo = health.IDCardNo
                        WHERE 1 = 1");

            base.Parameter.Clear();

            if (!string.IsNullOrEmpty(doctor))
            {
                sbQuery.Append("  AND baseinfo.CreateMenName = @CreateMenName");

                base.Parameter.Add("CreateMenName", doctor);
            }

            if (!string.IsNullOrEmpty(ids))
            {
                ids = "'" + ids.Replace(",", "','") + "'";

                sbQuery.Append("  AND baseinfo.IDCardNo IN (").Append(ids).Append(")");
            }

            #endregion

            DataTable dtAll = base.Search(sbQuery.ToString());

            DataTable dtIDCardNo = new DataTable();
            DataView dvC = dtAll.DefaultView;
            dtIDCardNo = dvC.ToTable(true, "IDCardNo");

            List<DataSet> lstDataSet = new List<DataSet>();
            foreach (DataRow row in dtIDCardNo.Rows)
            {
                string idCardNo = row["IDCardNo"].ToString();

                DataSet ds = new DataSet();

                DataTable dtBaseInfo = new DataTable();

                DataView dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "'";
                List<string> lstBaseColmn = new List<string>();
                lstBaseColmn.Add("CustomerName");
                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("Birthday");
                lstBaseColmn.Add("Sex");
                lstBaseColmn.Add("Address");
                lstBaseColmn.Add("HouseHoldAddress");
                lstBaseColmn.Add("WorkUnit");
                lstBaseColmn.Add("Phone");
                lstBaseColmn.Add("ContactName");
                lstBaseColmn.Add("ContactPhone");
                lstBaseColmn.Add("LiveType");
                lstBaseColmn.Add("户籍类型");
                lstBaseColmn.Add("Nation");
                lstBaseColmn.Add("Minority");

                lstBaseColmn.Add("BloodType");
                lstBaseColmn.Add("RH");
                lstBaseColmn.Add("Culture");
                lstBaseColmn.Add("Job");
                lstBaseColmn.Add("MaritalStatus");
                lstBaseColmn.Add("MedicalPayType");
                lstBaseColmn.Add("MedicalPayTypeOther");
                lstBaseColmn.Add("DrugAllergic");
                lstBaseColmn.Add("DrugAllergicOther");
                lstBaseColmn.Add("Exposure");
                lstBaseColmn.Add("Disease");
                lstBaseColmn.Add("DiseaseEx");
                lstBaseColmn.Add("DiseasEndition");
                lstBaseColmn.Add("DiseasenditionEx");
                lstBaseColmn.Add("HouseRelation");
                lstBaseColmn.Add("CreateBy");
                lstBaseColmn.Add("LastUpdateBy");
                lstBaseColmn.Add("LastUpdateDate");
                lstBaseColmn.Add("Doctor");
                lstBaseColmn.Add("FamilyIDCardNo");
                lstBaseColmn.Add("RecordID");
                lstBaseColmn.Add("VillageID");
                lstBaseColmn.Add("CreateDate");
                lstBaseColmn.Add("VillageName");
                lstBaseColmn.Add("TownName");
                lstBaseColmn.Add("CreateUnitName");
                lstBaseColmn.Add("CreateMenName");
                lstBaseColmn.Add("HouseName");
                lstBaseColmn.Add("OrgName");
                lstBaseColmn.Add("FamilyNum");
                lstBaseColmn.Add("FamilyStructure");
                lstBaseColmn.Add("TownMedicalCard");
                lstBaseColmn.Add("ResidentMedicalCard");
                lstBaseColmn.Add("PovertyReliefMedicalCard");
                lstBaseColmn.Add("LiveCondition");
                lstBaseColmn.Add("PreSituation");
                lstBaseColmn.Add("PreNum");
                lstBaseColmn.Add("YieldNum");
                lstBaseColmn.Add("Chemical");
                lstBaseColmn.Add("Poison");
                lstBaseColmn.Add("Radial");
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "ARCHIVE_BASEINFO";
                ds.Tables.Add(dtBaseInfo);

                DataTable dtEnv = new DataTable();
                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND envID is not null";

                lstBaseColmn = new List<string>();
                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("envID");
                lstBaseColmn.Add("BlowMeasure");
                lstBaseColmn.Add("FuelType");
                lstBaseColmn.Add("DrinkWater");
                lstBaseColmn.Add("Toilet");
                lstBaseColmn.Add("LiveStockRail");

                dtEnv = dv.ToTable(true, lstBaseColmn.ToArray());

                dtEnv.TableName = "ARCHIVE_BASEINFOARCHIVE_ENVIRONMENT";

                ds.Tables.Add(dtEnv);

                DataTable dtfh = new DataTable();
                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND fhID is not null";

                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("fhID");
                lstBaseColmn.Add("FamilyType");
                lstBaseColmn.Add("FatherHistory");
                lstBaseColmn.Add("FatherHistoryOther");
                lstBaseColmn.Add("MotherHistory");
                lstBaseColmn.Add("MotherHistoryOther");
                lstBaseColmn.Add("BrotherSisterHistory");
                lstBaseColmn.Add("BrotherSisterHistoryOther");
                lstBaseColmn.Add("ChildrenHistory");
                lstBaseColmn.Add("ChildrenHistoryOther");

                dtfh = dv.ToTable(true, lstBaseColmn.ToArray());
                dtfh.TableName = "ARCHIVE_FAMILYHISTORYINFO";
                ds.Tables.Add(dtfh);


                DataTable dtill = new DataTable();
                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "'  AND illID is not null";

                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("illID");
                lstBaseColmn.Add("IllnessType");
                lstBaseColmn.Add("IllnessName");
                lstBaseColmn.Add("Therioma");
                lstBaseColmn.Add("IllnessOther");
                lstBaseColmn.Add("JobIllness");
                lstBaseColmn.Add("IllnessNameOther");
                lstBaseColmn.Add("DiagnoseTime");


                dtill = dv.ToTable(true, lstBaseColmn.ToArray());
                dtill.TableName = "ARCHIVE_ILLNESSHISTORYINFO";
                ds.Tables.Add(dtill);

                DataTable dth = new DataTable();
                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND hID is not null";

                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("hID");
                lstBaseColmn.Add("Prevalence");
                lstBaseColmn.Add("PrevalenceOther");
                lstBaseColmn.Add("OrgTelphone");
                lstBaseColmn.Add("FamilyDoctor");
                lstBaseColmn.Add("FamilyDoctorTel");
                lstBaseColmn.Add("Nurses");
                lstBaseColmn.Add("NursesTel");
                lstBaseColmn.Add("HealthPersonnel");
                lstBaseColmn.Add("HealthPersonnelTel");
                lstBaseColmn.Add("Others");

                dth = dv.ToTable(true, lstBaseColmn.ToArray());
                dth.TableName = "archive_health_info";
                ds.Tables.Add(dth);

                lstDataSet.Add(ds);
            }

            return lstDataSet;
        }

        #endregion

        #region 家庭档案

        /// <summary>
        /// 家庭
        /// </summary>
        /// <param name="doctor">医生编号</param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetFamilyDataSet(string doctor, string ids)
        {
            List<DataSet> lstDataSet = new List<DataSet>();

            StringBuilder sbFaID = new StringBuilder();

            sbFaID.Append(@"SELECT 
                               DISTINCT FamilyIDCardNo  
                            FROM 
                                ARCHIVE_BASEINFO baseinfoC
                            WHERE
                                baseinfoC.FamilyIDCardNo IS NOT NULL");

            if (!string.IsNullOrEmpty(ids))
            {
                ids = "'" + ids.Replace(",", "','") + "'";

                sbFaID.Append("  AND baseinfoC.IDCardNo IN (").Append(ids).Append(")");
            }

            base.Parameter.Clear();

            DataTable dtAllF = base.Search(sbFaID.ToString());

            string faID = "";

            foreach (DataRow row in dtAllF.Rows)
            {
                faID += "," + row["FamilyIDCardNo"];
            }

            faID = faID.TrimStart(',');

            if (string.IsNullOrEmpty(faID))
            {
                return lstDataSet;
            }

            StringBuilder querySql = new StringBuilder();
            #region
            querySql.Append(@"
                        SELECT fa.ID
				            ,fa.FamilyRecordID
				            ,fa.IDCardNo  
				            ,fa.RecordID
				            ,fa.HomeAddr
				            ,fa.HomeAddrInfo
				            ,fa.ToiletType
				            ,fa.HouseType
				            ,fa.IsPoorfy
				            ,fa.LiveStatus
				            ,fa.IncomeAvg
				            ,fa.CreateUnit
				            ,fa.HouseArea
				            ,fa.Monthoil
				            ,fa.MonthSalt
				            ,fa.CreatedBy
				            ,fa.CreatedDate
				            ,fa.LastUpDateBy
				            ,fa.LastUpdateDate 
                            from ARCHIVE_FAMILY_INFO as fa
	     
                        WHERE
                            1=1
                ");

            base.Parameter.Clear();

            faID = "'" + faID.Replace(",", "','") + "'";

            querySql.Append("  AND fa.IDCardNo IN (").Append(faID).Append(")");

            #endregion

            DataTable dtAll = base.Search(querySql.ToString());

            DataTable dtIDCardNo = new DataTable();
            DataView dvC = dtAll.DefaultView;
            dtIDCardNo = dvC.ToTable(true, "IDCardNo");


            foreach (DataRow row in dtIDCardNo.Rows)
            {
                string idCardNo = row["IDCardNo"].ToString();

                DataSet ds = new DataSet();

                DataTable dtBaseInfo = new DataTable();

                DataView dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";
                List<string> lstBaseColmn = new List<string>();
                #region   famly
                lstBaseColmn.Add("ID");
                lstBaseColmn.Add("FamilyRecordID");
                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("RecordID");
                lstBaseColmn.Add("HomeAddr");
                lstBaseColmn.Add("HomeAddrInfo");
                lstBaseColmn.Add("ToiletType");
                lstBaseColmn.Add("HouseType");
                lstBaseColmn.Add("IsPoorfy");
                lstBaseColmn.Add("LiveStatus");
                lstBaseColmn.Add("IncomeAvg");
                lstBaseColmn.Add("CreateUnit");
                lstBaseColmn.Add("HouseArea");
                lstBaseColmn.Add("Monthoil");
                lstBaseColmn.Add("MonthSalt");
                lstBaseColmn.Add("CreatedBy");
                lstBaseColmn.Add("CreatedDate");
                lstBaseColmn.Add("LastUpDateBy");
                lstBaseColmn.Add("LastUpdateDate");
                #endregion
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "ARCHIVE_FAMILY_INFO";
                ds.Tables.Add(dtBaseInfo);

                StringBuilder sbMem = new StringBuilder();

                sbMem.Append(@"SELECT
                                    baseInfo.HouseRelation
                                    ,baseInfo.IDCardNo
                               FROM 
                                    ARCHIVE_BASEINFO as baseInfo
                               WHERE 
                                    baseInfo.FamilyIDCardNo= @FamilyIDCardNo");

                base.Parameter.Clear();

                base.Parameter.Add("FamilyIDCardNo", idCardNo);

                DataTable bas = base.Search(sbMem.ToString()).Copy();
                bas.TableName = "memBer";
                ds.Tables.Add(bas);

                lstDataSet.Add(ds);
            }

            return lstDataSet;
        }

        #endregion

        #region 健康体检


        /// <summary>
        /// 体检信息
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetTjMainDataSet(string doctor, string ids)
        {
            List<DataSet> lstDataSet = new List<DataSet>();

            if (string.IsNullOrEmpty(ids))
            {
                return lstDataSet;
            }

            var idsA = ids.Split(',');

            StringBuilder sbQueryIDCardNo = new StringBuilder();

            ids = "'" + ids.Replace(",", "','") + "'";

            sbQueryIDCardNo.Append("  IN (").Append(ids).Append(")");

            #region SQL
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"SELECT A.ID 
				                ,A.CheckDate
				                ,A.Doctor
				                ,A.Symptom
				                ,A.Other 
				                ,A.IDCardNo
                            FROM ARCHIVE_CUSTOMERBASEINFO AS A
                            INNER JOIN 
                            (SELECT 
		                            MAX(CheckDate) AS CheckDate
		                            ,IDCardNo
                            FROM ARCHIVE_CUSTOMERBASEINFO
                            WHERE IDCardNo ").Append(sbQueryIDCardNo);
            sbQuery.Append(@"GROUP BY IDCardNo
                            ) B
                            ON A.CheckDate=B.CheckDate AND A.IDCardNo=B.IDCardNo;");

            base.Parameter.Clear();


            #endregion

            DataSet dsAll = base.SearchDataSet(sbQuery.ToString());

            #region 筛选
            if (dsAll != null && dsAll.Tables.Count > 0)
            {
                foreach (var idCardNo in idsA)
                {
                    int pNTable = 0;
                    DataSet ds = new DataSet();

                    // 健康体检_客户体检基本信息
                    DataTable dtBaseInfo = dsAll.Tables[pNTable++];
                    DataView dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";
                    DataTable dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_CUSTOMERBASEINFO";

                    if (dtInfo.Rows.Count <= 0)
                    {
                        continue;
                    }

                    ds.Tables.Add(dtInfo);

                    lstDataSet.Add(ds);
                }
            }

            #endregion

            return lstDataSet;
        }


        /// <summary>
        /// 体检信息
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public DataSet GetTjDataSet(string outkey, string ids)
        {
            // DataSet lstDataSet = new DataSet();

            if (string.IsNullOrEmpty(ids))
            {
                return null;
            }

            var idsA = ids.Split(',');

            StringBuilder sbQueryIDCardNo = new StringBuilder();

            ids = "'" + ids.Replace(",", "','") + "'";

            sbQueryIDCardNo.Append("  IN (").Append(ids).Append(")");

            #region SQL
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"SELECT  
                                    baseinfo.Birthday
                                    ,baseinfo.PopulationType           
                                    ,baseinfo.CustomerName           
                                    ,cusinfo.ID 
                                    ,cusinfo.CheckDate
                                    ,cusinfo.Doctor
                                    ,cusinfo.Symptom
                                    ,cusinfo.Other 
                                    ,cusinfo.IDCardNo
                            FROM
                                    ARCHIVE_BASEINFO baseinfo
                                INNER JOIN
                                        ARCHIVE_CUSTOMERBASEINFO cusinfo
                                ON baseinfo.IDCardNo = cusinfo.IDCardNo and cusinfo.id=@outkey 
                          
                            WHERE 
                                baseinfo.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT 
                                        cdtion.ID
                                        ,cdtion.AnimalHeat
                                        ,cdtion.BreathRate
                                        ,cdtion.Waistline
                                        ,cdtion.Height
                                        ,cdtion.OldRecognise
                                        ,cdtion.OldEmotion
                                        ,cdtion.PulseRate
                                        ,cdtion.Weight
                                        ,cdtion.BMI
                                        ,cdtion.InterScore
                                        ,cdtion.GloomyScore
                                        ,cdtion.LeftPre
                                        ,cdtion.RightPre
                                        ,cdtion.WaistIp
                                        ,cdtion.LeftHeight
                                        ,cdtion.RightHeight
                                        ,cdtion.OldHealthStaus
                                        ,cdtion.Tem
                                        ,cdtion.OldSelfCareability   
                                        ,cdtion.IDCardNo
                                        ,cdtion.LeftReason
                                        ,cdtion.RightReason
                                        ,cdtion.OldMange
                                FROM
                                    ARCHIVE_GENERALCONDITION cdtion
                                WHERE   cdtion.OutKey=@outkey and 
                                    cdtion.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT 
                                    lifestyle.ID 
                                    ,lifestyle.SmokeDayNum
                                    ,lifestyle.SmokeAgeStart
                                    ,lifestyle.SmokeAgeForbiddon
                                    ,lifestyle.ExerciseRate
                                    ,lifestyle.ExerciseTimes
                                    ,lifestyle.DietaryHabit
                                    ,lifestyle.ExerciseExistense
                                    ,lifestyle.ExcisepersistTime
                                    ,lifestyle.SmokeCondition
                                    ,lifestyle.DrinkRate
                                    ,lifestyle.DayDrinkVolume
                                    ,lifestyle.IsDrinkForbiddon
                                    ,lifestyle.ForbiddonAge
                                    ,lifestyle.DrinkStartAge
                                    ,lifestyle.DrinkThisYear
                                    ,lifestyle.DrinkType
                                    ,lifestyle.CareerHarmFactorHistory
                                    ,lifestyle.Dust
                                    ,lifestyle.DustProtect
                                    ,lifestyle.Radiogen
                                    ,lifestyle.RadiogenProtect
                                    ,lifestyle.Physical
                                    ,lifestyle.PhysicalProtect
                                    ,lifestyle.Chem
                                    ,lifestyle.ChemProtect
                                    ,lifestyle.Other
                                    ,lifestyle.OtherProtect
                                    ,lifestyle.WorkType
                                    ,lifestyle.WorkTime
                                    ,lifestyle.DustProtectEx
                                    ,lifestyle.RadiogenProtectEx
                                    ,lifestyle.PhysicalProtectEx
                                    ,lifestyle.ChemProtectEx
                                    ,lifestyle.OtherProtectEx
                                    ,lifestyle.DrinkTypeOther
                                    ,lifestyle.IDCardNo
                                    ,lifestyle.ExerciseExistenseOther 
                            FROM
                                    ARCHIVE_LIFESTYLE lifestyle
                            WHERE   lifestyle.OutKey=@outkey and 
                                lifestyle.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT 
                                svfun.ID
                                ,svfun.Lips
                                ,svfun.ToothResides
                                ,svfun.ToothResidesOther
                                ,svfun.Pharyngeal
                                ,svfun.LeftView
                                ,svfun.Listen
                                ,svfun.RightView
                                ,svfun.SportFunction
                                ,svfun.LeftEyecorrect
                                ,svfun.RightEyecorrect
                                ,svfun.IDCardNo
                                ,svfun.HypodontiaEx
                                ,svfun.SaprodontiaEx
                                ,svfun.DentureEx
                                ,svfun.LipsEx
                                ,svfun.PharyngealEx
                            FROM
                                ARCHIVE_VISCERAFUNCTION svfun
                            WHERE   svfun.OutKey=@outkey 
                              and   svfun.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                exam.ID 
                                ,exam.Skin
                                ,exam.Sclere
                                ,exam.Lymph
                                ,exam.BarrelChest
                                ,exam.BreathSounds
                                ,exam.Rale
                                ,exam.HeartRate
                                ,exam.HeartRhythm
                                ,exam.Noise
                                ,exam.EnclosedMass
                                ,exam.Edema
                                ,exam.FootBack
                                ,exam.Anus
                                ,exam.Breast
                                ,exam.Vulva
                                ,exam.Vagina
                                ,exam.CervixUteri
                                ,exam.Corpus
                                ,exam.Attach
                                ,exam.Other 
                                ,exam.PressPain
                                ,exam.Liver
                                ,exam.Spleen
                                ,exam.Voiced
                                ,exam.SkinEx
                                ,exam.SclereEx
                                ,exam.LymphEx
                                ,exam.BreastEx
                                ,exam.AnusEx
                                ,exam.BreathSoundsEx
                                ,exam.RaleEx
                                ,exam.NoiseEx
                                ,exam.CervixUteriEx
                                ,exam.CorpusEx
                                ,exam.AttachEx
                                ,exam.VulvaEx
                                ,exam.VaginaEx
                                ,exam.PressPainEx
                                ,exam.LiverEx
                                ,exam.SpleenEx
                                ,exam.VoicedEx
                                ,exam.EnclosedMassEx
                                ,exam.EyeRound
                                ,exam.EyeRoundEx
                                ,exam.IDCardNo
                            FROM
                                ARCHIVE_PHYSICALEXAM exam
                            WHERE exam.OutKey = @outkey and    
                                exam.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    ssischeck.ID 
                                    ,ssischeck.HB
                                    ,ssischeck.WBC
                                    ,ssischeck.PLT
                                    ,ssischeck.PRO
                                    ,ssischeck.GLU
                                    ,ssischeck.KET
                                    ,ssischeck.BLD
                                    ,ssischeck.FPGL
                                    ,ssischeck.ECG
                                    ,ssischeck.ALBUMIN
                                    ,ssischeck.FOB
                                    ,ssischeck.HBALC
                                    ,ssischeck.HBSAG
                                    ,ssischeck.SGPT
                                    ,ssischeck.GOT
                                    ,ssischeck.BP
                                    ,ssischeck.TBIL
                                    ,ssischeck.CB
                                    ,ssischeck.SCR
                                    ,ssischeck.BUN
                                    ,ssischeck.PC
                                    ,ssischeck.HYPE
                                    ,ssischeck.TC
                                    ,ssischeck.TG
                                    ,ssischeck.LowCho
                                    ,ssischeck.HeiCho
                                    ,ssischeck.CHESTX
                                    ,ssischeck.BCHAO
                                    ,ssischeck.BloodOther
                                    ,ssischeck.UrineOther
                                    ,ssischeck.Other
                                    ,ssischeck.CERVIX
                                    ,ssischeck.GT
                                    ,ssischeck.ECGEx
                                    ,ssischeck.CHESTXEx
                                    ,ssischeck.BCHAOEx
                                    ,ssischeck.CERVIXEx
                                    ,ssischeck.FPGDL
                                    ,ssischeck.IDCardNo
                                    ,ssischeck.UA
                                    ,ssischeck.BloodType
                                    ,ssischeck.RH
                                    ,ssischeck.HCY
                                    ,ssischeck.BCHAOther
                                    ,ssischeck.BCHAOtherEx
                            FROM
                                    ARCHIVE_ASSISTCHECK ssischeck
                            WHERE   ssischeck.OutKey=@outkey and 
                                ssischeck.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    physdist.ID 
                                ,physdist.Mild
                                ,physdist.Faint
                                ,physdist.Yang
                                ,physdist.Yin
                                ,physdist.PhlegmDamp
                                ,physdist.Muggy
                                ,physdist.BloodStasis
                                ,physdist.QiConstraint
                                ,physdist.Characteristic
                                ,physdist.IDCardNo
                            FROM
                                ARCHIVE_MEDI_PHYS_DIST physdist
                            WHERE   physdist.OutKey=@outkey and 
                                physdist.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    qt.ID 
                                    ,qt.BrainDis
                                    ,qt.RenalDis
                                    ,qt.HeartDis
                                    ,qt.VesselDis
                                    ,qt.EyeDis
                                    ,qt.NerveDis
                                    ,qt.ElseDis
                                    ,qt.BrainOther
                                    ,qt.RenalOther
                                    ,qt.HeartOther
                                    ,qt.VesselOther
                                    ,qt.EyeOther
                                    ,qt.NerveOther
                                    ,qt.ElseOther
                                    ,qt.IDCardNo
                            FROM
                                        ARCHIVE_HEALTHQUESTION qt
                            WHERE   qt.OutKey =  @outkey and 
                                qt.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    hhistory.ID 
                                    ,hhistory.InHospitalDate
                                    ,hhistory.Reason
                                    ,hhistory.IllcaseNum
                                    ,hhistory.HospitalName 
                                    ,hhistory.OutHospitalDate 
                                    ,hhistory.IDCardNo
                            FROM
                                    ARCHIVE_HOSPITALHISTORY hhistory
                            WHERE   hhistory.OutKey = @outkey  and 
                                hhistory.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    fhistory.ID
                                    ,fhistory.HospitalName 
                                    ,fhistory.InHospitalDate 
                                    ,fhistory.IllcaseNums
                                    ,fhistory.Reasons
                                    ,fhistory.OutHospitalDate 
                                    ,fhistory.IDCardNo
                            FROM 
                                    ARCHIVE_FAMILYBEDHISTORY fhistory
                            WHERE   fhistory.OutKey = @outkey and 
                                fhistory.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    medication.ID
                                    ,medication.UseAge
                                    ,medication.UseNum
                                    ,medication.StartTime
                                    ,medication.EndTime
                                    ,medication.PillDependence
                                    ,medication.MedicinalName
                                    ,medication.IDCardNo
                            FROM 
                                    ARCHIVE_MEDICATION medication
                            WHERE   medication.OutKey = @outkey and 
                                medication.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    inchistory.ID 
                                    ,inchistory.PillName
                                    ,inchistory.InoculationDate
                                    ,inchistory.InoculationHistory
                                    ,inchistory.IDCardNo
                            FROM 
                                    ARCHIVE_INOCULATIONHISTORY inchistory 
                            WHERE  inchistory.OutKey=@outkey and 
                                inchistory.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    assguide.ID 
                                    ,assguide.IsNormal
                                    ,assguide.HealthGuide
                                    ,assguide.DangerControl
                                    ,assguide.Exception1
                                    ,assguide.Exception2
                                    ,assguide.Exception3
                                    ,assguide.Arm
                                    ,assguide.VaccineAdvice
                                    ,assguide.Other
                                    ,assguide.Exception4
                                    ,assguide.IDCardNo");
            //威海加了减腰围
            if (baseUrl.Contains("sdcsm_new"))
            {
                sbQuery.Append(" ,assguide.WaistlineArm");
            }

            sbQuery.Append(@"  FROM 
                                        ARCHIVE_ASSESSMENTGUIDE assguide
                            WHERE   assguide.OutKey = @outkey and  
                                assguide.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    oldinfo.ID as oldinfoID
                                    ,oldinfo.Energy
                                    ,oldinfo.Tired
                                    ,oldinfo.Breath
                                    ,oldinfo.Voice
                                    ,oldinfo.Emotion
                                    ,oldinfo.Spirit
                                    ,oldinfo.Alone
                                    ,oldinfo.Fear
                                    ,oldinfo.Weight 
                                    ,oldinfo.Eye
                                    ,oldinfo.FootHand
                                    ,oldinfo.Stomach
                                    ,oldinfo.Cold
                                    ,oldinfo.Influenza
                                    ,oldinfo.Nasal
                                    ,oldinfo.Snore
                                    ,oldinfo.Allergy
                                    ,oldinfo.Urticaria
                                    ,oldinfo.Skin 
                                    ,oldinfo.Scratch
                                    ,oldinfo.Mouth
                                    ,oldinfo.Arms
                                    ,oldinfo.Greasy
                                    ,oldinfo.Spot
                                    ,oldinfo.Eczema
                                    ,oldinfo.Thirsty
                                    ,oldinfo.Smell
                                    ,oldinfo.Abdomen
                                    ,oldinfo.Coolfood
                                    ,oldinfo.Defecate
                                    ,oldinfo.Defecatedry
                                    ,oldinfo.Tongue
                                    ,oldinfo.Vein
                                    ,oldinfo.IDCardNo
                                    ,redBaseinfo.CheckDate as RecordDate
                                    ,redBaseinfo.Doctor as FollowupDoctor
                                    ,question.OutKey
                            FROM 
                                    archive_medicine_cn oldinfo
                            LEFT JOIN ARCHIVE_MEDI_PHYS_DIST question
                                on oldinfo.ID=question.MedicineID
                            LEFT JOIN ARCHIVE_CUSTOMERBASEINFO redBaseinfo
                                on redBaseinfo.ID=question.OutKey
                            WHERE   question.OutKey = @outkey and 
                                oldinfo.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    oldresultinfo.ID as oldresultinfoID
                                    ,oldresultinfo.MedicineID
                                    ,oldresultinfo.MildScore
                                    ,oldresultinfo.FaintScore
                                    ,oldresultinfo.YangsCore
                                    ,oldresultinfo.YinScore
                                    ,oldresultinfo.PhlegmdampScore
                                    ,oldresultinfo.MuggyScore
                                    ,oldresultinfo.BloodStasisScore
                                    ,oldresultinfo.QiConstraintScore
                                    ,oldresultinfo.CharacteristicScore
                                    ,oldresultinfo.MildAdvising
                                    ,oldresultinfo.FaintAdvising
                                    ,oldresultinfo.YangAdvising
                                    ,oldresultinfo.YinAdvising
                                    ,oldresultinfo.PhlegmdampAdvising
                                    ,oldresultinfo.MuggyAdvising
                                    ,oldresultinfo.BloodStasisAdvising
                                    ,oldresultinfo.QiconstraintAdvising
                                    ,oldresultinfo.CharacteristicAdvising
                                    ,oldresultinfo.MildAdvisingEx
                                    ,oldresultinfo.FaintAdvisingEx
                                    ,oldresultinfo.YangadvisingEx
                                    ,oldresultinfo.YinAdvisingEx
                                    ,oldresultinfo.PhlegmdampAdvisingEx
                                    ,oldresultinfo.MuggyAdvisingEx
                                    ,oldresultinfo.BloodStasisAdvisingEx
                                    ,oldresultinfo.QiconstraintAdvisingEx
                                    ,oldresultinfo.CharacteristicAdvisingEx
                                    ,oldresultinfo.Mild
                                    ,oldresultinfo.Faint
                                    ,oldresultinfo.Yang
                                    ,oldresultinfo.Yin
                                    ,oldresultinfo.PhlegmDamp
                                    ,oldresultinfo.Muggy
                                    ,oldresultinfo.BloodStasis
                                    ,oldresultinfo.QiConstraint
                                    ,oldresultinfo.Characteristic
                                    ,oldresultinfo.IDCardNo
                                    ,redBaseinfo.CheckDate as RecordDate
                                    ,question.OutKey
                            FROM
                                    archive_medicine_result oldresultinfo
                            LEFT JOIN  ARCHIVE_MEDI_PHYS_DIST question
                                ON oldresultinfo.ID = question.MedicineResultID 
                            LEFT JOIN  ARCHIVE_CUSTOMERBASEINFO redBaseinfo
                                ON question.OutKey = redBaseinfo.ID 
                            WHERE  question.OutKey = @outkey and 
                                oldresultinfo.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            sbQuery.Append(@" SELECT
                                    selfInfo.ID as selfInfoID
                                    ,selfInfo.Dine
                                    ,selfInfo.Groming
                                    ,selfInfo.Dressing
                                    ,selfInfo.Tolet
                                    ,selfInfo.Activity
                                    ,selfInfo.TotalScore
                                    ,selfInfo.IDCardNo
                                    ,selfInfo.NextVisitAim
                                    ,selfInfo.NextfollowUpDate
                                    ,redBaseinfo.CheckDate as FollowUpDate
                                    ,redBaseinfo.Doctor as FollowUpDoctor
                            FROM
                                archive_selfcareability selfInfo
                            LEFT JOIN ARCHIVE_GENERALCONDITION rcondition
                                on rcondition.SelfID=selfInfo.ID
                            LEFT JOIN  ARCHIVE_CUSTOMERBASEINFO redBaseinfo
                                ON rcondition.OutKey = redBaseinfo.ID 
                            WHERE rcondition.OutKey=@outkey
                                and selfInfo.IDCardNo ").Append(sbQueryIDCardNo).Append(";");
            base.Parameter.Clear();
            base.Parameter.Add("@OutKey", outkey);
            //  base.Parameter.Add("@IDCardNo", ids);

            #endregion

            DataSet dsAll = base.SearchDataSet(sbQuery.ToString());

            #region 筛选
            DataSet ds = new DataSet();
            if (dsAll != null && dsAll.Tables.Count > 0)
            {
                foreach (var idCardNo in idsA)
                {
                    int pNTable = 0;


                    // 健康体检_客户体检基本信息
                    DataTable dtBaseInfo = dsAll.Tables[pNTable++];
                    DataView dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";
                    DataTable dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_CUSTOMERBASEINFO";

                    if (dtInfo.Rows.Count <= 0)
                    {
                        continue;
                    }

                    ds.Tables.Add(dtInfo);

                    //健康体检_一般状况
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_GENERALCONDITION";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_生活方式
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_LIFESTYLE";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_脏器功能
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_VISCERAFUNCTION";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_查体
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_PHYSICALEXAM";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_辅助检查
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_ASSISTCHECK";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_中医体质辨识
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_MEDI_PHYS_DIST";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_现存主要健康问题
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_HEALTHQUESTION";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_住院史表
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_HOSPITALHISTORY";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_家庭病床史表
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_FAMILYBEDHISTORY";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_主要用药情况表
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_MEDICATION";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_非免疫规划预防接种史表
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_INOCULATIONHISTORY";
                    ds.Tables.Add(dtInfo);

                    // 健康体检_健康评价与指导
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "ARCHIVE_ASSESSMENTGUIDE";
                    ds.Tables.Add(dtInfo);

                    //老年人体质辨识问题
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND oldinfoID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "OLD_MEDICINE_CN";
                    ds.Tables.Add(dtInfo);

                    // 老年人体质辨识结果
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND oldresultinfoID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "OLD_MEDICINE_RESULT";
                    ds.Tables.Add(dtInfo);

                    // 老年人体质辨识结果
                    dtBaseInfo = dsAll.Tables[pNTable++];
                    dv = dtBaseInfo.DefaultView;
                    dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND selfInfoID is not null";

                    dtInfo = dv.ToTable(true);
                    dtInfo.TableName = "OLDER_SELFCAREABILITY";
                    ds.Tables.Add(dtInfo);

                    //lstDataSet.Add(ds);
                }
            }

            #endregion

            return ds;
        }

        #endregion

        #region 老年人

        /// <summary>
        /// 老年人相关信息
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetLnrDataSet(string doctor, string ids)
        {
            #region SQL
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"SELECT 
                                    baseinfo.CustomerName
                                    ,baseinfo.IDCardNo
                                    ,oldinfo.ID as oldinfoID
                                    ,oldinfo.Energy
                                    ,oldinfo.Tired
                                    ,oldinfo.Breath
                                    ,oldinfo.Voice
                                    ,oldinfo.Emotion
                                    ,oldinfo.Spirit
                                    ,oldinfo.Alone
                                    ,oldinfo.Fear
                                    ,oldinfo.Weight
                                    ,oldinfo.Eye
                                    ,oldinfo.FootHand
                                    ,oldinfo.Stomach
                                    ,oldinfo.Cold
                                    ,oldinfo.Influenza
                                    ,oldinfo.Nasal
                                    ,oldinfo.Snore
                                    ,oldinfo.Allergy
                                    ,oldinfo.Urticaria
                                    ,oldinfo.Skin
                                    ,oldinfo.Scratch
                                    ,oldinfo.Mouth
                                    ,oldinfo.Arms
                                    ,oldinfo.Greasy
                                    ,oldinfo.Spot
                                    ,oldinfo.Eczema
                                    ,oldinfo.Thirsty
                                    ,oldinfo.Smell
                                    ,oldinfo.Abdomen
                                    ,oldinfo.Coolfood
                                    ,oldinfo.Defecate
                                    ,oldinfo.Defecatedry
                                    ,oldinfo.Tongue
                                    ,oldinfo.Vein
                                    ,oldinfo.CreatedBy
                                    ,oldinfo.CreatedDate
                                    ,oldinfo.LastUpdateBy
                                    ,oldinfo.LastUpdateDate
                                    ,oldinfo.FollowupDoctor
                                    ,oldinfo.RecordDate
                                    ,oldinfo.OutKey

                                    ,oldresultinfo.ID as oldresultinfoID
                                    ,oldresultinfo.MedicineID
                                    ,oldresultinfo.Mild
                                    ,oldresultinfo.Faint
                                    ,oldresultinfo.Yang
                                    ,oldresultinfo.Yin
                                    ,oldresultinfo.PhlegmDamp
                                    ,oldresultinfo.Muggy
                                    ,oldresultinfo.BloodStasis
                                    ,oldresultinfo.QIconStraint
                                    ,oldresultinfo.Characteristic 
                                    ,oldresultinfo.MildScore
                                    ,oldresultinfo.FaintScore
                                    ,oldresultinfo.YangsCore
                                    ,oldresultinfo.YinScore
                                    ,oldresultinfo.PhlegmdampScore
                                    ,oldresultinfo.MuggyScore
                                    ,oldresultinfo.BloodStasisScore
                                    ,oldresultinfo.QiConstraintScore
                                    ,oldresultinfo.CharacteristicScore
                                    ,oldresultinfo.MildAdvising
                                    ,oldresultinfo.FaintAdvising
                                    ,oldresultinfo.YangAdvising
                                    ,oldresultinfo.YinAdvising
                                    ,oldresultinfo.PhlegmdampAdvising
                                    ,oldresultinfo.MuggyAdvising
                                    ,oldresultinfo.BloodStasisAdvising
                                    ,oldresultinfo.QiconstraintAdvising
                                    ,oldresultinfo.CharacteristicAdvising
                                    ,oldresultinfo.MildAdvisingEx
                                    ,oldresultinfo.FaintAdvisingEx
                                    ,oldresultinfo.YangadvisingEx
                                    ,oldresultinfo.YinAdvisingEx
                                    ,oldresultinfo.PhlegmdampAdvisingEx
                                    ,oldresultinfo.MuggyAdvisingEx
                                    ,oldresultinfo.BloodStasisAdvisingEx
                                    ,oldresultinfo.QiconstraintAdvisingEx
                                    ,oldresultinfo.CharacteristicAdvisingEx
                                     ,oldresultinfo.OutKey

                                    ,selfInfo.ID as selfInfoID
                                    ,selfInfo.Dine
                                    ,selfInfo.Groming
                                    ,selfInfo.Dressing
                                    ,selfInfo.Tolet
                                    ,selfInfo.Activity
                                    ,selfInfo.TotalScore
                                    ,selfInfo.FollowUpDate as sFollowUpDate
                                    ,selfInfo.FollowUpDoctor as sFollowUpDoctor
                                    ,selfInfo.NextfollowUpDate as sNextfollowUpDate
                                    ,selfInfo.NextVisitAim as sNextVisitAim
                            FROM 
                                    ARCHIVE_BASEINFO baseinfo
                             LEFT JOIN
                                    OLDER_SELFCAREABILITY selfInfo
                                ON baseinfo.IDCardNo = selfInfo.IDCardNo  
                            LEFT JOIN
                                    OLD_MEDICINE_CN oldinfo
                                ON baseinfo.IDCardNo = oldinfo.IDCardNo and oldinfo.OutKey = selfInfo.id   
                            LEFT JOIN
                                    OLD_MEDICINE_RESULT oldresultinfo
                                ON baseinfo.IDCardNo = oldresultinfo.IDCardNo and oldresultinfo.OutKey =  selfInfo.id 
                              INNER JOIN  
                                 (
	                            SELECT MAX(FollowUpDate) AS FollowUpDate
					                            ,IDCardNo
	                            FROM OLDER_SELFCAREABILITY 
	                            WHERE 1=1                         

                          /*   LEFT JOIN
                                    ARCHIVE_MEDI_PHYS_DIST phy
                                ON baseinfo.IDCardNo = phy.IDCardNo */ ");


            if (!string.IsNullOrEmpty(ids))
            {
                ids = "'" + ids.Replace(",", "','") + "'";

                sbQuery.Append("  AND IDCardNo IN (").Append(ids).Append(")");
            }

            sbQuery.Append(@" GROUP BY IDCardNo
                                ) AS old
                                ON old.FollowUpDate=selfInfo.FollowUpDate AND old.IDCardNo=selfInfo.IDCardNo
                                /*AND find_in_set('4',baseinfo.PopulationType) */
                                WHERE 1=1 ");
            base.Parameter.Clear();
            if (!string.IsNullOrEmpty(doctor))
            {
                sbQuery.Append("  AND baseinfo.CreateMenName = @CreateMenName");

                base.Parameter.Add("@CreateMenName", doctor);
            }


            #endregion

            DataTable dtAll = base.Search(sbQuery.ToString());

            DataTable dtIDCardNo = new DataTable();
            DataView dvC = dtAll.DefaultView;
            dtIDCardNo = dvC.ToTable(true, "IDCardNo");

            List<DataSet> lstDataSet = new List<DataSet>();
            foreach (DataRow row in dtIDCardNo.Rows)
            {
                string idCardNo = row["IDCardNo"].ToString();

                DataSet ds = new DataSet();

                DataTable dtBaseInfo = new DataTable();


                DataView dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "'  AND oldinfoID is not null";
                List<string> lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("CustomerName");
                lstBaseColmn.Add("oldinfoID");
                lstBaseColmn.Add("Energy");
                lstBaseColmn.Add("Tired");
                lstBaseColmn.Add("Breath");
                lstBaseColmn.Add("Voice");
                lstBaseColmn.Add("Emotion");
                lstBaseColmn.Add("Spirit");
                lstBaseColmn.Add("Alone");
                lstBaseColmn.Add("Fear");
                lstBaseColmn.Add("Weight");
                lstBaseColmn.Add("Eye");
                lstBaseColmn.Add("FootHand");
                lstBaseColmn.Add("Stomach");
                lstBaseColmn.Add("Cold");
                lstBaseColmn.Add("Influenza");
                lstBaseColmn.Add("Nasal");
                lstBaseColmn.Add("Snore");
                lstBaseColmn.Add("Allergy");
                lstBaseColmn.Add("Urticaria");
                lstBaseColmn.Add("Skin");
                lstBaseColmn.Add("Scratch");
                lstBaseColmn.Add("Mouth");
                lstBaseColmn.Add("Arms");
                lstBaseColmn.Add("Greasy");
                lstBaseColmn.Add("Spot");
                lstBaseColmn.Add("Eczema");
                lstBaseColmn.Add("Thirsty");
                lstBaseColmn.Add("Smell");
                lstBaseColmn.Add("Abdomen");
                lstBaseColmn.Add("Coolfood");
                lstBaseColmn.Add("Defecate");
                lstBaseColmn.Add("Defecatedry");
                lstBaseColmn.Add("Tongue");
                lstBaseColmn.Add("Vein");
                lstBaseColmn.Add("FollowupDoctor");
                lstBaseColmn.Add("RecordDate");
                lstBaseColmn.Add("OutKey");
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "OLD_MEDICINE_CN";
                ds.Tables.Add(dtBaseInfo);

                dtBaseInfo = new DataTable();

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "'   AND oldresultinfoID is not null";
                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("oldresultinfoID");
                lstBaseColmn.Add("MedicineID");
                lstBaseColmn.Add("Mild");
                lstBaseColmn.Add("Faint");
                lstBaseColmn.Add("Yang");
                lstBaseColmn.Add("Yin");
                lstBaseColmn.Add("PhlegmDamp");
                lstBaseColmn.Add("Muggy");
                lstBaseColmn.Add("BloodStasis");
                lstBaseColmn.Add("QIconStraint");
                lstBaseColmn.Add("Characteristic");
                lstBaseColmn.Add("MildScore");
                lstBaseColmn.Add("FaintScore");
                lstBaseColmn.Add("YangsCore");
                lstBaseColmn.Add("YinScore");
                lstBaseColmn.Add("PhlegmdampScore");
                lstBaseColmn.Add("MuggyScore");
                lstBaseColmn.Add("BloodStasisScore");
                lstBaseColmn.Add("QiConstraintScore");
                lstBaseColmn.Add("CharacteristicScore");
                lstBaseColmn.Add("MildAdvising");
                lstBaseColmn.Add("FaintAdvising");
                lstBaseColmn.Add("YangAdvising");
                lstBaseColmn.Add("YinAdvising");
                lstBaseColmn.Add("PhlegmdampAdvising");
                lstBaseColmn.Add("MuggyAdvising");
                lstBaseColmn.Add("BloodStasisAdvising");
                lstBaseColmn.Add("QiconstraintAdvising");
                lstBaseColmn.Add("CharacteristicAdvising");
                lstBaseColmn.Add("MildAdvisingEx");
                lstBaseColmn.Add("FaintAdvisingEx");
                lstBaseColmn.Add("YangadvisingEx");
                lstBaseColmn.Add("YinAdvisingEx");
                lstBaseColmn.Add("PhlegmdampAdvisingEx");
                lstBaseColmn.Add("MuggyAdvisingEx");
                lstBaseColmn.Add("BloodStasisAdvisingEx");
                lstBaseColmn.Add("QiconstraintAdvisingEx");
                lstBaseColmn.Add("CharacteristicAdvisingEx");
                lstBaseColmn.Add("OutKey");

                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "OLD_MEDICINE_RESULT";
                ds.Tables.Add(dtBaseInfo);

                dtBaseInfo = new DataTable();

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "'  AND selfInfoID is not null";
                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("selfInfoID");
                lstBaseColmn.Add("Dine");
                lstBaseColmn.Add("Groming");
                lstBaseColmn.Add("Dressing");
                lstBaseColmn.Add("Tolet");
                lstBaseColmn.Add("Activity");
                lstBaseColmn.Add("TotalScore");
                lstBaseColmn.Add("sFollowUpDate");
                lstBaseColmn.Add("sFollowUpDoctor");
                lstBaseColmn.Add("sNextfollowUpDate");
                lstBaseColmn.Add("sNextVisitAim");

                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "OLDER_SELFCAREABILITY";
                dtBaseInfo.Columns["sFollowUpDate"].ColumnName = "FollowUpDate";
                dtBaseInfo.Columns["sFollowUpDoctor"].ColumnName = "FollowUpDoctor";
                dtBaseInfo.Columns["sNextfollowUpDate"].ColumnName = "NextfollowUpDate";
                dtBaseInfo.Columns["sNextVisitAim"].ColumnName = "NextVisitAim";

                ds.Tables.Add(dtBaseInfo);

                lstDataSet.Add(ds);
            }

            return lstDataSet;
        }

        #endregion

        #region 高血压

        /// <summary>
        /// 高血压相关信息
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetGxyDataSet(string doctor, string ids)
        {
            #region SQL
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"SELECT 
                                    baseinfo.CustomerName
                                    ,baseinfo.IDCardNo

                                    ,hybaseinfo.ID as hybaseinfoID
                                    ,hybaseinfo.ManagementGroup
                                    ,hybaseinfo.CaseOurce
                                    ,hybaseinfo.TerminateExcuse
                                    ,hybaseinfo.FatherHistory
                                    ,hybaseinfo.Symptom
                                    ,hybaseinfo.HypertensionComplication
                                    ,hybaseinfo.Hypotensor
                                    ,hybaseinfo.TerminateManagemen
                                    ,hybaseinfo.TerminateTime
                                    /*,hybaseinfo.OUtKey*/

                                    ,hyvisit.ID as hyvisitID
                                    ,hyvisit.FollowUpDate
                                    ,hyvisit.FollowUpDoctor
                                    ,hyvisit.NextFollowUpDate
                                    ,hyvisit.Symptom as hyvisitSymptom
                                    ,hyvisit.SympToMother
                                    ,hyvisit.Hypertension
                                    ,hyvisit.Hypotension
                                    ,hyvisit.Weight
                                    ,hyvisit.BMI
                                    ,hyvisit.Heartrate
                                    ,hyvisit.PhysicalSympToMother
                                    ,hyvisit.DailySmokeNum
                                    ,hyvisit.DailyDrinkNum
                                    ,hyvisit.SportTimePerWeek
                                    ,hyvisit.SportPerMinuteTime
                                    ,hyvisit.EatSaltType
                                    ,hyvisit.EatSaltTarget
                                    ,hyvisit.PsyChoadJustMent
                                    ,hyvisit.ObeyDoctorBehavior
                                    ,hyvisit.AssistantExam
                                    ,hyvisit.MedicationCompliance
                                    ,hyvisit.Adr
                                    ,hyvisit.AdrEx
                                    ,hyvisit.FollowUpType
                                    ,hyvisit.ReferralReason
                                    ,hyvisit.ReferralOrg
                                    ,hyvisit.FollowUpWay
                                    ,hyvisit.WeightTarGet
                                    ,hyvisit.BMITarGet
                                    ,hyvisit.DailySmokeNumTarget
                                    ,hyvisit.DailyDrinkNumTarget
                                    ,hyvisit.SportTimeSperWeekTarget
                                    ,hyvisit.SportPerMinutesTimeTarget
                                    ,hyvisit.DoctorView
                                 ,hyvisit.Hight
                                 ,hyvisit.IsReferral
                                 ,hyvisit.NextMeasures
                                 ,hyvisit.ReferralContacts
                                 ,hyvisit.ReferralResult
                                 ,hyvisit.Remarks
                                    ,me.ID as meID
                                    ,me.Type 
                                    ,me.Name
                                    ,me.DailyTime
                                    ,me.EveryTimeMg
                                    ,me.drugtype
                                    ,me.OUtKey
                                    ,me.DosAge
                            FROM 
                                    ARCHIVE_BASEINFO baseinfo
                            INNER JOIN
                                    CD_HYPERTENSIONFOLLOWUP hyvisit
                                ON baseinfo.IDCardNo = hyvisit.IDCardNo 
                            LEFT JOIN
                                    CD_HYPERTENSION_BASEINFO hybaseinfo
                                ON baseinfo.IDCardNo = hybaseinfo.IDCardNo  
                           /* LEFT JOIN
                                    ARCHIVE_GENERALCONDITION cond
                                  ON baseinfo.IDCardNo = cond.IDCardNo */
                            LEFT JOIN
                                   CD_DRUGCONDITION me
                                   ON baseinfo.IDCardNo = me.IDCardNo  and me.OutKey = hyvisit.id  
                                   AND (me.Type = '1' or me.Type = '7')
                            INNER JOIN 
                              (select max(FollowUpDate) as FollowUpDate,IDCardNo from  CD_HYPERTENSIONFOLLOWUP where 1=1 
                            ");
            if (!string.IsNullOrEmpty(ids))
            {
                ids = "'" + ids.Replace(",", "','") + "'";

                sbQuery.Append("  AND IDCardNo IN (").Append(ids).Append(")");
            }
            sbQuery.Append(@"   GROUP BY IDCardNo
                                ) AS chr
                                ON chr.FollowUpDate=hyvisit.FollowUpDate AND chr.IDCardNo=hyvisit.IDCardNo
							WHERE
                                find_in_set('6',baseinfo.PopulationType)");


            base.Parameter.Clear();
            if (!string.IsNullOrEmpty(doctor))
            {
                sbQuery.Append("  AND baseinfo.CreateMenName = @CreateMenName");
                base.Parameter.Add("CreateMenName", doctor);
            }



            #endregion

            DataTable dtAll = base.Search(sbQuery.ToString());

            DataTable dtIDCardNo = new DataTable();
            DataView dvC = dtAll.DefaultView;
            dtIDCardNo = dvC.ToTable(true, "IDCardNo");

            List<DataSet> lstDataSet = new List<DataSet>();
            foreach (DataRow row in dtIDCardNo.Rows)
            {
                string idCardNo = row["IDCardNo"].ToString();

                DataSet ds = new DataSet();

                DataTable dtBaseInfo = new DataTable();

                DataView dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND hybaseinfoID is not null";
                List<string> lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("hybaseinfoID");
                lstBaseColmn.Add("ManagementGroup");
                lstBaseColmn.Add("CaseOurce");
                lstBaseColmn.Add("TerminateExcuse");
                lstBaseColmn.Add("FatherHistory");
                lstBaseColmn.Add("Symptom");
                lstBaseColmn.Add("HypertensionComplication");
                lstBaseColmn.Add("Hypotensor");
                lstBaseColmn.Add("TerminateManagemen");
                lstBaseColmn.Add("TerminateTime");
                //lstBaseColmn.Add("OUtKey");
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_HYPERTENSION_BASEINFO";
                ds.Tables.Add(dtBaseInfo);

                dtBaseInfo = new DataTable();

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND hyvisitID is not null";
                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("CustomerName");
                lstBaseColmn.Add("hyvisitID");
                lstBaseColmn.Add("FollowUpDate");
                lstBaseColmn.Add("FollowUpDoctor");
                lstBaseColmn.Add("NextFollowUpDate");
                lstBaseColmn.Add("hyvisitSymptom");
                lstBaseColmn.Add("SympToMother");
                lstBaseColmn.Add("Hypertension");
                lstBaseColmn.Add("Hypotension");
                lstBaseColmn.Add("Weight");
                lstBaseColmn.Add("BMI");
                lstBaseColmn.Add("Heartrate");
                lstBaseColmn.Add("PhysicalSympToMother");
                lstBaseColmn.Add("DailySmokeNum");
                lstBaseColmn.Add("DailyDrinkNum");
                lstBaseColmn.Add("SportTimePerWeek");
                lstBaseColmn.Add("SportPerMinuteTime");
                lstBaseColmn.Add("EatSaltType");
                lstBaseColmn.Add("EatSaltTarget");
                lstBaseColmn.Add("PsyChoadJustMent");
                lstBaseColmn.Add("ObeyDoctorBehavior");
                lstBaseColmn.Add("AssistantExam");
                lstBaseColmn.Add("MedicationCompliance");
                lstBaseColmn.Add("Adr");
                lstBaseColmn.Add("AdrEx");
                lstBaseColmn.Add("FollowUpType");
                lstBaseColmn.Add("ReferralReason");
                lstBaseColmn.Add("ReferralOrg");
                lstBaseColmn.Add("FollowUpWay");
                lstBaseColmn.Add("WeightTarGet");
                lstBaseColmn.Add("BMITarGet");
                lstBaseColmn.Add("DailySmokeNumTarget");
                lstBaseColmn.Add("DailyDrinkNumTarget");
                lstBaseColmn.Add("SportTimeSperWeekTarget");
                lstBaseColmn.Add("SportPerMinutesTimeTarget");
                lstBaseColmn.Add("Hight");
                lstBaseColmn.Add("DoctorView");
                lstBaseColmn.Add("IsReferral");
                lstBaseColmn.Add("NextMeasures");
                lstBaseColmn.Add("ReferralContacts");
                lstBaseColmn.Add("ReferralResult");
                lstBaseColmn.Add("Remarks");
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_HYPERTENSIONFOLLOWUP";
                dtBaseInfo.Columns["hyvisitSymptom"].ColumnName = "Symptom";

                ds.Tables.Add(dtBaseInfo);

                dtBaseInfo = new DataTable();

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND meID is not null";
                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("meID");
                lstBaseColmn.Add("Type");
                lstBaseColmn.Add("Name");
                lstBaseColmn.Add("DailyTime");
                lstBaseColmn.Add("EveryTimeMg");
                lstBaseColmn.Add("drugtype");
                lstBaseColmn.Add("OUtKey");
                lstBaseColmn.Add("DosAge");
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_DRUGCONDITION";

                ds.Tables.Add(dtBaseInfo);

                lstDataSet.Add(ds);
            }

            return lstDataSet;
        }

        #endregion

        #region 糖尿病
        /// <summary>
        /// 糖尿病
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetTnbDataSet(string doctor, string ids)
        {
            #region Query
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.Append(@"SELECT 
			                    baseinfo.CustomerName
			                    ,baseinfo.IDCardNo
			                    , diabetes.ID as diabetesID
			                    , diabetes.ManagementGroup
			                    , diabetes.CaseSource
			                    , diabetes.FamilyHistory
			                    , diabetes.DiabetesType
			                    , diabetes.DiabetesTime
			                    , diabetes.DiabetesWork
			                    , diabetes.Insulin
			                    , diabetes.InsulinWeight
			                    , diabetes.EnalaprilMelete
			                    , diabetes.EndManage
			                    , diabetes.EndWhy
			                    , diabetes.EndTime
			                    , diabetes.HappnTime
			                    , diabetes.CreateUnit
			                    , diabetes.CurrentUnit
			                    , diabetes.CreateBy
			                    , diabetes.CreateDate
			                    , diabetes.LastUpdateBy
			                    , diabetes.LastUpdateDate
			                    , diabetes.IsDelete
			                    , diabetes.Symptom
			                    , diabetes.RenalLesionsTime
			                    , diabetes.NeuropathyTime
			                    , diabetes.HeartDiseaseTime
			                    , diabetes.RetinopathyTime
			                    , diabetes.FootLesionsTime
			                    , diabetes.CerebrovascularTime
			                    , diabetes.LesionsOther
			                    , diabetes.LesionsOtherTime
			                    , diabetes.Lesions
			
			                    , diabetesvisit.ID as diabetesvisitID 
			                    , diabetesvisit.VisitDate
			                    , diabetesvisit.VisitDoctor
			                    , diabetesvisit.NextVisitDate
			                    , diabetesvisit.Symptom  as visitSysptom
			                    , diabetesvisit.SymptomOther
			                    , diabetesvisit.Hypertension
			                    , diabetesvisit.Hypotension
			                    , diabetesvisit.Weight
			                    , diabetesvisit.BMI
			                    , diabetesvisit.DorsalisPedispulse
			                    , diabetesvisit.PhysicalSymptomMother
			                    , diabetesvisit.DailySmokeNum
			                    , diabetesvisit.DailyDrinkNum
			                    , diabetesvisit.SportTimePerWeek
			                    , diabetesvisit.SportPerMinuteTime
			                    , diabetesvisit.StapleFooddailyg
			                    , diabetesvisit.PsychoAdjustment
			                    , diabetesvisit.ObeyDoctorBehavior
			                    , diabetesvisit.FPG
			                    , diabetesvisit.HbAlc
			                    , diabetesvisit.ExamDate
			                    , diabetesvisit.AssistantExam
			                    , diabetesvisit.MedicationCompliance
			                    , diabetesvisit.Adr
			                    , diabetesvisit.AdrEx
			                    , diabetesvisit.HypoglyceMiarreAction
			                    , diabetesvisit.VisitType
			                    , diabetesvisit.InsulinType
			                    , diabetesvisit.InsulinUsage
			                    , diabetesvisit.VisitWay
			                    , diabetesvisit.ReferralReason
			                    , diabetesvisit.ReferralOrg
			                    , diabetesvisit.TargetWeight
			                    , diabetesvisit.BMITarget
			                    , diabetesvisit.DailySmokeNumTarget 
			                    , diabetesvisit.DailyDrinkNumTarget
			                    , diabetesvisit.SportTimePerWeekTarget
			                    , diabetesvisit.SportPerMinuteTimeTarget
			                    , diabetesvisit.StapleFooddailygTarget
                                ,diabetesvisit.LastUpdateDate
                                  ,diabetesvisit.DoctorView
                                  ,diabetesvisit.RBG 
                                  ,diabetesvisit.PBG 
                              ,diabetesvisit.Hight 
                              ,diabetesvisit.IsReferral 
                              ,diabetesvisit.DorsalisPedispulseType 
                              ,diabetesvisit.NextMeasures 
                              ,diabetesvisit.ReferralContacts 
                              ,diabetesvisit.ReferralResult 
                              ,diabetesvisit.Remarks 
                              ,diabetesvisit.InsulinAdjustType 
                              ,diabetesvisit.InsulinAdjustUsage 
			                     
                                , dition.ID as ditionID
                                , dition.Type
                                , dition.Name
                                , dition.DailyTime
                                , dition.EveryTimeMg
                                , dition.drugtype
                                , dition.OutKey
                                , dition.DosAge
			                    FROM ARCHIVE_BASEINFO baseinfo 
			                         LEFT JOIN CD_DIABETESFOLLOWUP diabetesvisit
			                    ON baseinfo.IDCardNo=diabetesvisit.IDCardNo
                              
			                    LEFT JOIN CD_DIABETES_BASEINFO diabetes
			                    ON baseinfo.IDCardNo=diabetes.IDCardNo  
			
			         /*
                                 LEFT JOIN
                                    ARCHIVE_GENERALCONDITION cond
                                  ON baseinfo.IDCardNo = cond.IDCardNo and cond.OutKey = diabetesvisit.id */


								LEFT JOIN CD_DRUGCONDITION AS dition
								ON baseinfo.IDCardNo=dition.IDCardNo and dition.OutKey = diabetesvisit.id 
                                    AND dition.Type in ('2','8')
			                       INNER JOIN 
                                        (select max(VisitDate) as VisitDate,IDCardNo from  CD_DIABETESFOLLOWUP where 1=1  
            ");

            sbQuery.Append(@"GROUP BY IDCardNo ) chr on chr.VisitDate =diabetesvisit.VisitDate and chr.IDCardNo= diabetesvisit.IDCardNo 
                     WHERE  
			                    find_in_set('7',baseinfo.PopulationType)
                ");

            if (!string.IsNullOrEmpty(ids))
            {
                ids = "'" + ids.Replace(",", "','") + "'";

                sbQuery.Append("  AND baseinfo.IDCardNo IN (").Append(ids).Append(")");
            }
            base.Parameter.Clear();
            if (!string.IsNullOrEmpty(doctor))
            {
                sbQuery.Append("  AND baseinfo.CreateMenName = @CreateMenName");

                base.Parameter.Add("CreateMenName", doctor);
            }


            #endregion

            DataTable dtAll = base.Search(sbQuery.ToString());

            DataTable dtIDcardNo = new DataTable();
            DataView dvC = dtAll.DefaultView;
            dtIDcardNo = dvC.ToTable(true, "IDCardNo");

            List<DataSet> lstDataSet = new List<DataSet>();
            foreach (DataRow row in dtIDcardNo.Rows)
            {
                string idCardNo = row["IDCardNo"].ToString();

                DataSet ds = new DataSet();

                DataTable dtBaseInfo = new DataTable();

                DataView dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND diabetesID is not null";
                List<string> lstBaseColmn = new List<string>();
                #region
                lstBaseColmn.Add("IDCardNo");
                //lstBaseColmn.Add("OutKey");
                lstBaseColmn.Add("CustomerName");
                lstBaseColmn.Add("diabetesID");
                lstBaseColmn.Add("ManagementGroup");
                lstBaseColmn.Add("CaseSource");
                lstBaseColmn.Add("FamilyHistory");
                lstBaseColmn.Add("DiabetesType");
                lstBaseColmn.Add("DiabetesTime");
                lstBaseColmn.Add("DiabetesWork");
                lstBaseColmn.Add("Insulin");
                lstBaseColmn.Add("InsulinWeight");
                lstBaseColmn.Add("EnalaprilMelete");
                lstBaseColmn.Add("EndManage");
                lstBaseColmn.Add("EndWhy");
                lstBaseColmn.Add("EndTime");
                lstBaseColmn.Add("HappnTime");
                lstBaseColmn.Add("CreateUnit");
                lstBaseColmn.Add("CurrentUnit");
                lstBaseColmn.Add("CreateBy");
                lstBaseColmn.Add("CreateDate");
                lstBaseColmn.Add("LastUpdateBy");
                lstBaseColmn.Add("LastUpdateDate");
                lstBaseColmn.Add("IsDelete");
                lstBaseColmn.Add("Symptom");
                lstBaseColmn.Add("RenalLesionsTime");
                lstBaseColmn.Add("NeuropathyTime");
                lstBaseColmn.Add("HeartDiseaseTime");
                lstBaseColmn.Add("RetinopathyTime");
                lstBaseColmn.Add("FootLesionsTime");
                lstBaseColmn.Add("CerebrovascularTime");
                lstBaseColmn.Add("LesionsOther");
                lstBaseColmn.Add("LesionsOtherTime");
                lstBaseColmn.Add("Lesions");
                #endregion
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_DIABETES_BASEINFO";
                ds.Tables.Add(dtBaseInfo);

                dtBaseInfo = new DataTable();

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND diabetesvisitID is not null";
                lstBaseColmn = new List<string>();
                #region
                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("CustomerName");
                lstBaseColmn.Add("diabetesvisitID");
                lstBaseColmn.Add("VisitDate");
                lstBaseColmn.Add("VisitDoctor");
                lstBaseColmn.Add("NextVisitDate");
                lstBaseColmn.Add("visitSysptom");
                lstBaseColmn.Add("SymptomOther");
                lstBaseColmn.Add("Hypertension");
                lstBaseColmn.Add("Hypotension");
                lstBaseColmn.Add("Weight");
                lstBaseColmn.Add("BMI");
                lstBaseColmn.Add("DorsalisPedispulse");
                lstBaseColmn.Add("PhysicalSymptomMother");
                lstBaseColmn.Add("DailySmokeNum");
                lstBaseColmn.Add("DailyDrinkNum");
                lstBaseColmn.Add("SportTimePerWeek");
                lstBaseColmn.Add("SportPerMinuteTime");
                lstBaseColmn.Add("StapleFooddailyg");
                lstBaseColmn.Add("PsychoAdjustment");
                lstBaseColmn.Add("ObeyDoctorBehavior");
                lstBaseColmn.Add("FPG");
                lstBaseColmn.Add("HbAlc");
                lstBaseColmn.Add("ExamDate");
                lstBaseColmn.Add("AssistantExam");
                lstBaseColmn.Add("MedicationCompliance");
                lstBaseColmn.Add("Adr");
                lstBaseColmn.Add("AdrEx");
                lstBaseColmn.Add("HypoglyceMiarreAction");
                lstBaseColmn.Add("VisitType");
                lstBaseColmn.Add("InsulinType");
                lstBaseColmn.Add("InsulinUsage");
                lstBaseColmn.Add("VisitWay");
                lstBaseColmn.Add("ReferralReason");
                lstBaseColmn.Add("ReferralOrg");
                lstBaseColmn.Add("TargetWeight");
                lstBaseColmn.Add("BMITarget");
                lstBaseColmn.Add("DailySmokeNumTarget");
                lstBaseColmn.Add("DailyDrinkNumTarget");
                lstBaseColmn.Add("SportTimePerWeekTarget");
                lstBaseColmn.Add("SportPerMinuteTimeTarget");
                lstBaseColmn.Add("StapleFooddailygTarget");
                lstBaseColmn.Add("LastUpdateDate");
                lstBaseColmn.Add("Hight");
                lstBaseColmn.Add("DoctorView");
                lstBaseColmn.Add("RBG");
                lstBaseColmn.Add("PBG");
                lstBaseColmn.Add("IsReferral");
                lstBaseColmn.Add("DorsalisPedispulseType");
                lstBaseColmn.Add("NextMeasures");
                lstBaseColmn.Add("ReferralContacts");
                lstBaseColmn.Add("ReferralResult");
                lstBaseColmn.Add("Remarks");
                lstBaseColmn.Add("InsulinAdjustType");
                lstBaseColmn.Add("InsulinAdjustUsage");
                #endregion
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_DIABETESFOLLOWUP";

                ds.Tables.Add(dtBaseInfo);

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ditionID is not null";
                lstBaseColmn = new List<string>();
                #region
                //lstBaseColmn.Add("ID");
                lstBaseColmn.Add("Type");
                lstBaseColmn.Add("OutKey");
                lstBaseColmn.Add("Name");
                lstBaseColmn.Add("DailyTime");
                lstBaseColmn.Add("EveryTimeMg");
                lstBaseColmn.Add("drugtype");
                lstBaseColmn.Add("DosAge");
                #endregion
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_DRUGCONDITION";
                ds.Tables.Add(dtBaseInfo);



                lstDataSet.Add(ds);
            }
            return lstDataSet;

        }

        #endregion

        #region 重症精神疾病
        /// <summary>
        /// 重症精神疾病
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetJsbDataSet(string doctor, string ids)
        {
            StringBuilder sbSql = new StringBuilder();
            #region SqlQuery
            sbSql.Append(@"SELECT
	                      baseinfo.CustomerName
	                     ,baseinfo.IDCardNo
		
		             , men.ID as menID
                     , men.GuardianRecordID
                     , men.GuardianName
                     , men.Ralation
                     , men.GuradianAddr
                     , men.GuradianPhone
                     , men.FirstTime
                     , men.AgreeManagement
                     , men.AgreeSignature
                     , men.AgreeTime
                     , men.Symptom
                     , men.SymptomOther
                     , men.OutPatien
                     , men.HospitalCount
                     , men.DiagnosisInfo
                     , men.DiagnosisHospital
                     , men.DiagnosisTime
                     , men.LastCure
                     , men.VillageContacts
                     , men.VillageTel
                     , men.LockInfo
                     , men.Economy
                     , men.SpecialistProposal
                     , men.FillformTime
                     , men.DoctorMark
                     , men.CreatedBy
                     , men.CreatedDate
                     , men.LastUpdateBy
                     , men.LastUpDateDate
                     , men.CreateUnit
                     , men.CurrentUnit 
                     , men.FirstTreatmenTTime
                     , men.MildTroubleFrequen
                     , men.CreateDistuFrequen
                     , men.CauseAccidFrequen
                     , men.AutolesionFrequen
                     , men.AttemptSuicFrequen
                     , men.AttemptSuicideNone

	                 , menVisit.ID as VID
                    , menVisit.FollowUpDate
                    , menVisit.Fatalness
                    , menVisit.PresentSymptom
                    , menVisit.PresentSymptoOther
                    , menVisit.Insight
                    , menVisit.SleepQuality
                    , menVisit.Diet
                    , menVisit.PersonalCare
                    , menVisit.Housework
                    , menVisit.ProductLaborWork
                    , menVisit.LearningAbility
                    , menVisit.SocialInterIntera
                    , menVisit.MildTroubleFrequen as VMildTroubleFrequen
                    , menVisit.CreateDistuFrequen as VCreateDistuFrequen
                    , menVisit.CauseAccidFrequen as VCauseAccidFrequen
                    , menVisit.AutolesionFrequen as VAutolesionFrequen
                    , menVisit.AttemptSuicFrequen as VAttemptSuicFrequen
                    , menVisit.AttemptSuicideNone as VAttemptSuicideNone
                    , menVisit.LockCondition
                    , menVisit.HospitalizatiStatus
                    , menVisit.LastLeaveHospTime
                    , menVisit.LaborExaminati
                    , menVisit.LaborExaminatiHave
                    , menVisit.MedicatioCompliance
                    , menVisit.AdnerDruReact
                    , menVisit.AdverDruReactHave
                    , menVisit.TreatmentEffect
                    , menVisit.WhetherReferral
                    , menVisit.ReferralReason
                    , menVisit.ReferralAgencDepar
                    , menVisit.RehabiliMeasu
                    , menVisit.RehabiliMeasuOther
                    , menVisit.FollowupClassificat
                    , menVisit.NextFollowUpDate
                    , menVisit.FollowupDoctor
                    , menVisit.CreatedBy
                    , menVisit.CreatedDate
                    , menVisit.LastUpdateBy
                    , menVisit.LastUpdateDate 

                     , dition.ID as ditionID
                    , dition.Type
                    , dition.Name
                    , dition.DailyTime
                    , dition.EveryTimeMg
                    , dition.DosAge


                    FROM ARCHIVE_BASEINFO baseinfo

                    LEFT JOIN CD_MENTALDISEASE_BASEINFO as men
                    ON baseinfo.IDCardNo=men.IDCardNo

                    LEFT JOIN CD_MENTALDISEASE_FOLLOWUP as menVisit
                    ON baseinfo.IDCardNo=menVisit.IDCardNo

                    LEFT JOIN CD_DRUGCONDITION dition
                    ON baseinfo.IDCardNo=dition.IDCardNo
                    AND dition.Type = '3'
                       WHERE  find_in_set('5',baseinfo.PopulationType)  
                ");
            base.Parameter.Clear();
            if (!string.IsNullOrEmpty(doctor))
            {
                sbSql.Append("  AND baseinfo.CreateMenName = @CreateMenName");

                base.Parameter.Add("CreateMenName", doctor);
            }
            if (!string.IsNullOrEmpty(ids))
            {
                ids = "'" + ids.Replace(",", "','") + "'";

                sbSql.Append("  AND baseinfo.IDCardNo IN (").Append(ids).Append(")");
            }

            #endregion


            DataTable dtAll = base.Search(sbSql.ToString());

            DataTable dtIDCardNo = new DataTable();
            DataView dvC = dtAll.DefaultView;
            dtIDCardNo = dvC.ToTable(true, "IDCardNo");

            List<DataSet> lstDataSet = new List<DataSet>();
            foreach (DataRow row in dtIDCardNo.Rows)
            {
                string idCardNo = row["IDCardNo"].ToString();

                DataSet ds = new DataSet();

                DataTable dtBaseInfo = new DataTable();

                DataView dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND menID is not null";
                List<string> lstBaseColmn = new List<string>();
                #region
                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("CustomerName");
                lstBaseColmn.Add("menID");
                lstBaseColmn.Add("GuardianRecordID");
                lstBaseColmn.Add("GuardianName");
                lstBaseColmn.Add("Ralation");
                lstBaseColmn.Add("GuradianAddr");
                lstBaseColmn.Add("GuradianPhone");
                lstBaseColmn.Add("FirstTime");
                lstBaseColmn.Add("AgreeManagement");
                lstBaseColmn.Add("AgreeSignature");
                lstBaseColmn.Add("AgreeTime");
                lstBaseColmn.Add("Symptom");
                lstBaseColmn.Add("SymptomOther");
                lstBaseColmn.Add("OutPatien");
                lstBaseColmn.Add("HospitalCount");
                lstBaseColmn.Add("DiagnosisInfo");
                lstBaseColmn.Add("DiagnosisHospital");
                lstBaseColmn.Add("DiagnosisTime");
                lstBaseColmn.Add("LastCure");
                lstBaseColmn.Add("VillageContacts");
                lstBaseColmn.Add("VillageTel");
                lstBaseColmn.Add("LockInfo");
                lstBaseColmn.Add("Economy");
                lstBaseColmn.Add("SpecialistProposal");
                lstBaseColmn.Add("FillformTime");
                lstBaseColmn.Add("DoctorMark");
                lstBaseColmn.Add("CreatedBy");
                lstBaseColmn.Add("CreatedDate");
                lstBaseColmn.Add("LastUpdateBy");
                lstBaseColmn.Add("LastUpDateDate");
                lstBaseColmn.Add("CreateUnit");
                lstBaseColmn.Add("CurrentUnit");
                lstBaseColmn.Add("FirstTreatmenTTime");
                lstBaseColmn.Add("MildTroubleFrequen");
                lstBaseColmn.Add("CreateDistuFrequen");
                lstBaseColmn.Add("CauseAccidFrequen");
                lstBaseColmn.Add("AutolesionFrequen");
                lstBaseColmn.Add("AttemptSuicFrequen");
                lstBaseColmn.Add("AttemptSuicideNone");

                #endregion
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_MENTALDISEASE_BASEINFO";
                ds.Tables.Add(dtBaseInfo);

                dtBaseInfo = new DataTable();

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND VID is not null";
                lstBaseColmn = new List<string>();
                #region
                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("FollowUpDate");
                lstBaseColmn.Add("Fatalness");
                lstBaseColmn.Add("PresentSymptom");
                lstBaseColmn.Add("PresentSymptoOther");
                lstBaseColmn.Add("Insight");
                lstBaseColmn.Add("SleepQuality");
                lstBaseColmn.Add("Diet");
                lstBaseColmn.Add("PersonalCare");
                lstBaseColmn.Add("Housework");
                lstBaseColmn.Add("ProductLaborWork");
                lstBaseColmn.Add("LearningAbility");
                lstBaseColmn.Add("SocialInterIntera");
                lstBaseColmn.Add("VMildTroubleFrequen");
                lstBaseColmn.Add("VCreateDistuFrequen");
                lstBaseColmn.Add("VCauseAccidFrequen");
                lstBaseColmn.Add("VAutolesionFrequen");
                lstBaseColmn.Add("VAttemptSuicFrequen");
                lstBaseColmn.Add("VAttemptSuicideNone");
                lstBaseColmn.Add("LockCondition");
                lstBaseColmn.Add("HospitalizatiStatus");
                lstBaseColmn.Add("LastLeaveHospTime");
                lstBaseColmn.Add("LaborExaminati");
                lstBaseColmn.Add("LaborExaminatiHave");
                lstBaseColmn.Add("MedicatioCompliance");
                lstBaseColmn.Add("AdnerDruReact");
                lstBaseColmn.Add("AdverDruReactHave");
                lstBaseColmn.Add("TreatmentEffect");
                lstBaseColmn.Add("WhetherReferral");
                lstBaseColmn.Add("ReferralReason");
                lstBaseColmn.Add("ReferralAgencDepar");
                lstBaseColmn.Add("RehabiliMeasu");
                lstBaseColmn.Add("RehabiliMeasuOther");
                lstBaseColmn.Add("FollowupClassificat");
                lstBaseColmn.Add("NextFollowUpDate");
                lstBaseColmn.Add("FollowupDoctor");
                lstBaseColmn.Add("CreatedBy");
                lstBaseColmn.Add("CreatedDate");
                lstBaseColmn.Add("LastUpdateBy");
                lstBaseColmn.Add("LastUpdateDate");
                #endregion
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_MENTALDISEASE_FOLLOWUP";
                //更改列名
                dtBaseInfo.Columns["VMildTroubleFrequen"].ColumnName = "MildTroubleFrequen";
                dtBaseInfo.Columns["VCreateDistuFrequen"].ColumnName = "CreateDistuFrequen";
                dtBaseInfo.Columns["VCauseAccidFrequen"].ColumnName = "CauseAccidFrequen";
                dtBaseInfo.Columns["VAutolesionFrequen"].ColumnName = "AutolesionFrequen";
                dtBaseInfo.Columns["VAttemptSuicFrequen"].ColumnName = "AttemptSuicFrequen";
                dtBaseInfo.Columns["VAttemptSuicideNone"].ColumnName = "AttemptSuicideNone";

                ds.Tables.Add(dtBaseInfo);


                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND ditionID is not null";
                lstBaseColmn = new List<string>();
                #region
                //lstBaseColmn.Add("ID");
                lstBaseColmn.Add("Type");
                lstBaseColmn.Add("Name");
                lstBaseColmn.Add("DailyTime");
                lstBaseColmn.Add("EveryTimeMg");
                lstBaseColmn.Add("DosAge");
                #endregion
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_DRUGCONDITION";
                ds.Tables.Add(dtBaseInfo);



                lstDataSet.Add(ds);
            }
            return lstDataSet;

        }

        #endregion


        #region 脑卒中

        /// <summary>
        /// 脑卒中相关信息
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetNczDataSet(string doctor, string ids)
        {
            #region SQL
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@" SELECT 
                                    baseinfo.IDCardNo
                                    ,visit.ID as visitID
                                    ,visit.FollowupDate
                                    ,visit.FollowupDoctor
                                    ,visit.NextFollowupDate
                                    ,visit.Symptom
                                    ,visit.SymptomOther
                                    ,visit.Hypertension
                                    ,visit.Hypotension
                                    ,visit.Weight
                                    ,visit.SignOther
                                    ,visit.SmokeDrinkAttention
                                    ,visit.SportAttention
                                    ,visit.EatSaltAttention
                                    ,visit.PsychicAdjust
                                    ,visit.ObeyDoctorBehavio
                                    ,visit.AssistantExam
                                    ,visit.MedicationCompliance
                                    ,visit.Adr
                                    ,visit.AdrEx
                                    ,visit.FollowupType
                                    ,visit.ReferralReason
                                    ,visit.ReferralOrg
                                    ,visit.FollowupWay
                                    ,visit.EatingDrug
                                     ,visit.DoctorView
                                    ,visit.Height
                                      /*2.0新增*/
                                     ,visit.FollowupTypeOther
                                     ,visit.StrokeType
                                     ,visit.Strokelocation
                                     ,visit.MedicalHistory
                                     ,visit.Syndrome
                                     ,visit.SyndromeOther
                                     ,visit.NewSymptom
                                     ,visit.NewSymptomOther
                                     ,visit.SmokeDay
                                     ,visit.DrinkDay
                                     ,visit.SportWeek
                                     ,visit.SportMinute
                                     ,visit.FPGL
                                     ,visit.Height
                                     ,visit.BMI
                                     ,visit.Waistline
                                     ,visit.LifeSelfCare
                                     ,visit.LimbRecover
                                     ,visit.RecoveryCure
                                     ,visit.RecoveryCureOther
                                     ,visit.IsReferral
                                     /*2.0新增*/
                                    ,me.ID as meID
                                    ,me.Type 
                                    ,me.OutKey 
                                    ,me.Name
                                    ,me.DailyTime
                                    ,me.EveryTimeMg
                                    ,me.drugtype
                                    ,me.DosAge
                            FROM 
                                    ARCHIVE_BASEINFO baseinfo
                            INNER JOIN
                                    CD_STROKE_FOLLOWUP visit
                                ON baseinfo.IDCardNo = visit.IDCardNo
                        /*    LEFT JOIN
                                    ARCHIVE_GENERALCONDITION cond
                                  ON baseinfo.IDCardNo = cond.IDCardNo  */
                            LEFT JOIN
                                   CD_DRUGCONDITION me
                                   ON baseinfo.IDCardNo = me.IDCardNo and me.OutKey = visit.id
                                   AND me.Type = '5'
                            INNER JOIN (
                                    select max(FollowupDate) as FollowupDate,IDCardNo from  CD_STROKE_FOLLOWUP
                                where 1=1  
                         ");
            if (!string.IsNullOrEmpty(ids))
            {
                ids = "'" + ids.Replace(",", "','") + "'";

                sbQuery.Append("  AND IDCardNo IN (").Append(ids).Append(")");
            }
            sbQuery.Append(@"  GROUP BY IDCardNo
		                    ) chr
                                ON chr.FollowupDate=visit.FollowupDate AND chr.IDCardNo=visit.IDCardNo	
		                    WHERE  
			                    find_in_set('9',baseinfo.PopulationType)");

            base.Parameter.Clear();

            if (!string.IsNullOrEmpty(doctor))
            {
                sbQuery.Append("  AND baseinfo.CreateMenName = @CreateMenName");

                base.Parameter.Add("CreateMenName", doctor);
            }

            #endregion

            DataTable dtAll = base.Search(sbQuery.ToString());

            DataTable dtIDCardNo = new DataTable();
            DataView dvC = dtAll.DefaultView;
            dtIDCardNo = dvC.ToTable(true, "IDCardNo");

            List<DataSet> lstDataSet = new List<DataSet>();
            foreach (DataRow row in dtIDCardNo.Rows)
            {
                string idCardNo = row["IDCardNo"].ToString();

                DataSet ds = new DataSet();

                DataTable dtBaseInfo = new DataTable();

                DataView dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND visitID is not null";
                List<string> lstBaseColmn = new List<string>();

                lstBaseColmn.Add("visitID");
                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("FollowupDate");
                lstBaseColmn.Add("FollowupDoctor");
                lstBaseColmn.Add("NextFollowupDate");
                lstBaseColmn.Add("Symptom");
                lstBaseColmn.Add("SymptomOther");
                lstBaseColmn.Add("Hypertension");
                lstBaseColmn.Add("Hypotension");
                lstBaseColmn.Add("Weight");
                lstBaseColmn.Add("SignOther");
                lstBaseColmn.Add("SmokeDrinkAttention");
                lstBaseColmn.Add("SportAttention");
                lstBaseColmn.Add("EatSaltAttention");
                lstBaseColmn.Add("PsychicAdjust");
                lstBaseColmn.Add("ObeyDoctorBehavio");
                lstBaseColmn.Add("AssistantExam");
                lstBaseColmn.Add("MedicationCompliance");
                lstBaseColmn.Add("Adr");
                lstBaseColmn.Add("AdrEx");
                lstBaseColmn.Add("FollowupType");
                lstBaseColmn.Add("ReferralReason");
                lstBaseColmn.Add("ReferralOrg");
                lstBaseColmn.Add("FollowupWay");
                lstBaseColmn.Add("EatingDrug");
                lstBaseColmn.Add("Height");
                lstBaseColmn.Add("DoctorView");

                #region
                lstBaseColmn.Add("FollowupTypeOther");
                lstBaseColmn.Add("StrokeType");
                lstBaseColmn.Add("Strokelocation");
                lstBaseColmn.Add("MedicalHistory");
                lstBaseColmn.Add("Syndrome");
                lstBaseColmn.Add("SyndromeOther");
                lstBaseColmn.Add("NewSymptom");
                lstBaseColmn.Add("NewSymptomOther");
                lstBaseColmn.Add("SmokeDay");
                lstBaseColmn.Add("DrinkDay");
                lstBaseColmn.Add("SportWeek");
                lstBaseColmn.Add("SportMinute");
                lstBaseColmn.Add("FPGL");
                lstBaseColmn.Add("BMI");
                lstBaseColmn.Add("Waistline");
                lstBaseColmn.Add("LifeSelfCare");
                lstBaseColmn.Add("LimbRecover");
                lstBaseColmn.Add("RecoveryCure");
                lstBaseColmn.Add("RecoveryCureOther");
                lstBaseColmn.Add("IsReferral");

                #endregion

                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_STROKE_FOLLOWUP";
                ds.Tables.Add(dtBaseInfo);


                dtBaseInfo = new DataTable();

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND meID is not null";
                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("meID");
                lstBaseColmn.Add("OutKey");
                lstBaseColmn.Add("Name");
                lstBaseColmn.Add("DailyTime");
                lstBaseColmn.Add("EveryTimeMg");
                lstBaseColmn.Add("drugtype");
                lstBaseColmn.Add("DosAge");
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_DRUGCONDITION";

                ds.Tables.Add(dtBaseInfo);

                lstDataSet.Add(ds);
            }

            return lstDataSet;
        }

        #endregion

        #region 冠心病

        /// <summary>
        /// 冠心病相关信息
        /// </summary>
        /// <param name="doctor"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<DataSet> GetGxbDataSet(string doctor, string ids)
        {
            #region SQL
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"  SELECT 
                                    baseinfo.IDCardNo
                                    ,visit.ID as visitID
                                     ,visit.Symptom
                                     ,visit.SymptomEx
                                     ,visit.Systolic
                                     ,visit.Diastolic
                                     ,visit.Weight
                                     ,visit.HearVoice
                                     ,visit.HeatRate
                                     ,visit.Apex
                                     ,visit.Smoking
                                     ,visit.Sports
                                     ,visit.Salt
                                     ,visit.Action
                                     ,visit.AssistCheck
                                     ,visit.AfterPill
                                     ,visit.Compliance
                                     ,visit.Untoward
                                     ,visit.UntowardEx
                                     ,visit.FollowType
                                     ,visit.ReferralReason
                                     ,visit.ReferralDepart
                                     ,visit.NextVisitDate
                                     ,visit.VisitDoctor
                                     ,visit.VisitDate
                                     ,visit.VisitType
                                     ,visit.DoctorView
                                     ,visit.Height
                                        /*2.0新增*/
						             ,visit.ChdType
						             ,visit.BMI
						             ,visit.FPGL
						             ,visit.TC
						             ,visit.TG
						             ,visit.LowCho
						             ,visit.HeiCho
						             ,visit.EcgCheckResult
						             ,visit.EcgExerciseResult
						             ,visit.CAG
						             ,visit.EnzymesResult
						             ,visit.HeartCheckResult
						             ,visit.SmokeDay
						             ,visit.DrinkDay
						             ,visit.SportWeek
						             ,visit.SportMinute
						             ,visit.SpecialTreated
						             ,visit.NondrugTreat
						             ,visit.Syndromeother
						             ,visit.IsReferral
                                        /*2.0新增*/
                                    ,me.ID as meID
                                    ,me.Type 
                                    ,me.OutKey 
                                    ,me.Name
                                    ,me.DailyTime
                                    ,me.EveryTimeMg
                                    ,me.drugtype
                                    ,me.DosAge
                            FROM 
                                    ARCHIVE_BASEINFO baseinfo
                            INNER JOIN
                                    CD_CHD_FOLLOWUP visit
                                ON baseinfo.IDCardNo = visit.IDCardNo
                           /* LEFT JOIN
                                    ARCHIVE_GENERALCONDITION cond
                                  ON baseinfo.IDCardNo = cond.IDCardNo */
                            LEFT JOIN
                                   CD_DRUGCONDITION me
                                   ON baseinfo.IDCardNo = me.IDCardNo  and me.OutKey = visit.id 
                                   AND me.Type = '4'
                            INNER JOIN (
                            select max(VisitDate) as VisitDate,IDCardNo from CD_CHD_FOLLOWUP where 1=1 
                           ");

            if (!string.IsNullOrEmpty(ids))
            {
                ids = "'" + ids.Replace(",", "','") + "'";

                sbQuery.Append("  AND IDCardNo IN (").Append(ids).Append(")");
            }
            sbQuery.Append(@" GROUP BY IDCardNo) as chr  on chr.VisitDate=visit.VisitDate and  chr.IDCardNO= visit.IDCardNO  WHERE
                                find_in_set('8',baseinfo.PopulationType) ");


            base.Parameter.Clear();

            if (!string.IsNullOrEmpty(doctor))
            {
                sbQuery.Append("  AND baseinfo.CreateMenName = @CreateMenName");

                base.Parameter.Add("CreateMenName", doctor);
            }



            #endregion

            DataTable dtAll = base.Search(sbQuery.ToString());

            DataTable dtIDCardNo = new DataTable();
            DataView dvC = dtAll.DefaultView;
            dtIDCardNo = dvC.ToTable(true, "IDCardNo");

            List<DataSet> lstDataSet = new List<DataSet>();
            foreach (DataRow row in dtIDCardNo.Rows)
            {
                string idCardNo = row["IDCardNo"].ToString();

                DataSet ds = new DataSet();

                DataTable dtBaseInfo = new DataTable();

                DataView dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND visitID is not null";
                List<string> lstBaseColmn = new List<string>();

                lstBaseColmn.Add("visitID");
                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("Symptom");
                lstBaseColmn.Add("SymptomEx");
                lstBaseColmn.Add("Systolic");
                lstBaseColmn.Add("Diastolic");
                lstBaseColmn.Add("Weight");
                lstBaseColmn.Add("HearVoice");
                lstBaseColmn.Add("HeatRate");
                lstBaseColmn.Add("Apex");
                lstBaseColmn.Add("Smoking");
                lstBaseColmn.Add("Sports");
                lstBaseColmn.Add("Salt");
                lstBaseColmn.Add("Action");
                lstBaseColmn.Add("AssistCheck");
                lstBaseColmn.Add("AfterPill");
                lstBaseColmn.Add("Compliance");
                lstBaseColmn.Add("Untoward");
                lstBaseColmn.Add("UntowardEx");
                lstBaseColmn.Add("FollowType");
                lstBaseColmn.Add("ReferralReason");
                lstBaseColmn.Add("ReferralDepart");
                lstBaseColmn.Add("NextVisitDate");
                lstBaseColmn.Add("VisitDoctor");
                lstBaseColmn.Add("VisitDate");
                lstBaseColmn.Add("VisitType");
                lstBaseColmn.Add("Height");
                lstBaseColmn.Add("DoctorView");
                #region 2.0 新增
                lstBaseColmn.Add("ChdType");
                lstBaseColmn.Add("BMI");
                lstBaseColmn.Add("FPGL");
                lstBaseColmn.Add("TC");
                lstBaseColmn.Add("TG");
                lstBaseColmn.Add("LowCho");
                lstBaseColmn.Add("HeiCho");
                lstBaseColmn.Add("EcgCheckResult");
                lstBaseColmn.Add("EcgExerciseResult");
                lstBaseColmn.Add("CAG");
                lstBaseColmn.Add("EnzymesResult");
                lstBaseColmn.Add("HeartCheckResult");
                lstBaseColmn.Add("SmokeDay");
                lstBaseColmn.Add("DrinkDay");
                lstBaseColmn.Add("SportWeek");
                lstBaseColmn.Add("SportMinute");
                lstBaseColmn.Add("SpecialTreated");
                lstBaseColmn.Add("NondrugTreat");
                lstBaseColmn.Add("Syndromeother");
                lstBaseColmn.Add("IsReferral");

                #endregion

                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_CHD_FOLLOWUP";
                ds.Tables.Add(dtBaseInfo);

                dtBaseInfo = new DataTable();

                dv = dtAll.DefaultView;
                dv.RowFilter = "IDCardNo = '" + idCardNo + "' AND meID is not null";
                lstBaseColmn = new List<string>();

                lstBaseColmn.Add("IDCardNo");
                lstBaseColmn.Add("meID");
                lstBaseColmn.Add("OutKey");
                lstBaseColmn.Add("Name");
                lstBaseColmn.Add("DailyTime");
                lstBaseColmn.Add("EveryTimeMg");
                lstBaseColmn.Add("drugtype");
                lstBaseColmn.Add("DosAge");
                dtBaseInfo = dv.ToTable(true, lstBaseColmn.ToArray());
                dtBaseInfo.TableName = "CD_DRUGCONDITION";

                ds.Tables.Add(dtBaseInfo);

                lstDataSet.Add(ds);
            }

            return lstDataSet;
        }

        #endregion

        #region
        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="strIDCardNo"></param>
        /// / <param name="saveFlag">用药注记</param>
        public void SaveDataSet(DataSet ds, string strIDCardNo, string saveFlag = "", string pk = "", string yctime = "", bool onlydah = false)
        {
            if (ds == null || ds.Tables.Count == 0)
            {
                return;
            }

            foreach (DataTable dt in ds.Tables)
            {
                // 多笔资料，先删除，后新增
                switch (dt.TableName.ToLower())
                {
                    // 个人基本信息_既往史
                    case "ARCHIVE_ILLNESSHISTORYINFO":

                    // 健康体检_住院史表
                    case "ARCHIVE_HOSPITALHISTORY":

                    // 健康体检_家庭病床史表
                    case "ARCHIVE_FAMILYBEDHISTORY":

                    // 健康体检_非免疫规划预防接种史表
                    case "ARCHIVE_INOCULATIONHISTORY":

                    // 健康体检_主要用药情况表
                    case "ARCHIVE_MEDICATION":

                    // 用药情况
                    case "CD_DRUGCONDITION":

                        string cType = "";

                        if (dt.TableName.ToLower() == "CD_DRUGCONDITION")
                        {
                            if (saveFlag.Contains(","))
                            {
                                string str = "";
                                var lst = saveFlag.Split(',');
                                foreach (var item in lst)
                                {
                                    str += "'" + item + "',";
                                }
                                cType = "AND Type in (" + str.TrimEnd(',') + ")";
                            }
                            else
                            {
                                cType = "AND Type = '" + saveFlag + "'";
                            }
                        }
                        //孕产妇
                        if (dt.TableName.ToLower() == "GRAVIDA_TWO2FIVE_FOLLOWUP")
                        {
                            cType += "and Times='" + yctime + "'";
                        }

                        // 体检和随访的用药、住院史、家庭病床史、预防接种史需要根据outkey删除
                        if (dt.TableName.ToLower() != "ARCHIVE_ILLNESSHISTORYINFO")
                        {
                            cType += " AND OutKey=" + pk;
                        }
                        DeleteQuerySQL(dt.TableName, strIDCardNo, cType);

                        if (dt.Rows.Count == 0)
                        {
                            continue;
                        }

                        AddDataByMulti(dt);

                        continue;
                    // 新生儿健康访视
                    case "CHILD_WITHIN_ONE_YEAR_OLD":
                    case "CHILD_ONE2THREE_YEAR_OLD":
                    case "CHILD_THREE2SIX_YEAR_OLD":

                        if (dt.Rows.Count == 0)
                        {
                            continue;
                        }

                        string flag = dt.Rows[0]["Flag"].ToString();

                        string otherWhere = " AND Flag = '" + flag + "'";

                        // 修改条数为0 则新增
                        int reIntF = EditDataBySingle(dt, otherWhere);
                        if (reIntF <= 0)
                        {
                            AddDataBySingle(dt);
                        }

                        //Flag
                        continue;

                    // 新生儿健康管理服务
                    case "CHILD_TCMHM_ONE":
                    case "CHILD_TCMHM_ONE2THREE":
                    case "CHILD_TCMHM_THREE2SIX":

                        if (dt.Rows.Count == 0)
                        {
                            continue;
                        }

                        string followupType = dt.Rows[0]["FollowupType"].ToString();

                        string otherWhereG = " AND FollowupType = '" + followupType + "'";

                        // 修改条数为0 则新增
                        int reIntG = EditDataBySingle(dt, otherWhereG);
                        if (reIntG <= 0)
                        {
                            AddDataBySingle(dt);
                        }

                        //Flag
                        continue;
                }

                if (dt.Rows.Count == 0)
                {
                    continue;
                }

                // 修改条数为0 则新增
                int reInt = EditDataBySingle(dt);
                if (onlydah == false)
                {
                    if (reInt <= 0)
                    {
                        AddDataBySingle(dt);
                    }
                }
            }
        }

        /// <summary>
        /// 保存体检、随访主表，并返回自增主键
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="strIDCardNo"></param>
        /// <param name="cDate"></param>
        /// <returns></returns>
        public int SaveMainTable(DataTable dt, string strIDCardNo, string cDate)
        {
            int id = QueryPK(dt, strIDCardNo, cDate);
            if (id > 0)
            {
                UpdateMainTable(dt, id);
            }
            else
            {
                id = AddMainTable(dt);
            }
            return id;
        }

        /// <summary>
        /// 根据身份证号体检日期（随访日期）查询对应ID编号
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="strIDCardNo"></param>
        /// <param name="cDate"></param>
        /// <returns></returns>
        private int QueryPK(DataTable dt, string strIDCardNo, string cDate)
        {
            string tblName = dt.TableName;
            StringBuilder strSql = new StringBuilder();
            try
            {
                base.Parameter.Clear();

                strSql.Append("select ID from ");
                strSql.Append(tblName);
                strSql.Append(" where 1=1 ");
                strSql.Append(" and IDCardNo=@IDCardNo ");
                base.Parameter.Add("@IDCardNo", strIDCardNo);

                switch (tblName)
                {
                    case "ARCHIVE_CUSTOMERBASEINFO": // 体检基本信息表
                        strSql.Append(" and CheckDate=@CheckDate ");
                        base.Parameter.Add("@CheckDate", cDate);
                        break;
                    case "CD_DIABETESFOLLOWUP": //糖尿病随访记录表
                        strSql.Append(" and VisitDate=@VisitDate");
                        base.Parameter.Add("@VisitDate", cDate);
                        break;
                    case "CD_HYPERTENSIONFOLLOWUP"://高血压随访记录表 
                    case "OLDER_SELFCAREABILITY":
                        strSql.Append(" and FollowUpDate=@FollowUpDate");
                        base.Parameter.Add("@FollowUpDate", cDate);
                        break;
                    default:
                        break;
                }

                DataTable dtResult = base.Search(strSql.ToString());

                if (dtResult.Rows.Count > 0)
                {
                    return int.Parse(dtResult.Rows[0][0].ToString());
                }
            }
            catch (Exception ex)
            {
                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);
            }
            return 0;
        }

        /// <summary>
        /// 新增体检记录、随访记录主表，并返回自增主键
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private int AddMainTable(DataTable dt)
        {
            string sbInsert = SetInsertSQL(dt);
            sbInsert += " ;select @@IDENTITY;";

            try
            {
                DataRow row = dt.Rows[0];
                base.Parameter.Clear();

                foreach (DataColumn colmn in dt.Columns)
                {
                    base.Parameter.Add(colmn.ColumnName, row[colmn.ColumnName].ToString() == "" ? null : row[colmn.ColumnName]);
                }

                DataTable dtResult = base.Search(sbInsert);

                if (dtResult.Rows.Count > 0)
                {
                    return int.Parse(dtResult.Rows[0][0].ToString());
                }
            }
            catch (Exception ex)
            {

                // CommonExtensions.WriteLogForTable(dt.Rows[0]["IDCardNo"] + "||" + msg + "||" + ex.Message);
                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);
            }
            return 0;

        }

        /// <summary>
        /// 根据查询pk修改体检基本信息、随访主表
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        private int UpdateMainTable(DataTable dt, int pk)
        {
            StringBuilder sbUpdate = new StringBuilder("");

            try
            {
                sbUpdate.Append("UPDATE ").Append(dt.TableName).Append(" SET ");

                string colmns = "";

                foreach (DataColumn colmn in dt.Columns)
                {
                    colmns += "," + colmn.ColumnName + " = @" + colmn.ColumnName;
                }

                sbUpdate.Append(colmns.TrimStart(','));
                sbUpdate.Append(" WHERE ID = @ID");

                //添加参数
                DataRow row = dt.Rows[0];

                base.Parameter.Clear();

                foreach (DataColumn colmn in dt.Columns)
                {
                    base.Parameter.Add(colmn.ColumnName, row[colmn.ColumnName].ToString() == "" ? null : row[colmn.ColumnName]);
                }
                base.Parameter.Add("@ID", pk);

                return base.ExeNonQuery(sbUpdate.ToString());
            }
            catch (Exception ex)
            {
                string msg = "";
                switch (dt.TableName)
                {
                    case "ARCHIVE_CUSTOMERBASEINFO":
                        msg = "体检信息下载";
                        break;
                    case "CD_HYPERTENSION_BASEINFO":
                        msg = "高血压随访下载";
                        break;
                    case "CD_DIABETES_BASEINFO":
                        msg = "糖尿病随访下载";
                        break;
                    default:
                        break;
                }
                // CommonExtensions.WriteLogForTable(dt.Rows[0]["IDCardNo"] + "||" + msg + "||" + ex.Message);
                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);
            }
            return 0;
        }
        #endregion

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="strIDCardNo"></param>
        /*public void SaveDataSet(DataSet ds, string strIDCardNo, string saveFlag = "")
        {
            if (ds == null || ds.Tables.Count == 0)
            {
                return;
            }

            foreach (DataTable dt in ds.Tables)
            {
                // 多笔资料，先删除，后新增
                switch (dt.TableName.ToLower())
                {
                    // 个人基本信息_既往史
                    case "ARCHIVE_ILLNESSHISTORYINFO":

                    // 健康体检_住院史表
                    case "ARCHIVE_HOSPITALHISTORY":

                    // 健康体检_家庭病床史表
                    case "ARCHIVE_FAMILYBEDHISTORY":

                    // 健康体检_非免疫规划预防接种史表
                    case "ARCHIVE_INOCULATIONHISTORY":

                    // 健康体检_主要用药情况表
                    case "ARCHIVE_MEDICATION":
                    // 高血压_用药
                    case "CD_DRUGCONDITION":

                        string cType = "";

                        if (dt.TableName.ToLower() == "CD_DRUGCONDITION")
                        {
                            cType = "AND Type = '" + saveFlag + "'";
                        }

                        DeleteQuerySQL(dt.TableName, strIDCardNo, cType);

                        if (dt.Rows.Count == 0)
                        {
                            continue;
                        }

                        AddDataByMulti(dt);

                        continue;
                }

                if (dt.Rows.Count == 0)
                {
                    continue;
                }

                // 修改条数为0 则新增
                int reInt = EditDataBySingle(dt);
                if (reInt <= 0)
                {
                    reInt = AddDataBySingle(dt);
                }
            }
        }
        */
        /// <summary>
        /// 新增躲避资料
        /// </summary>
        /// <param name="dt"></param>
        private void AddDataByMulti(DataTable dt)
        {
            string sbInsert = SetInsertSQL(dt);

            foreach (DataRow row in dt.Rows)
            {
                try
                {
                    base.Parameter.Clear();

                    foreach (DataColumn colmn in dt.Columns)
                    {
                        base.Parameter.Add(colmn.ColumnName, row[colmn.ColumnName].ToString() == "" ? null : row[colmn.ColumnName]);
                    }

                    base.ExeNonQuery(sbInsert);
                }
                catch (Exception ex)
                {
                    CommonExtensions.WriteLog(dt.TableName);
                    CommonExtensions.WriteLog(ex.Message);
                    CommonExtensions.WriteLog(ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// 新增单行
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private int AddDataBySingle(DataTable dt)
        {
            try
            {
                string sbInsert = SetInsertSQL(dt);

                DataRow row = dt.Rows[0];

                base.Parameter.Clear();

                foreach (DataColumn colmn in dt.Columns)
                {
                    base.Parameter.Add(colmn.ColumnName, row[colmn.ColumnName].ToString() == "" ? null : row[colmn.ColumnName]);
                }
                string ssss = "";
                return base.ExeNonQuery(sbInsert);
            }
            catch (Exception ex)
            {
                CommonExtensions.WriteLog(dt.TableName);
                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);

                return -1;
            }
        }

        /// <summary>
        /// 修改多行
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        /* private int EditDataBySingle(DataTable dt)
         {
             try
             {
                 string sbUpdate = SetUpdateSQL(dt);

                 DataRow row = dt.Rows[0];

                 base.Parameter.Clear();

                 foreach (DataColumn colmn in dt.Columns)
                 {
                     base.Parameter.Add(colmn.ColumnName, row[colmn.ColumnName].ToString() == "" ? null : row[colmn.ColumnName]);
                 }

                 return base.ExeNonQuery(sbUpdate);
             }
             catch (Exception ex)
             {
                 CommonExtensions.WriteLog(dt.TableName);
                 CommonExtensions.WriteLog(ex.Message);
                 CommonExtensions.WriteLog(ex.StackTrace);

                 return -1;
             }
         }
         */
        /// <summary>
        /// 修改多行
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>

        /// <summary>
        /// 修改多行
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private int EditDataBySingle(DataTable dt, string strOtherWhere = "")
        {
            try
            {
                string sbUpdate = SetUpdateSQL(dt, strOtherWhere);
                switch (dt.TableName)
                {
                    case "ARCHIVE_CUSTOMERBASEINFO":
                    case "CD_DIABETESFOLLOWUP":
                    case "CD_HYPERTENSIONFOLLOWUP":
                    case "ARCHIVE_BASEINFO":
                    case "ARCHIVE_BASEINFOARCHIVE_ENVIRONMENT":
                    case "ARCHIVE_FAMILYHISTORYINFO":
                    case "ARCHIVE_ILLNESSHISTORYINFO":
                    case "old_baseinfo":
                    case "OLDER_SELFCAREABILITY":
                    case "ARCHIVE_FAMILY_INFO":
                    case "CD_HYPERTENSION_BASEINFO":
                    case "CD_DIABETES_BASEINFO":
                    case "CD_MENTALDISEASE_BASEINFO":
                    case "CD_CHD_BASEINFO":
                    case "archive_health_info":
                        break;
                    default:
                        sbUpdate += " and OutKey=@OutKey ";
                        break;
                }
                DataRow row = dt.Rows[0];
                base.Parameter.Clear();

                foreach (DataColumn colmn in dt.Columns)
                {
                    base.Parameter.Add(colmn.ColumnName, row[colmn.ColumnName].ToString() == "" ? null : row[colmn.ColumnName]);
                }

                return base.ExeNonQuery(sbUpdate);
            }
            catch (Exception ex)
            {
                CommonExtensions.WriteLog(dt.TableName);
                CommonExtensions.WriteLog(ex.Message);
                CommonExtensions.WriteLog(ex.StackTrace);

                return -1;
            }
        }


        /// <summary>
        /// 修改语句
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private string SetUpdateSQL(DataTable dt, string strOtherWhere = "")
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return "";
            }

            StringBuilder sbUpdate = new StringBuilder("");

            sbUpdate.Append("UPDATE ").Append(dt.TableName).Append(" SET ");

            string colmns = "";

            foreach (DataColumn colmn in dt.Columns)
            {
                colmns += "," + colmn.ColumnName + " = @" + colmn.ColumnName;
            }

            sbUpdate.Append(colmns.TrimStart(','));

            sbUpdate.Append(" WHERE IDCardNo = @IDCardNo");

            sbUpdate.Append(" ");
            sbUpdate.Append(strOtherWhere);

            return sbUpdate.ToString();
        }

        /*todo*/
        private void SetQuerySQL(string dtName, string strIDCardNo)
        {
            StringBuilder sbInsert = new StringBuilder("");

            sbInsert.Append("SELECT ID FROM ").Append(dtName);
            sbInsert.Append(" WHERE IDCardNo = @IDCardNo");

            base.Parameter.Clear();

            base.Parameter.Add("IDCardNo", strIDCardNo);
            base.Search(sbInsert.ToString());
        }

        /// <summary>
        /// 删除后新增
        /// </summary>
        /// <param name="dtName"></param>
        /// <param name="strIDCardNo"></param>
        private void DeleteQuerySQL(string dtName, string strIDCardNo, string strWhere = "")
        {
            StringBuilder sbInsert = new StringBuilder("");

            sbInsert.Append("DELETE FROM ").Append(dtName);
            sbInsert.Append(" WHERE IDCardNo = @IDCardNo");

            if (!string.IsNullOrEmpty(strWhere))
            {
                sbInsert.Append(" ");
                sbInsert.Append(strWhere);
            }

            base.Parameter.Clear();

            base.Parameter.Add("IDCardNo", strIDCardNo);
            base.Search(sbInsert.ToString());
        }

        /// <summary>
        /// 修改语句
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private string SetUpdateSQL(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return "";
            }

            StringBuilder sbUpdate = new StringBuilder("");

            sbUpdate.Append("UPDATE ").Append(dt.TableName).Append(" SET ");

            string colmns = "";

            foreach (DataColumn colmn in dt.Columns)
            {
                colmns += "," + colmn.ColumnName + " = @" + colmn.ColumnName;
            }

            sbUpdate.Append(colmns.TrimStart(','));

            sbUpdate.Append(" WHERE IDCardNo = @IDCardNo");

            return sbUpdate.ToString();
        }

        /// <summary>
        /// 新增语句
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private string SetInsertSQL(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
            {
                return "";
            }

            StringBuilder sbInsert = new StringBuilder("");

            sbInsert.Append("INSERT INTO ").Append(dt.TableName).Append("(");

            string colmns = "";
            string colmnVals = "";

            foreach (DataColumn colmn in dt.Columns)
            {
                colmns += "," + colmn.ColumnName;
                colmnVals += ",@" + colmn.ColumnName;
            }

            sbInsert.Append(colmns.TrimStart(',')).Append(")");

            sbInsert.Append(" VALUES (").Append(colmnVals.TrimStart(',')).Append(")");

            return sbInsert.ToString();
        }

        /// <summary>
        /// 更新责任医生
        /// </summary>
        /// <param name="docter"></param>
        /// <param name="idCard"></param>
        /// <returns></returns>
        public int UpdateDocter(string docter, string idCard)
        {
            try
            {
                StringBuilder strSql = new StringBuilder("");

                strSql.Append(" UPDATE  ARCHIVE_BASEINFO SET Doctor=@Doctor");
                strSql.Append(" WHERE IDCardNo = @IDCardNo");

                base.Parameter.Clear();

                base.Parameter.Add("@IDCardNo", idCard);
                base.Parameter.Add("@Doctor", docter);
                return base.ExeNonQuery(strSql.ToString());
            }
            catch (Exception ex)
            {

            }
            return -1;
        }

        /// <summary>
        /// 更新档案号
        /// </summary>
        /// <param name="idCardNo"></param>
        /// <param name="pid"></param>
        /// <returns></returns>
        public int UpdateRecordId(string idCardNo, string pid)
        {
            try
            {
                StringBuilder strSql = new StringBuilder("");

                strSql.Append(" UPDATE  ARCHIVE_BASEINFO SET RecordID=@RecordID ");
                strSql.Append(" WHERE IDCardNo = @IDCardNo");

                base.Parameter.Clear();

                base.Parameter.Add("@IDCardNo", idCardNo);
                base.Parameter.Add("@RecordID", pid);
                return base.ExeNonQuery(strSql.ToString());
            }
            catch (Exception ex)
            {

            }
            return -1;
        }

        /// <summary>
        /// 查询体检签字维护资料
        /// </summary>
        /// <returns></returns>
        public string GetTjSignInfo(string idcardno, string outkey)
        {
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"SELECT FeedbackDate FROM archive_signature WHERE IDCardNo='" + idcardno + "' and outkey=" + outkey);

            base.Parameter.Clear();
            DataTable dt = base.Search(sbQuery.ToString());
            if (dt != null && dt.Rows.Count > 0)
            {
                return dt.Rows[0][0].ToString();
            }
            return "";
        }

        /// <summary>
        /// 查询体检签字维护资料
        /// </summary>
        /// <returns></returns>
        public DataTable GetTjSignData()
        {
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"SELECT * FROM archive_signature WHERE IDCardNo='签字维护' ORDER BY ID DESC LIMIT 1");

            base.Parameter.Clear();
            return base.Search(sbQuery.ToString());
        }

        public DataTable GetEcgFile(string idcarno, string tjDate)
        {
            StringBuilder sbQuery = new StringBuilder();

            sbQuery.Append(@"SELECT * FROM archive_ecg WHERE IDCardNo='" + idcarno + "' and CreateTime>='" + tjDate + " 00:00:00' and CreateTime<='"+tjDate+" 23:59:59' ORDER BY ID DESC LIMIT 1");

            base.Parameter.Clear();
            return base.Search(sbQuery.ToString());
        }
    }
}
