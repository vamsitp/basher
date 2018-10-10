## PACKAGE & PUBLISH

> BUILD **APPX** PACKAGE
```batch
REM /p:UseSubFolderForOutputDirDuringMultiPlatformBuild=false
msbuild Basher.csproj /p:Configuration=Release;AppxBundle=Always;AppxBundlePlatforms=x64 /v:m /clp:Summary;ShowTimestamp /p:Platform=x64
```

> PUBLISH RELEASE TO **APP-CENTER**
```batch
REM https://docs.microsoft.com/en-us/appcenter/cli/ | https://github.com/microsoft/appcenter-cli
REM :: NodeJS ::
appcenter login
appcenter distribute release -g "Users" -f "Basher\AppPackages\Basher_1.0.0.0_Test\Basher_1.0.0.0_x64.appxbundle" -r "Install the cert (Local Computer > Trusted Root) from: https://vtpasia.blob.core.windows.net/basher/Basher.cer" -a "vamsitp-ms/Basher"

appcenter analytics audience -a vamsitp-ms/Basher
appcenter analytics sessions show -a vamsitp-ms/Basher
appcenter distribute releases show -a vamsitp-ms/Basher -r 6

```

> PUBLISH RELEASE TO **GITHUB**
```batch
REM https://curl.haxx.se/docs/manual.html
REM https://developer.github.com/v3/repos/releases/#create-a-release
curl https://api.github.com/repos/vamsitp/basher/releases -d "{\"tag_name\": \"1.0.1\", \"target_commitish\": \"master\", \"name\": \"1.0.1\", \"body\": \"First release!\", \"draft\": false, \"prerelease\": false}" -u vamsitp -H "Content-Type:application/json" -H X-GitHub-OTP:

REM https://developer.github.com/v3/repos/releases/#upload-a-release-asset
curl https://api.github.com/repos/vamsitp/basher/releases/release_id/assets?name=Basher_1.0.0.0_x64.appxbundle --data-binary "Basher\AppPackages\Basher_1.0.0.0_Test\Basher_1.0.0.0_x64.appxbundle" -u vamsitp -H "Content-Type:application/octet-stream" -H X-GitHub-OTP:

```
