using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IntegrateGoogle.Core.Models;
using Newtonsoft.Json;

namespace IntegrateGoogle.Core
{
	public class GoogleProfile : IGoogleProfile
	{
		public async Task<UserProfile> GetuserProfile(string accesstoken)
		{
			var httpClient = new HttpClient
			{
				BaseAddress = new Uri("https://www.googleapis.com")
			};
			string url = $"https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token={accesstoken}";
			var response = await httpClient.GetAsync(url);
			return JsonConvert.DeserializeObject<UserProfile>(await response.Content.ReadAsStringAsync());
		}
	}
}
