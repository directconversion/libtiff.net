This differs from the original by a couple of lines to allow jumping to the end of the linked list of image pages (directories) in the TIFF. walking the linked list from the root each time a page is appended gets slower as the number of image pages gets large.  

"Stash the last value of nextdir to avoid looping through all directories"

Sadly this is not as stands a global fix, due to other functions which may modify the file.

LibTiff.NET
===========

The .NET version of original libtiff library written by Sam Leffler and others.

LibTiff.Net provides support for the Tag Image File Format (TIFF), a widely used format for storing image data.

Sample code
===========

Sample code for C# and VB.NET

https://github.com/BitMiracle/libtiff.net/tree/master/Samples


Documentation
=============

Help pages can be found here

https://bitmiracle.github.io/libtiff.net/help/


License
=======

LibTiff.Net is freely available for all uses under the New BSD license.

The library is free and can be used in commercial applications without royalty.

We don't promise that this software works. But if you find any bugs, please let us know!
