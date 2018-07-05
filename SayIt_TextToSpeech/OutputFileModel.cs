using Java.IO;
using System.IO;

namespace SayIt_TextToSpeech
{
    class OutputFileModel
    {
        private readonly string _fileLocation;
        private readonly Java.IO.File _downloadPath;

        public string FileName { get; set; }
        public Java.IO.File DownloadPath => _downloadPath;
        public string FilePath => Path.Combine(DownloadPath.AbsolutePath, _fileLocation);
        public string FileFullName => Path.Combine(FilePath, FileName);

        public OutputFileModel(string fileLocation)
        {
            _fileLocation = fileLocation;
            _downloadPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
        }
    }
}