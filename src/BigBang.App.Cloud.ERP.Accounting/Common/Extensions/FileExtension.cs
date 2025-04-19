using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BigBang.App.Cloud.ERP.Accounting.Common.Enums;
using BigBang.App.Cloud.ERP.Accounting.Resources;
using BigBang.WebServer.Common.Exceptions;

namespace BigBang.App.Cloud.ERP.Accounting.Common.Extensions;

public static class FileExtension
{
    public static async Task<byte[]> GetBytes(this Stream data)
    {
        if (data is null)
            throw new BigBangException(string.Format(Messages.Error_FieldRequired, Messages.Label_File));

        using var ms = new MemoryStream();
        await data.CopyToAsync(ms);
        return ms.ToArray();
    }

    //TODO: WE shoul use constraint DML
    public static FileType GetFileExtensionFromStream(this byte[] buffer)
    {
        switch (buffer)
        {
            case var _ when buffer.Take(4).SequenceEqual(Constants.PdfHeader):
                return FileType.Pdf;
            case var _ when buffer.Take(3).SequenceEqual(Constants.JpgHeader):
                return FileType.Jpg;
            case var _ when buffer.Take(8).SequenceEqual(Constants.PngHeader):
                return FileType.Png;
            case var _ when buffer.Take(4).SequenceEqual(Constants.GifHeader):
                return FileType.Gif;
            default:
                return FileType.Unknown;
        }
    }

    //TODO: use ImageSharp for create thumbnail
}