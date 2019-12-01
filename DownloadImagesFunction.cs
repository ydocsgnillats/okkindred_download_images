using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace okkindred_download_images
{
    public static class DownloadImagesFunction
    {
        static HttpClient httpClient;

        [FunctionName("okkindred_download_images")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("okkindred_download_images C# HTTP trigger function processed a request.");

            string requestBody;
            using (var sr = new StreamReader(req.Body))
            {
                requestBody = await sr.ReadToEndAsync();
            }

            var data = JsonConvert.DeserializeObject<RequestBody>(requestBody);

            // Check request
            if (data == null)
            {
                return new BadRequestObjectResult("Invalid request body");
            }

            if (data.images == null || data.images.Length == 0)
            {
                return new BadRequestObjectResult("images array missing from request body");
            }

            if (data.token == null || data.token.Length == 0)
            {
                return new BadRequestObjectResult("token missing from request body");
            }

            if (data.zip_filename == null || data.zip_filename.Length == 0)
            {
                return new BadRequestObjectResult("zip_filename missing from request body");
            }

            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }

            var downloadTasks = new List<Task<DownloadData>>();

            foreach (var image in data.images)
            {
                downloadTasks.Add(GetImageStream(image));
            }

            var outputMemStream = new MemoryStream();            
            using (var zipStream = new ZipOutputStream(outputMemStream))
            {
                zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

                // Cycles through downloaded images as they complete
                while (downloadTasks.Count > 0)
                {
                    var firstFinishedTask = await Task.WhenAny(downloadTasks);
                    downloadTasks.Remove(firstFinishedTask);

                    var downloadData = await firstFinishedTask;

                    var newEntry = new ZipEntry(downloadData.filename);
                    newEntry.DateTime = DateTime.Now;
   
                    zipStream.PutNextEntry(newEntry);

                    StreamUtils.Copy(downloadData.stream, zipStream, new byte[4096]);

                    zipStream.CloseEntry();
                }


                zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
                zipStream.Close();          // Must finish the ZipOutputStream before using outputMemStream.
            }

            outputMemStream.Position = 0;

            var response = new FileStreamResult(outputMemStream, "application/octet-stream");
            response.FileDownloadName = data.zip_filename;

            return response;           
        }

        private static async Task<DownloadData> GetImageStream(string file)
        {
            var downloadData = new DownloadData(file);
            var response = await httpClient.GetAsync(file);            
            downloadData.stream = await response.Content.ReadAsStreamAsync();

            return downloadData;
        }
    }
}
