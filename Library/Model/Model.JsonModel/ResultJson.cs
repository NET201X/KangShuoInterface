using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.JsonModel
{
    public class ResultJson
    {
        public JsonMsg msg { get; set; }
        public JsonBody body { get; set; }
        public string content { get; set; }
    }

    public class ResultJson2
    {
        public JsonBody2 body { get; set; }
    }

    public class JsonBody
    {
        public JsonResult d1 { get; set; }
    }

    public class JsonBody2
    {
        public JsonResult2 d1 { get; set; }
    }

    public class JsonResult2
    {
        public List<JsonSpecialPData> data { get; set; }
    }

    public class JsonResult
    {
        public JsonData data { get; set; }
    }

    //view-source:http://ehr.xyhsoft.com/jkda/server/jkda/zhgl/Ezhgl.js
    public class JsonSpecialPData
    {
        //pid
        public string a00_03_00 { get; set; }
        //区域编号
        public string a00_03_02 { get; set; }

        //行政号
        public string a00_03_70 { get; set; }

        //医生
        public string a00_03_91 { get; set; }

        //儿童专项和老年人专项,孕产妇专项的赋值 
        public string b01_60_00 { get; set; }
        public string b02_60_00 { get; set; }
        public string b04_052_00 { get; set; }

        //出生日期
        public string a00_03_09 { get; set; }
        //性别：1男；2女
        public string a00_03_07 { get; set; }
        //妇女age>14&&age<65
        public string a00_03_67 { get; set; }

        //老年人注记
        public string a00_03_68 { get; set; }
        //儿童注记age<6
        public string a00_03_66 { get; set; }
        //高血压
        public string a00_03_41 { get; set; }
        //糖尿病
        public string a00_03_42 { get; set; }
        //冠心病信息 
        public string a00_03_43 { get; set; }
        //慢阻肺信息 
        public string a00_03_44 { get; set; }
        //肿瘤病信息 
        public string a00_03_45 { get; set; }
        //脑卒中信息 
        public string a00_03_46 { get; set; }
        //精神病信息 
        public string a00_03_47 { get; set; }

        //--start用药
        //id
        public string b04_011_03 { get; set; }
        //name
        public string b04_62_02 { get; set; }
        //每日次数
        public string b04_011_04 { get; set; }
        //每次剂量(mg)
        public string b04_011_05 { get; set; }
        //用法
        public string b04_011_09 { get; set; }
        //--end用药

        //糖尿病 用药日/次
        public string b04_021_04 { set; get; }
        /// <summary>
        /// 糖尿病用药，量
        /// </summary>
        public string b04_021_05 { set; get; }
    }

    public class JsonData
    {
        public string d_id { get; set; }
        public string u_type { get; set; }
        public string s_id { get; set; }
        public string u_name { get; set; }
        public string e_id { get; set; }

        public List<JsonRow> rows { get; set; }
    }

    public class JsonRow
    {
        public string b04_62_00 { get; set; }
        public string b04_62_02 { get; set; }
        public string b04_62_10 { get; set; }
        public string b04_62_06 { get; set; }
        public string b04_62_05 { get; set; }
    }

    public class JsonMsg
    {
        public string state { get; set; }
        public string type { get; set; }
        public string msg { get; set; }
        public string mx { get; set; }
    }

    public class VIllageJsonS
    {
       public  List<VillageJson> data { set; get; }
    }
    public class VillageJson
    {
        public string b_bak { set; get; }
        public string b_gldw { set; get; }
        public string b_id { set; get; }
        public string b_name { set; get; }
        public string b_rgid { set; get; }
        public string b_ssjd { set; get; }
        public VillagePage page { set; get; }
    }
    public class VillagePage
    {
        public string currentPage { set; get; }
        public string currentResult { set; get; }
        public string entityOrField { set; get; }
        public string form { set; get; }
        public string id { set; get; }
        public string pageStr { set; get; }
        public string showCount { set; get; }
        public string totalPage { set; get; }
        public string totalResult { set; get; }
    }

}
