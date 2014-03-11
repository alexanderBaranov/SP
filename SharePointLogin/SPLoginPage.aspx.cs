using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.IdentityModel;
using Microsoft.SharePoint.IdentityModel.Pages;
using System.IdentityModel.Tokens;
using System.Web.Security;
using CreateReqCert;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;
using Microsoft.SharePoint.Administration;

namespace SPLogin.Layouts.SPLogin
{
    public partial class SPLoginPage : FormsSignInPage //LayoutsPageBase
    {
        //Function to load the page.
        //Check on secure connection via SSL Protocol.
        protected void Page_Load(object sender, EventArgs e)
        {
            Uri objUri = HttpContext.Current.Request.Url;
            if (objUri.Scheme == Uri.UriSchemeHttp)
            {
                Response.Redirect( "https://" + objUri.Host + ":" + 443 + objUri.PathAndQuery );
            }
            else if (objUri.Scheme == Uri.UriSchemeHttps) //if the SSL connection, then calls the function LoginToSite()
            {
                LoginToSite();
            }
        }

        protected void LoginToSite()
        {
            //Get the root certificate of the SSL Protocol
            X509Certificate2 objCertClient = new X509Certificate2(Request.ClientCertificate.Certificate);
            string strSN = objCertClient.SerialNumber.Replace("-", string.Empty); //Remove all symbols "-"
            //The connection to the database
            DCCertificatesDataContext db = new DCCertificatesDataContext(@"Data Source=.\SHAREPOINT;Initial Catalog=DBSharepointCertificates;Integrated Security=True;Pooling=False");
            var table = from c in db.RequestOfCerts
                        where c.SN == strSN.ToLower()
                        select new { c.users, c.certificate };

            string sUserName = string.Empty;
            bool bExistCert = false;
            //Compare all certificates in the database with the root certificate of the SSL Protocol
            foreach (var item in table)
            {
                X509Certificate2 objCert = new X509Certificate2( System.Text.Encoding.UTF8.GetBytes(item.certificate) );
                if (objCert.Equals(objCertClient))
                {
                    bExistCert = true;
                    sUserName = item.users;
                    break;
                }
            }
            //If certificates are equal , will do a redirect to the user's site via a secure Protocol
            if ( bExistCert )
            {
                System.Collections.Specialized.NameValueCollection dtr = Request.QueryString;
                string sreturnurl = (dtr.GetValues("source"))[0];
                //Collect the url of the page via a secure Protocol
                Uri objUri = HttpContext.Current.Request.Url;
                sreturnurl = "https://" + objUri.Host + ":" + 443 + sreturnurl;

                //string strProviderName = string.Empty;
                //foreach (MembershipProvider p in Membership.Providers)
                //{
                //    if (p.GetType().Equals(typeof(Microsoft.SE.AnonProvider.Users)))
                //    {
                //        strProviderName = p.Name;
                //        break;
                //    }
                //} 
                //Get a user token for a session
                SecurityToken tk = SPSecurityContext.SecurityTokenForFormsAuthentication(
                    new Uri(sreturnurl), "FBAMembership", "FBARoles", sUserName, strSN.ToLower());

                if (tk != null)
                {
                    //try setting the authentication cookie
                    SPFederationAuthenticationModule fam = SPFederationAuthenticationModule.Current;
                    fam.SetPrincipalAndWriteSessionToken(tk);

                    Response.Redirect( sreturnurl );
                }
                else
                {
                    Error.Visible = true;
                    Error.Text = SPLoginResource.ACCESS_DENIED;
                }
            }
            else
            {
                Error.Visible = true;
                Error.Text = SPLoginResource.ACCESS_DENIED_CERT_FALSE;
            }
        }
    }
}
