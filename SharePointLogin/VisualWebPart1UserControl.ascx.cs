using System;
using System.Data;
using System.Collections;
using System.Web.UI;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.Security.Cryptography.X509Certificates;
using System.Web.Security;

namespace CreateReqCert.VisualWebPart1
{
    public partial class VisualWebPart1UserControl : UserControl
    {
        public bool bFromRegPage = false; //if the user is not registered, for him this web part will work as registration form
        protected void Page_Load(object sender, EventArgs e)
        {
            SPUser oUser = SPContext.Current.Web.CurrentUser;
            if (null == oUser)
            {
                bFromRegPage = true;
                PanelLogin.Visible = true;
            }

            if ( countryList.Items.Count == 0 )
            {
                //It's a ComboBox for the choosing country
                Hashtable country = new Hashtable();
                country.Add("Российская Федерация", "RU");
                country.Add("Австралия", "AU");
                country.Add("Австрия", "AT");
                country.Add("Азербайджан", "AZ");
            
                countryList.DataSource = country;
                countryList.DataTextField = "key";
                countryList.DataValueField = "value";
                try
                {
                    countryList.DataBind();
                }
                catch ( SqlException err )
                {
                    Page.RegisterStartupScript("ErrorScript", "<script language=javascript> alert(" + err.ToString() + ")</script>");
                }
                catch (InvalidOperationException err)
                {
                    Page.RegisterStartupScript("ErrorScript", "<script language=javascript> alert(" + err.ToString() + ")</script>");
                }
            }

            if (DestinationCertList.Items.Count == 0)
            {
                //It's a ComboBox for the choosing destination certificate - OID
                Hashtable hashDestinationCerts = new Hashtable();
                hashDestinationCerts.Add("Сертификат проверки подлинности сервера", "1.3.6.1.5.5.7.3.1");
                hashDestinationCerts.Add("Сертификат цифровой подписи", "1.3.6.1.5.5.7.3.3");
                hashDestinationCerts.Add("Сертификат защиты элетронной почты", "1.3.6.1.5.5.7.3.4");
                hashDestinationCerts.Add("Сертификат штампа времени подписи", "1.3.6.1.5.5.7.3.8");
                hashDestinationCerts.Add("Сертификат для работы с OCSP", "1.3.6.1.5.5.7.3.9");
                hashDestinationCerts.Add("Сертификат IKE-посредника IP-безопасности", "1.3.6.1.5.5.8.2.2");
                hashDestinationCerts.Add("Сертификат проверки подлинности клиента", "1.3.6.1.5.5.7.3.2");

                DestinationCertList.DataSource = hashDestinationCerts;
                DestinationCertList.DataTextField = "key";
                DestinationCertList.DataValueField = "value";
                DestinationCertList.DataBind();
            }

            if (ProviderList.Items.Count == 0)
            {
                //It's a ComboBox for the choosing crypto provider
                Hashtable hashProviders = new Hashtable();
                hashProviders.Add("Crypto-Pro GOST R 34.10-2001 Cryptographic Service Provider", "75");
                hashProviders.Add("Microsoft Base Cryptographic Provider v1.0", "1");

                ProviderList.DataSource = hashProviders;
                ProviderList.DataTextField = "key";
                ProviderList.DataValueField = "value";
                ProviderList.DataBind();
            }
        }
        //Create request for the new certificate
        protected void sendReqOnCert_Click(object sender, EventArgs e)
        {
            //Filling in final form with the fields of the new certificate
            LoginBox.Visible = false;
            LoginResult.Visible = true;
            LoginResult.Text = LoginBox.Text;

            CNBox.Visible = false;
            CNResultText.Visible = true;
            CNResultText.Text = CNBox.Text;

            emailBox.Visible = false;
            emailResultText.Visible = true;
            emailResultText.Text = emailBox.Text;

            organizationBox.Visible = false;
            organizationResultBox.Visible = true;
            organizationResultBox.Text = organizationBox.Text;

            regionBox.Visible = false;
            regionResultText.Visible = true;
            regionResultText.Text = regionBox.Text;

            countryList.Visible = false;
            countryResultText.Visible = true;
            countryResultText.Text = countryList.SelectedItem.Text;

            innBox.Visible = false;
            innResultText.Visible = true;
            innResultText.Text = innBox.Text;

            adressBox.Visible = false;
            adressResultText.Visible = true;
            adressResultText.Text = adressBox.Text;

            ProviderList.Visible = false;
            ProviderResultText.Visible = true;
            ProviderResultText.Text = ProviderList.SelectedItem.Text;

            DestinationCertList.Visible = false;
            DestinationCertResultText.Visible = true;
            DestinationCertResultText.Text = DestinationCertList.SelectedItem.Text;

            requestBox.Visible = false;
            requestResultText.Visible = true;
            requestResultText.Text = requestBox.Text;

            sendReqOnCert.Visible = false;
            ExportKeyCheckBox.Visible = false;

            if (requestBox.Text.Length != 0)
            {
                //Sent a request to create a new certificate on address of site and get him
                SendRequest objSendReq = new SendRequest();
                string sCert = objSendReq.get_Cert( CNBox.Text,
                    "http://cryptopro.ru/certsrv/",
                    requestBox.Text);

                if (0 == sCert.Length)
                {
                    Status.Visible = false;
                    Status.Text = "<h1>Сертификат не создан!!!</h1>";
                    return;
                }

                CertText.Visible = true;
                CertText.Text = sCert;

                byte[] byteCert = System.Text.Encoding.UTF8.GetBytes(sCert);
                X509Certificate2 objCert = new X509Certificate2(byteCert);
                // Add new certificate to dat base of certificates
                using (DCCertificatesDataContext context = new DCCertificatesDataContext(@"Data Source=.\SHAREPOINT;Initial Catalog=DBSharepointCertificates;Integrated Security=True;Pooling=False"))
                {
                    string currentUser = "";
                    if (!bFromRegPage)
                    {
                        bool bisAdmin = false;
                        GetCurrentUser(ref currentUser, ref bisAdmin, Page.Request.Url.ToString());
                    }
                    else
                    {
                        currentUser = LoginBox.Text;
                    }

                    RequestOfCerts tableRowOfReqCert = new RequestOfCerts
                    {
                        users = currentUser,
                        CN = CNBox.Text,
	                    mail = emailBox.Text,
		                organization = organizationBox.Text,
		                region = regionBox.Text,
                        country = countryList.SelectedItem.Text,
				        inn = innBox.Text,
		                address = adressBox.Text,
                        provider = ProviderList.SelectedItem.Text,
                        destinationCert = DestinationCertList.SelectedItem.Text,
                        request = requestBox.Text,
                        certificate = sCert,
                        SN = objCert.GetSerialNumberString(),
                        ExpData = objCert.GetExpirationDateString()
                    };
                    context.RequestOfCerts.InsertOnSubmit( tableRowOfReqCert );

                    try
                    {
                        context.SubmitChanges();

                        if(bFromRegPage)
                        {
                            Membership.CreateUser(LoginBox.Text, objCert.GetSerialNumberString().ToLower());
                            Status.Visible = true;
                            Status.Text = "Пользователь создан успешно!";
                            LBInstallCert.Visible = true;
                        }
                    }
                    catch (ChangeConflictException) //Обнаружить конфиликт совместимого доступа
                    {
                        context.ChangeConflicts.ResolveAll(RefreshMode.KeepChanges); //Разрешение конфликта
                    }
                    catch (MembershipCreateUserException err)
                    {
                        Status.Text = err.ToString();
                    }
                }
            }
        }

        public void GetCurrentUser( ref string sUser, ref bool bIsAdmin, string currentPage )
        {
            SPSite RootSite = new SPSite( currentPage );
            SPWeb SiteCollection = RootSite.OpenWeb();
            DateTime dateNow = DateTime.Now;
            SiteCollection.AllowUnsafeUpdates = true;
            string path = RootSite.MakeFullUrl(System.Web.HttpContext.Current.Request.Url.AbsolutePath);
            SPFile file = SiteCollection.GetFile(path);
            String LastModifiedDate = file.TimeLastModified.ToLongDateString();
            String ModifiedBy = file.ModifiedBy.ToString();
            SPUser currentUser = SiteCollection.CurrentUser;
            sUser = currentUser.Name;//LoginName;
            bIsAdmin = currentUser.IsSiteAdmin;
        }

        protected void cancel_Click(object sender, EventArgs e)
        {
            Response.Redirect(Request.Path);
        }
    }
}
