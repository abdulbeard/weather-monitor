using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather.Monitor
{
    public class ImageRepository
    {
        private CloudBlobContainer blobContainer;
        private CloudBlobContainer tableReportsBlobContainer;
        public ImageRepository(CloudBlobClient blobClient)
        {
            blobContainer = blobClient.GetContainerReference("forecast-images");
            blobContainer.CreateIfNotExistsAsync().Wait();

            tableReportsBlobContainer = blobClient.GetContainerReference("table-reports");
            tableReportsBlobContainer.CreateIfNotExistsAsync().Wait();
        }

        public async Task<string> UploadImage(MemoryStream stream, string contentType)
        {
            CloudBlockBlob cloudBlockBlob = blobContainer.GetBlockBlobReference(Guid.NewGuid().ToString());
            cloudBlockBlob.Properties.ContentType = contentType;
            var byteArray = stream.GetBuffer();
            await cloudBlockBlob.UploadFromByteArrayAsync(byteArray, 0, byteArray.Length);
            return cloudBlockBlob.Uri.OriginalString;
        }

        public async Task<string> UploadHtmlTableResults(MemoryStream stream, string contentType)
        {
            CloudBlockBlob cloudBlockBlob = tableReportsBlobContainer.GetBlockBlobReference(Guid.NewGuid().ToString());
            cloudBlockBlob.Properties.ContentType = contentType;
            var byteArray = stream.GetBuffer();
            await cloudBlockBlob.UploadFromByteArrayAsync(byteArray, 0, byteArray.Length);
            return cloudBlockBlob.Uri.OriginalString;
        }
    }
}
