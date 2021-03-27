# Telega

[![NuGet version](https://shields.io/nuget/vpre/Telega)](https://www.nuget.org/packages/Telega)

A simple Telegram MTProto client that keeps up with latest layers.
Check out [documentation prototype](https://ilyalatt.github.io/Telega/docs).
You can see the a lot of examples [here](https://github.com/ilyalatt/Telega/tree/master/Telega.Playground).

## Quick start

* Clone the repository
* Run `Telega.Playground` (NET 5.0 needs to be installed)
* Sign in to your account
* Explore `Snippets`

## TLSharp

The reasons of Telega existence are TLSharp issues. Here are pros of Telega:

* Layer 124 generated directly from Telegram Desktop .tl scheme
* Netstandard 2.0 and 2.1 targets
* Lack of reflection at all (even in deserialization)
* RPC backround receive (vs pull after sending in TLSharp)
* Automatic RPC call queuing and delaying to prevent flood errors
* Atomic-like session updates (with a backup file usage)
* Simple exceptions instead of meaningless InvalidOperationException
* Download and Upload with proper DC migration handling
* MTProto 2.0

## Status

Telega is not actively developed. But I support it my free time. Feel free to open issues :)

## Versioning

Telega uses [SemVer](https://semver.org/). Before 1.0.0 version minor increments can break compatibility.
