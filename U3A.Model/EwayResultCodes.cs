using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U3A.Model
{
    public class EwayResultCode
    {
        public EwayResultCode(string Code, string BriefDescription, string LongDescription)
        {
            this.Code = Code;
            this.BriefDescription = BriefDescription;
            this.LongDescription = LongDescription;

        }
        public string Code { get; set; }
        public string BriefDescription { get; set; }
        public string LongDescription { get; set; }
        public string CodeAndDescription
        {
            get
            {
                return $"{Code} {BriefDescription}";
            }
        }
    }
    public class EwayResultCodes : List<EwayResultCode>
    {
        public EwayResultCodes()
        {
            AddRange(new List<EwayResultCode>(){
                    new EwayResultCode("--", "Transaction Cancelled", "The Transaction was cancelled by the operator."),
                    new EwayResultCode("00", "Transaction Approved", "Transaction approved successfully."),
                    new EwayResultCode("08", "Honour With Identification", "Transaction processed successfully - identification is NOT actually required for online transactions.This code is returned by some banks in place of 00 response."),
                    new EwayResultCode("01", "Refer to Issuer", "The customer’s card issuer has indicated there is a problem with the card number.The customer should use an alternate credit card, or contact their bank."),
                    new EwayResultCode("03", "No Merchant", @"This error indicates that either your merchant facility is non - functional or the details entered into eWAY are incorrect.
                    You will need to contact your bank, advise of this response and confirm your merchant account is active and is an 'Ecommerce' terminal(rather than an Eftpos terminal).
                    If problems persist, raise an eWAY Support Case and the matter will be escalated."),
                    new EwayResultCode("04", "Pick Up Card", "The customer’s card issuer has declined the transaction and requested that the card be retained as the card may have been reported as lost or stolen.The customer should use an alternate credit card, or contact their bank."),
                    new EwayResultCode("05", "Do Not Honour", "This error is a generic bank response code that has several possible causes.However it does generally indicate a card error, not an error with your merchant facility.The '05' error indicates your bank declining the customer's card for an unspecified reason."),
                    new EwayResultCode("06", "Error", @"The customer’s card issuer has declined the transaction as there is a problem with the card number.The customer should use an alternate credit card, or contact their bank.
                    Note: This response code can also be returned via the Rapid API if you run a transaction query prior to the transaction being completed."),

                    new EwayResultCode("12", "Invalid Transaction", @"The bank has declined the transaction because of an invalid format or field.This indicates the card details were incorrect.Check card data entered and try again.
                    Ensure there are no spaces, or special characters(, &, $) in the card number."),
                    new EwayResultCode("13", "Invalid Amount", @"The customer’s card issuer has declined the transaction because of an invalid format or field.Check the transaction information and try processing the transaction again.
                    An invalid character(e.g.a dollar sign or a space) may be being passed to Eway.
                    Note: Transaction amounts should be passed to Eway in cents form only - ie, $10 should be passed as 1000.
                    You may need to speak with your web developer to review the code that is being passed to Eway for transaction amount.
                    If you are attempting to charge in a currency other than your local currency, contact Eway to ensure your account is able to support foreign currencies, and has been configured correctly."),

                    new EwayResultCode("14", "Invalid Card Number", "The card issuing bank has declined the transaction as the credit card number is incorrectly entered, or does not exist.Check card details and try processing again."),
                    new EwayResultCode("15", "No Issuer", "The customer’s card issuer does not exist. Check the card information and try processing the transaction again."),
                    new EwayResultCode("22", "Suspected Malfunction", "The customer’s card issuer could not be contacted during the transaction. The customer should check the card information and try processing the transaction again."),
                    new EwayResultCode("30", "Format Error", "The customer’s card issuer does not recognise the transaction details being entered.This is due to a format error.The customer should check the transaction information and try processing the transaction again."),
                    new EwayResultCode("31", "Bank Not Supported By Switch", "The customer’s card issuer has declined the transaction as it does not allow transactions originating through mail / telephone, fax, email or Internet orders."),
                    new EwayResultCode("34", "Suspected Fraud", @"Retain Card The customer’s card issuer has declined the transaction as there is a suspected fraud on this credit card number.
                    You should check transactions processed after any declined transactions receiving this particular error to monitor for fraudulent transactions on alternate cards.
                    If you do see multiple fraudulent transactions, contact Eway immediately via a Support Case."),

                    new EwayResultCode("41", "Lost Card", "The customer’s card issuer has declined the transaction as the card has been reported lost.The customer should use an alternate credit card, or contact their bank."),
                    new EwayResultCode("42", "No Universal Account", "The customer’s card issuer has declined the transaction as the account type selected is not valid for this credit card number.The customer should use an alternate credit card, or contact their bank."),
                    new EwayResultCode("43", "Stolen Card", "The customer’s card has been reported as stolen.While you could contact this customer yourself, it's very possible that this transaction is fraudulent. Tread carefully."),
                    new EwayResultCode("51", "Insufficient Funds", "The customer’s card issuer has declined the transaction as the credit card does not have sufficient funds.Advise your customer of this fact, and they should either use an alternate card or contact their bank."),
                    new EwayResultCode("54", "Expired Card", @"The customer’s card issuer has declined the transaction saying that the card has expired.Contact your customer and confirm that the correct dates were entered and that there were no mistakes(e.g. 05 / 11 rather than 05 / 12).
                    Note: Invalid expiry dates(ie, expiry year is in the past) are not able to be passed through Eway"),

                    new EwayResultCode("56", "No Card Record", "The customer’s card issuer has declined the transaction as the credit card number does not exist.The customer should use an alternate credit card."),
                    new EwayResultCode("57", "Function Not Permitted to Cardholder", "The customer’s card issuer has declined the transaction as this credit card cannot be used for this type of transaction.The customer should use an alternate credit card, or contact their bank."),
                    new EwayResultCode("58", "Function Not Permitted to Terminal", @"The customer’s card issuer has declined the transaction as this credit card cannot be used for this type of transaction.This may be associated with a test credit card number.The customer should use an alternate credit card, or contact their bank.
                    This is also often a response expected for test cards on the live gateway, when test credentials are used."),
                    new EwayResultCode("59", "Suspected Fraud", "The customer’s card issuer has declined this transaction as the credit card appears to be fraudulent.While you could contact this customer yourself, it's very possible that this transaction is fraudulent. Tread carefully."),
                    new EwayResultCode("62", "Restricted Card", "The customer’s card issuer has declined the transaction as the credit card has some restrictions.The customer should use an alternate credit card, or contact their bank."),
                    new EwayResultCode("63", "Security Violation", "The customer’s card issuer has declined the transaction. The customer should use an alternate credit card, and contact their bank"),
                    new EwayResultCode("67", "Capture Card", @"The customer’s card issuer has declined the transaction as the card is suspected to be a counterfeit.The customer’s card issue has requested that your customer’s credit card be retained by you.The customer should use an alternate credit card, or contact their bank.
                    While you could contact this customer yourself, it's very possible that this transaction is fraudulent. Tread carefully. "),
                    new EwayResultCode("82", "CVV Validation Error", "The customer’s card issuer has declined the transaction as the CVV is incorrect. The customer should check the CVV details (the 3 numbers on the back for Visa/MC, or 4 numbers on the front for AMEX) and try again. If not successful, the customer should use an alternate credit card."),
                    new EwayResultCode("91", "Card Issuer Unavailable", @"The customer’s card issuer is unable to be contacted to authorise the transaction.The customer should attempt to process this transaction again.
                    If the problem persists, there may be an issue with the card issuing bank, and the cardholder should contact their bank."),

                    new EwayResultCode("92", "Unable To Route Transaction", "The customer’s card issuer cannot be found for routing.This response code is often returned when the customer is using a test credit card number.The customer should attempt to process this transaction again."),
                    new EwayResultCode("W1", "Eway Status Error", @"There has been an error connecting to the banking connector to process the payment.This may occur due to an outage.
                    Check the Eway status page status.eway.com.au for outage and incident information."),

                    new EwayResultCode("W2", "Eway Status Error", @"There has been an error connecting to the banking connector to process the payment.This may occur due to an outage.
                    Check the Eway status page status.eway.com.au for outage and incident information."),

                    new EwayResultCode("W9", "Eway Status Error", @"There has been an error connecting to the banking connector to process the payment.This may occur due to an outage.
                    Check the Eway status page status.eway.com.au for outage and incident information.")
            });
        }
    }
}
