using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace Umbraco.Core.Packaging
{
    public class UnpackHelper : IUnpackHelper
    {
        public string ReadTextFileFromArchive(string packageFilePath, string fileToRead)
        {
            CheckPackageExists(packageFilePath);

            using (var fs = File.OpenRead(packageFilePath))
            {
                using (var zipStream = new ZipInputStream(fs))
                {
                    ZipEntry zipEntry;
                    while ((zipEntry = zipStream.GetNextEntry()) != null)
                    {
                        if (zipEntry.Name.EndsWith(fileToRead, StringComparison.CurrentCultureIgnoreCase))
                        {
                            using (var reader = new StreamReader(zipStream))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }

                    zipStream.Close();
                }
                fs.Close();
            }

            throw new FileNotFoundException(string.Format("Could not find file in package file {0}", packageFilePath), fileToRead);
        }

        private static void CheckPackageExists(string packageFilePath)
        {
            if (File.Exists(packageFilePath) == false)
                throw new ArgumentException(string.Format("Package file: {0} could not be found", packageFilePath),
                    "packageFilePath");
        }


        public bool CopyFileFromArchive(string packageFilePath, string fileInPackageName, string destinationfilePath)
        {
            CheckPackageExists(packageFilePath);

            bool fileFoundInArchive = false;
            bool fileOverwritten = false;

            using (var fs = File.OpenRead(packageFilePath))
            {
                using (var zipInputStream = new ZipInputStream(fs))
                {
                    ZipEntry zipEntry;
                    while ((zipEntry = zipInputStream.GetNextEntry()) != null)
                    {
                        if(zipEntry.Name.Equals(fileInPackageName))
                        {
                            fileFoundInArchive = true;

                            fileOverwritten = File.Exists(destinationfilePath);

                            using (var streamWriter = File.Open(destinationfilePath, FileMode.Create))
                            {
                                var data = new byte[2048];
                                int size;
                                while ((size = zipInputStream.Read(data, 0, data.Length)) > 0)
                                {
                                    streamWriter.Write(data, 0, size);
                                }

                                streamWriter.Close();
                            }

                            break;
                        }
                    }

                    zipInputStream.Close();
                }
                fs.Close();
            }

            if (fileFoundInArchive == false) throw new ArgumentException(string.Format("Could not find file: {0} in package file: {1}", fileInPackageName, packageFilePath), "fileInPackageName");

            return fileOverwritten;
        }

        public IEnumerable<string> FindMissingFiles(string packageFilePath, IEnumerable<string> expectedFiles)
        {
            CheckPackageExists(packageFilePath);

            var exp = expectedFiles.ToDictionary(k => k, v => true);

            using (var fs = File.OpenRead(packageFilePath))
            {
                using (var zipInputStream = new ZipInputStream(fs))
                {
                    ZipEntry zipEntry;
                    while ((zipEntry = zipInputStream.GetNextEntry()) != null)
                    {
                        if (exp.ContainsKey(zipEntry.Name))
                        {
                            exp[zipEntry.Name] = false;
                        }
                    }

                    zipInputStream.Close();
                }
                fs.Close();
            }


            return exp.Where(kv => kv.Value).Select(kv => kv.Key);

        }
    }
}