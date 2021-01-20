using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Utilities.Common
{
    public static class DataSetTmp
    {
        /// <summary>
        /// 个人档案
        /// </summary>
        public static DataSet GrdaDataSet = GrdaDataSet == null ? GetGrdaDataSet() : GrdaDataSet;

        public static DataSet RecordIdDataSet = RecordIdDataSet == null ? GetRecordIdSet() : RecordIdDataSet;

        /// <summary>
        /// 体检信息
        /// </summary>
        public static DataSet TjDataSet = TjDataSet == null ? GetTJDataSet() : TjDataSet;

        /// <summary>
        /// 老年人
        /// </summary>
        public static DataSet LnrDataSet = LnrDataSet == null ? GetLnrDataSet() : LnrDataSet;

        /// <summary>
        /// 高血压
        /// </summary>
        public static DataSet GxyDataSet = GxyDataSet == null ? GetGxyDataSet() : GxyDataSet;
        
        /// <summary>
        /// 糖尿病
        /// </summary>
        public static DataSet TnbDataSet = TnbDataSet == null ? GetTnbDataSet() : TnbDataSet;
        /// <summary>
        /// 精神疾病
        /// </summary>
        public static DataSet JsbDataSet = JsbDataSet == null ? GetJsbDataSet() : JsbDataSet;
        /// <summary>
        /// 脑卒中病
        /// </summary>
        public static DataSet NczDataSet = NczDataSet == null ? GetNczDataSet() : NczDataSet;

        /// <summary>
        /// 冠心病
        /// </summary>
        public static DataSet GxbDataSet = GxbDataSet == null ? GetGxbDataSet() : GxbDataSet;

        /// <summary>
        /// 孕妇
        /// </summary>
        public static DataSet YfDataSet = YfDataSet == null ? GetYfDataSet() : YfDataSet;
        /// <summary>
        /// 家庭
        /// </summary>
        public static DataSet JtDataSet = JtDataSet == null ? GetJtDataSet() : JtDataSet;

        private static DataSet GetJtDataSet()
        {
            DataSet ds = new DataSet();

            #region  ARCHIVE_BASEINFO

            DataTable dtDataGrda = new DataTable();
            dtDataGrda.TableName = "ARCHIVE_BASEINFO";
            dtDataGrda.Columns.Add("IDCardNo");
            dtDataGrda.Columns.Add("FamilyIDCardNo");
            dtDataGrda.Columns.Add("HouseRelation");

            #endregion

            #region  ARCHIVE_FAMILY_INFO

            DataTable dtDataF = new DataTable();
            dtDataF.TableName = "ARCHIVE_FAMILY_INFO";
            dtDataF.Columns.Add("IDCardNo");
            dtDataF.Columns.Add("FamilyRecordID");
            dtDataF.Columns.Add("HomeAddrInfo");
            dtDataF.Columns.Add("IncomeAvg");
            dtDataF.Columns.Add("HouseArea");

            #endregion

            ds.Tables.Add(dtDataGrda);
            ds.Tables.Add(dtDataF);

            return ds;
        }

        private static DataSet GetGrdaDataSet()
        {
            DataSet ds = new DataSet();

            #region 基本信息
            DataTable baseinfoDT = new DataTable();
            baseinfoDT.TableName = "ARCHIVE_BASEINFO";
            baseinfoDT.Columns.Add("RecordID");
            baseinfoDT.Columns.Add("IDCardNo");

            baseinfoDT.Columns.Add("WorkUnit");
            baseinfoDT.Columns.Add("LiveType");
            baseinfoDT.Columns.Add("Nation");
            baseinfoDT.Columns.Add("RH");
            baseinfoDT.Columns.Add("Culture");
            baseinfoDT.Columns.Add("Job");
            baseinfoDT.Columns.Add("MaritalStatus");
            baseinfoDT.Columns.Add("MedicalPayType");
            baseinfoDT.Columns.Add("DrugAllergic");
            baseinfoDT.Columns.Add("Disease");
            baseinfoDT.Columns.Add("DiseasEndition");
            baseinfoDT.Columns.Add("CustomerName");
            baseinfoDT.Columns.Add("Doctor");
            baseinfoDT.Columns.Add("Sex");
            baseinfoDT.Columns.Add("Birthday");
            baseinfoDT.Columns.Add("ContactName");
            baseinfoDT.Columns.Add("ContactPhone");
            baseinfoDT.Columns.Add("BloodType");
            baseinfoDT.Columns.Add("Phone");
            baseinfoDT.Columns.Add("MedicalPayTypeOther");
            baseinfoDT.Columns.Add("DrugAllergicOther");
            baseinfoDT.Columns.Add("DiseaseEx");
            baseinfoDT.Columns.Add("DiseasenditionEx");
            baseinfoDT.Columns.Add("Address");
            baseinfoDT.Columns.Add("HouseHoldAddress");
            baseinfoDT.Columns.Add("Minority");
            baseinfoDT.Columns.Add("Exposure");
            baseinfoDT.Columns.Add("HouseRelation");
            baseinfoDT.Columns.Add("CreateMenName");
            baseinfoDT.Columns.Add("CreateDate");
            baseinfoDT.Columns.Add("CreateUnit");
            baseinfoDT.Columns.Add("CreateUnitName");
            baseinfoDT.Columns.Add("TownName");
            baseinfoDT.Columns.Add("VillageName");

            baseinfoDT.Columns.Add("ProvinceID");
            baseinfoDT.Columns.Add("CityID");
            baseinfoDT.Columns.Add("DistrictID");
            baseinfoDT.Columns.Add("TownID");
            baseinfoDT.Columns.Add("VillageID");
            baseinfoDT.Columns.Add("PopulationType");
            baseinfoDT.Columns.Add("LastUpdateDate");

            baseinfoDT.Columns.Add("FamilyIDCardNo");
            baseinfoDT.Columns.Add("HouseName");
            baseinfoDT.Columns.Add("OrgName");
            baseinfoDT.Columns.Add("FamilyNum");
            baseinfoDT.Columns.Add("FamilyStructure");
            baseinfoDT.Columns.Add("TownMedicalCard");
            baseinfoDT.Columns.Add("ResidentMedicalCard");
            baseinfoDT.Columns.Add("PovertyReliefMedicalCard");
            baseinfoDT.Columns.Add("LiveCondition");
            baseinfoDT.Columns.Add("PreSituation");
            baseinfoDT.Columns.Add("PreNum");
            baseinfoDT.Columns.Add("YieldNum");
            baseinfoDT.Columns.Add("Chemical");
            baseinfoDT.Columns.Add("Poison");
            baseinfoDT.Columns.Add("Radial");

            ds.Tables.Add(baseinfoDT);
            #endregion

            #region 生活环境
            DataTable environmentDT = new DataTable();
            environmentDT.TableName = "ARCHIVE_BASEINFOARCHIVE_ENVIRONMENT";

            environmentDT.Columns.Add("IDCardNo");
            environmentDT.Columns.Add("BlowMeasure");
            environmentDT.Columns.Add("FuelType");
            environmentDT.Columns.Add("DrinkWater");
            environmentDT.Columns.Add("Toilet");
            environmentDT.Columns.Add("LiveStockRail");
            ds.Tables.Add(environmentDT);
            #endregion

            #region  家族史

            //家族史
            DataTable familyHistoryinfoDT = new DataTable();
            familyHistoryinfoDT.TableName = "ARCHIVE_FAMILYHISTORYINFO";
            familyHistoryinfoDT.Columns.Add("FatherHistory");
            familyHistoryinfoDT.Columns.Add("MotherHistory");
            familyHistoryinfoDT.Columns.Add("BrotherSisterHistory");
            familyHistoryinfoDT.Columns.Add("ChildrenHistory");
            familyHistoryinfoDT.Columns.Add("FatherHistoryOther");
            familyHistoryinfoDT.Columns.Add("MotherHistoryOther");
            familyHistoryinfoDT.Columns.Add("BrotherSisterHistoryOther");
            familyHistoryinfoDT.Columns.Add("ChildrenHistoryOther");
            familyHistoryinfoDT.Columns.Add("IDCardNo");

            ds.Tables.Add(familyHistoryinfoDT);
            #endregion

            #region  既往史
            //既往史
            DataTable illnesshistoryinfoDT = new DataTable();
            illnesshistoryinfoDT.TableName = "ARCHIVE_ILLNESSHISTORYINFO";

            illnesshistoryinfoDT.Columns.Add("IDCardNo");
            illnesshistoryinfoDT.Columns.Add("IllnessType");
            illnesshistoryinfoDT.Columns.Add("IllnessName");
            illnesshistoryinfoDT.Columns.Add("Therioma");
            illnesshistoryinfoDT.Columns.Add("IllnessOther");
            illnesshistoryinfoDT.Columns.Add("JobIllness");
            illnesshistoryinfoDT.Columns.Add("IllnessNameOther");
            illnesshistoryinfoDT.Columns.Add("DiagnoseTime");

            ds.Tables.Add(illnesshistoryinfoDT);
            #endregion

            #region 健康信息卡
            DataTable healthDT = new DataTable();
            healthDT.TableName = "archive_health_info";
            healthDT.Columns.Add("IDCardNo");
            healthDT.Columns.Add("Prevalence");
            healthDT.Columns.Add("PrevalenceOther");
            healthDT.Columns.Add("OrgTelphone");
            healthDT.Columns.Add("FamilyDoctor");
            healthDT.Columns.Add("FamilyDoctorTel");
            healthDT.Columns.Add("Nurses");
            healthDT.Columns.Add("NursesTel");
            healthDT.Columns.Add("HealthPersonnel");
            healthDT.Columns.Add("HealthPersonnelTel");
            healthDT.Columns.Add("Others");

            ds.Tables.Add(healthDT);

            #endregion

            return ds;
        }

        private static DataSet GetRecordIdSet()
        {
            DataSet ds = new DataSet();
            DataTable baseinfoDT = new DataTable();
            baseinfoDT.TableName = "ARCHIVE_BASEINFO";
            baseinfoDT.Columns.Add("RecordID");
            baseinfoDT.Columns.Add("IDCardNo");
            //baseinfoDT.Columns.Add("CreateMenName");
            //baseinfoDT.Columns.Add("FamilyIDCardNo");

            ds.Tables.Add(baseinfoDT);

            return ds;
        }

        private static DataSet GetTJDataSet()
        {
            DataSet ds = new DataSet();

            #region ARCHIVE_CUSTOMERBASEINFO
            DataTable dt = new DataTable();
            dt.TableName = "ARCHIVE_CUSTOMERBASEINFO";

            dt.Columns.Add("CustomerID");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("CheckDate");
            dt.Columns.Add("Doctor");
            dt.Columns.Add("Symptom");
            dt.Columns.Add("Other");
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("CreateDate");
            /*dt.Columns.Add("CreateBy");
           
            dt.Columns.Add("LastUpdateBy");
            dt.Columns.Add("LastUpdateDate");
            dt.Columns.Add("IsDel");*/

            #endregion

            ds.Tables.Add(dt);

            #region ARCHIVE_GENERALCONDITION
            dt = new DataTable();
            dt.TableName = "ARCHIVE_GENERALCONDITION";
             dt.Columns.Add("RightReason");
             dt.Columns.Add("LeftReason");
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("AnimalHeat");
            dt.Columns.Add("BreathRate");
            dt.Columns.Add("Waistline");
            dt.Columns.Add("Height");
            dt.Columns.Add("OldRecognise");
            dt.Columns.Add("OldEmotion");
            dt.Columns.Add("PulseRate");
            dt.Columns.Add("Weight");
            dt.Columns.Add("BMI");
            dt.Columns.Add("InterScore");
            dt.Columns.Add("GloomyScore");
            dt.Columns.Add("LeftPre");
            dt.Columns.Add("RightPre");
            dt.Columns.Add("WaistIp");
            dt.Columns.Add("LeftHeight");
            dt.Columns.Add("RightHeight");
            dt.Columns.Add("OldHealthStaus");
            dt.Columns.Add("Tem");
            dt.Columns.Add("OldSelfCareability");
            dt.Columns.Add("OldMange");
            #endregion

            ds.Tables.Add(dt);

            #region ARCHIVE_LIFESTYLE
            dt = new DataTable();
            dt.TableName = "ARCHIVE_LIFESTYLE";
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("SmokeDayNum");
            dt.Columns.Add("SmokeAgeStart");
            dt.Columns.Add("SmokeAgeForbiddon");
            dt.Columns.Add("ExerciseRate");
            dt.Columns.Add("ExerciseTimes");
            dt.Columns.Add("DietaryHabit");
            dt.Columns.Add("ExerciseExistense");
            dt.Columns.Add("ExcisepersistTime");
            dt.Columns.Add("SmokeCondition");
            dt.Columns.Add("DrinkRate");
            dt.Columns.Add("DayDrinkVolume");
            dt.Columns.Add("IsDrinkForbiddon");
            dt.Columns.Add("ForbiddonAge");
            dt.Columns.Add("DrinkStartAge");
            dt.Columns.Add("DrinkThisYear");
            dt.Columns.Add("DrinkType");
            dt.Columns.Add("CareerHarmFactorHistory");
            dt.Columns.Add("Dust");
            dt.Columns.Add("DustProtect");
            dt.Columns.Add("Radiogen");
            dt.Columns.Add("RadiogenProtect");
            dt.Columns.Add("Physical");
            dt.Columns.Add("PhysicalProtect");
            dt.Columns.Add("Chem");
            dt.Columns.Add("ChemProtect");
            dt.Columns.Add("Other");
            dt.Columns.Add("OtherProtect");
            dt.Columns.Add("WorkType");
            dt.Columns.Add("WorkTime");
            dt.Columns.Add("DustProtectEx");
            dt.Columns.Add("RadiogenProtectEx");
            dt.Columns.Add("PhysicalProtectEx");
            dt.Columns.Add("ChemProtectEx");
            dt.Columns.Add("OtherProtectEx");
            dt.Columns.Add("DrinkTypeOther");
            dt.Columns.Add("ExerciseExistenseOther");

            #endregion

            ds.Tables.Add(dt);

            #region ARCHIVE_VISCERAFUNCTION

            dt = new DataTable();
            dt.TableName = "ARCHIVE_VISCERAFUNCTION";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("Lips");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("ToothResides");
            dt.Columns.Add("ToothResidesOther");
            dt.Columns.Add("Pharyngeal");
            dt.Columns.Add("LeftView");
            dt.Columns.Add("Listen");
            dt.Columns.Add("RightView");
            dt.Columns.Add("SportFunction");
            dt.Columns.Add("LeftEyecorrect");
            dt.Columns.Add("RightEyecorrect");
            dt.Columns.Add("HypodontiaEx");
            dt.Columns.Add("SaprodontiaEx");
            dt.Columns.Add("DentureEx");
            #endregion

            ds.Tables.Add(dt);

            #region ARCHIVE_PHYSICALEXAM

            dt = new DataTable();
            dt.TableName = "ARCHIVE_PHYSICALEXAM";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("Skin");
            dt.Columns.Add("Sclere");
            dt.Columns.Add("Lymph");
            dt.Columns.Add("BarrelChest");
            dt.Columns.Add("BreathSounds");
            dt.Columns.Add("Rale");
            dt.Columns.Add("HeartRate");
            dt.Columns.Add("HeartRhythm");
            dt.Columns.Add("Noise");
            dt.Columns.Add("EnclosedMass");
            dt.Columns.Add("Edema");
            dt.Columns.Add("FootBack");
            dt.Columns.Add("Anus");
            dt.Columns.Add("Breast");
            dt.Columns.Add("Vulva");
            dt.Columns.Add("Vagina");
            dt.Columns.Add("CervixUteri");
            dt.Columns.Add("Corpus");
            dt.Columns.Add("Attach");
            dt.Columns.Add("Other");
            dt.Columns.Add("PressPain");
            dt.Columns.Add("Liver");
            dt.Columns.Add("Spleen");
            dt.Columns.Add("Voiced");
            dt.Columns.Add("SkinEx");
            dt.Columns.Add("SclereEx");
            dt.Columns.Add("LymphEx");
            dt.Columns.Add("BreastEx");
            dt.Columns.Add("AnusEx");
            dt.Columns.Add("BreathSoundsEx");
            dt.Columns.Add("RaleEx");
            dt.Columns.Add("NoiseEx");
            dt.Columns.Add("CervixUteriEx");
            dt.Columns.Add("CorpusEx");
            dt.Columns.Add("AttachEx");
            dt.Columns.Add("VulvaEx");
            dt.Columns.Add("VaginaEx");
            dt.Columns.Add("PressPainEx");
            dt.Columns.Add("LiverEx");
            dt.Columns.Add("SpleenEx");
            dt.Columns.Add("VoicedEx");
            dt.Columns.Add("EnclosedMassEx");
            dt.Columns.Add("EyeRound");
            dt.Columns.Add("EyeRoundEx");

            #endregion
            ds.Tables.Add(dt);

            #region ARCHIVE_ASSISTCHECK
            dt = new DataTable();
            dt.TableName = "ARCHIVE_ASSISTCHECK";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("HB");
            dt.Columns.Add("WBC");
            dt.Columns.Add("PLT");
            dt.Columns.Add("PRO");
            dt.Columns.Add("GLU");
            dt.Columns.Add("KET");
            dt.Columns.Add("BLD");
            dt.Columns.Add("FPGL");
            dt.Columns.Add("ECG");
            dt.Columns.Add("ALBUMIN");
            dt.Columns.Add("FOB");
            dt.Columns.Add("HBALC");
            dt.Columns.Add("HBSAG");
            dt.Columns.Add("SGPT");
            dt.Columns.Add("GOT");
            dt.Columns.Add("BP");
            dt.Columns.Add("TBIL");
            dt.Columns.Add("CB");
            dt.Columns.Add("SCR");
            dt.Columns.Add("BUN");
            dt.Columns.Add("PC");
            dt.Columns.Add("HYPE");
            dt.Columns.Add("TC");
            dt.Columns.Add("TG");
            dt.Columns.Add("LowCho");
            dt.Columns.Add("HeiCho");
            dt.Columns.Add("CHESTX");
            dt.Columns.Add("BCHAO");
            dt.Columns.Add("BloodOther");
            dt.Columns.Add("UrineOther");
            dt.Columns.Add("Other");
            dt.Columns.Add("CERVIX");
            //dt.Columns.Add("GT");
            dt.Columns.Add("ECGEx");
            dt.Columns.Add("CHESTXEx");
            dt.Columns.Add("BCHAOEx");
            dt.Columns.Add("CERVIXEx");
            dt.Columns.Add("FPGDL");
            dt.Columns.Add("UA");
            dt.Columns.Add("BloodType");
            dt.Columns.Add("RH");
            dt.Columns.Add("HCY");
            dt.Columns.Add("BCHAOther");
            dt.Columns.Add("BCHAOtherEx");
            #endregion

            ds.Tables.Add(dt);

            #region ARCHIVE_MEDI_PHYS_DIST
            dt = new DataTable();
            dt.TableName = "ARCHIVE_MEDI_PHYS_DIST";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("Mild");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("Faint");
            dt.Columns.Add("Yang");
            dt.Columns.Add("Yin");
            dt.Columns.Add("PhlegmDamp");
            dt.Columns.Add("Muggy");
            dt.Columns.Add("BloodStasis");
            dt.Columns.Add("QiConstraint");
            dt.Columns.Add("Characteristic");
            #endregion

            ds.Tables.Add(dt);

            #region ARCHIVE_HEALTHQUESTION
            dt = new DataTable();
            dt.TableName = "ARCHIVE_HEALTHQUESTION";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("BrainDis");
            dt.Columns.Add("RenalDis");
            dt.Columns.Add("HeartDis");
            dt.Columns.Add("VesselDis");
            dt.Columns.Add("EyeDis");
            dt.Columns.Add("NerveDis");
            dt.Columns.Add("ElseDis");
            dt.Columns.Add("BrainOther");
            dt.Columns.Add("RenalOther");
            dt.Columns.Add("HeartOther");
            dt.Columns.Add("VesselOther");
            dt.Columns.Add("EyeOther");
            dt.Columns.Add("NerveOther");
            dt.Columns.Add("ElseOther");
            #endregion
            ds.Tables.Add(dt);

            #region ARCHIVE_HOSPITALHISTORY
            dt = new DataTable();
            dt.TableName = "ARCHIVE_HOSPITALHISTORY";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("InHospitalDate");
            dt.Columns.Add("Reason");
            dt.Columns.Add("IllcaseNum");
            dt.Columns.Add("HospitalName");
            dt.Columns.Add("OutHospitalDate");
            #endregion
            ds.Tables.Add(dt);

            #region ARCHIVE_FAMILYBEDHISTORY
            dt = new DataTable();
            dt.TableName = "ARCHIVE_FAMILYBEDHISTORY";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("HospitalName");
            dt.Columns.Add("InHospitalDate");
            dt.Columns.Add("IllcaseNums");
            dt.Columns.Add("Reasons");
            dt.Columns.Add("OutHospitalDate");
            #endregion
            ds.Tables.Add(dt);

            #region ARCHIVE_MEDICATION
            dt = new DataTable();
            dt.TableName = "ARCHIVE_MEDICATION";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("UseAge");
            dt.Columns.Add("UseNum");
            dt.Columns.Add("StartTime");
            dt.Columns.Add("EndTime");
            dt.Columns.Add("PillDependence");
            dt.Columns.Add("MedicinalName");
            #endregion
            ds.Tables.Add(dt);

            #region ARCHIVE_INOCULATIONHISTORY
            dt = new DataTable();
            dt.TableName = "ARCHIVE_INOCULATIONHISTORY";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("OutKey");
      
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("PillName");
            dt.Columns.Add("InoculationDate");
            dt.Columns.Add("InoculationHistory");
            #endregion
            ds.Tables.Add(dt);

            #region ARCHIVE_ASSESSMENTGUIDE
            dt = new DataTable();
            dt.TableName = "ARCHIVE_ASSESSMENTGUIDE";
            dt.Columns.Add("PhysicalID");
            dt.Columns.Add("OutKey");
            dt.Columns.Add("IDCardNo");
            dt.Columns.Add("IsNormal");
            dt.Columns.Add("HealthGuide");
            dt.Columns.Add("DangerControl");
            dt.Columns.Add("Exception1");
            dt.Columns.Add("Exception2");
            dt.Columns.Add("Exception3");
            dt.Columns.Add("Arm");
            dt.Columns.Add("VaccineAdvice");
            dt.Columns.Add("Other");
            dt.Columns.Add("Exception4");
            dt.Columns.Add("WaistlineArm");
            #endregion
            ds.Tables.Add(dt);

            #region  OLD_MEDICINE_CN

            DataTable dtDataLnrTZ = new DataTable();
            dtDataLnrTZ.TableName = "OLD_MEDICINE_CN";
            dtDataLnrTZ.Columns.Add("PhysicalID");
            dtDataLnrTZ.Columns.Add("Energy");
            dtDataLnrTZ.Columns.Add("OutKey");
            dtDataLnrTZ.Columns.Add("Tired");
            dtDataLnrTZ.Columns.Add("Breath");
            dtDataLnrTZ.Columns.Add("Voice");
            dtDataLnrTZ.Columns.Add("Emotion");
            dtDataLnrTZ.Columns.Add("Spirit");
            dtDataLnrTZ.Columns.Add("Alone");
            dtDataLnrTZ.Columns.Add("Fear");
            dtDataLnrTZ.Columns.Add("Weight");
            dtDataLnrTZ.Columns.Add("Eye");
            dtDataLnrTZ.Columns.Add("FootHand");
            dtDataLnrTZ.Columns.Add("Stomach");
            dtDataLnrTZ.Columns.Add("Cold");
            dtDataLnrTZ.Columns.Add("Influenza");
            dtDataLnrTZ.Columns.Add("Nasal");
            dtDataLnrTZ.Columns.Add("Snore");
            dtDataLnrTZ.Columns.Add("Allergy");
            dtDataLnrTZ.Columns.Add("Urticaria");
            dtDataLnrTZ.Columns.Add("Skin");
            dtDataLnrTZ.Columns.Add("Scratch");
            dtDataLnrTZ.Columns.Add("Mouth");
            dtDataLnrTZ.Columns.Add("Arms");
            dtDataLnrTZ.Columns.Add("Greasy");
            dtDataLnrTZ.Columns.Add("Spot");
            dtDataLnrTZ.Columns.Add("Eczema");
            dtDataLnrTZ.Columns.Add("Thirsty");
            dtDataLnrTZ.Columns.Add("Smell");
            dtDataLnrTZ.Columns.Add("Abdomen");
            dtDataLnrTZ.Columns.Add("Coolfood");
            dtDataLnrTZ.Columns.Add("Defecate");
            dtDataLnrTZ.Columns.Add("Defecatedry");
            dtDataLnrTZ.Columns.Add("Tongue");
            dtDataLnrTZ.Columns.Add("Vein");
            //dtDataLnr.Columns.Add("CreatedBy");
            //dtDataLnr.Columns.Add("CreatedDate");
            //dtDataLnr.Columns.Add("LastUpdateBy");
            //dtDataLnr.Columns.Add("LastUpdateDate");
            dtDataLnrTZ.Columns.Add("FollowupDoctor");
            dtDataLnrTZ.Columns.Add("RecordDate");
            dtDataLnrTZ.Columns.Add("RecordID");
            dtDataLnrTZ.Columns.Add("IDCardNo");
            //dtDataLnr.Columns.Add("IsDel");

            #endregion

            #region OLD_MEDICINE_RESULT

            DataTable dtDataLnrResult = new DataTable();
            dtDataLnrResult.TableName = "OLD_MEDICINE_RESULT";

            //dtDataLnrResult.Columns.Add("ID");
            dtDataLnrResult.Columns.Add("PhysicalID");
            dtDataLnrResult.Columns.Add("OutKey");
            //dtDataLnrResult.Columns.Add("MedicineID");
            //dtDataLnrResult.Columns.Add("Mild");
            //dtDataLnrResult.Columns.Add("Faint");
            //dtDataLnrResult.Columns.Add("Yang");
            //dtDataLnrResult.Columns.Add("Yin");
            //dtDataLnrResult.Columns.Add("PhlegmDamp");
            //dtDataLnrResult.Columns.Add("Muggy");
            //dtDataLnrResult.Columns.Add("BloodStasis");
            //dtDataLnrResult.Columns.Add("QIconStraint");
            //dtDataLnrResult.Columns.Add("Characteristic");
            dtDataLnrResult.Columns.Add("MildScore");
            dtDataLnrResult.Columns.Add("FaintScore");
            dtDataLnrResult.Columns.Add("YangsCore");
            dtDataLnrResult.Columns.Add("YinScore");
            dtDataLnrResult.Columns.Add("PhlegmdampScore");
            dtDataLnrResult.Columns.Add("MuggyScore");
            dtDataLnrResult.Columns.Add("BloodStasisScore");
            dtDataLnrResult.Columns.Add("QiConstraintScore");
            dtDataLnrResult.Columns.Add("CharacteristicScore");
            dtDataLnrResult.Columns.Add("MildAdvising");
            dtDataLnrResult.Columns.Add("FaintAdvising");
            dtDataLnrResult.Columns.Add("YangAdvising");
            dtDataLnrResult.Columns.Add("YinAdvising");
            dtDataLnrResult.Columns.Add("PhlegmdampAdvising");
            dtDataLnrResult.Columns.Add("MuggyAdvising");
            dtDataLnrResult.Columns.Add("BloodStasisAdvising");
            dtDataLnrResult.Columns.Add("QiconstraintAdvising");
            dtDataLnrResult.Columns.Add("CharacteristicAdvising");
            dtDataLnrResult.Columns.Add("MildAdvisingEx");
            dtDataLnrResult.Columns.Add("FaintAdvisingEx");
            dtDataLnrResult.Columns.Add("YangadvisingEx");
            dtDataLnrResult.Columns.Add("YinAdvisingEx");
            dtDataLnrResult.Columns.Add("PhlegmdampAdvisingEx");
            dtDataLnrResult.Columns.Add("MuggyAdvisingEx");
            dtDataLnrResult.Columns.Add("BloodStasisAdvisingEx");
            dtDataLnrResult.Columns.Add("QiconstraintAdvisingEx");
            dtDataLnrResult.Columns.Add("CharacteristicAdvisingEx");
            //dtDataLnrResult.Columns.Add("IsDel");
            dtDataLnrResult.Columns.Add("IDCardNo");

            #endregion

            ds.Tables.Add(dtDataLnrTZ);
            ds.Tables.Add(dtDataLnrResult);

            #region   OLDER_SELFCAREABILITY
            DataTable dtDataLnrZL = new DataTable();

            dtDataLnrZL.TableName = "OLDER_SELFCAREABILITY";
            dtDataLnrZL.Columns.Add("PhysicalID");
            dtDataLnrZL.Columns.Add("Dine");
            dtDataLnrZL.Columns.Add("OutKey");
            dtDataLnrZL.Columns.Add("Groming");
            dtDataLnrZL.Columns.Add("Dressing");
            dtDataLnrZL.Columns.Add("Tolet");
            dtDataLnrZL.Columns.Add("Activity");
            dtDataLnrZL.Columns.Add("TotalScore");
            dtDataLnrZL.Columns.Add("FollowUpDate");
            dtDataLnrZL.Columns.Add("FollowUpDoctor");
            dtDataLnrZL.Columns.Add("NextfollowUpDate");

            dtDataLnrZL.Columns.Add("RecordID");
            dtDataLnrZL.Columns.Add("IDCardNo");
            //dtDataLnr.Columns.Add("CreatedBy");
            //dtDataLnr.Columns.Add("CreatedDate");
            //dtDataLnr.Columns.Add("LastUpDateBy");
            //dtDataLnr.Columns.Add("LastUpDateDate");

            #endregion
            ds.Tables.Add(dtDataLnrZL);

            return ds;
        }
           
        private static DataSet GetLnrDataSet()
        {
            DataSet ds = new DataSet();

            // 为DataTable添加列 columns
            #region  OLD_MEDICINE_CN

            DataTable dtDataLnrTZ = new DataTable();
            dtDataLnrTZ.TableName = "OLD_MEDICINE_CN";
            dtDataLnrTZ.Columns.Add("OutKey");
            dtDataLnrTZ.Columns.Add("Energy");
            dtDataLnrTZ.Columns.Add("Tired");
            dtDataLnrTZ.Columns.Add("Breath");
            dtDataLnrTZ.Columns.Add("Voice");
            dtDataLnrTZ.Columns.Add("Emotion");
            dtDataLnrTZ.Columns.Add("Spirit");
            dtDataLnrTZ.Columns.Add("Alone");
            dtDataLnrTZ.Columns.Add("Fear");
            dtDataLnrTZ.Columns.Add("Weight");
            dtDataLnrTZ.Columns.Add("Eye");
            dtDataLnrTZ.Columns.Add("FootHand");
            dtDataLnrTZ.Columns.Add("Stomach");
            dtDataLnrTZ.Columns.Add("Cold");
            dtDataLnrTZ.Columns.Add("Influenza");
            dtDataLnrTZ.Columns.Add("Nasal");
            dtDataLnrTZ.Columns.Add("Snore");
            dtDataLnrTZ.Columns.Add("Allergy");
            dtDataLnrTZ.Columns.Add("Urticaria");
            dtDataLnrTZ.Columns.Add("Skin");
            dtDataLnrTZ.Columns.Add("Scratch");
            dtDataLnrTZ.Columns.Add("Mouth");
            dtDataLnrTZ.Columns.Add("Arms");
            dtDataLnrTZ.Columns.Add("Greasy");
            dtDataLnrTZ.Columns.Add("Spot");
            dtDataLnrTZ.Columns.Add("Eczema");
            dtDataLnrTZ.Columns.Add("Thirsty");
            dtDataLnrTZ.Columns.Add("Smell");
            dtDataLnrTZ.Columns.Add("Abdomen");
            dtDataLnrTZ.Columns.Add("Coolfood");
            dtDataLnrTZ.Columns.Add("Defecate");
            dtDataLnrTZ.Columns.Add("Defecatedry");
            dtDataLnrTZ.Columns.Add("Tongue");
            dtDataLnrTZ.Columns.Add("Vein");
            //dtDataLnr.Columns.Add("CreatedBy");
            //dtDataLnr.Columns.Add("CreatedDate");
            //dtDataLnr.Columns.Add("LastUpdateBy");
            //dtDataLnr.Columns.Add("LastUpdateDate");
            dtDataLnrTZ.Columns.Add("FollowupDoctor");
            dtDataLnrTZ.Columns.Add("RecordDate");
            dtDataLnrTZ.Columns.Add("RecordID");
            dtDataLnrTZ.Columns.Add("IDCardNo");
            //dtDataLnr.Columns.Add("IsDel");

            #endregion

            #region OLD_MEDICINE_RESULT

            DataTable dtDataLnrResult = new DataTable();
            dtDataLnrResult.TableName = "OLD_MEDICINE_RESULT";

            //dtDataLnrResult.Columns.Add("ID");
            //dtDataLnrResult.Columns.Add("PhysicalID");
            //dtDataLnrResult.Columns.Add("MedicineID");
            dtDataLnrResult.Columns.Add("Mild");
            dtDataLnrResult.Columns.Add("Faint");
            dtDataLnrResult.Columns.Add("Yang");
            dtDataLnrResult.Columns.Add("Yin");
            dtDataLnrResult.Columns.Add("PhlegmDamp");
            dtDataLnrResult.Columns.Add("Muggy");
            dtDataLnrResult.Columns.Add("BloodStasis");
            dtDataLnrResult.Columns.Add("QIconStraint");
            dtDataLnrResult.Columns.Add("Characteristic");
            dtDataLnrResult.Columns.Add("MildScore");
            dtDataLnrResult.Columns.Add("OutKey");
            dtDataLnrResult.Columns.Add("FaintScore");
            dtDataLnrResult.Columns.Add("YangsCore");
            dtDataLnrResult.Columns.Add("YinScore");
            dtDataLnrResult.Columns.Add("PhlegmdampScore");
            dtDataLnrResult.Columns.Add("MuggyScore");
            dtDataLnrResult.Columns.Add("BloodStasisScore");
            dtDataLnrResult.Columns.Add("QiConstraintScore");
            dtDataLnrResult.Columns.Add("CharacteristicScore");
            dtDataLnrResult.Columns.Add("MildAdvising");
            dtDataLnrResult.Columns.Add("FaintAdvising");
            dtDataLnrResult.Columns.Add("YangAdvising");
            dtDataLnrResult.Columns.Add("YinAdvising");
            dtDataLnrResult.Columns.Add("PhlegmdampAdvising");
            dtDataLnrResult.Columns.Add("MuggyAdvising");
            dtDataLnrResult.Columns.Add("BloodStasisAdvising");
            dtDataLnrResult.Columns.Add("QiconstraintAdvising");
            dtDataLnrResult.Columns.Add("CharacteristicAdvising");
            dtDataLnrResult.Columns.Add("MildAdvisingEx");
            dtDataLnrResult.Columns.Add("FaintAdvisingEx");
            dtDataLnrResult.Columns.Add("YangadvisingEx");
            dtDataLnrResult.Columns.Add("YinAdvisingEx");
            dtDataLnrResult.Columns.Add("PhlegmdampAdvisingEx");
            dtDataLnrResult.Columns.Add("MuggyAdvisingEx");
            dtDataLnrResult.Columns.Add("BloodStasisAdvisingEx");
            dtDataLnrResult.Columns.Add("QiconstraintAdvisingEx");
            dtDataLnrResult.Columns.Add("CharacteristicAdvisingEx");
            //dtDataLnrResult.Columns.Add("IsDel");
            dtDataLnrResult.Columns.Add("IDCardNo");

            #endregion

            #region   OLDER_SELFCAREABILITY
            DataTable dtDataLnrZL = new DataTable();

            dtDataLnrZL.TableName = "OLDER_SELFCAREABILITY";

            dtDataLnrZL.Columns.Add("Dine");
            dtDataLnrZL.Columns.Add("Groming");
            dtDataLnrZL.Columns.Add("Dressing");
            dtDataLnrZL.Columns.Add("Tolet");
            dtDataLnrZL.Columns.Add("Activity");
            dtDataLnrZL.Columns.Add("TotalScore");
            dtDataLnrZL.Columns.Add("FollowUpDate");
            dtDataLnrZL.Columns.Add("FollowUpDoctor");
            dtDataLnrZL.Columns.Add("NextfollowUpDate");

            dtDataLnrZL.Columns.Add("RecordID");
            dtDataLnrZL.Columns.Add("IDCardNo");
            dtDataLnrZL.Columns.Add("NextVisitAim");
            //dtDataLnr.Columns.Add("CreatedBy");
            //dtDataLnr.Columns.Add("CreatedDate");
            //dtDataLnr.Columns.Add("LastUpDateBy");
            //dtDataLnr.Columns.Add("LastUpDateDate");

            #endregion

            #region ARCHIVE_MEDI_PHYS_DIST
            DataTable dtPhysdist = new DataTable();
            dtPhysdist.TableName = "ARCHIVE_MEDI_PHYS_DIST";
            dtPhysdist.Columns.Add("IDCardNo");
            dtPhysdist.Columns.Add("Mild");
            dtPhysdist.Columns.Add("OutKey");
            dtPhysdist.Columns.Add("Faint");
            dtPhysdist.Columns.Add("Yang");
            dtPhysdist.Columns.Add("Yin");
            dtPhysdist.Columns.Add("PhlegmDamp");
            dtPhysdist.Columns.Add("Muggy");
            dtPhysdist.Columns.Add("BloodStasis");
            dtPhysdist.Columns.Add("QiConstraint");
            dtPhysdist.Columns.Add("Characteristic");
            #endregion

            ds.Tables.Add(dtDataLnrTZ);
            ds.Tables.Add(dtDataLnrResult);
            ds.Tables.Add(dtDataLnrZL);
            ds.Tables.Add(dtPhysdist);

            return ds;
        }

        private static DataSet GetGxyDataSet()
        {
            DataSet ds = new DataSet();

            #region  CD_HYPERTENSION_BASEINFO

            DataTable dtDataGxy = new DataTable();
            dtDataGxy.TableName = "CD_HYPERTENSION_BASEINFO";
            dtDataGxy.Columns.Add("IDCardNo");
            //dtDataGxy.Columns.Add("OutKey");
            dtDataGxy.Columns.Add("RecordID");
            dtDataGxy.Columns.Add("ManagementGroup");
            dtDataGxy.Columns.Add("CaseOurce");
            dtDataGxy.Columns.Add("FatherHistory");
            dtDataGxy.Columns.Add("Symptom");
            dtDataGxy.Columns.Add("HypertensionComplication");
            dtDataGxy.Columns.Add("Hypotensor");
            dtDataGxy.Columns.Add("TerminateManagemen");
            dtDataGxy.Columns.Add("TerminateTime");
            dtDataGxy.Columns.Add("TerminateExcuse");

            #endregion
            ds.Tables.Add(dtDataGxy);

            #region  CD_HYPERTENSIONFOLLOWUP

            dtDataGxy = new DataTable();
            dtDataGxy.TableName = "CD_HYPERTENSIONFOLLOWUP";
            dtDataGxy.Columns.Add("IDCardNo");
            dtDataGxy.Columns.Add("FollowUpDate");
            dtDataGxy.Columns.Add("FollowUpDoctor");
            dtDataGxy.Columns.Add("NextFollowUpDate");
            dtDataGxy.Columns.Add("Symptom");
            dtDataGxy.Columns.Add("SympToMother");
            dtDataGxy.Columns.Add("Hypertension");
            dtDataGxy.Columns.Add("Hypotension");
            dtDataGxy.Columns.Add("Weight");
            dtDataGxy.Columns.Add("Hight");
            dtDataGxy.Columns.Add("BMI");
            dtDataGxy.Columns.Add("Heartrate");
            dtDataGxy.Columns.Add("PhysicalSympToMother");
            dtDataGxy.Columns.Add("DailySmokeNum");
            dtDataGxy.Columns.Add("DailyDrinkNum");
            dtDataGxy.Columns.Add("SportTimePerWeek");
            dtDataGxy.Columns.Add("SportPerMinuteTime");
            dtDataGxy.Columns.Add("EatSaltType");
            dtDataGxy.Columns.Add("EatSaltTarget");
            dtDataGxy.Columns.Add("PsyChoadJustMent");
            dtDataGxy.Columns.Add("ObeyDoctorBehavior");
            dtDataGxy.Columns.Add("AssistantExam");
            dtDataGxy.Columns.Add("MedicationCompliance");
            dtDataGxy.Columns.Add("Adr");
            dtDataGxy.Columns.Add("AdrEx");
            dtDataGxy.Columns.Add("FollowUpType");
            dtDataGxy.Columns.Add("ReferralReason");
            dtDataGxy.Columns.Add("ReferralOrg");
            dtDataGxy.Columns.Add("FollowUpWay");
            dtDataGxy.Columns.Add("WeightTarGet");
            dtDataGxy.Columns.Add("BMITarGet");
            dtDataGxy.Columns.Add("DailySmokeNumTarget");
            dtDataGxy.Columns.Add("DailyDrinkNumTarget");
            dtDataGxy.Columns.Add("SportTimeSperWeekTarget");
            dtDataGxy.Columns.Add("SportPerMinutesTimeTarget");
            dtDataGxy.Columns.Add("DoctorView");
            dtDataGxy.Columns.Add("IsReferral");
            dtDataGxy.Columns.Add("NextMeasures");
            dtDataGxy.Columns.Add("ReferralContacts");
            dtDataGxy.Columns.Add("ReferralResult");
            dtDataGxy.Columns.Add("Remarks");
            #endregion
            ds.Tables.Add(dtDataGxy);

            #region  CD_DRUGCONDITION

            dtDataGxy = new DataTable();
            dtDataGxy.TableName = "CD_DRUGCONDITION";
            dtDataGxy.Columns.Add("IDCardNo");
            dtDataGxy.Columns.Add("Type");
            dtDataGxy.Columns.Add("OutKey");
            dtDataGxy.Columns.Add("Name");
            dtDataGxy.Columns.Add("DailyTime");
            dtDataGxy.Columns.Add("EveryTimeMg");
            dtDataGxy.Columns.Add("DosAge");

            #endregion
            ds.Tables.Add(dtDataGxy);

            return ds;
        }

        private static DataSet GetTnbDataSet()
        {
            DataTable dtDataTnb = new DataTable();
            dtDataTnb.TableName = "CD_DIABETES_BASEINFO";
            #region   addColumn
            //dtDataTnb.Columns.Add("ID");
            //dtDataTnb.Columns.Add("CustomerID");
            dtDataTnb.Columns.Add("RecordID");
            dtDataTnb.Columns.Add("IDCardNo");
            //dtDataTnb.Columns.Add("OutKey");
            dtDataTnb.Columns.Add("ManagementGroup");
            dtDataTnb.Columns.Add("CaseSource");
            dtDataTnb.Columns.Add("FamilyHistory");
            dtDataTnb.Columns.Add("DiabetesType");
            dtDataTnb.Columns.Add("DiabetesTime");
            dtDataTnb.Columns.Add("DiabetesWork");
            dtDataTnb.Columns.Add("Insulin");
            dtDataTnb.Columns.Add("InsulinWeight");
            dtDataTnb.Columns.Add("EnalaprilMelete");
            dtDataTnb.Columns.Add("EndManage");
            //dtDataTnb.Columns.Add("EndWhy");
            //dtDataTnb.Columns.Add("EndTime");
            //dtDataTnb.Columns.Add("HappnTime");
            //dtDataTnb.Columns.Add("CreateUnit");
            //dtDataTnb.Columns.Add("CurrentUnit");
            //dtDataTnb.Columns.Add("CreateBy");
            //dtDataTnb.Columns.Add("CreateDate");
            //dtDataTnb.Columns.Add("LastUpdateBy");
            //dtDataTnb.Columns.Add("LastUpdateDate");
            //dtDataTnb.Columns.Add("IsDelete");
            dtDataTnb.Columns.Add("Symptom");
            dtDataTnb.Columns.Add("RenalLesionsTime");
            dtDataTnb.Columns.Add("NeuropathyTime");
            dtDataTnb.Columns.Add("HeartDiseaseTime");
            dtDataTnb.Columns.Add("RetinopathyTime");
            dtDataTnb.Columns.Add("FootLesionsTime");
            dtDataTnb.Columns.Add("CerebrovascularTime");
            //dtDataTnb.Columns.Add("LesionsOther");
            //dtDataTnb.Columns.Add("LesionsOtherTime");
            dtDataTnb.Columns.Add("Lesions");


            #endregion

            DataTable dtDataSF = new DataTable();
            dtDataSF.TableName = "CD_DIABETESFOLLOWUP";
            #region Add Columns
            
            dtDataSF.Columns.Add("IDCardNo");
            dtDataSF.Columns.Add("PBG");
            dtDataSF.Columns.Add("IsReferral");
            dtDataSF.Columns.Add("RBG");
            dtDataSF.Columns.Add("CustomerName");
            dtDataSF.Columns.Add("VisitDate");
            dtDataSF.Columns.Add("VisitDoctor");
            dtDataSF.Columns.Add("NextVisitDate");
            dtDataSF.Columns.Add("Symptom");
            dtDataSF.Columns.Add("SymptomOther");
            dtDataSF.Columns.Add("Hypertension");
            dtDataSF.Columns.Add("Hypotension");
            dtDataSF.Columns.Add("Weight");
            dtDataSF.Columns.Add("Hight");
            dtDataSF.Columns.Add("BMI");
            dtDataSF.Columns.Add("DorsalisPedispulse");
            dtDataSF.Columns.Add("PhysicalSymptomMother");
            dtDataSF.Columns.Add("DailySmokeNum");
            dtDataSF.Columns.Add("DailyDrinkNum");
            dtDataSF.Columns.Add("SportTimePerWeek");
            dtDataSF.Columns.Add("SportPerMinuteTime");
            dtDataSF.Columns.Add("StapleFooddailyg");
            dtDataSF.Columns.Add("PsychoAdjustment");
            dtDataSF.Columns.Add("ObeyDoctorBehavior");
            dtDataSF.Columns.Add("FPG");
            dtDataSF.Columns.Add("HbAlc");
            dtDataSF.Columns.Add("ExamDate");
            dtDataSF.Columns.Add("AssistantExam");
            dtDataSF.Columns.Add("MedicationCompliance");
            dtDataSF.Columns.Add("Adr");
            dtDataSF.Columns.Add("AdrEx");
            dtDataSF.Columns.Add("HypoglyceMiarreAction");
            dtDataSF.Columns.Add("VisitType");
            dtDataSF.Columns.Add("InsulinType");
            dtDataSF.Columns.Add("InsulinUsage");
            dtDataSF.Columns.Add("VisitWay");
            dtDataSF.Columns.Add("ReferralReason");
            dtDataSF.Columns.Add("ReferralOrg");
            dtDataSF.Columns.Add("TargetWeight");
            dtDataSF.Columns.Add("BMITarget");
            dtDataSF.Columns.Add("DailySmokeNumTarget");
            dtDataSF.Columns.Add("DailyDrinkNumTarget");
            dtDataSF.Columns.Add("SportTimePerWeekTarget");
            dtDataSF.Columns.Add("SportPerMinuteTimeTarget");
            //dtDataSF.Columns.Add("CreateBy");
            //dtDataSF.Columns.Add("CreateDate");
            //dtDataSF.Columns.Add("LastUpdateBy");
            //dtDataSF.Columns.Add("LastUpdateDate");
            //dtDataSF.Columns.Add("IsDelete");
            dtDataSF.Columns.Add("StapleFooddailygTarget");
            dtDataSF.Columns.Add("DoctorView");
            dtDataSF.Columns.Add("DorsalisPedispulseType");
            dtDataSF.Columns.Add("NextMeasures");
            dtDataSF.Columns.Add("ReferralContacts");
            dtDataSF.Columns.Add("ReferralResult");
            dtDataSF.Columns.Add("Remarks");
            dtDataSF.Columns.Add("InsulinAdjustType");
            dtDataSF.Columns.Add("InsulinAdjustUsage");
            #endregion
            DataTable dtYongYao = new DataTable();
            dtYongYao.TableName = "CD_DRUGCONDITION";
            #region
            dtYongYao.Columns.Add("IDCardNo");
            dtYongYao.Columns.Add("Type");
            dtYongYao.Columns.Add("OutKey");
            dtYongYao.Columns.Add("Name");
            dtYongYao.Columns.Add("DailyTime");
            dtYongYao.Columns.Add("EveryTimeMg");
            dtYongYao.Columns.Add("DosAge");
            #endregion

            DataSet ds = new DataSet();
            ds.Tables.Add(dtDataTnb);
            ds.Tables.Add(dtDataSF);
            ds.Tables.Add(dtYongYao);
            return ds;
        }
        
        private static DataSet GetJsbDataSet()
        {
            DataSet ds = new DataSet();
            DataTable dtDataJsb = new DataTable();
            dtDataJsb.TableName = "CD_MENTALDISEASE_BASEINFO";
            #region  add Columns
            //dtDataJsb.Columns.Add("ID");
            //dtDataJsb.Columns.Add("CustomerID");
            //dtDataJsb.Columns.Add("RecordID");
            dtDataJsb.Columns.Add("IDCardNo");
            dtDataJsb.Columns.Add("GuardianRecordID");
            dtDataJsb.Columns.Add("GuardianName");
            dtDataJsb.Columns.Add("Ralation");
            dtDataJsb.Columns.Add("GuradianAddr");
            dtDataJsb.Columns.Add("GuradianPhone");
            dtDataJsb.Columns.Add("FirstTime");
            dtDataJsb.Columns.Add("AgreeManagement");
            dtDataJsb.Columns.Add("AgreeSignature");
            dtDataJsb.Columns.Add("AgreeTime");
            dtDataJsb.Columns.Add("Symptom");
            dtDataJsb.Columns.Add("SymptomOther");
            dtDataJsb.Columns.Add("OutPatien");
            dtDataJsb.Columns.Add("HospitalCount");
            dtDataJsb.Columns.Add("DiagnosisInfo");
            dtDataJsb.Columns.Add("DiagnosisHospital");
            dtDataJsb.Columns.Add("DiagnosisTime");
            dtDataJsb.Columns.Add("LastCure");
            dtDataJsb.Columns.Add("VillageContacts");
            dtDataJsb.Columns.Add("VillageTel");
            dtDataJsb.Columns.Add("LockInfo");
            dtDataJsb.Columns.Add("Economy");
            dtDataJsb.Columns.Add("SpecialistProposal");
            dtDataJsb.Columns.Add("FillformTime");
            dtDataJsb.Columns.Add("DoctorMark");
            //dtDataJsb.Columns.Add("CreatedBy");
            //dtDataJsb.Columns.Add("CreatedDate");
            //dtDataJsb.Columns.Add("LastUpdateBy");
            //dtDataJsb.Columns.Add("LastUpDateDate");
            //dtDataJsb.Columns.Add("CreateUnit");
            //dtDataJsb.Columns.Add("CurrentUnit");
            //dtDataJsb.Columns.Add("IsDel");
            dtDataJsb.Columns.Add("FirstTreatmenTTime");
            dtDataJsb.Columns.Add("MildTroubleFrequen");
            dtDataJsb.Columns.Add("CreateDistuFrequen");
            dtDataJsb.Columns.Add("CauseAccidFrequen");
            dtDataJsb.Columns.Add("AutolesionFrequen");
            dtDataJsb.Columns.Add("AttemptSuicFrequen");
            dtDataJsb.Columns.Add("AttemptSuicideNone");

            #endregion
            ds.Tables.Add(dtDataJsb);

            DataTable dtDataSF = new DataTable();
            dtDataSF.TableName = "CD_MENTALDISEASE_FOLLOWUP";
            #region add columns
            //dtDataSF.Columns.Add("ID");
            //dtDataSF.Columns.Add("CustomerID");
            //dtDataSF.Columns.Add("RecordID");
            dtDataSF.Columns.Add("IDCardNo");
            dtDataSF.Columns.Add("FollowUpDate");
            dtDataSF.Columns.Add("Fatalness");
            dtDataSF.Columns.Add("PresentSymptom");
            dtDataSF.Columns.Add("PresentSymptoOther");
            dtDataSF.Columns.Add("Insight");
            dtDataSF.Columns.Add("SleepQuality");
            dtDataSF.Columns.Add("Diet");
            dtDataSF.Columns.Add("PersonalCare");
            dtDataSF.Columns.Add("Housework");
            dtDataSF.Columns.Add("ProductLaborWork");
            dtDataSF.Columns.Add("LearningAbility");
            dtDataSF.Columns.Add("SocialInterIntera");
            dtDataSF.Columns.Add("MildTroubleFrequen");
            dtDataSF.Columns.Add("CreateDistuFrequen");
            dtDataSF.Columns.Add("CauseAccidFrequen");
            dtDataSF.Columns.Add("AutolesionFrequen");
            dtDataSF.Columns.Add("AttemptSuicFrequen");
            dtDataSF.Columns.Add("AttemptSuicideNone");
            dtDataSF.Columns.Add("LockCondition");
            dtDataSF.Columns.Add("HospitalizatiStatus");
            dtDataSF.Columns.Add("LastLeaveHospTime");
            dtDataSF.Columns.Add("LaborExaminati");
            dtDataSF.Columns.Add("LaborExaminatiHave");
            dtDataSF.Columns.Add("MedicatioCompliance");
            dtDataSF.Columns.Add("AdnerDruReact");
            dtDataSF.Columns.Add("AdverDruReactHave");
            dtDataSF.Columns.Add("TreatmentEffect");
            dtDataSF.Columns.Add("WhetherReferral");
            dtDataSF.Columns.Add("ReferralReason");
            dtDataSF.Columns.Add("ReferralAgencDepar");
            dtDataSF.Columns.Add("RehabiliMeasu");
            dtDataSF.Columns.Add("RehabiliMeasuOther");
            dtDataSF.Columns.Add("FollowupClassificat");
            dtDataSF.Columns.Add("NextFollowUpDate");
            dtDataSF.Columns.Add("FollowupDoctor");
            //dtDataSF.Columns.Add("CreatedBy");
            //dtDataSF.Columns.Add("CreatedDate");
            //dtDataSF.Columns.Add("LastUpdateBy");
            //dtDataSF.Columns.Add("LastUpdateDate");
            //dtDataSF.Columns.Add("IsDel");



            #endregion
            ds.Tables.Add(dtDataSF);
            DataTable dtYongYao = new DataTable();
            dtYongYao.TableName = "CD_DRUGCONDITION";
            #region addcolumns
            dtYongYao.Columns.Add("IDCardNo");
            dtYongYao.Columns.Add("Type");
            dtYongYao.Columns.Add("Name");
            dtYongYao.Columns.Add("DailyTime");
            dtYongYao.Columns.Add("EveryTimeMg");
            dtYongYao.Columns.Add("DosAge");
            #endregion
            ds.Tables.Add(dtYongYao);
            return ds;
        }

        private static DataSet GetNczDataSet()
        {
            DataTable dtDatabase = new DataTable();
            dtDatabase.TableName = "CD_STROKE_BASEINFO";
            #region   CD_STROKE_BASEINFO

            dtDatabase.Columns.Add("RecordID");
            dtDatabase.Columns.Add("OutKey");
            dtDatabase.Columns.Add("IDCardNo");
            dtDatabase.Columns.Add("IllSource");
            dtDatabase.Columns.Add("IllTime");
            dtDatabase.Columns.Add("DiagnosisHource");
            dtDatabase.Columns.Add("Familyhistory");
            dtDatabase.Columns.Add("HosState");
            dtDatabase.Columns.Add("Mrs");
            dtDatabase.Columns.Add("GroupLevel");
            dtDatabase.Columns.Add("DangerousElement");
            dtDatabase.Columns.Add("DgrElementOther");
            dtDatabase.Columns.Add("Ct");
            dtDatabase.Columns.Add("Mri");
            dtDatabase.Columns.Add("StrokeType");
            dtDatabase.Columns.Add("StrokePosition");
            dtDatabase.Columns.Add("SelfAbility");
            dtDatabase.Columns.Add("DrugsRely");
            dtDatabase.Columns.Add("SpecialTreatment");
            dtDatabase.Columns.Add("OtherTreatment");
            dtDatabase.Columns.Add("StopManager");
            dtDatabase.Columns.Add("StopTime");
            dtDatabase.Columns.Add("StopReason");

            dtDatabase.Columns.Add("OccurTime");

            #endregion

            DataTable dtDataSF = new DataTable();
            dtDataSF.TableName = "CD_STROKE_FOLLOWUP";
            #region CD_STROKE_FOLLOWUP

            dtDataSF.Columns.Add("IDCardNo");
            dtDataSF.Columns.Add("FollowupDate");
            dtDataSF.Columns.Add("FollowupDoctor");
            dtDataSF.Columns.Add("NextFollowupDate");
            dtDataSF.Columns.Add("Symptom");
            dtDataSF.Columns.Add("SymptomOther");
            dtDataSF.Columns.Add("Hypertension");
            dtDataSF.Columns.Add("Hypotension");
            dtDataSF.Columns.Add("Weight");
            dtDataSF.Columns.Add("Height");
            dtDataSF.Columns.Add("SignOther");
            dtDataSF.Columns.Add("SmokeDrinkAttention");
            dtDataSF.Columns.Add("SportAttention");
            dtDataSF.Columns.Add("EatSaltAttention");
            dtDataSF.Columns.Add("PsychicAdjust");
            dtDataSF.Columns.Add("ObeyDoctorBehavio");
            dtDataSF.Columns.Add("AssistantExam");
            dtDataSF.Columns.Add("MedicationCompliance");
            dtDataSF.Columns.Add("Adr");
            dtDataSF.Columns.Add("AdrEx");
            dtDataSF.Columns.Add("FollowupType");
            dtDataSF.Columns.Add("ReferralReason");
            dtDataSF.Columns.Add("ReferralOrg");
            dtDataSF.Columns.Add("FollowupWay");
            dtDataSF.Columns.Add("RecordID");
            dtDataSF.Columns.Add("EatingDrug");
            dtDataSF.Columns.Add("DoctorView");
            #region 2.0 新增
            dtDataSF.Columns.Add("FollowupTypeOther");
            dtDataSF.Columns.Add("StrokeType");
            dtDataSF.Columns.Add("Strokelocation");
            dtDataSF.Columns.Add("MedicalHistory");
            dtDataSF.Columns.Add("Syndrome");
            dtDataSF.Columns.Add("SyndromeOther");
            dtDataSF.Columns.Add("NewSymptom");
            dtDataSF.Columns.Add("NewSymptomOther");
            dtDataSF.Columns.Add("SmokeDay");
            dtDataSF.Columns.Add("DrinkDay");
            dtDataSF.Columns.Add("SportWeek");
            dtDataSF.Columns.Add("SportMinute");
            dtDataSF.Columns.Add("FPGL");
            dtDataSF.Columns.Add("BMI");
            dtDataSF.Columns.Add("Waistline");
            dtDataSF.Columns.Add("LifeSelfCare");
            dtDataSF.Columns.Add("LimbRecover");
            dtDataSF.Columns.Add("RecoveryCure");
            dtDataSF.Columns.Add("RecoveryCureOther");
            #endregion
            #endregion

            DataTable dtYongYao = new DataTable();
            dtYongYao.TableName = "CD_DRUGCONDITION";
            #region CD_DRUGCONDITION
            dtYongYao.Columns.Add("IDCardNo");
            dtYongYao.Columns.Add("Type");
            dtYongYao.Columns.Add("OutKey");
            dtYongYao.Columns.Add("Name");
            dtYongYao.Columns.Add("DailyTime");
            dtYongYao.Columns.Add("EveryTimeMg");
            dtYongYao.Columns.Add("drugtype");
            dtYongYao.Columns.Add("DosAge");
            #endregion

            DataSet ds = new DataSet();
            ds.Tables.Add(dtDatabase);
            ds.Tables.Add(dtDataSF);
            ds.Tables.Add(dtYongYao);

            return ds;
        }

        private static DataSet GetGxbDataSet()
        {
            DataTable dtDataSF = new DataTable();
            dtDataSF.TableName = "CD_CHD_FOLLOWUP";
            #region CD_STROKE_FOLLOWUP

            dtDataSF.Columns.Add("RecordID");
            dtDataSF.Columns.Add("IDCardNo");
            dtDataSF.Columns.Add("Symptom");
            dtDataSF.Columns.Add("SymptomEx");
            dtDataSF.Columns.Add("Systolic");
            dtDataSF.Columns.Add("Diastolic");
            dtDataSF.Columns.Add("Weight");
            dtDataSF.Columns.Add("Height");
            dtDataSF.Columns.Add("HearVoice");
            dtDataSF.Columns.Add("HeatRate");
            dtDataSF.Columns.Add("Apex");
            dtDataSF.Columns.Add("Smoking");
            dtDataSF.Columns.Add("Sports");
            dtDataSF.Columns.Add("Salt");
            dtDataSF.Columns.Add("Action");
            dtDataSF.Columns.Add("AssistCheck");
            dtDataSF.Columns.Add("AfterPill");
            dtDataSF.Columns.Add("Compliance");
            dtDataSF.Columns.Add("Untoward");
            dtDataSF.Columns.Add("UntowardEx");
            dtDataSF.Columns.Add("FollowType");
            dtDataSF.Columns.Add("ReferralReason");
            dtDataSF.Columns.Add("ReferralDepart");
            dtDataSF.Columns.Add("NextVisitDate");
            dtDataSF.Columns.Add("VisitDoctor");
            dtDataSF.Columns.Add("VisitDate");
            dtDataSF.Columns.Add("VisitType");
            dtDataSF.Columns.Add("DoctorView");
            #region 2.0新增
            dtDataSF.Columns.Add("ChdType");
            dtDataSF.Columns.Add("BMI");
            dtDataSF.Columns.Add("FPGL");
            dtDataSF.Columns.Add("TC");
            dtDataSF.Columns.Add("TG");
            dtDataSF.Columns.Add("LowCho");
            dtDataSF.Columns.Add("HeiCho");
            dtDataSF.Columns.Add("EcgCheckResult");
            dtDataSF.Columns.Add("EcgExerciseResult");
            dtDataSF.Columns.Add("CAG");
            dtDataSF.Columns.Add("EnzymesResult");
            dtDataSF.Columns.Add("HeartCheckResult");
            dtDataSF.Columns.Add("SmokeDay");
            dtDataSF.Columns.Add("DrinkDay");
            dtDataSF.Columns.Add("SportWeek");
            dtDataSF.Columns.Add("SportMinute");
            dtDataSF.Columns.Add("SpecialTreated");
            dtDataSF.Columns.Add("NondrugTreat");
            dtDataSF.Columns.Add("Syndromeother");
           // dtDataSF.Columns.Add("DoctorView");
            dtDataSF.Columns.Add("IsReferral"); 
            #endregion
            #endregion



            DataTable dtYongYao = new DataTable();
            dtYongYao.TableName = "CD_DRUGCONDITION";
            #region CD_DRUGCONDITION
            dtYongYao.Columns.Add("IDCardNo");
            dtYongYao.Columns.Add("Type");
            dtYongYao.Columns.Add("OutKey");
            dtYongYao.Columns.Add("Name");
            dtYongYao.Columns.Add("DailyTime");
            dtYongYao.Columns.Add("EveryTimeMg");
            dtYongYao.Columns.Add("drugtype");
            dtYongYao.Columns.Add("DosAge");
            #endregion

            DataSet ds = new DataSet();

            ds.Tables.Add(dtDataSF);
            ds.Tables.Add(dtYongYao);

            return ds;
        }

        private static DataSet GetYfDataSet()
        {
            DataTable dtDataBase = new DataTable();
            dtDataBase.TableName = "GRAVIDA_BASEINFO";

            dtDataBase.Columns.Add("RecordID");
            dtDataBase.Columns.Add("IDCardNo");
            dtDataBase.Columns.Add("Name");
            dtDataBase.Columns.Add("Age");
            dtDataBase.Columns.Add("Culture");
            dtDataBase.Columns.Add("Job");
            //dtDataBase.Columns.Add("Address");
            //dtDataBase.Columns.Add("Nation");
            dtDataBase.Columns.Add("Birthday");
            //dtDataBase.Columns.Add("Living");
            //dtDataBase.Columns.Add("Phone");
            //dtDataBase.Columns.Add("HealthResot");
            dtDataBase.Columns.Add("TownName");
            //dtDataBase.Columns.Add("VillageName");
            dtDataBase.Columns.Add("PwPhone");
            dtDataBase.Columns.Add("HusbandName");
            dtDataBase.Columns.Add("HusbandPhone");
            dtDataBase.Columns.Add("HouseholdTown");
            //dtDataBase.Columns.Add("HouseholdVillage");
            dtDataBase.Columns.Add("AddrTown");
            //dtDataBase.Columns.Add("AddrVillage");
            dtDataBase.Columns.Add("AddrPhone");
            dtDataBase.Columns.Add("WorkUnit");
            dtDataBase.Columns.Add("UnitPhone");
            dtDataBase.Columns.Add("HusbandAge");
            dtDataBase.Columns.Add("HusbandCulture");
            dtDataBase.Columns.Add("HusbandNation");
            dtDataBase.Columns.Add("HusbandUnit");
            dtDataBase.Columns.Add("HbUnitPhone");
            dtDataBase.Columns.Add("HusbandJob");
            dtDataBase.Columns.Add("CardNum");
            dtDataBase.Columns.Add("CreateDate");


            DataTable dtDataSF1 = new DataTable();
            dtDataSF1.TableName = "GRAVIDA_FIRSTFOLLOWUP";
            dtDataSF1.Columns.Add("RecordID");
            dtDataSF1.Columns.Add("IDCardNo");
            dtDataSF1.Columns.Add("RecordDate");
            dtDataSF1.Columns.Add("PregancyWeeks");
            dtDataSF1.Columns.Add("CustomerAge");
            dtDataSF1.Columns.Add("HusbandName");
            dtDataSF1.Columns.Add("HusbandAge");
            dtDataSF1.Columns.Add("HusbandPhone");
            dtDataSF1.Columns.Add("PregancyCount");
            dtDataSF1.Columns.Add("NatrualChildBirthCount");
            dtDataSF1.Columns.Add("CaeSareanCount");
            dtDataSF1.Columns.Add("LastMenStruation");
            dtDataSF1.Columns.Add("LastMenStruationDate");
            dtDataSF1.Columns.Add("ExpectedDueDate");
            dtDataSF1.Columns.Add("CustomerHistory");
            dtDataSF1.Columns.Add("CustomerHistoryEx");
            dtDataSF1.Columns.Add("FamilyHistory");
            dtDataSF1.Columns.Add("FamilyHistoryEx");
            dtDataSF1.Columns.Add("PersonalHistory");
            dtDataSF1.Columns.Add("PersonalHistoryEx");
            dtDataSF1.Columns.Add("GyNecoloGyHistory");
            dtDataSF1.Columns.Add("AbortionInfo");
            dtDataSF1.Columns.Add("Deadfetus");
            dtDataSF1.Columns.Add("StillBirthInfo");
            dtDataSF1.Columns.Add("NewBornDead");
            dtDataSF1.Columns.Add("NewBornDefect");
            dtDataSF1.Columns.Add("Height");
            dtDataSF1.Columns.Add("Weight");
            dtDataSF1.Columns.Add("Bmi");
            dtDataSF1.Columns.Add("HBloodpressure");
            dtDataSF1.Columns.Add("LBloodpressure");
            dtDataSF1.Columns.Add("Heart");
            dtDataSF1.Columns.Add("Heartex");
            dtDataSF1.Columns.Add("Lung");
            dtDataSF1.Columns.Add("Lungex");
            dtDataSF1.Columns.Add("Vulva");
            dtDataSF1.Columns.Add("VulvaEx");
            dtDataSF1.Columns.Add("Vagina");
            dtDataSF1.Columns.Add("VaginaEx");
            dtDataSF1.Columns.Add("CervixuTeri");
            dtDataSF1.Columns.Add("CervixuTeriex");
            dtDataSF1.Columns.Add("Corpus");
            dtDataSF1.Columns.Add("CorpusEx");
            dtDataSF1.Columns.Add("Attach");
            dtDataSF1.Columns.Add("AttachEx");
            dtDataSF1.Columns.Add("OverAlassessMent");
            dtDataSF1.Columns.Add("HealthZhiDao");
            dtDataSF1.Columns.Add("HealthZhiDaoOthers");
            dtDataSF1.Columns.Add("Referral");
            dtDataSF1.Columns.Add("ReferralReason");
            dtDataSF1.Columns.Add("ReferralOrg");
            dtDataSF1.Columns.Add("NextfollowupDate");
            dtDataSF1.Columns.Add("FollowupDoctor");
            dtDataSF1.Columns.Add("GynecologyHistoryEx");
            dtDataSF1.Columns.Add("OverAlassessmentEx");

            DataTable dtDataSF25 = new DataTable();
            dtDataSF25.TableName = "GRAVIDA_TWO2FIVE_FOLLOWUP";
            dtDataSF25.Columns.Add("RecordID");
            dtDataSF25.Columns.Add("IDCardNo");
            dtDataSF25.Columns.Add("Times");
            dtDataSF25.Columns.Add("FollowupDate");
            dtDataSF25.Columns.Add("PregancyWeeks");
            dtDataSF25.Columns.Add("ChiefComPlaint");
            dtDataSF25.Columns.Add("Weight");
            dtDataSF25.Columns.Add("UteruslowHeight");
            dtDataSF25.Columns.Add("AbdominalCirumference");
            dtDataSF25.Columns.Add("FetusPosition");
            dtDataSF25.Columns.Add("FHR");
            dtDataSF25.Columns.Add("HBloodPressure");
            dtDataSF25.Columns.Add("LBloodPressure");
            dtDataSF25.Columns.Add("HB");
            dtDataSF25.Columns.Add("AssistanTexam");
            dtDataSF25.Columns.Add("Classification");
            dtDataSF25.Columns.Add("ClassificationEx");
            dtDataSF25.Columns.Add("Advising");
            dtDataSF25.Columns.Add("AdvisingOther");
            dtDataSF25.Columns.Add("Referral");
            dtDataSF25.Columns.Add("ReferralReason");
            dtDataSF25.Columns.Add("ReferralOrg");
            dtDataSF25.Columns.Add("NextFollowupDate");
            dtDataSF25.Columns.Add("FollowupDoctor");
            dtDataSF25.Columns.Add("PRO");

            DataTable dtDataCheck = new DataTable();
            dtDataCheck.TableName = "GRAVIDA_PRE_ASSISTCHECK";

            dtDataCheck.Columns.Add("RecordID");
            dtDataCheck.Columns.Add("IDCardNo");
            dtDataCheck.Columns.Add("HB");
            dtDataCheck.Columns.Add("WBC");
            dtDataCheck.Columns.Add("PlT");
            dtDataCheck.Columns.Add("BloodOther");
            dtDataCheck.Columns.Add("PRO");
            dtDataCheck.Columns.Add("GLU");
            dtDataCheck.Columns.Add("KET");
            dtDataCheck.Columns.Add("BLD");
            dtDataCheck.Columns.Add("UrineOthers");
            dtDataCheck.Columns.Add("BloodType");
            dtDataCheck.Columns.Add("RH");
            dtDataCheck.Columns.Add("FPGL");
            dtDataCheck.Columns.Add("SGPT");
            dtDataCheck.Columns.Add("GOT");
            dtDataCheck.Columns.Add("BP");
            dtDataCheck.Columns.Add("TBIL");
            dtDataCheck.Columns.Add("CB");
            dtDataCheck.Columns.Add("SCR");
            dtDataCheck.Columns.Add("BUN");
            dtDataCheck.Columns.Add("VaginalSecretions");
            dtDataCheck.Columns.Add("VaginalSecretionSothers");
            dtDataCheck.Columns.Add("VaginalCleaess");
            dtDataCheck.Columns.Add("HBSAG");
            dtDataCheck.Columns.Add("HBSAB");
            dtDataCheck.Columns.Add("HBEAG");
            dtDataCheck.Columns.Add("HBEAB");
            dtDataCheck.Columns.Add("HBCAB");
            dtDataCheck.Columns.Add("LUES");
            dtDataCheck.Columns.Add("HIV");
            dtDataCheck.Columns.Add("BCHAO");

            DataTable dtDataV = new DataTable();
            dtDataV.TableName = "GRAVIDA_POSTPARTUM";
            dtDataV.Columns.Add("RecordID");
            dtDataV.Columns.Add("IDCardNo");
            dtDataV.Columns.Add("FollowupDate");
            dtDataV.Columns.Add("Tem");
            dtDataV.Columns.Add("HealthCondition");
            dtDataV.Columns.Add("Mentalcondition");
            dtDataV.Columns.Add("HBbloodPressure");
            dtDataV.Columns.Add("LBloodPressure");
            dtDataV.Columns.Add("Breast");
            dtDataV.Columns.Add("BreastEx");
            dtDataV.Columns.Add("Lochia");
            dtDataV.Columns.Add("LochiaEx");
            dtDataV.Columns.Add("Uterus");
            dtDataV.Columns.Add("UterusEx");
            dtDataV.Columns.Add("Wound");
            dtDataV.Columns.Add("WoundEx");
            dtDataV.Columns.Add("Other");
            dtDataV.Columns.Add("Classification");
            dtDataV.Columns.Add("ClassificationEx");
            dtDataV.Columns.Add("Advising");
            dtDataV.Columns.Add("AdvisingOther");
            dtDataV.Columns.Add("Referral");
            dtDataV.Columns.Add("ReferralReason");
            dtDataV.Columns.Add("ReferralOrg");
            dtDataV.Columns.Add("NextFollowupDate");
            dtDataV.Columns.Add("FollowupDoctor");

            DataTable dtDataV42 = new DataTable();
            dtDataV42.TableName = "GRAVIDA_POSTPARTUM_42DAY";
            dtDataV42.Columns.Add("RecordID");
            dtDataV42.Columns.Add("IDCardNo");
            dtDataV42.Columns.Add("FollowupDate");
            dtDataV42.Columns.Add("Healthcondition");
            dtDataV42.Columns.Add("Mentalcondition");
            dtDataV42.Columns.Add("Hbloodpressure");
            dtDataV42.Columns.Add("LBloodPressure");
            dtDataV42.Columns.Add("Breast");
            dtDataV42.Columns.Add("BreastEx");
            dtDataV42.Columns.Add("Lochia");
            dtDataV42.Columns.Add("LochiaEx");
            dtDataV42.Columns.Add("Uterus");
            dtDataV42.Columns.Add("UterusEx");
            dtDataV42.Columns.Add("Wound");
            dtDataV42.Columns.Add("WoundEx");
            dtDataV42.Columns.Add("Other");
            dtDataV42.Columns.Add("Classification");
            dtDataV42.Columns.Add("ClassificationEx");
            dtDataV42.Columns.Add("Advising");
            dtDataV42.Columns.Add("AdvisingOther");
            dtDataV42.Columns.Add("Treat");
            dtDataV42.Columns.Add("ReferralReason");
            dtDataV42.Columns.Add("ReferralOrg");
            dtDataV42.Columns.Add("NextFollowupDate");
            dtDataV42.Columns.Add("FollowupDoctor");

            DataSet ds = new DataSet();

            ds.Tables.Add(dtDataBase);
            ds.Tables.Add(dtDataSF1);
            ds.Tables.Add(dtDataSF25);
            ds.Tables.Add(dtDataCheck);
            ds.Tables.Add(dtDataV);
            ds.Tables.Add(dtDataV42);

            return ds;
        }
    }
}
