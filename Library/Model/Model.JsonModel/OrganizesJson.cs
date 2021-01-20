using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.JsonModel
{
    public class OrganizesJson
    {
        public OrganizesBody body { get; set; }
    }

    public class OrganizesBody
    {
        public OrganizesResult d1 { get; set; }
    }

    public class OrganizesResult
    {
        public List<OrganizesData> data { get; set; }
    }

    public class OrganizesData
    {
        public string t_id { get; set; }
        public string u_id { get; set; }
        public string u_status { get; set; }
        public string u_name { get; set; }
        public string u_code { get; set; }
    }
    

}
