using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
//using CERTENROLLLib;

namespace CreateReqCert
{
    class SendRequest
    {
        public string get_Cert(
           string tsCertName,
           string tsURLAdress,
           string tsCertRequest)
        {
            // DN certificate requested:
            string strDn = "CN=" + tsCertName + ",O=CryptoPro,C=RU";

            // Requested and installed the root certificate.
            //InstallRootCert(tsURLAdress);

            // Send the request and receive the certificate.
            string certString = SubmitRequest(tsURLAdress, tsCertRequest);

            // Устанавливаем сертификат.
            //InstallResponse(certString);
            return certString;
        }

        // Obtaining and installing a certificate authority.
        void InstallRootCert(string address)
        {
            Uri httpSite = new Uri(address + "/certcarc.asp");

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(
                httpSite);
            httpRequest.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)httpRequest.GetResponse();

            Stream receiveStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(receiveStream,
                Encoding.UTF8);

            // Read the answer.
            string answer = readStream.ReadToEnd();
            response.Close();
            readStream.Close();

            // After this substring in the answer contains
            // the number of the current root CA certificate.
            string pattern = "var nRenewals=";
            int start = answer.IndexOf(pattern) + pattern.Length;
            string num = answer.Substring(start, answer.IndexOf(';', start) - start);
            // This email address is available the current root CA certificate.
            Uri certAdress = new Uri(address + "/certnew.cer?ReqID=CACert&Renewal=" + num + "&enc=bin");

            // Read the certificate.
            HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create(certAdress);
            request2.Method = "Get";
            HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse();
            Stream receiveStream2 = response2.GetResponseStream();

            int i = receiveStream2.ReadByte();
            byte[] buffer = new byte[0];
            while (i != -1)
            {
                Array.Resize<byte>(ref buffer, buffer.Length + 1);
                buffer[buffer.Length - 1] = (byte)i;
                i = receiveStream2.ReadByte();
            }

            // And install it in the root
            X509Store store = new X509Store("Root", StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            X509Certificate2 root = new X509Certificate2(buffer);
            store.Add(root);
        }

        // Sending the request and receiving the certificate
        string SubmitRequest(string address, string request)
        {
            // Create the certificate request form.
            StringBuilder strBuild = new StringBuilder("CertRequest=");

            // Each character in the string containing the query is replaced by its
            // hexadecimal representation, lowering the whitespace characters.
            foreach (char c in request.ToCharArray())
            {
                if (!Char.IsWhiteSpace(c))
                    strBuild.Append(Uri.HexEscape(c));
            }

            // The final form of the request sent to the certification center:
            strBuild.Append("&Mode=newreq&TargetStoreFlags=0&SaveCert=no&" + "FriendlyType=0");

            byte[] reqBytes = Encoding.ASCII.GetBytes(strBuild.ToString());

            // The address of page on which served request form
            // the certificate.
            Uri httpSite = new Uri(address + "/certfnsh.asp");

            // Формируем запрос.
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(httpSite);
            httpRequest.Method = "POST";
            httpRequest.ContentType = "application/x-www-form-urlencoded";
            httpRequest.ContentLength = reqBytes.Length;

            // Create the query.
            Stream newStream = httpRequest.GetRequestStream();
            newStream.Write(reqBytes, 0, reqBytes.Length);
            newStream.Close();

            // Get the response of the certification authority.
            HttpWebResponse response = (HttpWebResponse)httpRequest.GetResponse();
            Stream receiveStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);

            // Required certificate is generated, the response contains its
            // ID.
            string answer = readStream.ReadToEnd();
            response.Close();
            readStream.Close();

            // After this substring in the answer contains
            // the ID generated certificate
            string pattern = "ReqID=";
            int start = answer.IndexOf(pattern);
            while (start > 0 && answer.Substring(start + pattern.Length, 6).Equals("cacert", StringComparison.OrdinalIgnoreCase))
            {
                start = answer.IndexOf(pattern, start + 1);
            }
            if (start < 0)
                throw new ApplicationException("Неправильный формат ответа");
            start += pattern.Length;
            string ID = answer.Substring(start, answer.IndexOf('&', start) - start);
            // Address where available generated certificate:
            Uri certAdress = new Uri(address + "/certnew.cer?ReqID=" + ID + "&enc=bin");

            // Read the data at the specified address.
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(certAdress);
            req.Method = "GET";
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            receiveStream = res.GetResponseStream();

            byte[] buffer = new byte[0];
            int n = receiveStream.ReadByte();
            while (n != -1)
            {
                Array.Resize<byte>(ref buffer, buffer.Length + 1);
                buffer[buffer.Length - 1] = (byte)n;
                n = receiveStream.ReadByte();
            }

            // Encode in Base64
            return Convert.ToBase64String(buffer, 0, buffer.Length, Base64FormattingOptions.InsertLineBreaks);
        }

        //// Устанавливаем сертификат.
        //static void InstallResponse(string certString)
        //{
        //    // Объект, реализующий IX509Enrollment
        //    IX509Enrollment enroll = (IX509Enrollment)new CX509Enrollment();

        //    enroll.Initialize(X509CertificateEnrollmentContext.ContextUser);

        //    enroll.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedCertificate
        //        | InstallResponseRestrictionFlags.AllowUntrustedRoot,
        //        certString, EncodingType.XCN_CRYPT_STRING_BASE64, null);
        //}
    }
}
