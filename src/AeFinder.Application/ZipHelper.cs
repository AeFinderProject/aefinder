using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;
using Ionic.Zlib;

namespace AeFinder;

public class ZipHelper
{
    public static void ZipDirectory(string zipFileName, string sourceDirectory)
    {
        var zip = new FastZip();
        zip.CreateZip(zipFileName, sourceDirectory,true, string.Empty);
    }
    
    public static string DecompressDeflateData(byte[] compressedData)
    {
        using (var compressedStream = new MemoryStream(compressedData))
        using (var deflateStream = new ZlibStream(compressedStream, Ionic.Zlib.CompressionMode.Decompress))
        using (var reader = new StreamReader(deflateStream))
        {
            return reader.ReadToEnd();
        }
    }
}