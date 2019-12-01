using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace okkindred_download_images
{
    public class DownloadData
    {
        public DownloadData(string url)
        {
            this.url = url;
            this.filename = Path.GetFileName(this.url);
        }

        public string url { get; private set; }
        public string filename { get; private set; }
        public Stream stream { get; set; }
    }
}
