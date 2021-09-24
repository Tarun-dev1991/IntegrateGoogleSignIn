using System;

namespace IntegrateGoogleSignIn.Models
{
	public class AnalyticalData
	{
		public string Id {
			get
			{
				return string.Format("{0}_{1}_{2}", City, userDefinedValue, dateHourMinute.ToShortDateString());
			}
		}
		public string SourceMedium { get; set; }

		public string City { get; set; }

		public string pagePathLevel1 { get; set; }

		public string pagePathLevel2 { get; set; }

		public string landingPagePath { get; set; }

		public DateTime dateHourMinute { get; set; }

		public string userDefinedValue { get; set; }

		public string ProductListClicks { get; set; }

		public string User { get; set; }

		public string Visitors { get; set; }

		public string TimeOnPage { get; set; }
	}
}