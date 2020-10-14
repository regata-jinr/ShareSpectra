using Microsoft.VisualStudio.TestTools.UnitTesting;
using Extensions;

namespace ExtensionsTest
{
    [TestClass]
    public class FileManagerTest
    {
        private const string PathBase = @"D:\Spectra\test\2019\05";

        public FileManagerTest()
        {
            FileManager.ConString = @"Server=RUMLAB\REGATALOCAL;Database=NAA_DB_TEST;Trusted_Connection=True;";
        }

        [TestMethod]
        public void UploadFileTest()
        {

        }

        [TestMethod]
        public void UploadFilesTest()
        {
        }

        [TestMethod]
        public void UploadFoldersTest()
        {
        }

        [TestMethod]
        public void UploadNonExistedFolderTest()
        {
        }

        [TestMethod]
        public void UploadFolderWithoutCNFFilesTest()
        {
        }

        [TestMethod]
        public void UploadNonSpectraFilesTest()
        {
        }

        [TestMethod]
        public void UploadFilesFromLogTest()
        {
        }

        [TestMethod]
        public void UploadFilesFromEmptyLogTest()
        {
        }
        [TestMethod]
        public void UploadFilesFromNonExistedLogTest()
        {
        }

        [TestMethod]
        public void UploadFileWithRewritingTest()
        {
        }

        [TestMethod]
        public void UploadFileWithErrorAndErrorLogIsBusyTest()
        {
        }
    }
}
