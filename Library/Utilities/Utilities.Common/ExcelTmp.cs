using System.Collections.Generic;

namespace Utilities.Common
{
    public static class ExcelTmp
    {
        public static Dictionary<string, string> GetTmpColumn(string tmpName)
        {
            switch (tmpName)
            {
                case "grda":

                    return GrdaColumn();
                case "tj":

                    return TjColumn();

                case "tj_zys":

                    return Tj_ZYSColumn();

                case "tj_jtbcs":

                    return Tj_JTBCSColumn();

                // 主要用药情况
                case "tj_zyyyqk":

                    return Tj_ZYYYQKColumn();

                // 非免疫规划接种史
                case "tj_fmyghjzs":

                    return Tj_FMYGHJZSColumn();

                // 老年人
                case "lnr":

                    return LnrColumn();
                // 老年人自理
                case "lnr_zl":

                    return LnrZLColumn();

                // 高血压管理卡
                case "gxy_glk":

                    return GxyGLKColumn();
                
                // 高血压随访
                case "gxy_sf":

                    return GxySFColumn();

                default:
                    break;
            }

            return new Dictionary<string, string>();
        }

        /// <summary>
        /// 个人档案
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> GrdaColumn()
        {
            Dictionary<string, string> dycolumns = new Dictionary<string, string>();

            dycolumns.Add("证件编号", "证件编号");
            dycolumns.Add("relation", "与户主关系");
            dycolumns.Add("档案状态", "档案状态");
            dycolumns.Add("memberName", "姓名");
            dycolumns.Add("gender", "性别");
            dycolumns.Add("birthday", "出生日期");
            dycolumns.Add("phone", "本人电话");
            dycolumns.Add("workUnit", "工作单位");
            dycolumns.Add("connectPhone", "联系人电话");
            dycolumns.Add("connectName", "联系人姓名");
            dycolumns.Add("houseFlag", "常住类型");
            dycolumns.Add("nation", "民族");
            dycolumns.Add("bloodType", "血型");
            dycolumns.Add("bloodHR", "RH");
            dycolumns.Add("memberProfession", "职业");
            dycolumns.Add("culturdegree", "文化程度");
            dycolumns.Add("劳动程度", "劳动程度");
            dycolumns.Add("marriageStatus", "婚姻状况");
            dycolumns.Add("medicalPayWay", "医疗费用支付方式");
            dycolumns.Add("medicalCardNo", "医疗保险号");
            dycolumns.Add("新农合号", "新农合号");
            dycolumns.Add("houseHoldAddress", "居住地址");
            dycolumns.Add("address", "详细地址");
            dycolumns.Add("所属片区", "所属片区");
            dycolumns.Add("archiveType", "档案类别");
            dycolumns.Add("药物过敏史--有无", "药物过敏史--有无");
            dycolumns.Add("medicineAllergicHis", "药物过敏史");
            dycolumns.Add("medicineAllergicHisCN", "药物过敏史--其他");
            dycolumns.Add("既往史--疾病有无", "既往史--疾病有无");
            dycolumns.Add("既往史--疾病", "既往史--疾病");
            dycolumns.Add("既往史--手术有无", "既往史--手术有无");
            dycolumns.Add("既往史--手术", "既往史--手术");
            dycolumns.Add("既往史--外伤有无", "既往史--外伤有无");
            dycolumns.Add("既往史--外伤", "既往史--外伤");
            dycolumns.Add("既往史--输血有无", "既往史--输血有无");
            dycolumns.Add("既往史--输血", "既往史--输血");
            dycolumns.Add("家族史--有无", "家族史--有无");
            dycolumns.Add("家族史", "家族史");
            dycolumns.Add("暴露史--有无", "暴露史--有无");
            dycolumns.Add("暴露史--化学品", "暴露史--化学品");
            dycolumns.Add("暴露史--毒物", "暴露史--毒物");
            dycolumns.Add("暴露史--射线", "暴露史--射线");
            dycolumns.Add("heredityDisease", "遗传病史--有无");
            dycolumns.Add("heredityDiseaseCN", "遗传病史--疾病名称");
            dycolumns.Add("残疾状况--有无", "残疾状况--有无");
            dycolumns.Add("残疾状况", "残疾状况");
            dycolumns.Add("disableStatusCN", "残疾状况--其他");
            dycolumns.Add("personKitchenAir", "生活环境--厨房排风设施");
            dycolumns.Add("personFuel", "生活环境--燃料类型");
            dycolumns.Add("personWaterType", "生活环境--饮水");
            dycolumns.Add("toilet", "生活环境--厕所");
            dycolumns.Add("personLivestock", "禽畜栏");
            dycolumns.Add("调查时间", "调查时间");
            dycolumns.Add("setupDate", "录入时间");
            dycolumns.Add("setupPerson", "录入人");
            dycolumns.Add("updateDate", "最近更新时间");
            dycolumns.Add("updateAuthor", "最近修改人");
            dycolumns.Add("department", "当前所属机构");

            return dycolumns;
        }

        /// <summary>
        /// 体检信息-健康体检信息
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> TjColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            // 身份证号
            temd.Add("idNumber", "身份证号-IDCARD");

            temd.Add("checkDate", "体检日期-CHECKDATE");

            temd.Add("doctorCN", "责任医生-DOCTOR");


            //temd.Add("examSymptom", "一般情况-症状");
            //temd.Add("examSymptomCN", "一般情况-症状CN");

            temd.Add("examSymptom", "症状-SYMPTOM");
            temd.Add("examSymptomCN", "症状其他-SYMPTOMOTHER");


            temd.Add("temprature", "一般情况-体温-TEM");
            temd.Add("pulseRate", "一般情况-脉率-PULSERATE");
            temd.Add("breathingRate", "一般情况-呼吸频率-BREATH_RATE");
            temd.Add("leftDiastolic", "一般情况-血压-左侧-LEFTPRE");
            temd.Add("leftSystolic", "一般情况-血压-左侧-LEFTHEIGHT");
            temd.Add("rightDiastolic", "一般情况-血压-右侧-RIGHTPRE");
            temd.Add("rightSystolic", "一般情况-血压-右侧-RIGHTHEIGHT");


            temd.Add("一般情况-血压-左侧原因-LEFTRESION", "一般情况-血压-左侧原因-LEFTRESION");
            temd.Add("一般情况-血压-右侧原因-RIGHTRESION", "一般情况-血压-右侧原因-RIGHTRESION");

            temd.Add("height", "一般情况-身高-HEIGHT");
            temd.Add("weight", "一般情况-体重-WEIGHT");
            temd.Add("waistLine", "一般情况-腰围-WAISTLINE");
            temd.Add("physicalIndex", "一般情况-体重指数(BMI)-BMI");

            temd.Add("老年人健康状态自我评估-OLD_HEALTHSTATUS", "老年人健康状态自我评估-OLD_HEALTHSTATUS");
            temd.Add("老年人生活自理能力自我评估-OLD_SELFCAREABILITY", "老年人生活自理能力自我评估-OLD_SELFCAREABILITY");
            temd.Add("老年人认知能力-OLD_RECOGNISE", "老年人认知能力-OLD_RECOGNISE");

            temd.Add("简易智力状态检查-INTELSCORE，总分", "简易智力状态检查-INTELSCORE，总分");
            temd.Add("老年人情感状态-OLD_EMOTION", "老年人情感状态-OLD_EMOTION");
            temd.Add("老年人抑郁评分检查，总分-GLOOMYSCORE", "老年人抑郁评分检查，总分-GLOOMYSCORE");

            temd.Add("execiseFrequency", "锻炼频率-EXERCISERATE");
            temd.Add("execiseTime", "每次锻炼时间-EXERCISETIME");
            temd.Add("upholdExeciseTime", "坚持锻炼时间-EXCISEPERSISTTIME");
            temd.Add("execiseType", "锻炼方式-EXERCISEEXISTENSE");
            temd.Add("ysxg", "饮食习惯-EATHOBBY");
            //temd.Add("ysxgCN", "饮食习惯CN");


            temd.Add("smokeStatus", "吸烟状况-SMOKECONDITION");
            temd.Add("smokingPerDay", "日吸烟量-SMOKEDAYNUM");
            temd.Add("smokingStartAge", "开始吸烟年龄-SMOKEAGESTART");
            temd.Add("endSmokingAge", "戒烟年龄-SMOKEAGEFORBIDDON");
            temd.Add("drinkFrequency", "饮酒频率-DRINKRATE");
            temd.Add("drinkingPerday", "日饮酒量-DAYDRINKVOLUME");
            temd.Add("isNotDrink", "是否戒酒-ISDRINKFORBIDDON");
            temd.Add("isNotDrinkCN", "戒酒年龄-FORBIDDONAGE");
            temd.Add("drinkingStartAge", "开始饮酒年龄-DRINKSTARTAGE");


            temd.Add("drinkingFlag", "近一年内是否曾醉酒-DRINKTHISYEAR");


            temd.Add("everyDrinkType", "饮酒种类-DRINKTYPE");
            temd.Add("everyDrinkTypeCN", "饮酒种类其他-DRINKTYPEOTHER");

            temd.Add("professionDiscover", "职业病危害因素接触史-CAREERHARMFACTORHISTORY");
            temd.Add("profession", "工种-WORKTYPE");
            temd.Add("workDate", "工作时间-WORKTIME");

            temd.Add("dust", "粉尘-DUST");
            //temd.Add("dustProtect", "职业病-粉尘-有无防护");
            //temd.Add("dustProtectCN", "职业病-粉尘防护措施");

            temd.Add("dustProtect", "防护措施-DUSTPROTECT");
            temd.Add("dustProtectCN", "防护措施有-DUSTPROTECT_EX");

            temd.Add("radial", "放射物质-FANGSHE");
            temd.Add("radialProtect", "防护措施-FANGSHEPROTECT");
            temd.Add("radialProtectCN", "防护措施有-FANGSHEPROTECT_EX");


            temd.Add("physicalReason", "物理因素-PHYSICAL");
            temd.Add("physicalReasonProtect", "防护措施-PHYSICALPROTECT");
            temd.Add("physicalReasonProtectCN", "防护措施有-PHYSICALPROTECT_EX");

            temd.Add("chemical", "化学物质-CHEM");
            temd.Add("chemicalProtect", "防护措施-CHEMPROTECT");
            temd.Add("chemicalProtectCN", "防护措施有-CHEMPROTECT_EX");


            temd.Add("otherHazard", "其他-PROTECTOTHER");
            temd.Add("otherHazardProtect", "防护措施-OTHERPROTECT");
            temd.Add("otherHazardProtectCN", "防护措施有-OTHERPROTECT_EX");


            temd.Add("lip", "口唇-LIPS");
            temd.Add("口唇其他-LIPSOTHER", "口唇其他-LIPSOTHER");

            /*
            齿列1:正常,2:缺齿,3:龋齿,4:义齿(假牙)
            temd.Add("isTeethNormal", "齿列是否正常");
            temd.Add("isDecayedTooth", "龋牙");
            temd.Add("isLosetEeth", "缺牙");
            temd.Add("isFalseTeeth", "义牙");
            */
            temd.Add("齿列-正常-TOOTHRESIDES", "齿列-正常-TOOTHRESIDES");

            temd.Add("isTeethNormal", "齿列是否正常");

            temd.Add("isLosetEeth", "是否缺牙");
            temd.Add("allFalseLoset", "全缺齿");
            temd.Add("loseTeethNum", "缺牙-左上");
            temd.Add("loseLeftDownTeethNum", "缺牙-左下");
            temd.Add("loseRightUpTeethNum", "缺牙-右上");
            temd.Add("loseRightDownTeethNum", "缺牙-右下");

            temd.Add("isDecayedTooth", "是否龋牙");
            temd.Add("decayedLeftDownToothNum", "龋牙-左下");
            temd.Add("decayedRightUpToothNum", "龋牙-右上");
            temd.Add("decayedRightDownToothNum", "龋牙-右下");

            temd.Add("isFalseTeeth", "是否义牙(假牙)");
            temd.Add("allFalseTeeth", "全义牙(假牙)");
            temd.Add("falseTeethNum", "义牙-左上");
            temd.Add("falseLeftDownTeethNum", "义牙-左下");
            temd.Add("falseRightUpTeethNum", "义牙-右上");
            temd.Add("falseRightDownTeethNum", "义牙-右下");

            /*temd.Add("allFalseLoset", "齿列");//？
           //补充allFalseLoset 全缺齿
           //补充allFalseTeeth 全假牙
           temd.Add("isDecayedTooth", "龋牙");
           temd.Add("decayedToothNum", "龋牙-左上");
           temd.Add("decayedLeftDownToothNum", "龋牙-左下");
           temd.Add("decayedRightUpToothNum", "龋牙-右上");
           temd.Add("decayedRightDownToothNum", "龋牙-右下");

           temd.Add("isFalseTeeth", "义牙");
           temd.Add("falseTeethNum", "义牙-左上");
           temd.Add("falseLeftDownTeethNum", "义牙-左下");
           temd.Add("falseRightUpTeethNum", "义牙-右上");
           temd.Add("falseRightDownTeethNum", "义牙-右下");

           temd.Add("isLosetEeth", "缺牙");
           temd.Add("loseTeethNum", "缺牙-左上");
           temd.Add("loseLeftDownTeethNum", "缺牙-左下");
           temd.Add("loseRightUpTeethNum", "缺牙-右上");
           temd.Add("loseRightDownTeethNum", "缺牙-右下");*/


            temd.Add("其他-TOOTHRESIDESOTHER", "其他-TOOTHRESIDESOTHER");

            temd.Add("pharynx", "咽部-PHARYNGEAL");

            temd.Add("咽部其他-PHARYNGEALOTHER", "咽部其他-PHARYNGEALOTHER");

            temd.Add("leftEyesight", "左眼-LEFTVIEW");
            temd.Add("rightEyesight", "右眼-RIGHTVIEW");
            temd.Add("leftRecEyesight", "视 力：左眼矫正-LEFTEYECORRECT");
            temd.Add("rightRecEyesight", "视 力：右眼矫正-RIGHTEYECORRECT");
            temd.Add("audition", "听 力-LISTEN");
            temd.Add("sportFunc", "运动功能-SPORTFUNCTION");
            temd.Add("eyeGround", "眼 底-EYEROUND");
            temd.Add("eyeGroundCN", "眼底异常原因-EYEROUND_EX");
            temd.Add("cutis", "皮 肤-SKIN");
            temd.Add("cutisCN", "皮肤其他-SKINOTHER");
            temd.Add("sclera", "巩 膜-SCLERA");
            temd.Add("scleraCN", "巩膜其他-SCLERAOTHER");
            temd.Add("lymph", "淋巴结-LYMPH");
            temd.Add("lymphCN", "淋巴结其他-LYMPHOTHER");
            temd.Add("bucketChest", "桶状胸-BARRELCHEST");
            temd.Add("breathSound", "呼吸音-BREATHSOUNDS");
            temd.Add("breathSoundCN", "呼吸音异常-BREATHSOUNDS_EX");

            temd.Add("rale", "罗音-RALE");
            temd.Add("raleCN", "罗音异常-RALE_EX");

            //temd.Add("cardiacRate1", "心率1");
            //temd.Add("cardiacRate2", "心率2");
            //temd.Add("cardiacRate3", "心率3");
            //temd.Add("cardiacRate4", "心率4");

            temd.Add("cardiacRate1", "心率-HEARTRATE");
            temd.Add("heartRate", "心律-HEARTRHYTHM");

            //temd.Add("heartRate", "心律");//心律

            temd.Add("murmur", "杂音-NOISE");
            temd.Add("murmurCN", "杂音异常-NOISE_EX");

            temd.Add("tenderness", "压痛-PRESSPAIN");
            temd.Add("tendernessCN", "压痛异常-PRESSPAIN_EX");
            temd.Add("masses", "包块-ENCLOSEDMASS");
            temd.Add("massesCN", "包块异常-ENCLOSEDMASS_EX");
            temd.Add("hepatauxe", "肝大-LIVER");
            temd.Add("hepatauxeCN", "肝大异常-LIVER_EX");
            temd.Add("splenomegaly", "脾大-SPLEEN");
            temd.Add("splenomegalyCN", "脾大异常-SPLEEN_EX");
            temd.Add("sonant", "移动性浊音-VOICED");
            temd.Add("sonantCN", "移动性浊音异常-VOICED_EX");

            temd.Add("edemalowErextremity", "下肢水肿-EDEMA");//？

            temd.Add("footArteriopalmus", "足背动脉搏动-FOOTBACK");
            temd.Add("anusTactus", "肛门指诊-ANUS");
            temd.Add("anusTactusCN", "肛门指诊其他-ANUS_EX");


            temd.Add("乳腺", "乳腺");
            temd.Add("乳腺其他-BREAST_EX", "乳腺其他-BREAST_EX");

            temd.Add("外阴-VULVA", "外阴-VULVA");
            temd.Add("外阴异常-VULVA_EX", "外阴异常-VULVA_EX");

            temd.Add("阴道-VAGINA", "阴道-VAGINA");
            temd.Add("阴道异常-VAGINA_EX", "阴道异常-VAGINA_EX");

            temd.Add("宫颈-CERVIXUTERI", "宫颈-CERVIXUTERI");
            temd.Add("宫颈异常-CERVIXUTERI_EX", "宫颈异常-CERVIXUTERI_EX");


            temd.Add("宫体-CORPUS", "宫体-CORPUS");
            temd.Add("宫体异常-CORPUS_EX", "宫体异常-CORPUS_EX");

            temd.Add("附件-ATTACH", "附件-ATTACH");
            temd.Add("附件异常-ATTACH_EX", "附件异常-ATTACH_EX");
            temd.Add("其他-ATTACHOTHER", "其他-ATTACHOTHER");

            temd.Add("cruarin", "血红蛋白-HB");
            temd.Add("leucocyte", "白细胞-WBC");
            temd.Add("heamatoblast", "血小板-PLT");
            temd.Add("otherBlood", "血常规其他-BLOOD_OTHER");
            temd.Add("urineProtein", "尿蛋白-PRO"); //
            temd.Add("urineSugar", "尿糖-GLU");
            temd.Add("urineAcetone", "尿酮体-KET");
            temd.Add("urineBlood", "尿潜血");
            temd.Add("otherUrine", "尿常规其他-URINE_OTHERS");

            /*temd.Add("fastingBloodGlucose1", "空腹血糖1");
            temd.Add("fastingBloodGlucose2", "空腹血糖2");
            temd.Add("afterMealBloodGlucose1", "餐后血糖1");
            temd.Add("afterMealBloodGlucose2", "餐后血糖2");*/

            temd.Add("fastingBloodGlucose2", "空腹血糖-FPGL");
            temd.Add("afterMealBloodGlucose2", "餐后2H血糖-FPGDL");
            temd.Add("electrocardiogram", "心电图-ECG");
            temd.Add("electrocardiogramCN", "心电图异常-ECG_EX");
            temd.Add("urineAlbumin", "尿微量白蛋白-ALBUMIN");
            temd.Add("fecalOccultBlood", "大便潜血-FOB");
            temd.Add("glycosylatedHemoglobin", "糖化血红蛋白-HBALC");
            temd.Add("hepatitisAntigen", "乙型肝炎表面抗原-HBSAG");
            temd.Add("sgpt", "血清谷丙转氨酶-SGPT");
            temd.Add("sgot", "血清谷草转氨酶-GOT");
            temd.Add("albumin", "白蛋白-BP");
            temd.Add("totalBilirubin", "总胆红素-TBIL");
            temd.Add("conjugatedBilirubin", "结合胆红素-CB");
            temd.Add("serumCreatinine", "血清肌酐-SCR");
            temd.Add("bloodUreanitrogen", "血尿素氮-BUN");
            temd.Add("bloodKalium", "血钾浓度-PC");
            temd.Add("bloodNatrium", "血钠浓度-HYPE");
            temd.Add("cholesterolTotal", "总胆固醇-TC");
            temd.Add("glycerinTrilaurate", "甘油三酯-TG");
            temd.Add("lowDensityCholesterin", "血清低密度脂蛋白胆固醇-LOW_CHO");
            temd.Add("highDensityCholesterin", "血清高密度脂蛋白胆固醇-HEI_CHO");
            temd.Add("chestX", "胸部X线片-CHESTX");
            temd.Add("chestXCN", "胸部X线片异常-CHESTX_EX");
            temd.Add("bultrasonic", "B超-BCHAO");
            temd.Add("bultrasonicCN", "BB超异常-BCHAO_EX");
            temd.Add("宫颈涂片-CERVIX", "宫颈涂片-CERVIX");
            temd.Add("宫颈涂片异常-CERVIX_EX", "宫颈涂片异常-CERVIX_EX");
            temd.Add("其他-CERVIXOTHER", "其他-CERVIXOTHER");
            //  phlegmaticHygrosisPhy
            temd.Add("gentlePhysique", "平和质-MILD");
            //suffocatingphy
            temd.Add("deficiencyPhysique", "气虚质-FAINT");
            //temd.Add("yangdeficienphy", "阳虚质-YANG");
            temd.Add("yangDeficiencyPhy", "阳虚质-YANG");
            temd.Add("yinDeficiencyPhy", "阴虚质-YIN");
            //phlegmaticHygrosisPhy
            temd.Add("phlegmaticHygrosisPhy", "痰湿质-PHLEGMDAMP");
            //muggyphy
            temd.Add("muggyphy", "湿热质-MUGGY");
            //bloodsasisphy
            temd.Add("bloodsasisphy", "血瘀质-BLOODSTASIS");
            //suffocatingphy
            temd.Add("suffocatingphy", "气郁质-QICONSTRAINT");
            //
            temd.Add("specialPhy", "特秉质-CHARACTERISTIC");
            temd.Add("cerebrovascular", "脑血管疾病-BRAIN_DIS");
            temd.Add("cerebrovascularCN", "脑血管疾病其他-BRAIN_OTHER");
            temd.Add("kidneyDisease", "肾脏疾病-RENAL_DIS");
            temd.Add("kidneyDiseaseCN", "肾脏疾病其他-RENAL_OTHER");
            temd.Add("heartDisease", "心脏疾病-HEART_DIS");
            temd.Add("heartDiseaseCN", "心脏疾病其他-HEART_OTHER");

            temd.Add("vesselDisease", "血管疾病-VESSEL_DIS");
            temd.Add("vesselDiseaseCN", "血管疾病其他-VESSEL_OTHER");


            temd.Add("eyeDisease", "眼部疾病-EYE_DIS");
            temd.Add("eyeDiseaseCN", "眼部疾病其他-EYE_OTHER");
            temd.Add("nerveDisease", "神经系统疾病-NERVE_DIS");
            temd.Add("nerveDiseaseCN", "神经系统疾病其他-NERVE_DIS_OTHER");
            temd.Add("otherDisease", "其他系统疾病-ELSE_DIS");
            temd.Add("otherDiseaseCN", "其他系统疾病其他-ELSE_DIS_OTHER");


            temd.Add("住院史-HOSPITALHISTORY", "住院史-HOSPITALHISTORY");
            temd.Add("家庭病床史-FAMILYBEDHISTORY", "家庭病床史-FAMILYBEDHISTORY");
            temd.Add("主要用药情况-MEDICATION", "主要用药情况-MEDICATION");
            temd.Add("非免疫规划预防接种史-INOCULATIONHISTORY", "非免疫规划预防接种史-INOCULATIONHISTORY");

            temd.Add("healthEvaluate", "健康评价-ISNORMAL");
            temd.Add("healthAbnormity1", "异常1-EXCEPTION1");
            temd.Add("healthAbnormity2", "异常2-EXCEPTION2");
            temd.Add("healthAbnormity3", "异常3-EXCEPTION3");
            temd.Add("healthAbnormity4", "异常4-EXCEPTION4");


            // temd.Add("家庭病床史", "家庭病床史");

            temd.Add("healthGuide", "健康指导-HEALTHZHIDAO");
            //temd.Add("healthGuideCN", "健康指导CN");


            temd.Add("危险因素控制-DANGERCONTROL", "危险因素控制-DANGERCONTROL"); //value凭借
            //戒烟，减体重，健康饮酒，锻炼
            //             饮食：isDiet     接种疫苗：isVaccination     其他：isOtherDangerFactor                               isOtherDangerFactor
            //temd.Add("危险控制-其他", "危险控制因素-其他"); //目标拼接
            //危险控制其他 reduceWeightAim  vaccinationName  otherDangerFactorName	

            temd.Add("reduceWeightAim", "减体重目标-AIM");
            temd.Add("vaccinationName", "建议疫苗接种-ADVICE");
            temd.Add("buildDate", "创建时间-CREATED_DATE");
            temd.Add("updateDate", "最后一次更新时间");
            temd.Add("buildOrganization", "创建机构-DEPARTMENT");
            temd.Add("buildAuthor", "创建人-CREATED_BY");
            temd.Add("updateAuthor", "最近修改人-LAST_UPDATE_BY");

            // personModel
            temd.Add("memberArchiveCode", "个人档案号-gerendanganhao");
            temd.Add("memberName", "姓名-name");
            temd.Add("genderCN", "性别-sex");

            temd.Add("birthday", "出生日期-birthday");
            temd.Add("phone", "联系电话-tel");
            temd.Add("address", "居住地址-addr");

            temd.Add("otherDangerFactorName", "危险因素控制其他-DANGERCONTROL_EX");
            temd.Add("updateOrganization", "当前所属机构-CDEPARTMENT");

            /*齿列-缺齿-TOOTHRESIDES2
            齿列-缺齿-TOOTHRESIDES21
            齿列-缺齿-TOOTHRESIDES22
            齿列-缺齿-TOOTHRESIDES23
            齿列-缺齿-TOOTHRESIDES24
            齿列-龋齿-TOOTHRESIDES3
            齿列-龋齿-TOOTHRESIDES31
            齿列-龋齿-TOOTHRESIDES32
            齿列-龋齿-TOOTHRESIDES33
            齿列-龋齿-TOOTHRESIDES34
            齿列-义齿-TOOTHRESIDES4
            齿列-义齿-TOOTHRESIDES41
            齿列-义齿-TOOTHRESIDES42
            齿列-义齿-TOOTHRESIDES43
            齿列-义齿-TOOTHRESIDES44
            其他说明-TOOTHRESIDESOTHERNOTE*/


            /*temd.Add("otherAssistantCheck", "其他");//？

            temd.Add("buildAuthorCN", "创建人CN");

            temd.Add("buildOrganizationCN", "创建机构CN");


            temd.Add("updateOrganization", "更新机构");
            temd.Add("updateOrganizationCN", "更新机构CN");

            temd.Add("updateAuthorCN", "更新人CN");

            temd.Add("cerebrovascular", "脑血管疾病");
            temd.Add("cerebrovascularCN", "脑血管疾病CN");*/


            return temd;
        }

        /// <summary>
        /// 体检信息-住院史
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> Tj_ZYSColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            temd.Add("idNumber", "身份证号-IDCARD");
            temd.Add("memberName", "姓名-name");
            temd.Add("checkDate", "体检日期-CHECKDATE");
            temd.Add("doctorCN", "责任医生-DOCTOR");

            temd.Add("enterDate", "入院日期-INHOSPITALDATE");

            temd.Add("exitDate", "出院日期-OUTHOSPITALDATE");

            temd.Add("cause", "原因-REASON");
            temd.Add("hospitalName", "医疗机构名称-HOSPITALNAME");
            temd.Add("illNo", "病案号-ILLCASENUM");

            return temd;
        }

        /// <summary>
        /// 体检信息-家庭病床史
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> Tj_JTBCSColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            temd.Add("idNumber", "身份证号-IDCARD");
            temd.Add("memberName", "姓名-name");
            temd.Add("checkDate", "体检日期-CHECKDATE");
            temd.Add("doctorCN", "责任医生-DOCTOR");

            temd.Add("buildSickbedDate", "建床日期-INHOSPITALDATE");

            temd.Add("revokeSickbedDate", "撤床日期-OUTHOSPITALDATE");

            temd.Add("sickCause", "原因-REASONS");
            temd.Add("sickHospitalName", "医疗机构名称-HOSPITALNAMES");
            temd.Add("sickNo", "病案号-ILLCASENUMS");

            return temd;
        }

        /// <summary>
        /// 体检信息-主要用药情况
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> Tj_ZYYYQKColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            temd.Add("idNumber", "身份证号-IDCARD");
            temd.Add("memberName", "姓名-name");
            temd.Add("checkDate", "体检日期-CHECKDATE");
            temd.Add("doctorCN", "责任医生-DOCTOR");

            temd.Add("physicalMedichineName", "药物名称-MEDICINALNAME");
            temd.Add("directions", "用法-USEAGE");
            temd.Add("therapyDose", "用量-USENUM");
            temd.Add("medichineDate", "用药时间-ENDTIME");

            return temd;
        }

        /// <summary>
        /// 体检信息-非免疫规划接种史
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> Tj_FMYGHJZSColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            temd.Add("idNumber", "身份证号-IDCARD");
            temd.Add("memberName", "姓名-name");
            temd.Add("checkDate", "体检日期-CHECKDATE");
            temd.Add("doctorCN", "责任医生-DOCTOR");

            temd.Add("immName", "接种名称-PILLNAME");
            temd.Add("immDate", "接种日期-INOCULATIONDATE");
            temd.Add("immOrganization", "接种机构-INOCULATIONHISTORY");

            return temd;
        }

        /// <summary>
        /// 老年人
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> LnrColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            temd.Add("idNumber", "身份证号");
            temd.Add("memberArchiveCode", "健康体检编号");

            temd.Add("energy", "Question1");
            temd.Add("fatigue", "Question2");
            temd.Add("breathe", "Question3");
            temd.Add("speak", "Question4");
            temd.Add("unpleasant", "Question5");
            temd.Add("nervout", "Question6");
            temd.Add("solitary", "Question7");
            temd.Add("scare", "Question8");
            temd.Add("weight", "Question9");
            temd.Add("eye", "Question10");
            temd.Add("hand", "Question11");
            temd.Add("craw", "Question12");
            temd.Add("cold", "Question13");
            temd.Add("catchaCold", "Question14");
            temd.Add("runathehose", "Question15");
            temd.Add("mouth", "Question16");

            temd.Add("food", "Question17");
            temd.Add("derma", "Question18");
            temd.Add("bleeding", "Question19");
            temd.Add("fingermatl", "Question20");
            temd.Add("oraldry", "Question21");
            temd.Add("ache", "Question22");
            temd.Add("face", "Question23");
            temd.Add("spot", "Question24");
            temd.Add("sore", "Question25");
            temd.Add("drinking", "Question26");
            temd.Add("bittertaste", "Question27");
            temd.Add("abdomen", "Question28");
            temd.Add("uncomfortable", "Question29");
            temd.Add("unwell", "Question30");
            temd.Add("malatse", "Question31");
            temd.Add("tongue", "Question32");
            temd.Add("vein", "Question33");

            temd.Add("buildAuthor", "创建人");
            temd.Add("buildDate", "创建时间");
            temd.Add("updateAuthor", "修改人");
            temd.Add("updateDate", "修改时间");
            temd.Add("doctorCN", "随访医生");
            temd.Add("fillDate", "填表日期");

            temd.Add("somatotype1", "气虚质得分");
            temd.Add("somatotype2", "阳虚质得分");
            temd.Add("somatotype3", "阴虚质得分");
            temd.Add("somatotype4", "痰湿质得分");
            temd.Add("somatotype5", "湿热质得分");
            temd.Add("somatotype6", "血瘀质得分");
            temd.Add("somatotype7", "气郁质得分");
            temd.Add("somatotype8", "特兼质得分");
            temd.Add("somatotype9", "平和质得分");

            temd.Add("somatotypeOne", "气虚质");
            temd.Add("somatotypeTwo", "阳虚质");
            temd.Add("somatotypeShree", "阴虚质");
            temd.Add("somatotypeFour", "痰湿质");
            temd.Add("somatotypeFive", "湿热质");
            temd.Add("somatotypeSix", "血瘀质");
            temd.Add("somatotypeSeven", "气郁质");
            temd.Add("somatotypeEight", "特兼质");
            temd.Add("somatotypeNine", "平和质");

            // QXZLX
            temd.Add("气虚质指导", "气虚质指导");

            // YXZLX
            temd.Add("阳虚质指导", "阳虚质指导");

            // YINXZ
            temd.Add("阴虚质指导", "阴虚质指导");

            // TSZLX
            temd.Add("痰湿质指导", "痰湿质指导");

            // SRZLX
            temd.Add("湿热质指导", "湿热质指导");

            // XXZLX
            temd.Add("血瘀质指导", "血瘀质指导");

            // QYZLX
            temd.Add("气郁质指导", "气郁质指导");

            // TBZLX
            temd.Add("特兼质指导", "特兼质指导");

            // PHZLX
            temd.Add("平和质指导", "平和质指导");

            temd.Add("气虚质指导_其他", "气虚质指导_其他");
            temd.Add("阳虚质指导_其他", "阳虚质指导_其他");
            temd.Add("阴虚质指导_其他", "阴虚质指导_其他");
            temd.Add("痰湿质指导_其他", "痰湿质指导_其他");
            temd.Add("湿热质指导_其他", "湿热质指导_其他");
            temd.Add("血瘀质指导_其他", "血瘀质指导_其他");
            temd.Add("气郁质指导_其他", "气郁质指导_其他");
            temd.Add("特兼质指导_其他", "特兼质指导_其他");
            temd.Add("平和质指导_其他", "平和质指导_其他");

            return temd;
        }

        /// <summary>
        /// 老年人_自理
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> LnrZLColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            temd.Add("idNumber", "身份证号");
            temd.Add("memberArchiveCode", "档案编号");

            temd.Add("dine", "Question1");
            temd.Add("cleanup", "Question2");
            temd.Add("dressed", "Question3");
            temd.Add("defecation", "Question4");
            temd.Add("activity", "Question5");

            temd.Add("gesamturteil", "总得分");
            temd.Add("fillTime", "随访日期");


            return temd;
        }

        /// <summary>
        /// 高血压管理卡
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> GxyGLKColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            temd.Add("memberArchiveCode", "档案号-RecordID");
            temd.Add("memberName", "姓名：");
            temd.Add("idNumber", "身份证号码：");

            temd.Add("hyperGrade", " 管理组别：");
            temd.Add("病例来源：", "病例来源：");
            temd.Add("hyperFamilyHistoryComm", "家族史：");

            temd.Add("目前症状（可多选）：", "目前症状（可多选）：");
            temd.Add("高血压并发症情况：", "高血压并发症情况：");

            temd.Add("hypotensiveDrug", "是否使用降压药：");
            temd.Add("smokeSituation", "吸烟情况：");
            temd.Add("drinkSituation", "饮酒情况：");
            temd.Add("physicalActivity", "体育锻炼：");

            temd.Add("height", "身高：");
            temd.Add("weight", "体重：");
            temd.Add("BMI：", "BMI：");
            temd.Add("腰围:", "腰围：");

            // 高压
            temd.Add("maxBloodPressure1", "高压值：");

            // 低压
            temd.Add("maxBloodPressure2", "低压值：");

            temd.Add("bloodGlucose1", "空腹血糖：");

            temd.Add("lipopmteinCholesterol1", "高密度脂蛋白：");
            temd.Add("lipopmteinCholesterol2", "低密度脂蛋白：");

            temd.Add("triglycerides", "甘油三酯：");
            temd.Add("totalCholesterol", "胆固醇：");

            temd.Add("confirmedDiagnosisDate", "确诊时间：");
            temd.Add("buildDate", "录入时间：");

            temd.Add("updateDate", "最近更新时间：");
            temd.Add("buildAuthorCN", "录入人：");

            temd.Add("updateAuthorCN", "最近更新人：");
            temd.Add("buildOrganizationCN", "创建机构：");
            temd.Add("updateOrganizationCN", "当前所属机构：");

            return temd;
        }

        /// <summary>
        /// 高血压随访
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> GxySFColumn()
        {
            Dictionary<string, string> temd = new Dictionary<string, string>();

            temd.Add("memberArchiveCode", "个人档案号");
            temd.Add("memberName", "姓名");
            temd.Add("idNumber", "身份证号");

            temd.Add("investigateDate", "随访日期");
            temd.Add("visitAssort", "随访方式");

            /* 症状（以英文逗号分隔）1无症状　2头痛头晕 3恶心呕吐4眼花耳鸣 
            5呼吸困难6心悸胸闷 7鼻衄出血不止8四肢发麻  9下肢水肿*/
            temd.Add("症状", "症状");

            // systolicPressure/diastolicPressure
            temd.Add("血压", "血压");

            // weight/weight1
            temd.Add("体重", "体重");

            temd.Add("height", "身高");

            // bodyMassIndex/bodyMassIndexTwo
            temd.Add("体质指数", "体质指数");

            temd.Add("heartRate2", "心率");

            temd.Add("others", "其他");


            // smoke/smoke1
            temd.Add("日吸烟量", "日吸烟量");

            // drink/drink1
            temd.Add("日饮酒量", "日饮酒量");

            // campaignTimesWeek/campaignTimesWeek1
            temd.Add("运动频率", "运动频率");

            // campaignMinutes/campaignMinutes1
            temd.Add("每次持续时间", "每次持续时间");

            // saltLevel1/saltLevel2
            temd.Add("摄盐情况", "摄盐情况");

            temd.Add("heartAdjust", "心理调整");

            temd.Add("followDoctor", "遵医行为");

            temd.Add("aideInspection", "辅助检查");


            temd.Add("isDrugReaction", "药物不良反应");
            temd.Add("isDrugReactionCN", "副作用详细描述");


            temd.Add("drugCompliance", "服药依从性");
            temd.Add("visitAssort", "此次随访分类");

            // 高血压 type:1
            temd.Add("用药情况", "用药情况");


            temd.Add("药物名称", "药物名称");
            temd.Add("用法", "用法");

            temd.Add("referralsOrganization", "机构及科别");
            temd.Add("referralsReason", "原因");

            temd.Add("investigateNextdate", "下次随访时间");
            temd.Add("investigateDoctorSignameCN", "随访医生签名");

            temd.Add("buildDate", "录入时间");

            temd.Add("updateDate", "最近更新时间：");
            temd.Add("buildAuthorCN", "录入人：");

            temd.Add("updateAuthorCN", "最近更新人：");
            temd.Add("buildOrganizationCN", "创建机构：");
            temd.Add("updateOrganizationCN", "当前所属机构：");

            return temd;
        }
    }
}
