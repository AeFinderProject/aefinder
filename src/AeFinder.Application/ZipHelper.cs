using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Http;

namespace AeFinder;

public class ZipHelper
{
    public static void ZipDirectory(string zipFileName, string sourceDirectory)
    {
        var zip = new FastZip();
        zip.CreateZip(zipFileName, sourceDirectory,true, string.Empty);
    }

    public static void UnZip(Stream source, Stream destination, string entryName = null)
    {
        using (var s = new ZipInputStream(source))
        {
            ZipEntry theEntry;
            while ((theEntry = s.GetNextEntry()) != null)
            {
                if (!entryName.IsNullOrWhiteSpace() &&
                    !string.Equals(theEntry.Name, entryName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var buffer = new byte[4096];
                StreamUtils.Copy(s, destination, buffer);
                destination.Seek(0, SeekOrigin.Begin);
                break;
            }
        }
    }
}