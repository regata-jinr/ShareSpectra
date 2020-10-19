using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Extensions
{
    public static class FileManager
    {
        public static bool   DoesRewrite = true;

        private static CancellationToken Token { 
            get
            {
                var ct = new CancellationTokenSource();
                ct.CancelAfter(ConTimeOut);
                return ct.Token;
            } 
        }

        public static TimeSpan ConTimeOut = TimeSpan.FromSeconds(20);

        private static IReadOnlyDictionary<string, string> typeID = new Dictionary<string, string> {
            { "SLI-1", "kji"   },
            { "SLI-2", "kji"   },
            { "LLI-1", "dji-1" },
            { "LLI-2", "dji-2" },
        };

        private static async Task WriteError(SharingFilesErrors se)
        {
            try
            {
                using (var ic = new InfoContext())
                {
                    if (ic.UnSharedFiles.Where(u => u.fileS == se.fileS).Any()) return;

                    await ic.UnSharedFiles.AddAsync(se, Token);
                    await ic.SaveChangesAsync(Token);
                }
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException($"Connection timeout for writing error message to DB for file {se}");
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
            catch (OperationCanceledException oce)
            {
                throw oce;
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
               await WriteError(sfe);
            }
        }

        private static async Task UploadFileToCloud(string file)
        {
            try
            {
                var token = "";
                if (await WebDavClientApi.UploadFile(file, Token))
                    token = await WebDavClientApi.MakeShareable(file, Token);

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
                    }
                    else if (!IsExists)
                    {
                        await ic.SharedSpectra.AddAsync(ss, Token);
                    }
                    else return;

                    var sss = await ic.UnSharedFiles.Where(s => s.fileS == ss.fileS).FirstOrDefaultAsync(Token);

                    if (sss != null)
                        ic.UnSharedFiles.Remove(sss);

                    await ic.SaveChangesAsync(Token);

                }
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException($"Connection timeout while uploading file {file} to cloud");
            }
            catch (Exception ex)
            {
                var sfe = new SharingFilesErrors() 
                {
                    fileS = Path.GetFileNameWithoutExtension(file),
                    fileSPath = Path.GetDirectoryName(file),
                    ErrorMessage = ex.Message
                };
               await WriteError(sfe);
            }
        }

        public static async Task UploadFilesToCloud(string[] files)
        {
            foreach (var f in files)
                await UploadFileToCloud(f);
        }

        public static async Task<List<SharingFilesErrors>> ShowFilesWithErrors()
        {
            try
            {
                using (var ic = new InfoContext())
                {
                    return await ic.UnSharedFiles.ToListAsync(Token);
                }
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException($"Connection timeout while trying to get files list form errors register");
            }
        }

        public static async Task UploadFilesWithErrors()
        {
            try
            {
                using (var ic = new InfoContext())
                {
                    if (!await ic.UnSharedFiles.AnyAsync(Token)) return;

                    await UploadFilesToCloud(await ic.UnSharedFiles.Select(s => s.ToString()).ToArrayAsync(Token));
                }
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException($"Connection timeout while uploading files from errors register");
            }
        }
      
    } // public static class FileManager
}     // namespace Extensions

