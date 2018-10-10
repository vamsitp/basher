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
REM https://developer.github.com/v3/repos/releases/#create-a-release
curl "https://api.github.com/repos/vamsitp/basher/releases" -v -i -X POST -H "Content-Type:application/json" -u vamsitp:password -d '{ "tag_name": "v1.0.0", "target_commitish": "master", "name": "v1.0.0", "body": "First release!", "draft": false, "prerelease": false }' 

REM https://developer.github.com/v3/repos/releases/#upload-a-release-asset
curl "https://api.github.com/repos/vamsitp/basher/releases/release_id/assets?name=Basher_1.0.0.0_x64.appxbundle" -v -i -X POST -H "Content-Type:application/octet-stream" -u vamsitp:password --data-binary "Basher\AppPackages\Basher_1.0.0.0_Test\Basher_1.0.0.0_x64.appxbundle" 

```
