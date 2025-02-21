using System.IO;
using System.IO.Compression;
using System.Text;
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
    
    public static string DecompressDeflateData(byte[] compressedData)
    {
        using (var compressedStream = new MemoryStream(compressedData))
        using (var deflateStream = new ZLibStream(compressedStream, CompressionMode.Decompress))
        using (var reader = new StreamReader(deflateStream))
        {
            return reader.ReadToEnd();
        }
    }
    
    public static byte[] ConvertIFormFileToByteArray(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        using (var memoryStream = new MemoryStream())
        {
            file.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
    
    public static Stream ConvertStringToStream(string jsonData)
    {
        var memoryStream = new MemoryStream();

        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
        {
            streamWriter.Write(jsonData);
            streamWriter.Flush(); 
        }
        memoryStream.Position = 0;

        return memoryStream;
    }
}