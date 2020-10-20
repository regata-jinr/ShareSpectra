using Microsoft.VisualStudio.TestTools.UnitTesting;
using Extensions;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CanberraDataAccessLib;
using System;

namespace ExtensionsTest
{
    [TestClass]
    public class FileManagerTest
    {
        private const string _pathLLI1Test = @"D:\Spectra\test\dji-1";
        private const string _pathSLITest  = @"D:\Spectra\test\kji";
        private const string _ext = ".cnf";

        private IReadOnlyList<string> _sliFiles = Directory.GetFiles(_pathSLITest).ToList();
        private IReadOnlyList<string> _lli1Files = Directory.GetFiles(_pathLLI1Test).ToList();

        private string MatchDir(string file, string type)
        {
            return Path.Combine(@"D:\Spectra",
                                  DateTime.Now.Year.ToString(),
                                  DateTime.Now.Month.ToString("D2"),
                                  type, file);
        }


        public FileManagerTest()
        {
            var i = 0;
            foreach (var f in Directory.GetFiles(@"D:\Spectra\2020\03\dji-1").Take(3).ToArray())
            {
                File.Copy(f, Path.Combine(_pathSLITest, $"test{i++}{_ext}"),true);
                File.Copy(f, Path.Combine(_pathLLI1Test, $"test{i++}{_ext}"),true);
            }
        }

        [TestMethod]
        public async Task UploadFileTest()
        {
            var ct = new CancellationTokenSource();
            ct.CancelAfter(TimeSpan.FromSeconds(10));
            await FileManager.CopyAndUpload(_sliFiles[0], "SLI-2");

            var nf = MatchDir(Path.GetFileName(_sliFiles[0]), "kji");
            Assert.IsTrue(await WebDavClientApi.IsExists(nf, ct.Token));
            Assert.IsTrue(await WebDavClientApi.RemoveFile(nf, ct.Token));
            Assert.IsFalse(await WebDavClientApi.IsExists(nf, ct.Token));
        }

        [TestMethod]
        public async Task UploadFilesTest()
        {
            foreach (var f in _lli1Files)
            {
                var ct = new CancellationTokenSource();
                await FileManager.CopyAndUpload(f, "LLI-1");

                var nf = MatchDir(Path.GetFileName(f), "dji-1");
                ct.CancelAfter(TimeSpan.FromSeconds(10));
                Assert.IsTrue(await WebDavClientApi.IsExists(nf, ct.Token));
                Assert.IsTrue(await WebDavClientApi.RemoveFile(nf, ct.Token));
                Assert.IsFalse(await WebDavClientApi.IsExists(nf, ct.Token));
            }
        }

        [TestMethod]
        public async Task FillErrorsLogAndUploadTest()
        {
            DataAccess spec = new DataAccessClass();
            try
            {
                var cf = _sliFiles[0];

                // make file busy
                spec.Open(cf,OpenMode.dReadWrite);

                await FileManager.CopyAndUpload(cf, "SLI-2");

                var lst = await FileManager.ShowFilesWithErrors();
                Assert.IsTrue(lst.Any());

                spec.Close();
                
                await FileManager.UploadFilesWithErrors();
                lst = await FileManager.ShowFilesWithErrors();
                Assert.IsFalse(lst.Any());
            }
            finally
            {
                if (spec.IsOpen)
                    spec.Close();
            }
        }

       
        [TestMethod]
        public async Task UploadFilesFromLogTest()
        {
            await FileManager.UploadFilesWithErrors();
            var lst = await FileManager.ShowFilesWithErrors();
            Assert.IsFalse(lst.Any());
        }

    }
}
