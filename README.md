# LibExifCore

LibExifCore is a .NET Core library used for processing EXIF data.

## Example Usage
```
using LibExifCore;
EXIFParser parser = new EXIFParser(imgPath);

foreach(string key in parser.Tags.Keys)
{
    string s = string.Format("{0}: {1}", key, parser.Tags[key]);
    Console.WriteLine(s);
}
```

# Thanks

This library was much easier to implement thanks to the work done by these projects:

https://github.com/exiftool/exiftool
https://github.com/exif-heic-js/exif-heic-js
