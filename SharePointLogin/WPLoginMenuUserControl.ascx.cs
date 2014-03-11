using System;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.IdentityModel;
using Microsoft.SharePoint.Administration;
using System.Security.Cryptography.X509Certificates;
using System.Data.Linq;
using CreateReqCert;
using CreateReqCert.VisualWebPart1;
using CreateReqCert.VisualWebPartOfCertDataOfDB;
using System.Web.Security;

namespace SPLogin.WPLoginMenu
{
    public partial class WPLoginMenuUserControl : UserControl
    {
        //The form of the show authentication methods on the portal
        protected void Page_Load(object sender, EventArgs e)
        {
            SPUser oUser = SPContext.Current.Web.CurrentUser;
            if (null != oUser)
            {
                PanelInput.Visible = false;
                LabelCurrentUser.Visible = true;
                LabelCurrentUser.Text = SPLoginResource.YOU_ARE_LOGGED_IN_AS + oUser.Name; //If the user is signed up, it will show the status of registration with the user name
            }
        }
        //If the user clicks Windows authentication, then let's show the standard input form Login and password
        protected void LinkButtonWindowsAuthentification(object sender, EventArgs e)
        {
            SPIisSettings iisSettings = SPContext.Current.Site.WebApplication.IisSettings[SPUrlZone.Default];
            if (null != iisSettings && iisSettings.UseWindowsClaimsAuthenticationProvider)
            {
                ShowFormAuthentification(iisSettings.WindowsClaimsAuthenticationProvider);
            }
        }
        //If the user clicks authentication certificate, it will show a form to specify the certificate
        protected void LinkButtonFormAuthentification(object sender, EventArgs e)
        {
            SPIisSettings iisSettings = SPContext.Current.Site.WebApplication.IisSettings[SPUrlZone.Default];
            if (null != iisSettings && iisSettings.UseFormsClaimsAuthenticationProvider)
            {
                ShowFormAuthentification(iisSettings.FormsClaimsAuthenticationProvider);
            }
        }
        //Redirect to the form authentification certificate
        protected void ShowFormAuthentification(SPAuthenticationProvider provider)
        {
            SPSite spServer = SPControl.GetContextSite(Context);
            SPWeb spWeb = SPControl.GetContextWeb(Context);
            string querystring = "ReturnUrl=" + spWeb.Url;

            SPUtility.Redirect(provider.AuthenticationRedirectionUrl.ToString(), SPRedirectFlags.Default, this.Context, querystring);
        }
        //if the user chose the registration
        protected void LinkButtonRegistration(object sender, EventArgs e)
        {
            PanelInput.Visible = false;
            PanelRegistration.Visible = true;
            PanelGetCert.Visible = true;
            Rgistering.Visible = true;
        }

        protected void Rgistering_Click(object sender, EventArgs e)
        {
            if (0 == CertFromStore.Text.Length)//if the user chose the registration on certificate else default registration
            {
                if (null == FileUpload.PostedFile)
                {
                    return;
                }

                Int32 fileLen;

                fileLen = FileUpload.PostedFile.ContentLength;

                Byte[] InputFileOfCert = new Byte[fileLen];

                System.IO.Stream myStream;
                myStream = FileUpload.FileContent;
                myStream.Read(InputFileOfCert, 0, fileLen);

                CreateUser(InputFileOfCert);
            }
            else
            {
                CreateUser(System.Text.Encoding.UTF8.GetBytes( CertFromStore.Text ));
            }
        }
        //Download a certificate from a file
        protected void LoadFromFile_Click(object sender, EventArgs e)
        {
            FileUpload.Visible = true;
            CertFromStore.Visible = false;
        }
        //Download a certificate from a store
        protected void LoadFromStore_Click(object sender, EventArgs e)
        {
            FileUpload.Visible = false;
            CertFromStore.Visible = true;
            Page.RegisterStartupScript("LoadScript", "<script language=javascript> GetCertFromStorage(); </script>");
        }
        //Create a new user
        public void CreateUser(byte[] InputFileOfCert)
        {
            //Load the certificate from file and remove from it string "-----BEGIN CERTIFICATE-----" and "-----END CERTIFICATE-----"
            string strCert = System.Text.Encoding.UTF8.GetString( InputFileOfCert );
            strCert = strCert.Trim();
            strCert = strCert.Replace("\r\n", string.Empty);
            strCert = strCert.Trim(("-----BEGIN CERTIFICATE-----").ToCharArray());
            strCert = strCert.Trim(("-----END CERTIFICATE-----").ToCharArray());
            //Load the certificate and get serial number from it
            X509Certificate2 Cert = new X509Certificate2( InputFileOfCert );
            string strSN = Cert.GetSerialNumberString();

            DCCertificatesDataContext context = new DCCertificatesDataContext(@"Data Source=.\SHAREPOINT;Initial Catalog=DBSharepointCertificates;Integrated Security=True;Pooling=False");
            var table = from c in context.RequestOfCerts
                        where c.SN == strSN.ToLower()
                        select new { c.certificate };
            //If registration certificate already include in date base of certificate then return from this function
            foreach (var item in table)
            {
                X509Certificate2 objCert = new X509Certificate2( System.Text.Encoding.UTF8.GetBytes(item.certificate) );
                if (objCert.Equals(Cert))
                {
                    Status.Visible = true;
                    Status.Text = SPLoginResource.ERROR_USER_LOGGED;
                    return;
                }
            }
            //Include a new certificate in data base certificates
            RequestOfCerts tableRowOfReqCert = new RequestOfCerts
            {
                users = Login.Text,
                //CN = Cert.GetName(),
                //    mail = Cert.,
                //    organization = ,
                //    region = ,
                //    country = ,
                //    inn = ,
                //    address = ,
                //    provider = ,
                //    destinationCert = ,
                //    request = ,
                certificate = strCert,
                SN = strSN.ToLower(),
                ExpData = Cert.GetExpirationDateString()
            };

            context.RequestOfCerts.InsertOnSubmit(tableRowOfReqCert);
            //Add new user in Membership
            try
            {
                context.SubmitChanges();
                //MembershipCreateStatus Status;
                Membership.CreateUser(Login.Text, strSN.ToLower());

                Status.Visible = true;
                Status.Text = SPLoginResource.USER_CREATE_SUCCESSFUL;
            }
            catch (MembershipCreateUserException e)
            {
                Status.Text = e.ToString();
            }
            catch (ChangeConflictException) //Обнаружить конфиликт совместимого доступа
            {
                context.ChangeConflicts.ResolveAll(RefreshMode.KeepChanges); //Разрешение конфликта
            }

        }
        //Check status a new certificate
        protected void LBCheckCert_Click(object sender, EventArgs e)
        {
            PanelGetCert.Visible = true;
            ValidCert.Visible = true;
            PanelInput.Visible = false;
        }
        //Valid a new certificate
        protected void ValidCert_Click(object sender, EventArgs e)
        {
            ImageOfStatusCert.Visible = true;
            Status.Visible = true;
            string strCert = string.Empty;

            if (0 == CertFromStore.Text.Length)
            {
                if (null == FileUpload.PostedFile)
                {
                    return;
                }

                strCert = System.Text.Encoding.UTF8.GetString(FileUpload.FileBytes);
            }
            else
            {
                strCert = CertFromStore.Text;
            }

            string sImageUrl = string.Empty;
            string sStatus = string.Empty;
            //Check status certtificate
            VisualWebPartOfCertDataOfDBUserControl oDBCerts = new VisualWebPartOfCertDataOfDBUserControl();
            oDBCerts.CheckStatusOfCert( strCert, ref sImageUrl, ref sStatus);
            //Show the status 
            ImageOfStatusCert.ImageUrl = sImageUrl;
            Status.Text = sStatus;
        }
    }
}