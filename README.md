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

### Supported File Types
* HEIC
* JPEG

# Thanks

This library was much easier to implement thanks to the work done by these projects:

* https://github.com/exiftool/exiftool
* https://github.com/exif-heic-js/exif-heic-js

This repository provided a handy collection of images for testing:

* https://github.com/ianare/exif-samples
