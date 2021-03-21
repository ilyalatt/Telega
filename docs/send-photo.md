---
id: send-photo
title: Send Photo
---

Add imports:

```csharp
using Telega.Rpc.Dto.Functions.Users;
using Telega.Rpc.Dto.Types;
```

Download photo:

```csharp
var photoUrl = "https://cdn1.img.jp.sputniknews.com/images/406/99/4069980.png";
var webClient = new System.Net.WebClient();
var photo = await webClient.DownloadDataTaskAsync(photoUrl);
```

Upload photo:

```csharp
var tgPhoto = await tg.Upload.UploadFile(
    "photo.png",
    photo.Length,
    new System.IO.MemoryStream(photo)
);
```

Send photo:

```csharp
var recipient = new InputPeer.SelfTag();
await tg.Messages.SendPhoto(
    peer: recipient,
    file: tgPhoto,
    message: ""
);
```
