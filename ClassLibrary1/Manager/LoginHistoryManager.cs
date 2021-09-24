using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DatabaseMiddleware.Core;
using IntegrateGoogle.Core.Models;
using MySql.Data.MySqlClient;

namespace IntegrateGoogle.Core.Manager
{
	public class LoginHistoryManager : ILoginHistoryManager
	{
		private IDatabaseMiddleware _db;
		public LoginHistoryManager(IDatabaseMiddleware db)
		{
			_db = db;
			_db.SetDatabase(ConfigurationManager.ConnectionStrings["myConnectionString"].ConnectionString);
		}
		public void AddLogin(LoginHistory history)
		{
            
            string query = @"usp_LoginData_Insert";
			List<IDbDataParameter> dataParameters = new List<IDbDataParameter>();


			dataParameters.Add(new SqlParameter("username", history.UserName));
			dataParameters.Add(new SqlParameter("name", history.Name));
			dataParameters.Add(new SqlParameter("authtype", history.AuthType));

			_db.ExecuteProcedure(query, dataParameters.ToArray());
		}

		public List<LoginHistory> GetUsers()
		{
			string query = @"Select username, name from loginhistory";
			List<LoginHistory> his = new List<LoginHistory>();
			DataSet ds = _db.GetDataSetFromSql(query);
			if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
			{
				foreach (DataRow row in ds.Tables[0].Rows)
				{
					LoginHistory summary = new LoginHistory()
					{
						UserName = row.Field<string>("username"),
						Name = row.Field<string>("name")
					};

					his.Add(summary);
				}
			}

			return his;
		}
	}
}
