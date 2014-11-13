// Available at https://github.com/Developer-Autodesk/Library-view.and.data.api-javascript

//Change '3dViewDiv' to the Div id in your application that will be associated with the viewer
var divid = '3dViewDiv';

// Bucket to use for uploading the file
var bucketName = null;

var baseurl = "https://developer.api.autodesk.com";
var env = 'AutodeskProduction';

// File to Load in viewer
var fileName2Load = null;

// Access token to use for http requests to the view and data api server
var _accessToken = null;

// Viewer 3D
var viewer3D;

var xmlhttp;

// Timer to check the translation progress
var translationProgressTimer = null;

// Geometry nodes
var geometryItems;
var geometryFilter3d = { 'type': 'geometry', 'role': '3d' };

// Viewer Document
var currentViewerDoc;

// Document Id that is to be loaded
var documentId;

$(document).ready(function () {
    // initialisation stuff 

    var accessTokenElem = document.getElementById("AccessTokenFld");
    _accessToken = accessTokenElem.value;
    //alert(_accessToken);

    setToken();

    var documentURNElem = document.getElementById("ViewerURNFld");
    documentId = documentURNElem.value;
    //alert(documentId);
    //*/

    // Translation is started, but may not be complete
    //  So lets periodically check the translation progress.
    if (translationProgressTimer == null) {
        translationProgressTimer = setInterval(CheckTranslationProgress, 3000);
    }
  
});

// Load the viewer document after we have the document urn
function LoadViewerDocument() {
    var options = {
        'document': documentId,
        'accessToken': _accessToken,
        'env': env
    };

    Autodesk.Viewing.Initializer(options, function () {
        // Create a Viewer3D. 
        var viewer3DContainerDiv = document.getElementById(divid);

        //viewer3D = new Autodesk.Viewing.BaseViewer3D(viewer3DContainerDiv, {});
        viewer3D = new Autodesk.Viewing.Private.GuiViewer3D(viewer3DContainerDiv, {});
        //viewer3D = new Autodesk.Viewing.Viewer3D(viewer3DContainerDiv, {});

        // Initialize the viewer
        viewer3D.initialize();

        // Load the document and associate the document with our Viewer3D
        Autodesk.Viewing.Document.load("urn:"+ documentId, onSuccessDocumentLoadCB, onErrorDocumentLoadCB);
    });
}

// Document successfully loaded 
function onSuccessDocumentLoadCB(viewerDocument) {

    currentViewerDoc = viewerDocument;
    var rootItem = viewerDocument.getRootItem();

    //store in globle variable
    geometryItems = Autodesk.Viewing.Document.getSubItemsWithProperties(rootItem, geometryFilter3d, true);

    if (geometryItems.length > 0) {

        var item3d = viewerDocument.getViewablePath(geometryItems[0]);

        // Load the viewable to the viewer
        viewer3D.load(item3d);

        console.log("Loading 3d Geometry from document : " + documentId);
    }
    else {
        console.log("3d Geometry not found in document : " + documentId);
    }
}

// Some error during document load
function onErrorDocumentLoadCB(errorMsg, errorCode) {
    console.log("Unable to load the document : " + documentId + errorMsg);
}

function xmlHttpRequestHandler() {
    //console.log(xmlhttp.responseText);
    //if (xmlhttp.readyState == 4 && xmlhttp.status==200)  
    //{
    //  OK 
    //}
}

// This is expected to set the cookie upon server response
// Subsequent http requests to this domain will automatically send the cookie for authentication
function setToken() {
    xmlhttp = new XMLHttpRequest();
    xmlhttp.open('POST', baseurl + '/utility/v1/settoken', false);
    xmlhttp.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
    xmlhttp.onreadystatechange = xmlHttpRequestHandler;
    xmlhttp.onerror = xmlHttpRequestErrorHandler;
    xmlhttp.withCredentials = true;
    xmlhttp.send("access-token=" + _accessToken);
}

// This function will be called periodically to check the status of the 
// file translation. If the translation is done, it will call try loading the document in viewer
function CheckTranslationProgress() {
    if (documentId) {
       
        var base64URN = documentId;

        xmlhttp = new XMLHttpRequest();
        xmlhttp.open('GET', baseurl + '/viewingservice/v1/' + base64URN, false);
        xmlhttp.onreadystatechange = function () {
            if (xmlhttp.readyState == 4 && xmlhttp.status == 200) {
                // From the response string, get the progress.
                var response = xmlhttp.responseText;
                var passedTXT = response;
                var index = passedTXT.indexOf("\"progress\":\"") + "\"progress\":\"".length;
                var status = passedTXT.substring(index, passedTXT.indexOf("\"", index + 1));
                updateProgressBar(parseFloat(status))
                if (status == "complete") {
                    // Done, lets load the document in viewer.
                    updateProgressBar(100);
                    Ready();
                }
            }
        };
        xmlhttp.onerror = xmlHttpRequestErrorHandler;
        xmlhttp.withCredentials = true;
        xmlhttp.send();
    }
    //*/
}

function updateProgressBar(percentCompleted) {
    var percent = Math.round((percentCompleted * 100) / 100);
    document.getElementById("szliderbar").style.width = percent + '%';
    document.getElementById("szazalek").innerHTML = percent + '%';
}

// Callback for http web request
function xmlHttpRequestHandler() {
    //if (xmlhttp.readyState == 4 && xmlhttp.status==200)  
    //{
    //  OK 
    //}
}

// Callback in case of error for http web request
function xmlHttpRequestErrorHandler() {
    // Reset busy indicator
    console.log(xmlhttp.responseText);
    normal();
}

// Resets the busy indicator and loads the document in viewer.
function Ready() {

    // Reset busy indicator
    normal();

    if (translationProgressTimer != null) {
        // Stop the translation progress check timer.
        window.clearInterval(translationProgressTimer);
        translationProgressTimer = null;
    }

    // Load the document in viewer
    LoadViewerDocument();
}

// Error callback in case of issues in reading the file contents
function errorHandler(evt) {

    // Reset busy indicator
    normal();

    switch (evt.target.error.code) {
        case evt.target.error.NOT_FOUND_ERR:
            console.log("FileReader error : File Not Found!");
            break;

        case evt.target.error.NOT_READABLE_ERR:
            console.log("FileReader error : File is not readable");
            break;

        case evt.target.error.ABORT_ERR:
            console.log("FileReader error : Abort error");
            break;  // noop

        default:
            console.log("FileReader error : Unknown error");
            break;
    };
}

function updateProgress(evt) {
    // evt is an ProgressEvent.
    if (evt.lengthComputable) {
        var percentLoaded = Math.round((evt.loaded / evt.total) * 100);
        // Increase the progress bar length.
        if (percentLoaded < 100) {
        }
    }
}

function busy() {
    // Display busy...
    // Change to wait cursor
    $("body").css("cursor", "progress");

    document.getElementById("szliderbar").style.width = 0 + '%';
    document.getElementById("szazalek").innerHTML = 'Please wait...';
}

function normal() {
    // Display normal...
    // Restore normal cursor
    $("body").css("cursor", "default");

    document.getElementById("szliderbar").style.width = 100 + '%';
    document.getElementById("szazalek").innerHTML = 100 + '%';
}