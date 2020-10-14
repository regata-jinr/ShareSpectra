using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Linq;

namespace Extensions
{
    //TODO: add tests!

    public static class FileManager
    {
        public const string ErrorFilePath = @"D:\Spectra\CloudErrors.json";

        public static string ConString = "";
        public static bool   DoesRewrite = true;

        private static IReadOnlyDictionary<string, string> typeID = new Dictionary<string, string> {
            { "SLI-1", "kji"   },
            { "SLI-2", "kji"   },
            { "LLI-1", "dji-1" },
            { "LLI-2", "dji-2" },
        };

        private static void WriteError(SharedError se)
        {
            if (File.ReadAllText(ErrorFilePath).Contains(Path.GetFileNameWithoutExtension(se.FileSpectra))) return;

            // FIXME: in case of more the one detector in parallel exception will be thrown
            File.AppendAllLines(ErrorFilePath, new string[] { se.ToString() });
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
                WriteError(new SharedError { FileSpectra = newFile, ErrorMessage = ex.Message }); 
            }
        }

        private static async Task UploadFileToCloud(string file)
        {
            try
            {
                if (string.IsNullOrEmpty(ConString)) throw new InvalidOperationException("Connection string is not specified");

                var ct = new CancellationTokenSource();
                ct.CancelAfter(TimeSpan.FromSeconds(15));
                var token = "";
                if (await WebDavClientApi.UploadFile(file, ct.Token))
                    token = await WebDavClientApi.MakeShareable(file, ct.Token);

                if (string.IsNullOrEmpty(token)) throw new InvalidOperationException("File hasn't got token!");

                bool IsExists = false;
                using (SqlConnection con = new SqlConnection(ConString))
                {
                    var query = @"select count(*) from SharedSpectra where fileS = @files";
                    using (SqlCommand sCmd = new SqlCommand(query, con))
                    {
                        sCmd.Parameters.AddWithValue("@files", Path.GetFileNameWithoutExtension(file));
                        con.Open();
                        IsExists = ((int)sCmd.ExecuteScalar() != 0);
                    }
                }

                if (!DoesRewrite && IsExists) return;

                using (SqlConnection con = new SqlConnection(ConString))
                {
                    var query = @"exec removeSpectra @files";
                    using (SqlCommand sCmd = new SqlCommand(query, con))
                    {
                        sCmd.Parameters.AddWithValue("@files", Path.GetFileNameWithoutExtension(file));
                        con.Open();
                        sCmd.ExecuteScalar();
                    }
                }

                using (SqlConnection con = new SqlConnection(ConString))
                {
                    var query = @"exec addSpectra @files, @token";
                    using (SqlCommand sCmd = new SqlCommand(query, con))
                    {
                        sCmd.Parameters.AddWithValue("@files", Path.GetFileNameWithoutExtension(file));
                        sCmd.Parameters.AddWithValue("@token", Path.GetFileNameWithoutExtension(token));
                        con.Open();
                        sCmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new SharedError { FileSpectra = file, ErrorMessage = ex.Message }); 
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

        public static async Task UploadFilesFromLog(string logPath)
        {
                await UploadFilesToCloud(File.ReadAllLines(logPath).Select(l => l.Split('\t')[0]).ToArray()); 
        }

        private static void RemoveFileFromLog(string file)
        {
            var fileCont = File.ReadAllLines(ErrorFilePath);

            if (!fileCont.Where(f => f.Contains(file)).Any()) return;

            // FIXME: in case of more the one detector in parallel exception will be thrown
            File.WriteAllLines(ErrorFilePath, fileCont.Where(f => !f.Contains(file)).ToArray());
        }

    } // public static class FileManager
}     // namespace Extensions

