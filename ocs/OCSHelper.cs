using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Rtc.Collaboration;
using Microsoft.Rtc.Signaling;
using System.Net;
namespace OCSCallerCSharp.help
{
    class OCSHelper
    {
        private String _ocsaccount = "osc_account";
        private String _ocsaccountemail = "osc_account@oscar.com";
        private String _oscaccountpsd = "oscar";
        private String _oscaccountdomain = "DOMAIN";
        private String _sip = "sip.oscar.com";

        private String _logfilename = "osclog.txt";
        private CollaborationPlatform _collabPlatform;
       // private static CollaborationPlatform _serverCollabPlatform;
        private UserEndpoint _userEndpoint;
        private static string _applicationName = "OCS Call";
        private bool _isPlatformStarted;
        private AutoResetEvent _platformShutdownCompletedEvent = new AutoResetEvent(false);
        private AutoResetEvent _platformStartupCompleted = new AutoResetEvent(false);
        private AutoResetEvent _endpointInitCompletedEvent = new AutoResetEvent(false);
        private Microsoft.Rtc.Signaling.SipTransportType _transportType = Microsoft.Rtc.Signaling.SipTransportType.Tls;

        public OCSHelper()
        {
        }

        public OCSHelper(OCSHelper source)
        {
        }

        public void log(String slog)
        {
            //string fname = Directory.GetCurrentDirectory() + "\\" + _logfilename;
            string fname =  "D:\\iis_project\\log" + "\\" + _logfilename;
           
            FileInfo finfo = new FileInfo(fname);
            if (!finfo.Exists)
            {
                FileStream fs = File.Create(fname);
                fs.Close();
                finfo = new FileInfo(fname);
            }
            if(finfo.Length>1024*1024*200)
            {
                File.Move(Directory.GetCurrentDirectory()+"\\"+_logfilename,Directory.GetCurrentDirectory()+DateTime.Now.TimeOfDay+"\\"+_logfilename);
            }
           /* try
            {
                using (FileStream fs = finfo.OpenWrite())
                {
                    StreamWriter w = new StreamWriter(fs);
                    w.BaseStream.Seek(0, SeekOrigin.End);
                    w.Write("{0}{1}\n\r",DateTime.Now.ToLongTimeString(),DateTime.Now.ToLongDateString());
                    w.Write(slog + "\n\r");
                    w.Write("----------------------------------------------\n\r");
                    w.Flush();
                    w.Close();
                    fs.Close();
                }
            }catch(Exception e){
                Console.WriteLine(e.ToString());
                File.Create(fname).Close();
            }finally{

            }*/

        }


        internal void ShutdownPlatform()
        {
            if (_collabPlatform != null)
            {
                _collabPlatform.BeginShutdown(EndPlatformShutdown, _collabPlatform);
            }
           // if (_serverCollabPlatform!=null)
           // {
           //     _serverCollabPlatform.BeginShutdown(EndPlatformShutdown, _serverCollabPlatform);
           // }
            _platformShutdownCompletedEvent.WaitOne();
        }
       
        public UserEndpoint CreateEstablishedUserEndpoint()
        {
            UserEndpointSettings userEndpointSettings;
            UserEndpoint userEndpoint = null;
            try
            {
                userEndpointSettings = new UserEndpointSettings("sip:" + _ocsaccountemail, _sip);
                //userEndpointSettings.Credential = System.Net.CredentialCache.DefaultNetworkCredentials;
                userEndpointSettings.Credential = new NetworkCredential(_ocsaccount, _oscaccountpsd, _oscaccountdomain);
                log("Login success : srv_eplm");
                userEndpoint = CreateUserEndpoint(userEndpointSettings);
                EstablishUserEndpoint(userEndpoint);
            }
            catch (InvalidOperationException iOpEx)
            {
                log("Invalid Operation Exception: " + iOpEx.ToString());
            }
            return userEndpoint;
        }

        public UserEndpoint CreateUserEndpoint(UserEndpointSettings userEndpointSettings)
        {
            if (_collabPlatform == null)
            {
                // Initalize and startup the platform.
                ClientPlatformSettings clientPlatformSettings = new ClientPlatformSettings(_applicationName, _transportType);
                _collabPlatform = new CollaborationPlatform(clientPlatformSettings);
            }

            _userEndpoint = new UserEndpoint(_collabPlatform, userEndpointSettings);
            return _userEndpoint;
        }

        public bool EstablishUserEndpoint(UserEndpoint userEndpoint)
        {
            if (_isPlatformStarted == false)
            {
                userEndpoint.Platform.BeginStartup(EndPlatformStartup, userEndpoint.Platform);
                _platformStartupCompleted.WaitOne();
                _isPlatformStarted = true;
            }
            userEndpoint.BeginEstablish(EndEndpointEstablish, userEndpoint);
            _endpointInitCompletedEvent.WaitOne();
            return true;
        }

        private void EndPlatformShutdown(IAsyncResult ar)
        {
            CollaborationPlatform collabPlatform = ar.AsyncState as CollaborationPlatform;
            try
            {
                //xueming add
                //_userEndpoint.EndEstablish();

                collabPlatform.EndShutdown(ar);
               //_collabPlatform.EndShutdown(ar);
                _userEndpoint = null;
                _collabPlatform = null;
                _isPlatformStarted = false;
            }
            finally
            {
                _platformShutdownCompletedEvent.Set();
            }
        }

        private void EndPlatformStartup(IAsyncResult ar)
        {
            CollaborationPlatform collabPlatform = ar.AsyncState as CollaborationPlatform;
            try
            {
                collabPlatform.EndStartup(ar);
            }
            catch (OperationFailureException opFailEx)
            {
                log(opFailEx.Message);
                throw;
            }
            catch (ConnectionFailureException connFailEx)
            {
                log(connFailEx.Message);
                throw;
            }
            catch (RealTimeException realTimeEx)
            {
                // RealTimeException may be thrown as a result of any UCMA operation.
                log(realTimeEx.Message);
                throw;
            }
            finally
            {
                // Again, just for sync. reasons.
                _platformStartupCompleted.Set();
            }
        }

        private void EndEndpointEstablish(IAsyncResult ar)
        {
            LocalEndpoint currentEndpoint = ar.AsyncState as LocalEndpoint;
            try
            {
                currentEndpoint.EndEstablish(ar);
            }
            catch (AuthenticationException authEx)
            {
                log(authEx.ToString());
                throw;
            }
            catch (ConnectionFailureException connFailEx)
            {
                // ConnectionFailureException will be thrown when the endpoint cannot connect to the server, or the credentials are invalid.
                log(connFailEx.Message);
                throw;
            }
            catch (InvalidOperationException iOpEx)
            {
                // InvalidOperationException will be thrown when the endpoint is not in a valid state to connect. To connect, the platform must be started and the Endpoint Idle.
                log(iOpEx.Message);
                throw;
            }
            finally
            {
                // Again, just for sync. reasons.
                _endpointInitCompletedEvent.Set();
            }
        }
    }
}
