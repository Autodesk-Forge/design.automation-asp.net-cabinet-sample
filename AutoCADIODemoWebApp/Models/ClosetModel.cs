using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;

namespace MvcApplication2.Models
{
    public class ClosetModel
    {
        [Display(Name = "Width (feet)")]
        public string Width { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "Depth (feet)")]
        public string Depth { get; set; }

        [Display(Name = "Total Height (feet)")]
        public string Height { get; set; }

        [Display(Name = "Door Height (% of Total Height)")]
        public string DoorHeightPercentage { get; set; }

        [Display(Name = "Ply Thickness (inches)")]
        public string PlyThickness { get; set; }

        [Display(Name = "Number of drawers")]
        public string NumberOfDrawers { get; set; }

        [Display(Name = "Split Drawers ?")]
        public bool IsSplitDrawers { get; set; }

        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "ViewerURN")]
        public string ViewerURN { get; set; }

        [DataType(DataType.Text)]
        [Display(Name = "AccessToken")]
        public string AccessToken { get; set; }

        public ClosetModel()
        {
            Width = "6";
            Depth = "3";
            Height = "8";
            PlyThickness = "2";
            DoorHeightPercentage = "40";
            NumberOfDrawers = "3";
            IsSplitDrawers = true;
            EmailAddress = "";
            ViewerURN = String.Empty;
            AccessToken = String.Empty;
        }
    }
}
