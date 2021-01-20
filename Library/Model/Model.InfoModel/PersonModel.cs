using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.InfoModel
{
    public class PersonModel
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
        /// 家庭id
        /// </summary>
        public string fid { set; get; }

        /// <summary>
        /// 关系
        /// </summary>
        public string houseRelation { set; get; }
    }

    public class ComparPerson : IEqualityComparer<PersonModel>
    {
        public bool Equals(PersonModel x, PersonModel y)
        {
            if (x == null && y == null)
                return false;
            return x.pid == y.pid;
        }

        public int GetHashCode(PersonModel obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
    /// <summary>
    /// 乡镇
    /// </summary>
    public class Town 
    {
        public object code { set; get; }
        public object text { set; get; }
        //public List<Village> villageList = new List<Village>();

    }
    public class Village
    {
        public object code { set; get; }
        public object text { set; get; }
    }

    #region 人群分类查询条件
    public class QueryList
    {
        public string qTown { get; set; }
        public string qVill { get; set; }
        public bool qLnr { get; set; }
        public bool qGxy { get; set; }
        public bool qTnb { get; set; }
        public bool qGxb { get; set; }
        public bool qNcz { get; set; }
        public bool qZl { get; set; }
        public bool qMzf { get; set; }
        public bool qJsb { get; set; }
    }
    #endregion
}
