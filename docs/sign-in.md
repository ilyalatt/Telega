---
id: sign-in
title: Sign In
slug: /
---

Install `Telega` package.
If you use dotnet CLI just type `dotnet add package Telega`.

Add imports:

```csharp
using System;
using Telega.Client;
```

Prepare API credentials.
You can read how to obtain full-fledged credentials [here](https://core.telegram.org/api/obtaining_api_id).

```csharp
// This credentials are Telegram Desktop limited test credentials.
// They can cause 'The protocol is violated' error sometimes.
var apiId = 17349;
var apiHash = "344583e45741c457fe1862106095a5eb";
```

Create the client:

```csharp
var tg = await TelegramClient.Connect(apiId);
```

Sign in with code verification:

```csharp
var phone = "YOUR_PHONE_HERE";
var codeHash = await tg.Auth.SendCode(apiHash, phone);
var code = Console.ReadLine();
await tg.Auth.SignIn(phone, codeHash, code);
```

It will throw `TgPasswordNeededException` if there is a cloud password.
So here is what you need to do:

```csharp
var password = "YOUR_PASSWORD_HERE";
await tg.Auth.CheckPassword(password);
```
