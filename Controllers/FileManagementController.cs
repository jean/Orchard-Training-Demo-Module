/*
 * In Orchard Core if you want to work with assets you can use the Media feature that will help you manage your files
 * in the tenant's Media folder from the dashboard. If the OrchardCore.Media.Azure is enabled the storage will be the
 * Azure Blob Storage. But what if you want to upload, delete or read files right from the code and maybe outside the
 * Media folder?
 *
 * There is a service called IFileStore which is not registered in the service provider. The features that need to do
 * specific file operations have an own service which inherits from IFileStore (or somehow depends on this). For
 * example the IMediaFileStore service can be used if you want to manage files in the Media folder. This is accessible
 * from anywhere in the code if the Media feature is enabled.
 *
 * If you want to manage files anywhere on the file system you need to have your own service which will use the
 * FileSystemStore (which implements the IFileStore interface) with your own base path given. You'll see an example for
 * that too.
 */

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lombiq.TrainingDemo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.FileStorage;
using OrchardCore.Media;

namespace Lombiq.TrainingDemo.Controllers
{
    public class FileManagementController : Controller
    {
        // Let's have the paths here in constants to avoid repeating ourselves.
        private const string TestFileRelativePath = "TrainingDemo/TestFile1.txt";
        private const string UploadedFileFolderRelativePath = "TrainingDemo/Uploaded";


        private readonly IMediaFileStore _mediaFileStore;
        private readonly INotifier _notifier;
        private readonly IHtmlLocalizer H;
        private readonly ICustomFileStore _customFileStore;


        public FileManagementController(
            IMediaFileStore mediaFileStore,
            INotifier notifier,
            IHtmlLocalizer<FileManagementController> htmlLocalizer,
            ICustomFileStore customFileStore)
        {
            _mediaFileStore = mediaFileStore;
            _notifier = notifier;
            _customFileStore = customFileStore;
            H = htmlLocalizer;
        }


        // This action will demonstrate how to create a file in the Media folder and read it from there.
        public async Task<string> CreateFileInMediaFolder()
        {
            // You need to initialize a stream if you have a specific text you want to write in the file. If you
            // already have a stream for that just use it (you'll see it later)!
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hi there!")))
            {
                // The third parameter here is optional - if true, it will override the file if already exists.
                await _mediaFileStore.CreateFileFromStream(TestFileRelativePath, stream, true);
            }

            // Use this method to check if the file exists (it will be null if the file doesn't exist). It's similar to
            // the built-in FileInfo class but not that robust.
            var fileInfo = await _mediaFileStore.GetFileInfoAsync(TestFileRelativePath);

            // The IMediaFileStore has its own specific methods such as mapping the file path to a public URL. Since
            // the files in the Media folder are accessible from outside this can be handy.
            var publicUrl = _mediaFileStore.MapPathToPublicUrl(TestFileRelativePath);

            return $"Successfully created file! File size: {fileInfo.Length} bytes. Public URL: {publicUrl}";
        }

        // If you've created the file just go to the Dashboard and check if you can see it in the Assets. Also you can
        // find it in the App_Data/Sites/{TenantName}/Media/TrainingDemo folder.

        // This action will read the file you've created earlier.
        public async Task<string> ReadFileFromMediaFolder()
        {
            // This way you can check if the given file exists.
            if (await _mediaFileStore.GetFileInfoAsync(TestFileRelativePath) == null)
            {
                return "Create the file first!";
            }

            // If you want to extract the content of the file use a StreamReader to read the stream.
            using (var stream = await _mediaFileStore.GetFileStreamAsync(TestFileRelativePath))
            using (var streamReader = new StreamReader(stream))
            {
                var content = streamReader.ReadToEnd();

                return $"File content: {content}";
            }
        }

        // Now let's see a scenario where you have a file uploader component and you want to save that file to the
        // Media folder. If you want to see how our demo uploader looks like then go to the
        // Views/FileManagement/UploadFileToMedia.cshtml.
        public ActionResult UploadFileToMedia() => View();

        [HttpPost, ActionName(nameof(UploadFileToMedia))]
        public async Task<ActionResult> UploadFileToMediaPost(IFormFile file)
        {
            // You can use the Combine method to combine paths which is pretty much equivalent to the built-in method.
            var mediaFilePath = _mediaFileStore.Combine(UploadedFileFolderRelativePath, file.FileName);

            // In this case you already have a stream so use it to create file.
            using (var stream = file.OpenReadStream())
            {
                await _mediaFileStore.CreateFileFromStream(mediaFilePath, stream);
            }

            _notifier.Information(H["Successfully uploaded file!"]);

            return RedirectToAction(nameof(UploadFileToMedia));
        }

        // NEXT STATION: Services/CustomFileStore.cs

        public async Task<string> CreateFileInCustomFolder()
        {
            // Now it will be the same process as it was with the IMediaFileStore but it will be our ICustomFileStore
            // this time. The files will be created inside our CustomFiles folder as it was described in Startup.cs.
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("Hi there in the custom file storage!")))
            {
                await _customFileStore.CreateFileFromStream(TestFileRelativePath, stream, true);
            }

            var fileInfo = await _customFileStore.GetFileInfoAsync(TestFileRelativePath);

            return $"Successfully created file in the custom file storage! File size: {fileInfo.Length} bytes.";
        }
    }
}