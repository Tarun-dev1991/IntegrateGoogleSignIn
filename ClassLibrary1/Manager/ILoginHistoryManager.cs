using IntegrateGoogle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrateGoogle.Core.Manager
{
	public interface ILoginHistoryManager
	{
		void AddLogin(LoginHistory history);
		List<LoginHistory> GetUsers();

	}
}
