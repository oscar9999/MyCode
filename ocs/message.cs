using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using OCSCallerCSharp;

/// <summary>
/// Summary description for message
/// </summary>
[WebService(Namespace = "http://servername/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
[System.Web.Script.Services.ScriptService]
public class message : System.Web.Services.WebService {

    public message () {

        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    [WebMethod]
    public string SendMessage(String subject,String content,String sendtos) {
        OCSCall call = new OCSCall();
        call.sendMessage(subject, content, sendtos);
        return "ok";
    }
    
}