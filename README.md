# WeasylLib

This is a C# (.NET Framework) library that interfaces with the Weasyl API.

**For the time being, any development on this library will occur in the [CrosspostSharp](https://github.com/libertyernie/CrosspostSharp) repo.**

Supported endpoints:

* whoami
* useravatar
* submissions/(submitid)/view
* characters/{charid}/view
* users/(login_name)/gallery

The library can also scrape character submission IDs (charids) for a given user.

Dependencies:
* Newtonsoft.Json
