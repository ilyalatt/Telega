---
id: send-message
title: Send Message
---

Add imports:

```csharp
using Telega.Rpc.Dto.Functions.Users;
using Telega.Rpc.Dto.Types;
```

Send a message to your account:

```csharp
var recipient = new InputPeer.SelfTag();
await tg.Messages.SendMessage(recipient, "Greetings from Telega!");
```
