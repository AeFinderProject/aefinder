using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;

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
        using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
        using (var resultStream = new MemoryStream())
        {
            deflateStream.CopyTo(resultStream);
            resultStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(resultStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}