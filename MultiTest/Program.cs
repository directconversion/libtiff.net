using BitMiracle.LibTiff.Classic;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

Console.WriteLine("Hello World!");
var fn = @"C:\tmp\Cirdan202006\PreFix\F_7(b) 10mm PMMA flat 28KV.tif";
using Tiff inputTiff = Tiff.Open(fn, "r");
var tifinf = inputTiff.GetTiffInfo();
var img = ReadTiffMulti(fn);
WriteTiff(Path.ChangeExtension(fn, ".out.tif"), img.pix, img.pix.Length, img.height, img.width, TypeCode.Single, "TEST");


static (int width, int height, Single[][] pix) ReadTiffMulti(string fileName)
{
    var result = new List<Single[]>();
    using (Tiff tiff = Tiff.Open(fileName, "r"))
    {
        int pageCount = 0;
        //tiff.NumberOfDirectories();
        var (w, h, tc) = tiff.GetTiffInfo();
        var a = (w, h, tc);
        // TiffPageInfo(tiff);
        //(int width, int height, TypeCode tcTiff) = tinf;
        result.Add(ReadFrameData(tiff, a));
        pageCount++;
        while (tiff.ReadDirectory())
        {
            //(w, h, tc) = tiff.GetTiffInfo();// TiffPageInfo(tiff);
            //tinf = TiffPageInfo(tiff);
            result.Add(ReadFrameData(tiff, (w, h, tc)));
            pageCount++;
        }
        return (w, h, result.ToArray());
    }

    static Single[] ReadFrameData(Tiff tiff, (int width, int height, TypeCode tc) tinf)
    {
        (int width, int height, TypeCode tc) = tinf;
        int strideBytesIn = width * tc.Size();
        int strideBytesOut = width * sizeof(Single);
        var result = new Single[width * height];
        byte[] buffer = ArrayPool<byte>.Shared.Rent(tiff.ScanlineSize());
        var ax = Array.CreateInstance(tc.ToType(), width);
        for (int i = 0; i < height; i++)
        {
            tiff.ReadScanline(buffer, i);
            int resultOffset = i * width;
            Buffer.BlockCopy(buffer, 0, ax, 0, strideBytesIn);
            Array.Copy(ax, 0, result, resultOffset, width);
        }
        ArrayPool<byte>.Shared.Return(buffer);
        return result;
    }
}
static int WriteTiff(string fileName,
 float[][] data,
int pageCount, int pageHeight, int width, TypeCode tc,
string Header = "")
{
    using (Tiff output = OpenTiffOutMulti(fileName, pageCount: pageCount, pageHeight: pageHeight, width: width, tc: tc, Header: Header))
    {
        Int64 strideBytes = width * tc.Size();
        byte[] buffer = new byte[strideBytes];
        var t = writePages(output, tc, width, pageHeight, data);
        //for (int nPage = 0; nPage < pageCount; nPage++)
        //{
        //}
        // really slow to clean up
        // and an earlier pause
        // GC ?
        output.Close();
    }
    Console.WriteLine($"Done Tiff. ");
    return 0;

    static int writePages(Tiff output, TypeCode tc, int width, int pageHeight, float[][] data)
    {
        int strideBytes = tc.Size() * width;
        byte[] buffer = new byte[strideBytes];
        //Int64 sum = 0;
        int nPage = 0;
        foreach (float[] raster in data)
        {
            if (nPage > 0)
            {
                NewPage(output, (short)nPage, pageHeight, width, tc);
            }
            for (int y = 0; y < pageHeight; y++)
            {
                MemoryMarshal.AsBytes(raster.AsSpan(y * width, width))
                    .CopyTo(buffer);
                output.WriteScanline(buffer, y);
            }
            nPage++;
            Console.Write("*");
            //output.FlushData();
        }
        //output.WriteDirectory();

        output.FlushData();
        Console.WriteLine();
        Console.WriteLine($"Done Tiff Image Data ");
        //                Console.WriteLine($"Sum = {sum} ");
        Console.WriteLine();
        return 0;
    }
}
static Tiff NewPage(Tiff output, Int16 nPage,
    int pageHeight, int width, TypeCode tc)
{
    // create new directory and make it current
    output.WriteDirectory(true);
    //int dirCount = output.NumberOfDirectories();
    //Debug.Assert(nPage == dirCount);
    output.CreateDirectory();
    //    output.SetDirectory(nPage);

    //output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE);//Should use this
    // output.SetField(TiffTag.PAGENUMBER, nPage);//Should use this
    output.SetField(TiffTag.IMAGEWIDTH, width);
    output.SetField(TiffTag.IMAGELENGTH, pageHeight);
    output.SetField(TiffTag.BITSPERSAMPLE, 8 * tc.Size());
    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
    output.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
    output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
    //int m_rowsperstrip = output.DefaultStripSize(256);
    output.SetField(TiffTag.ROWSPERSTRIP, pageHeight);
    output.SetField(TiffTag.XRESOLUTION, 0.01);
    output.SetField(TiffTag.YRESOLUTION, 0.01);
    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
    output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.CENTIMETER);

    output.SetField(TiffTag.SAMPLEFORMAT, tc.TiffSampleFormat());
    //output.WriteDirectory();
    if (nPage % 100 == 0)
    {
        Console.WriteLine($"Page {nPage}");
    }
    //output.CheckpointDirectory();
    output.Flush();
    //output.WriteCheck(tiles: false, "test");
    return output;
}
static Tiff OpenTiffOutMulti(string fileName,
            int pageCount, int pageHeight, int width, TypeCode tc,
            string Header = "")
{
    Int16 nPage = 0;
    string code = (pageHeight * width * (Int64)pageCount * tc.Size() >= 2L * 1048576 * 1024) ? "w8" : "w";
    Tiff output = Tiff.Open(fileName, code);

    //            output.SetField(TiffTag.PAGENUMBER, nPage);
    output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE);
    output.SetField(TiffTag.IMAGEWIDTH, width);
    output.SetField(TiffTag.IMAGELENGTH, pageHeight);
    output.SetField(TiffTag.BITSPERSAMPLE, 8 * tc.Size());
    output.SetField(TiffTag.COMPRESSION, Compression.NONE);
    output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
    output.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
    output.SetField(TiffTag.IMAGEDESCRIPTION, Header ?? "");
    output.SetField(TiffTag.MAKE, "DirectConversion");
    output.SetField(TiffTag.MODEL, "XCTEST");
    //NO  output.SetField(TiffTag.PAGENUMBER, nPage);
    //output.SetField(TiffTag.DATETIME,    1   System.Byte[]
    output.SetField(TiffTag.DOCUMENTNAME, "");
    //output.SetField(TiffTag.STRIPOFFSETS, );
    //output.SetField(TiffTag.ROWSPERSTRIP, );
    //output.SetField(TiffTag.STRIPBYTECOUNTS, );
    //min max for display!
    output.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
    output.SetField(TiffTag.SAMPLESPERPIXEL, 1);
    int m_rowsperstrip = output.DefaultStripSize(256);
    output.SetField(TiffTag.ROWSPERSTRIP, pageHeight);
    output.SetField(TiffTag.XRESOLUTION, 0.01);
    output.SetField(TiffTag.YRESOLUTION, 0.01);
    output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
    output.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.CENTIMETER);

    output.SetField(TiffTag.SAMPLEFORMAT, tc.TiffSampleFormat());
    output.CheckpointDirectory();
    //output.Flush();
    //output.WriteCheck(tiles: false, "test");
    //output.CheckpointDirectory();
    //output.WriteDirectory();
    return output;
}
public static class TiffInfoExtensions
{
    public static BitMiracle.LibTiff.Classic.SampleFormat TiffSampleFormat(this TypeCode tc)
=> tc switch
{
    TypeCode.Boolean => SampleFormat.INT,
    TypeCode.SByte => SampleFormat.INT,
    TypeCode.Int16 => SampleFormat.INT,
    TypeCode.Int32 => SampleFormat.INT,
    TypeCode.Int64 => SampleFormat.INT,
    TypeCode.Byte => SampleFormat.UINT,
    TypeCode.UInt16 => SampleFormat.UINT,
    TypeCode.UInt32 => SampleFormat.UINT,
    TypeCode.UInt64 => SampleFormat.UINT,
    TypeCode.Single => SampleFormat.IEEEFP,
    TypeCode.Double => SampleFormat.IEEEFP,
    _ => throw new NotSupportedException($"TypeCode not numeric {tc}")
};
    public static int Size(this TypeCode tc) =>

         tc switch
         {
             TypeCode.Byte => 1,
             TypeCode.SByte => 1,
             TypeCode.UInt16 => 2,
             TypeCode.Int16 => 2,
             TypeCode.UInt32 => 4,
             TypeCode.Int32 => 4,
             TypeCode.UInt64 => 8,
             TypeCode.Int64 => 8,

             TypeCode.Single => 4,
             TypeCode.Double => 8,
             _ => throw new ArgumentOutOfRangeException(nameof(tc))
         };
    public static Type ToType(this TypeCode tc)
    {
        Type t;
        t = tc switch
        {
            TypeCode.Boolean => typeof(bool),
            TypeCode.SByte => typeof(byte),
            TypeCode.Int16 => typeof(short),
            TypeCode.Int32 => typeof(Int32),
            TypeCode.Int64 => typeof(Int64),
            TypeCode.Byte => typeof(byte),
            TypeCode.UInt16 => typeof(Int16),
            TypeCode.UInt32 => typeof(Int32),
            TypeCode.UInt64 => typeof(Int64),
            TypeCode.Single => typeof(float),
            TypeCode.Double => typeof(double),
            _ => throw new NotSupportedException($"TypeCode not numeric {tc}")
        };
        return t;
    }
    public static void Deconstruct(this TiffInfo tinfo, out int width, out int height, out TypeCode tc)
    {
        width = tinfo.width;
        height = tinfo.height;
        tc = tinfo.tc;
    }
    public static TiffInfo GetTiffInfo(this Tiff tiff)
    {
        int sampleFormat = tiff.GetFieldDefaulted(TiffTag.SAMPLEFORMAT)[0].ToInt();
        int bps = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();
        TypeCode tc = ((SampleFormat)sampleFormat, bps).ToTypeCode();
        var result = new TiffInfo()
        {
            test = 7,
            numberOfDirectories = tiff.NumberOfDirectories(),
            width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt(),
            height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt(),
            bps = bps,
            sampleFormat = sampleFormat,
            camera = tiff.GetFieldDefaulted(TiffTag.CAMERASERIALNUMBER)?[0].ToString() ?? "Unknown",
            dateTime = tiff.GetFieldDefaulted(TiffTag.DATETIME)?[0].ToString() ?? "Unknown",
            imageName = tiff.GetFieldDefaulted(TiffTag.DOCUMENTNAME)?[0].ToString() ?? "Unknown",
            imageDescription = tiff.GetFieldDefaulted(TiffTag.IMAGEDESCRIPTION)?[0].ToString() ?? "Unknown",
            //frameCountMaybe = tiff.GetField(TiffTag.FRAMECOUNT)?[0].ToInt() ?? default,
            frameCountMaybe = tiff.GetField(TiffTag.FRAMECOUNT)?[0].ToInt() ?? default(int?),
            tc = tc
        };
        //int spp = tiff.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
        //int compression = tiff.GetField(TiffTag.COMPRESSION)[0].ToInt();
        return result;
    }
    public static TypeCode ToTypeCode(this (SampleFormat sampleFormat, int bps) tcInfo)
    {
        TypeCode tc = tcInfo switch
        {
            (SampleFormat.UINT, 8) => TypeCode.Byte,
            (SampleFormat.UINT, 16) => TypeCode.UInt16,
            (SampleFormat.UINT, 32) => TypeCode.UInt32,
            (SampleFormat.IEEEFP, 32) => TypeCode.Single,
            (SampleFormat.IEEEFP, 64) => TypeCode.Double,
            (SampleFormat.INT, 8) => TypeCode.SByte,
            (SampleFormat.INT, 16) => TypeCode.Int16,
            (SampleFormat.INT, 32) => TypeCode.Int32,
            _ => throw new NotSupportedException($"TypeCode not handled {tcInfo}")
        };
        return tc;
    }
}
public struct TiffInfo
{
    public int test { get; set; }
    public int numberOfDirectories { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public int bps { get; set; }
    public int sampleFormat { get; set; }
    public string? camera { get; set; }
    public string? dateTime { get; set; }
    public string? imageName { get; set; }
    public string? imageDescription { get; set; }
    public int? frameCountMaybe { get; set; }
    public TypeCode tc { get; set; }
    public static TiffInfo ReadTiffInfo(string fileName)
    {
        using (Tiff tiff = Tiff.Open(fileName, "r"))
        {
            var info = tiff.GetTiffInfo();
            return info;
        }
    }
}