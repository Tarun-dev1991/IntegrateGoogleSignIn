using Newtonsoft.Json;

namespace IntegrateGoogle.Core.Models
{
	//* JsonProperty(PropertyName = "name") attribute used here  
	//* to map json property name with object property name   
	//* while deseralizing a json into object 
	public partial class UserProfile
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("verified_email")]
		public bool VerifiedEmail { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("given_name")]
		public string GivenName { get; set; }

		[JsonProperty("family_name")]
		public string FamilyName { get; set; }

		[JsonProperty("link")]
		public string Link { get; set; }

		[JsonProperty("picture")]
		public string Picture { get; set; }

		[JsonProperty("gender")]
		public string Gender { get; set; }

		[JsonProperty("locale")]
		public string Locale { get; set; }
	}
}
