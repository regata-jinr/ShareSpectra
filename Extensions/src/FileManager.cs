using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Extensions
{
    public static class FileManager
    {
        public static async Task<bool> UploadFileToCloud(string file, CancellationToken Token)
        {
            var result = false;
            var token = "";
            result = await WebDavClientApi.UploadFile(file, Token);
            if (result)
                token = await WebDavClientApi.MakeShareable(file, Token);
            else
                throw new InvalidOperationException("Can't upload file");

            if (string.IsNullOrEmpty(token)) throw new InvalidOperationException("File hasn't got token!");

            var ss = new SharedSpectra()
            {
                fileS = Path.GetFileNameWithoutExtension(file),
                token = token
            };

            using (var ic = new InfoContext())
            {
                bool IsExists = ic.SharedSpectra.Where(s => s.fileS == ss.fileS).Any();

                if (IsExists)
                {
                    ic.SharedSpectra.Update(ss);
                }
                else
                {
                    await ic.SharedSpectra.AddAsync(ss, Token);
                }

                await ic.SaveChangesAsync(Token);
                return result;
            }
        }

    } // public static class FileManager
}     // namespace Extensions

