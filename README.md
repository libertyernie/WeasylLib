# WeasylLib

This is a C# (.NET Framework) library that interfaces with the Weasyl API.

Supported endpoints:

* whoami
* submissions/(submitid)/view
* characters/{charid}/view
* users/(login_name)/gallery

The library can also scrape character submission IDs (charids) for a given user.

TODO: add OAuth2 support.

Dependencies:
* Newtonsoft.Json
