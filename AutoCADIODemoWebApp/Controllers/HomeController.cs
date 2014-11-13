using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

using System.Text;
using System.IO;
using RestSharp;

namespace MvcApplication2.Controllers
{
    public class HomeController : Controller
    {
        // Closet parameters
        private static string _width = String.Empty;
        private static string _depth = String.Empty;
        private static string _height = String.Empty;
        private static string _plyThickness = String.Empty;
        private static string _doorHeightPercentage = String.Empty;
        private static string _numberOfDrawers = String.Empty;
        private static int _iNumOfDrawers = 1;
        private static bool _isSplitDrawers = true;
        private static string _emailAddress = String.Empty;

        // View and Data API 
        RestClient _client = new RestClient("https://developer.api.autodesk.com");
        static String _accessToken = String.Empty;
        static String _bucketName = "autocadiobucket";
        static Boolean _bucketFound = false;
        static String _closetDrawingPath = String.Empty; // for email attachment
        static String _imagePath = String.Empty; //  for email attachment
        static String _fileUrn = String.Empty; // For viewing using View & Data API

        public HomeController()
        {
            // Set up AutoCAD IO
            Autodesk.AcadIOUtils.SetupAutoCADIOContainer(Properties.Settings.Default.AutoCADIOClientId, Properties.Settings.Default.AutoCADIOClientSecret);

            Autodesk.GeneralUtils.S3BucketName = Properties.Settings.Default.S3BucketName;

            // Set up View and Data API
            SetupViewer();
            //*/
        }

        void SetupViewer()
        {
            // Authentication
            bool authenticationDone = false;

            RestRequest authReq = new RestRequest();
            authReq.Resource = "authentication/v1/authenticate";
            authReq.Method = Method.POST;
            authReq.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            authReq.AddParameter("client_id", UserSettings.CONSUMER_KEY);
            authReq.AddParameter("client_secret", UserSettings.CONSUMER_SECRET);
            authReq.AddParameter("grant_type", "client_credentials");

            IRestResponse result = _client.Execute(authReq);
            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                String responseString = result.Content;
                int len = responseString.Length;
                int index = responseString.IndexOf("\"access_token\":\"") + "\"access_token\":\"".Length;
                responseString = responseString.Substring(index, len - index - 1);
                int index2 = responseString.IndexOf("\"");
                _accessToken = responseString.Substring(0, index2);

                //Set the token.
                RestRequest setTokenReq = new RestRequest();
                setTokenReq.Resource = "utility/v1/settoken";
                setTokenReq.Method = Method.POST;
                setTokenReq.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                setTokenReq.AddParameter("access-token", _accessToken);

                IRestResponse resp = _client.Execute(setTokenReq);
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    authenticationDone = true;
                }
            }

            if(! authenticationDone)
            {
                 ViewData["Message"] = "View and Data client authentication failed !";
                _accessToken = String.Empty;
                return;
            }

            RestRequest bucketReq = new RestRequest();
            bucketReq.Resource = "oss/v1/buckets";
            bucketReq.Method = Method.POST;
            bucketReq.AddParameter("Authorization", "Bearer " + _accessToken, ParameterType.HttpHeader);
            bucketReq.AddParameter("Content-Type", "application/json", ParameterType.HttpHeader);

            //bucketname is the name of the bucket.
            string body = "{\"bucketKey\":\"" + _bucketName + "\",\"servicesAllowed\":{},\"policy\":\"transient\"}";
            bucketReq.AddParameter("application/json", body, ParameterType.RequestBody);

           result = _client.Execute(bucketReq);

            if (result.StatusCode == System.Net.HttpStatusCode.Conflict ||
                result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                _bucketFound = true;
            }
            else
            {
                ViewData["Message"] = "View and Data bucket could not be accessed !";
                _bucketFound = false;
                return;
            }
        }

        void UploadDrawingFile(String drawingFilePath)
        {
            _fileUrn = String.Empty;

            RestRequest uploadReq = new RestRequest();

            string strFilename = System.IO.Path.GetFileName(drawingFilePath);
            string objectKey = HttpUtility.UrlEncode(strFilename);

            FileStream file = System.IO.File.Open(drawingFilePath, FileMode.Open);
            byte[] fileData = null;
            int nlength = (int)file.Length;
            using (BinaryReader reader = new BinaryReader(file))
            {
                fileData = reader.ReadBytes(nlength);
            }

            uploadReq.Resource = "oss/v1/buckets/" + _bucketName + "/objects/" + objectKey;
            uploadReq.Method = Method.PUT;
            uploadReq.AddParameter("Authorization", "Bearer " + _accessToken, ParameterType.HttpHeader);
            uploadReq.AddParameter("Content-Type", "application/stream");
            uploadReq.AddParameter("Content-Length", nlength);
            uploadReq.AddParameter("requestBody", fileData, ParameterType.RequestBody);

            IRestResponse resp = _client.Execute(uploadReq);

            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string responseString = resp.Content;

                int len = responseString.Length;
                string id = "\"id\" : \"";
                int index = responseString.IndexOf(id) + id.Length;
                responseString = responseString.Substring(index, len - index - 1);
                int index2 = responseString.IndexOf("\"");
                string urn = responseString.Substring(0, index2);

                byte[] bytes = Encoding.UTF8.GetBytes(urn);
                string urn64 = Convert.ToBase64String(bytes);

                RestRequest bubleReq = new RestRequest();
                bubleReq.Resource = "viewingservice/v1/register";
                bubleReq.Method = Method.POST;
                bubleReq.AddParameter("Authorization", "Bearer " + _accessToken, ParameterType.HttpHeader);
                bubleReq.AddParameter("Content-Type", "application/json;charset=utf-8", ParameterType.HttpHeader);

                string body = "{\"urn\":\"" + urn64 + "\"}";
                bubleReq.AddParameter("application/json", body, ParameterType.RequestBody);

                IRestResponse BubbleResp = _client.Execute(bubleReq);

                String fileId = String.Format("urn:adsk.objects:os.object:{0}/{1}", _bucketName, objectKey);
                byte[] bytes1 = Encoding.UTF8.GetBytes(fileId);
                string urn641 = Convert.ToBase64String(bytes1);

                if (BubbleResp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //Translation started
                    _fileUrn = urn64;
                }
                else if (BubbleResp.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    //Translated file already present
                    _fileUrn = urn64;
                }
                else
                {
                    // Error
                    _fileUrn = String.Empty;
                }
            }
        }

        bool CheckProgress()
        {
            bool isComplete = false;

            if (String.IsNullOrEmpty(_fileUrn))
                return false;

            RestRequest statusReq = new RestRequest();
            statusReq.Resource = "/viewingservice/v1/" + _fileUrn;
            statusReq.Method = Method.GET;
            statusReq.AddParameter("Authorization", "Bearer " + _accessToken, ParameterType.HttpHeader);
            IRestResponse result = _client.Execute(statusReq);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                dynamic json = SimpleJson.DeserializeObject(result.Content);
                System.Collections.Generic.Dictionary<string, object>.KeyCollection keys = json.Keys;
                System.Collections.Generic.Dictionary<string, object>.ValueCollection Values = json.Values;

                for (int i = 0; i < Values.Count; i++)
                {
                    var key = keys.ElementAt(i);
                    var item = Values.ElementAt(i);
                    if (key is string && item is string)
                    {
                        if (String.Compare((string)key, "progress") == 0)
                        {
                            String percentComplete = (string)item;
                            if(percentComplete.Contains("complete"))
                            {
                                isComplete = true;
                                break;
                            }
                        }
                    }
                }
            }

            return isComplete;
        }

        public ActionResult Index(Models.ClosetModel cm, String Command)
        {
            ViewBag.Message = " ";

            if (String.IsNullOrEmpty(Command))
            {
                cm.ViewerURN = String.Empty;
                cm.AccessToken = _accessToken;

                return View(cm);
            }

            // Validation
            String message = ValidateInputs(cm);
            if (!String.IsNullOrEmpty(message))
            {
                ViewData["Message"] = message;
                return View(cm);
            }

            _emailAddress = cm.EmailAddress;
            if (Command.Contains("Email"))
            {
                if (String.IsNullOrEmpty(_emailAddress) || !IsValidEmail(_emailAddress))
                {// Invalid email address
                    ViewData["Message"] = "Please provide a valid email address";
                    return View(cm);
                }
            }

            try
            {
                // Create a drawing with closet created based on the user inputs
                String baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Create the closet drawing
                String templateDwgPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BlankIso.dwg");
                            
                String script = String.Format("CreateCloset{0}{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}_.VSCURRENT{0}sketchy{0}_.Zoom{0}Extents{0}_.SaveAs{0}{0}Result.dwg{0}", Environment.NewLine, _width, _depth, _height, _plyThickness, _doorHeightPercentage, _numberOfDrawers, _isSplitDrawers ? 1 : 0);
                if (Autodesk.AcadIOUtils.UpdateActivity("CreateCloset", script))
                {
                    String resultDrawingPath = String.Empty;

                    // Get the AutoCAD IO result by running "CreateCloset" activity
                    resultDrawingPath = GetAutoCADIOResult(templateDwgPath, "CreateCloset");

                    if (!String.IsNullOrEmpty(resultDrawingPath))
                    {
                        // Get a PNG image from the drawing for email attachment
                        _imagePath = GetAutoCADIOResult(resultDrawingPath, "PlotToPNG");

                        System.IO.File.Copy(_imagePath, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\Preview.png"), true);
                        _imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images\\Preview.png");

                        DateTime dt = DateTime.Now;
                        _closetDrawingPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, String.Format("Closet{0}{1}{2}{3}{4}{5}{6}.dwg", dt.Year.ToString(), dt.Month.ToString(), dt.Day.ToString(), dt.Hour.ToString(), dt.Minute.ToString(), dt.Second.ToString(), dt.Millisecond.ToString()));
                        System.IO.File.Copy(resultDrawingPath, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _closetDrawingPath), true);

                        // Send an email with drawing and image as attachments
                        if (Command.Contains("Email"))
                        {
                            if (SendEmail())
                            {
                                ViewData["Message"] = "Email sent !!";
                            }
                            else
                                ViewData["Message"] = "Sorry, Email was not sent !!";
                        }

                        // Preview the drawing in Viewer
                        if (Command.Contains("Preview"))
                        {
                            // Get the urn to show in viewer
                            if(_bucketFound)
                            {
                                UploadDrawingFile(_closetDrawingPath);

                                // Required for the view to reflect changes in the model
                                ModelState.Clear();
                                cm.ViewerURN = _fileUrn;
                                cm.AccessToken = _accessToken;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // An error !
                ViewData["Message"] = ex.Message;
            }
            //*/

            return View(cm);
        }

        private String ValidateInputs(Models.ClosetModel cm)
        {
            // Validation
            _width = cm.Width;
            if (String.IsNullOrEmpty(_width))
            {// Width is not provided
                return "Please provide a value for the closet width in feet";
            }

            _depth = cm.Depth;
            if (String.IsNullOrEmpty(_depth))
            {// Depth is not provided
                return "Please provide a value for the closet depth in feet";
            }

            _height = cm.Height;
            if (String.IsNullOrEmpty(_height))
            {// Height is not provided
                return "Please provide a value for the closet height in feet";
            }

            _plyThickness = cm.PlyThickness;
            if (String.IsNullOrEmpty(_plyThickness))
            {// Ply Thickness is not provided
                return "Please provide a value for the Ply thickness in inches";
            }

            _doorHeightPercentage = cm.DoorHeightPercentage;
            if (String.IsNullOrEmpty(_doorHeightPercentage))
            {// Door Height is not provided
                return "Please provide a value for the door height as a percentage of total closet height";
            }

            _numberOfDrawers = cm.NumberOfDrawers;
            if (String.IsNullOrEmpty(_numberOfDrawers))
            {// Number of drawers is not provided
                return "Please provide the number of drawers";
            }
            _iNumOfDrawers = 1;
            if (!int.TryParse(_numberOfDrawers, out _iNumOfDrawers))
            {// Invalid entry for number of apps
                return "Please provide the number of drawers";
            }

            _isSplitDrawers = cm.IsSplitDrawers;

            return String.Empty;
        }

        private bool SendEmail()
        {
            try
            {
                //create the mail message
                using (System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage())
                {
                    //set the addresses
                    mail.From = new System.Net.Mail.MailAddress(UserSettings.MAIL_USERNAME);
                    mail.To.Add(_emailAddress);

                    //set the content
                    mail.Subject = "Closet Drawing";
                    mail.Attachments.Add(new System.Net.Mail.Attachment(_closetDrawingPath));

                    //first we create the Plain Text part
                    System.Net.Mail.AlternateView plainView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(String.Format("{0}Width (feet) : {1}{0}Depth (feet) : {2}{0}Height (feet) : {3}{0}Ply Thickness (inches) : {4}{0}Door Height as % of total height: {5}{0}Number of drawers : {6}{0}Is Split drawers ? : {7}{0}"
                        , Environment.NewLine, _width, _depth, _height, _plyThickness, _doorHeightPercentage, _iNumOfDrawers, _isSplitDrawers), null, "text/plain");

                    //then we create the Html part
                    //to embed images, we need to use the prefix 'cid' in the img src value
                    //the cid value will map to the Content-Id of a Linked resource.
                    //thus <img src='cid:companylogo'> will map to a LinkedResource with a ContentId of 'companylogo'
                    System.Net.Mail.AlternateView htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString("<html><body><h3>Here is a preview of the closet. AutoCAD drawing file is attached.</h3><br/><img src='cid:closetimg'/></body></html>", null, "text/html");

                    if (! String.IsNullOrEmpty(_imagePath))
                    {
                        //create the LinkedResource (embedded image)
                        System.Net.Mail.LinkedResource closetLR = new System.Net.Mail.LinkedResource(_imagePath);
                        closetLR.ContentId = "closetimg";
                        htmlView.LinkedResources.Add(closetLR);
                    }

                    //add the views
                    mail.AlternateViews.Add(plainView);
                    mail.AlternateViews.Add(htmlView);

                    //send the message
                    using (System.Net.Mail.SmtpClient smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587))
                    {
                        smtpClient.Credentials = new System.Net.NetworkCredential(UserSettings.MAIL_USERNAME, UserSettings.MAIL_PASSWORD);
                        smtpClient.EnableSsl = true;
                        smtpClient.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string strIn)
        {
            // Return true if strIn is in valid e-mail format.
            return Regex.IsMatch(strIn, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        private String GetAutoCADIOResult(string drawingPath, String activityId)
        {
            String resultPath = String.Empty;
            if (System.IO.File.Exists(drawingPath))
            {
                try
                {
                    // Step 1 : Upload the drawing to S3 storage
                    String hostDwgS3Url = Autodesk.GeneralUtils.UploadDrawingToS3(drawingPath);

                    if (String.IsNullOrEmpty(hostDwgS3Url))
                        return "UploadDrawingToS3 returned empty url";

                    // Step 2 : Submit an AutoCAD IO Workitem using the activity id
                    String resulturl = Autodesk.AcadIOUtils.SubmitWorkItem(activityId, hostDwgS3Url);

                    // Step 3 : Display the result in a web browser and download the result
                    if (String.IsNullOrEmpty(resulturl) == false)
                    {
                        Autodesk.GeneralUtils.Download(resulturl, ref resultPath);
                        if (String.IsNullOrEmpty(resultPath))
                        {
                            resultPath = String.Format("Download resultPath is empty !!");
                        }

                    }
                    else
                        resultPath = "SubmitWorkItem returned empty string";
                }
                catch (System.Exception ex)
                {
                    resultPath = ex.Message;
                }
                finally
                {
                }
            }
            else
                resultPath = String.Format("{0} does not exist !! File.Exists false", drawingPath);

            return resultPath;
        }


    }
}
