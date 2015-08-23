# CardsAgainstIRCv3
> `<PeNGu1N_oF_d00m> ​heddwch: <CardsAgainstIRC> 1: The logic hasn't exploded too`

Quick setup:

Requirements:

1. nuget
2. VS2015 / mono

First, restore packages (on mono, `nuget restore` depending on where `nuget` is). In VS2015, this is done automatically.
Then, build (mono: `xbuild /target:CardsAgainstIRC3 /p:Configuration=Release`, VS2015, press build or use `msbuild`, same as mono)

Copy `CardsAgainstIRC3.exe`, `NLog.config`, `NLog.dll`, `Newtonsoft.Json.dll` to somewhere, then copy `config.json` and edit it to set your preferences. Then run `CardsAgainstIRC3.exe`

Tested on Windows and FreeBSD
