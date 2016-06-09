workflow-aspdotnet-autocad.io
=============================

(Formely AutoCAD I/O)

[![.net](https://img.shields.io/badge/.net-4.5-green.svg)](http://www.microsoft.com/en-us/download/details.aspx?id=30653)
[![odata](https://img.shields.io/badge/odata-4.0-yellow.svg)](http://www.odata.org/documentation/)
[![ver](https://img.shields.io/badge/Design%20Automation%20API-2.0-blue.svg)](https://developer.autodesk.com/api/autocadio/v2/)
[![visual studio](https://img.shields.io/badge/Visual%20Studio-2012%7C2013-yellow.svg)](https://www.visualstudio.com/)
[![License](http://img.shields.io/:license-mit-red.svg)](http://opensource.org/licenses/MIT)

##Description

An ASP.Net Web application that uses Design Automation and Derivitive API to preview and create a closet drawing

##Dependencies
* As this sample includes a reference to  [design.automation-.net-library](https://github.com/Developer-Autodesk/design.automation-.net-library), please build that sample firstly.  
* Visual Studio 2012. 2013 or 2015 should be also fine, but has not yet been tested.
* Get [credentials of AWS](http://docs.aws.amazon.com/general/latest/gr/aws-security-credentials.html) and create one S3 bucket
* Get your credentials of Design Automation at http://developer.autodesk.com* 


Also, please ensure that the following custom activities have been created in AutoCAD IO. 
These activities can be created using the UI provided by “workflow-winform-autocad.io” sample if you wish. 
Here is the link to that sample : https://github.com/Developer-Autodesk/workflow-winform-autocad.io

The scripts used by the custom activities are provided below :

![Picture](https://github.com/Developer-Autodesk/workflow-aspdotnet-autocad.io/blob/master/assets/CustomActivities.PNG)

The script used by the CreateCloset activity makes use of a custom command named “CreateCloset”. 
This is part of CreateCloset.bundle included in this sample. 
Create an AppPackage using this bundle and link it to the CreateCloset activity while creating it.

After you have the initial setup ready, 
Open the AutoCADIODemoWebApp sample project in Visual Studio 2012
Add reference to AutoCADIOUtil library

In the project settings, provide the following details:
-	AutoCAD IO Client Id
-	AutoCAD IO Client Secret
-	Bucket name in your AWS S3 Storage

![Picture](https://github.com/Developer-Autodesk/workflow-aspdotnet-autocad.io/blob/master/assets/1.png)

Open “Web.Config” file and provide AWS credentials. 
This will allow the sample project to access Amazon S3 storage in your AWS profile.

![Picture](https://github.com/Developer-Autodesk/workflow-aspdotnet-autocad.io/blob/master/assets/2.png)

Open “UserSettings.cs” file and provide View & Data API credentials. 
This will allow the sample project to load the drawing in a viewer using Autodesk view & data API.

Also provide your email credentials. 
This will allow this web application to send the drawing as an attachment in an email.

![Picture](https://github.com/Developer-Autodesk/workflow-aspdotnet-autocad.io/blob/master/assets/3.png)

Build the sample project
Host the web app or run it locally. This will display the web page as shown in below screenshot :

![Picture](https://github.com/Developer-Autodesk/workflow-aspdotnet-autocad.io/blob/master/assets/4.png)

Change the closet parameters as needed.
Click on “Preview” button
This generates a drawing with the closet model using AutoCAD IO and the drawing is loaded in the viewer
as shown in below screenshot.

![Picture](https://github.com/Developer-Autodesk/workflow-aspdotnet-autocad.io/blob/master/assets/5.png)

Click on “Send Email” button
This generate a drawing with the closet model using AutoCAD IO and this drawing is emailed as an attachment
to the email id provided. A screenshot of the email that is sent is shown below.

![Picture](https://github.com/Developer-Autodesk/workflow-aspdotnet-autocad.io/blob/master/assets/6.png)
