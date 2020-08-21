using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace VMFGenerator
{
    public static class Visgroups
    {
        public static Dictionary<string, int> VisgroupNameToID = new Dictionary<string, int>();
        public static Dictionary<string, Color> VisgroupNameToColor = new Dictionary<string, Color>(); 
        public const string TAR_LAYOUT = "tar_layout";
        public const string TAR_MASK = "tar_mask";
        public const string TAR_COVER = "tar_cover";
    }
}
