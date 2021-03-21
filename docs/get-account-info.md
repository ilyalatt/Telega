---
id: get-account-info
title: Get Account Info
---

Add imports:

```csharp
using Telega.Client;
using Telega.Rpc.Dto.Functions.Users;
using Telega.Rpc.Dto.Types;
```

Print your account info:

```csharp
var myUser = new InputUser.SelfTag();
var myInfo = await tg.Call(new GetFullUser(myUser));
Console.WriteLine(myInfo);
```

It should print something like

```yaml
User.Default:
    Id: 453178214
    AccessHash: -741123124234221231443
    FirstName: Keks
    LastName: ~
    Username: ~
    Phone: 314512524513
    Photo.Default:
        PhotoId: 345342462341251
        PhotoSmall:
            VolumeId: 232462413
            LocalId: 235123
        PhotoBig:
            VolumeId: 232462413
            LocalId: 235124
        DcId: 3
    Status.Offline:
        WasOnline: 1606375198
    BotInfoVersion: ~
    RestrictionReason: ~
    BotInlinePlaceholder: ~
    LangCode: ~
About: ~
Settings:
    GeoDistance: ~
ProfilePhoto.Default:
    Id: 142432342352341123
    AccessHash: 46264572342342342
    FileReference:
        Count: 21
    Date: 1569268238
    Sizes:
        - Default:
            Type: a
            Location:
                VolumeId: 232462413
                LocalId: 235123
            W: 160
            H: 160
            Size: 6214
        - Default:
            Type: b
            Location:
                VolumeId: 232462413
                LocalId: 235124
            W: 320
            H: 320
            Size: 22579
        - Default:
            Type: c
            Location:
                VolumeId: 232462413
                LocalId: 235125
            W: 640
            H: 640
            Size: 82325
    VideoSizes: ~
    DcId: 3
NotifySettings:
    ShowPreviews: true
    Silent: false
    MuteUntil: 0
    Sound: default
BotInfo: ~
PinnedMsgId: ~
CommonChatsCount: 0
FolderId: ~
TtlPeriod: ~
```
