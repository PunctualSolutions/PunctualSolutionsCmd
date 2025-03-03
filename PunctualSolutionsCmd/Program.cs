using ShellProgressBar;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

var path       = args[0];
var imageFiles = GetImageFiles(path);
if (imageFiles.Count == 0)
{
    Console.WriteLine("没有搜索到图片");
    return;
}

if (double.TryParse(args[1], out var imageQuality) && imageQuality is < 0.3 or > 1)
{
    Console.WriteLine("图片质量必须在0.5-1之间");
    return;
}

using (var bar = new ProgressBar(imageFiles.Count, "压缩图片", new ProgressBarOptions
                                                           {
                                                               ProgressCharacter   = '─',
                                                               ProgressBarOnBottom = true,
                                                           }))
    foreach (var imageFile in imageFiles)
    {
        await CompressImage(imageFile);
        bar.Tick(imageFile);
        continue;

        async Task CompressImage(string filePath)
        {
            try
            {
                using var image  = await Image.LoadAsync(filePath);
                var       width  = (int)(image.Width  * imageQuality);
                var       height = (int)(image.Height * imageQuality);
                image.Mutate(x => x.Resize(new ResizeOptions
                                           {
                                               Size = new(width, height),
                                               Mode = ResizeMode.Max,
                                           }));
                var format = image.Metadata.DecodedImageFormat;
                switch (format)
                {
                    case { Name: "JPEG", }:
                        image.Save(filePath, new JpegEncoder
                                             {
                                                 Quality = (int)(imageQuality * 100),
                                             });
                        break;
                    case { Name: "PNG", }:
                        var compressionLevel = imageQuality switch
                        {
                            < 0.4 => PngCompressionLevel.BestCompression,
                            < 0.7 => PngCompressionLevel.DefaultCompression,
                            _     => PngCompressionLevel.NoCompression,
                        };
                        image.Save(filePath, new PngEncoder
                                             {
                                                 CompressionLevel = compressionLevel,
                                             });
                        break;
                    default:
                        image.Save(filePath);
                        break;
                }
            }
            catch (Exception e)
            {
                bar.WriteLine($"压缩图片 {filePath} 失败: {e.Message}");
            }
        }
    }

Console.WriteLine("图片压缩完成");
return;

List<string> GetImageFiles(string directoryPath)
{
    string[] imageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", "tif", "tga",];

    return Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).Where(
        file => Array.Exists(imageExtensions, ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToList();
}