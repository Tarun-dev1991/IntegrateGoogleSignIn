using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace IntegrateGoogleSignIn.Controllers
{
    public class SearchBarController : Controller
    {
        // GET: SearchBar
        string conStr = ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString;
        SqlConnection conn;
        SqlCommand cmd;
        SqlDataAdapter adp;
        DataTable dt;

        public JsonResult RedList(string v)
        {
            conn = new SqlConnection(conStr);
            adp = new SqlDataAdapter(string.Format("select * from searchMaster where searcgclass = 'red' and viewid = {0}", v), conn);
            dt = new DataTable();
            adp.Fill(dt);
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            foreach (DataRow dr in dt.Rows)
            {
                var dict = new Dictionary<string, string>();
                dict["searchtext"] = dr["searchtext"].ToString();
                dict["searchid"] = dr["searchid"].ToString();
                dict["class"] = dr["searcgclass"].ToString();
                list.Add(dict);
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GreenList(string v)
        {
            conn = new SqlConnection(conStr);
            adp = new SqlDataAdapter(string.Format("select * from searchMaster where searcgclass = 'green' and viewid = {0}", v), conn);
            dt = new DataTable();
            adp.Fill(dt);
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            foreach (DataRow dr in dt.Rows)
            {
                var dict = new Dictionary<string, string>();
                dict["searchtext"] = dr["searchtext"].ToString();
                dict["searchid"] = dr["searchid"].ToString();
                dict["class"] = dr["searcgclass"].ToString();
                list.Add(dict);

            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult BlueList(string v)
        {
            conn = new SqlConnection(conStr);
            adp = new SqlDataAdapter(string.Format("select * from searchMaster where searcgclass = 'blue' and viewid = {0}", v), conn);
            dt = new DataTable();
            adp.Fill(dt);
            List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();
            foreach (DataRow dr in dt.Rows)
            {
                var dict = new Dictionary<string, string>();
                dict["searchtext"] = dr["searchtext"].ToString();
                dict["searchid"] = dr["searchid"].ToString();
                dict["class"] = dr["searcgclass"].ToString();
                list.Add(dict);
            }
            
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult RedListSearchExists(string v, string searchText)
        {
            conn = new SqlConnection(conStr);
            adp = new SqlDataAdapter(string.Format("select Top 1 * from searchMaster where searcgclass = 'red' and viewid = {0} and searchtext = '{1}'", v, searchText), conn);
            dt = new DataTable();
            adp.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                return Json(1, JsonRequestBehavior.AllowGet);
            }
            return Json(0, JsonRequestBehavior.AllowGet);
        }
        public JsonResult BlueListSearchExists(string v, string searchText)
        {
            conn = new SqlConnection(conStr);
            adp = new SqlDataAdapter(string.Format("select Top 1 * from searchMaster where searcgclass = 'blue' and viewid = {0} and searchtext = '{1}'", v, searchText), conn);
            dt = new DataTable();
            adp.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                return Json(1, JsonRequestBehavior.AllowGet);
            }
            return Json(0, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GreenListSearchExists(string v, string searchText)
        {
            conn = new SqlConnection(conStr);
            adp = new SqlDataAdapter(string.Format("select Top 1 * from searchMaster where searcgclass = 'green' and viewid = {0} and searchtext = '{1}'", v, searchText), conn);
            dt = new DataTable();
            adp.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                return Json(1, JsonRequestBehavior.AllowGet);
            }
            return Json(0, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public string AddRed(string st, string v)
        {
            conn = new SqlConnection(conStr);

            cmd = new SqlCommand("Insert into searchMaster (searchtext,viewid,searcgclass) values(@st,@v,@sc)", conn);
            cmd.Parameters.AddWithValue("@st", st);
            cmd.Parameters.AddWithValue("@v", v);
            cmd.Parameters.AddWithValue("@sc", "red");

            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();

            return "0";
        }
        [HttpPost]
        public string AddGreen(string st, string v)
        {
            conn = new SqlConnection(conStr);

            cmd = new SqlCommand("Insert into searchMaster (searchtext,viewid,searcgclass) values(@st,@v,@sc)", conn);
            cmd.Parameters.AddWithValue("@st", st);
            cmd.Parameters.AddWithValue("@v", v);
            cmd.Parameters.AddWithValue("@sc", "green");

            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();

            return "0";
        }
        [HttpPost]
        public string AddBlue(string st, string v)
        {
            conn = new SqlConnection(conStr);
            cmd = new SqlCommand("Insert into searchMaster (searchtext,viewid,searcgclass) values(@st,@v,@sc)", conn);
            cmd.Parameters.AddWithValue("@st", st);
            cmd.Parameters.AddWithValue("@v", v);
            cmd.Parameters.AddWithValue("@sc", "blue");

            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();

            return "0";
        }

        public string DeleteIt(int sid)
        {
            conn = new SqlConnection(conStr);

            cmd = new SqlCommand("delete from searchMaster where searchid=@sid", conn);
            cmd.Parameters.AddWithValue("@sid", sid);

            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();

            return "0";
        }



    }
}