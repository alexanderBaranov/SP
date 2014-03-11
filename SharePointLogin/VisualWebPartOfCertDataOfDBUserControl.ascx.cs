using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using CreateReqCert.VisualWebPart1;
using DigtCryptoLib;
using System.Security.Cryptography.X509Certificates;

namespace CreateReqCert.VisualWebPartOfCertDataOfDB
{
    public partial class VisualWebPartOfCertDataOfDBUserControl : UserControl
    {
        //Load a data base certificates
        protected void Page_Load(object sender, EventArgs e)
        {
            using (DCCertificatesDataContext db = new DCCertificatesDataContext(@"Data Source=.\SHAREPOINT;Initial Catalog=DBSharepointCertificates;Integrated Security=True;Pooling=False"))
            {
                string currentUser = "";
                bool bisAdmin = false;

                VisualWebPart1UserControl oWebPart1 = new VisualWebPart1UserControl();
                oWebPart1.GetCurrentUser(ref currentUser, ref bisAdmin, Page.Request.Url.ToString()); //Get current user

                GridReqCert.DataSource = bisAdmin ? db.RequestOfCerts : db.RequestOfCerts.Where(c => c.users == currentUser); //Check right admin or user
                GridReqCert.DataBind();
            }
        }
        //Divide table on the page
        protected void gridView_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            GridReqCert.PageIndex = e.NewPageIndex;
            GridReqCert.DataBind();
        }
        //Shown in the table following fields: image status, status text, blob of certificat.
        protected void gridView_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                ImageButton IBControl = (ImageButton)e.Row.FindControl("ShowCert");
                string sImageUrl = string.Empty;
                string sStatus = string.Empty;
                string sCert = DataBinder.Eval(e.Row.DataItem, "certificate").ToString();

                CheckStatusOfCert( 
                    sCert, 
                    ref sImageUrl, 
                    ref sStatus);

                IBControl.ToolTip = sStatus;
                IBControl.ImageUrl = sImageUrl;

                DateTime df = DateTime.Parse(new X509Certificate2(System.Text.Encoding.UTF8.GetBytes( sCert )).GetExpirationDateString());
                if ((DateTime.Now.CompareTo(df) < 0) && ((df - DateTime.Now).Days < 30))
                {
                    ((Image)e.Row.FindControl("WarningImage")).Visible = true;
                }
            }
        }

        public void CheckStatusOfCert(String sCert, ref string sImageUrl, ref string sStatus)
        {
            try
            {
                Certificate oCert = new DigtCryptoLib.Certificate();
                oCert.Import(sCert);
                VERIFYSTATUS vStatus = oCert.IsValid(POLICY_TYPE.POLICY_TYPE_NONE);

                switch ( vStatus )
                {
                    case VERIFYSTATUS.VS_CORRECT:
                        {
                            sImageUrl = "cert_ok.ico";
                            sStatus = "Корректен";
                            break;
                        }
                    case VERIFYSTATUS.VS_UNSUFFICIENT_INFO:
                        {
                            sImageUrl = "cert_unknown.ico";
                            sStatus = "Статус неизвестен";
                            break;
                        }
                    case VERIFYSTATUS.VS_UNCORRECT:
                        {
                            sImageUrl = "cert_bad.ico";
                            sStatus = "Некорректен";
                            break;
                        }
                    case VERIFYSTATUS.VS_INVALID_CERTIFICATE_BLOB:
                        {
                            sImageUrl = "cert_bad.ico";
                            sStatus = "Недействительный блоб сертификата";
                            break;
                        }
                    case VERIFYSTATUS.VS_CERTIFICATE_TIME_EXPIRIED:
                        {
                            sImageUrl = "cert_bad.ico";
                            sStatus = "Время действия сертификата истекло или еще не наступило";
                            break;
                        }
                    case VERIFYSTATUS.VS_CERTIFICATE_NO_CHAIN:
                        {
                            sImageUrl = "cert_bad.ico";
                            sStatus = "Невозможно построить цепочку сертификации";
                            break;
                        }
                    case VERIFYSTATUS.VS_LOCAL_CRL_NOT_FOUND:
                        {
                            sImageUrl = "cert_unknown.ico";
                            sStatus = "Не найден локальный СОС";
                            Util oUtil = new Util();
                            oUtil.UpdateCrlByCert(oCert);

                            CheckStatusOfCert(sCert, ref sImageUrl, ref sStatus);
                            break;
                        }
                    case DigtCryptoLib.VERIFYSTATUS.VS_CRL_TIME_EXPIRIED:
                        {
                            sImageUrl = "cert_unknown.ico";
                            sStatus = "Истекло время действия СОС";
                            Util oUtil = new Util();
                            oUtil.UpdateCrlByCert(oCert);

                            CheckStatusOfCert(sCert, ref sImageUrl, ref sStatus);
                            break;
                        }
                    case DigtCryptoLib.VERIFYSTATUS.VS_CERTIFICATE_IN_CRL:
                        {
                            sImageUrl = "cert_bad.ico";
                            sStatus = "Сертфикат находится в СОС";
                            break;
                        }
                    case DigtCryptoLib.VERIFYSTATUS.VS_CERTIFICATE_IN_LOCAL_CRL:
                        {
                            sImageUrl = "cert_bad.ico";
                            sStatus = "Сертфикат находится в локальном СОС";
                            break;
                        }
                    case DigtCryptoLib.VERIFYSTATUS.VS_CERTIFICATE_CORRECT_BY_LOCAL_CRL:
                        {
                            sImageUrl = "cert_ok.ico";
                            sStatus = "Сертификат действителен по локальному СОС";
                            break;
                        }
                    case DigtCryptoLib.VERIFYSTATUS.VS_CERTIFICATE_USING_RESTRICTED:
                        {
                            sImageUrl = "cert_bad.ico";
                            sStatus = "Использование сертификата ограничено";
                            break;
                        }
                    default:
                        {
                            sImageUrl = "cert_unknown.ico";
                            sStatus = "Неизвестный статус:" + vStatus;
                            break;
                        }
                }

                sImageUrl = "~/_layouts/images/ImagesOfSPCertificate/" + sImageUrl;

            }
            catch (Exception err)
            {
                Page.RegisterStartupScript("ErrorScript", "<script language=javascript> alert(" + err.ToString() + ")</script>");
            }
        }
    }
}
