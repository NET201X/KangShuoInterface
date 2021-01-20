using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.InfoModel
{
   public  class LnrPerson
    {
        /// <summary>
        /// 个人主键
        /// </summary>
        public string pid { set; get; }

        /// <summary>
        /// 身份证号
        /// </summary>
        public string idNumber { set; get; }

        /// <summary>
        /// 人员档案编号
        /// </summary>
        public string memberArchiveCode { set; get; }

        /// <summary>
        /// 人员姓名
        /// </summary>
        public string memberName { set; get; }

        /// <summary>
        /// 出生日期
        /// </summary>
        public string briday { set; get; }

        /// <summary>
        /// 居住地址
        /// </summary>
        public string addr { set; get; }


        /// <summary>
        /// 联系电话
        /// </summary>
        public string phone { set; get; }


        /// <summary>
        /// 当前所属机构
        /// </summary>
        public string orgtion { set; get; }

        /// <summary>
        /// 录入人
        /// </summary>
        public string doctorname { set; get; }


    }
}
