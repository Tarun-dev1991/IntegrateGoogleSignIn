using System;
using System.Collections.Generic;

namespace IntegrateGoogleSignIn.Helpers
{
    #region --> Base Methods for Auth
    public class TokenVm
    {
        public string scope { get; set; }
        public string nonce { get; set; }
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string app_id { get; set; }
        public int expires_in { get; set; }
    }

    public class PayPalMakePaymentData
    {
        public string intent { get; set; }
        public PayPalMakePaymentRedirectUrls redirect_urls { get; set; }
        public PayPalPayer payer { get; set; }
        public PayPalTransaction[] transactions { get; set; }
    }
    public class PayPalTransaction
    {
        public PayPalAmount amount { get; set; }
        public PayPalItemList item_list { get; set; }
        public string description { get; set; }
    }
    public class PayPalItemList
    {
        public PayPalItem[] items { get; set; }
    }

    public class PayPalItem
    {
        public string quantity { get; set; }
        public string name { get; set; }
        public string price { get; set; }
        public string currency { get; set; }
        //		public string sku { get; set; }
        public string description { get; set; }
        public string tax { get; set; }
    }

    public class PayPalAmount
    {
        public string total { get; set; }
        public string currency { get; set; }
        public PayPalAmountDetails details { get; set; }
    }

    public class PayPalAmountDetails
    {
        public string subtotal { get; set; }
        public string tax { get; set; }
        public string shipping { get; set; }
    }
    public class PayPalMakePaymentRedirectUrls
    {
        public string return_url { get; set; }
        public string cancel_url { get; set; }
    }

    public class PayPalPayer
    {
        public string payment_method { get; set; }
        public PayPalPayerInfo payer_info { get; set; }
    }
    public class PayPalPayerInfo
    {
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string payer_id { get; set; }
    }

    public class PayPalPaymentResponse
    {
        public List<PayPalPaymentLinksResponse> Links { get; set; }
        public string DisplayError { get; set; }
    }

    public class PayPalPaymentLinksResponse
    {
        public string Href { get; set; }
        public string Rel { get; set; }
        public string Method { get; set; }
    }

    #endregion

    #region --> CreateAgreement

    public class CreateAgreementReqVm
    {
        public string name { get; set; }
        public string description { get; set; }
        public string start_date { get; set; }
        public PayerVm payer { get; set; }
        public PlanVm plan { get; set; }
    }

    public class PayerVm
    {
        public string payment_method { get; set; }
    }

    public class PlanVm
    {
        public string id { get; set; }
    }



    public class CreateAgreementResVm
    {
        public string name { get; set; }
        public string description { get; set; }
        public Payer payer { get; set; }
        public Plan plan { get; set; }
        public List<Link> links { get; set; }
        public DateTime start_date { get; set; }
    }

    public class Payer
    {
        public string payment_method { get; set; }
        public Payer_Info payer_info { get; set; }
    }

    public class Payer_Info
    {
        public string email { get; set; }
    }

    public class Plan
    {
        public string id { get; set; }
        public string state { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public List<Payment_Definitions> payment_definitions { get; set; }
        public Merchant_Preferences merchant_preferences { get; set; }
    }

    public class Merchant_Preferences
    {
        public Setup_Fee setup_fee { get; set; }
        public string max_fail_attempts { get; set; }
        public string return_url { get; set; }
        public string cancel_url { get; set; }
        public string auto_bill_amount { get; set; }
        public string initial_fail_amount_action { get; set; }
    }

    public class Setup_Fee
    {
        public string currency { get; set; }
        public string value { get; set; }
    }

    public class Payment_Definitions
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string frequency { get; set; }
        public Amount amount { get; set; }
        public List<Charge_Models> charge_models { get; set; }
        public string cycles { get; set; }
        public string frequency_interval { get; set; }
    }

    public class Amount
    {
        public string currency { get; set; }
        public string value { get; set; }
    }

    public class Charge_Models
    {
        public string id { get; set; }
        public string type { get; set; }
        public Amount1 amount { get; set; }
    }

    public class Amount1
    {
        public string currency { get; set; }
        public string value { get; set; }
    }

    public class Link
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
    }
    #endregion

    #region --> Execute Agreement


    public class ExecuteAgreementResVm
    {
        public string id { get; set; }
        public string state { get; set; }
        public string description { get; set; }
        public Payer payer { get; set; }
        public Plan plan { get; set; }
        public DateTime start_date { get; set; }
        public Shipping_Address1 shipping_address { get; set; }
        public Agreement_Details agreement_details { get; set; }
        public List<Link> links { get; set; }
    }

   

    public class Shipping_Address
    {
        public string recipient_name { get; set; }
        public string line1 { get; set; }
        public string line2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postal_code { get; set; }
        public string country_code { get; set; }
    }
     
 
    public class Shipping_Address1
    {
        public string recipient_name { get; set; }
        public string line1 { get; set; }
        public string line2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postal_code { get; set; }
        public string country_code { get; set; }
    }

    public class Agreement_Details
    {
        public Outstanding_Balance outstanding_balance { get; set; }
        public string cycles_remaining { get; set; }
        public string cycles_completed { get; set; }
        public DateTime next_billing_date { get; set; }
        public DateTime last_payment_date { get; set; }
        public Last_Payment_Amount last_payment_amount { get; set; }
        public DateTime final_payment_date { get; set; }
        public string failed_payment_count { get; set; }
    }

    public class Outstanding_Balance
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class Last_Payment_Amount
    {
        public string value { get; set; }
        public string currency { get; set; }
    }
 


    #endregion
}