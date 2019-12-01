using System;
using System.Collections.Generic;
using System.Text;

namespace okkindred_download_images
{
    public class RequestBody
    {
        public string token { get; set; }
        public string[] images { get; set; }
        public string zip_filename { get; set; }
    }
}
