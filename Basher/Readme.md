## README

> BUILD **APPX** PACKAGE
```batch
REM /p:UseSubFolderForOutputDirDuringMultiPlatformBuild=false
msbuild Basher.csproj /p:Configuration=Release;AppxBundle=Always;AppxBundlePlatforms=x64 /v:m /clp:Summary;ShowTimestamp /p:Platform=x64
```

```batch
REM https://docs.microsoft.com/en-us/appcenter/cli/ | https://github.com/microsoft/appcenter-cli
REM :: NodeJS ::
appcenter login
appcenter distribute release -g "Users" -f "D:\Dev\visualstudio.com\vamsitp\Waid\Basher\AppPackages\Basher_1.0.0.0_Test\Basher_1.0.0.0_x64.appxbundle" -r "Install the cert (Local Computer > Trusted Root) from: https://vtpasia.blob.core.windows.net/basher/Basher.cer" -a "vamsitp-ms/Basher"

appcenter analytics audience -a vamsitp-ms/Basher
appcenter analytics sessions show -a vamsitp-ms/Basher
appcenter distribute releases show -a vamsitp-ms/Basher -r 6

```

> **INSTALLATION**
1. Download the package
2. Install the cert from https://vtpasia.blob.core.windows.net/basher/Basher.cer (Local Computer > Trusted Root)
3. Install the package from an elevated CMD prompt

> [LOGS WEBHOOK](https://outlook.office.com/webhook/fcb2a114-00da-4d0d-a78e-78451c515c0b@72f988bf-86f1-41af-91ab-2d7cd011db47/IncomingWebhook/7b7fdbb789e44b588fda1f7a667a35a0/a501ef2c-a09b-44bc-b57e-c407a485f080)

> **PAT**
> https://some-team.visualstudio.com/_details/security/tokens 

> **CREDITS**
1. [MONSTER ICON](https://opengameart.org/content/enemy-game-character-dark-monster)
2. [ROCKSTAR ANIMATION](https://gfycat.com/gifs/detail/FineLeadingElephant)
3. [DINO LOGO](https://dribbble.com/shots/3064570-Unable-to-connect)