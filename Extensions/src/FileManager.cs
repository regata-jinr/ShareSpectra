using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Extensions
{
    //TODO: add tests!

    public static class FileManager
    {
        public static bool   DoesRewrite = true;

        public static TimeSpan ConTimeOut = TimeSpan.FromSeconds(10);

        private static IReadOnlyDictionary<string, string> typeID = new Dictionary<string, string> {
            { "SLI-1", "kji"   },
            { "SLI-2", "kji"   },
            { "LLI-1", "dji-1" },
            { "LLI-2", "dji-2" },
        };

        private static async Task WriteError(SharingFilesErrors se)
        {
            using (var ic = new InfoContext())
            {
                if (ic.UnSharedFiles.Where(u => u.fileS == se.fileS).Any()) return;

                var ct = new CancellationTokenSource();
                ct.CancelAfter(ConTimeOut);
                await ic.UnSharedFiles.AddAsync(se, ct.Token);
            }
        }
        
        public static async Task CopyAndUpload(string fileS, string typeI)
        {
            string newFile = "";
            try
            {
                if (!typeID.ContainsKey(typeI)) return;
                typeI = typeID[typeI];

                var dir = Path.Combine(@"D:\Spectra",
                                       DateTime.Now.Year.ToString(),
                                       DateTime.Now.Month.ToString("D2"),
                                       typeI);

                Directory.CreateDirectory(dir);

                newFile = Path.Combine(dir, Path.GetFileName(fileS));
                File.Copy(fileS, newFile, true);

                if (File.Exists(newFile))
                    File.Delete(fileS);

               await UploadFileToCloud(newFile);

            }
            catch (Exception ex)
            {

                if (string.IsNullOrEmpty(newFile)) return;

                var sfe = new SharingFilesErrors()
                {
                    fileS = Path.GetFileNameWithoutExtension(newFile),
                    fileSPath = Path.GetDirectoryName(newFile),
                    ErrorMessage = ex.Message
                };
                WriteError(sfe);
            }
        }

        private static async Task UploadFileToCloud(string file)
        {
            try
            {
                var ct = new CancellationTokenSource();
                ct.CancelAfter(ConTimeOut);

                var token = "";
                if (await WebDavClientApi.UploadFile(file, ct.Token))
                    token = await WebDavClientApi.MakeShareable(file, ct.Token);

                if (string.IsNullOrEmpty(token)) throw new InvalidOperationException("File hasn't got token!");


                var ss = new SharedSpectra()
                {
                    fileS = Path.GetFileNameWithoutExtension(file),
                    token = token
                };

                using (var ic = new InfoContext())
                {
                    bool IsExists = ic.SharedSpectra.Where(s => s.fileS == ss.fileS).Any();

                    if (IsExists && DoesRewrite)
                    {
                        ic.SharedSpectra.Update(ss);
                        return;
                    }

                    var ctn = new CancellationTokenSource();
                    ctn.CancelAfter(ConTimeOut);
                    await ic.SharedSpectra.AddAsync(ss, ctn.Token);

                    var sss = await ic.UnSharedFiles.Where(s => s.fileS == ss.fileS).FirstOrDefaultAsync();

                    if (sss != null)
                        ic.UnSharedFiles.Remove(sss);
                }
            }
            catch (Exception ex)
            {
                var sfe = new SharingFilesErrors() 
                {
                    fileS = Path.GetFileNameWithoutExtension(file),
                    fileSPath = Path.GetDirectoryName(file),
                    ErrorMessage = ex.Message
                };
                WriteError(sfe);
            }
            
        }

        public static async Task UploadFilesToCloud(string[] files)
        {

            foreach (var f in files)
                await UploadFileToCloud(f);
            
        }

        public static async Task UploadFilesFromFolders(string[] folders)
        {
            foreach (var dir in folders)
                await UploadFilesToCloud(Directory.GetFiles(dir, "*.cnf")); 
        }

        public static async Task ShowFilesWithErrors()
        {
            
        }
        public static async Task UploadFilesWithErrors()
        {
            using (var ic = new InfoContext())
            {
                await UploadFilesToCloud(ic.UnSharedFiles.Select(s => s.ToString()).ToArray());
            }
        }
      
    } // public static class FileManager
}     // namespace Extensions

