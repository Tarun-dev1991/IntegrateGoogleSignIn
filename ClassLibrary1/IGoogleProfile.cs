using IntegrateGoogle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrateGoogle.Core
{
	public interface IGoogleProfile
	{
		Task<UserProfile> GetuserProfile(string accesstoken);
	}
}
