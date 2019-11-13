# Telega

[![NuGet version](https://badge.fury.io/nu/Telega.svg)](https://www.nuget.org/packages/Telega)

You can see the library usage example [here](https://github.com/ilyalatt/Telega/tree/master/Telega.Example/Program.cs).

Also you can read [the introduction](https://github.com/ilyalatt/Telega/wiki/Introduction).

## Quick start

* Clone the repository.
* Run `Telega.Rpc.Dto.Generator` to generate DTOs. You can use `./generate-dto.sh`. NET Core 3.0 is used.
* Create config.json in `Telega.Example`. It should look like

```json
{
  "apiId": 12345,
  "apiHash": "api-hash",
  "phone": "123456789",
  "password": "password-if-needed"
}
```

* Run `Telega.Example`. Sign in to your account. The app should send a picture to your 'Saved Messages' chat.

If you are not familiar with functional programming you can read [LanguageExt introduction](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Introduction).
If you are not familiar with LanguageExt you can read [LanguageExt readme](https://github.com/louthy/language-ext).

## Structure

* `Telega.Rpc.Dto.Generator` - a generator of DTO objects.
* `Telega.Rpc.Dto` - a directory in Telega project that contains generated Types and Functions (they are excluded from git because the size is ~5MB). Based on [LanguageExt](https://github.com/louthy/language-ext). DTOs handle serialization without reflection.
* `Telega` - the main project that contains [MTProto](https://core.telegram.org/mtproto) and [Telegram API](https://core.telegram.org/api#telegram-api) implementations, TelegramClient and other auxiliary classes.

## Difference from TLSharp

The root of Telega is TLSharp. However the library is designed completely different.

* netstandard 2.0 target
* layer 98
* a new DTO generator which is based on .tl scheme
* completely redesigned DTOs
* elimination of all reflection usages (even in deserialization)
* RPC backround receive (vs pull after sending in the original library)
* RPC queue (it is needed to fix a simultaneos requests problem)
* session atomic-like updates (with a backup file usage)
* enhanced exceptions (no more InvalidOperationException)
* enhanced factorization via [Pollard's rho algorithm](https://en.wikipedia.org/wiki/Pollard%27s_rho_algorithm)
* MTProto 2.0
* major refactoring of the whole library

## Versioning

Telega is being actively developed. It will have breaking changes. After the 1.0.0 version it will folow [SemVer](https://semver.org/) except `Telega.Rpc.Dto` and `Telega.Internal` changes. Before the 1.0.0 version a minor increment can have breaking changes and patch can have a new functionality (so minor is used like major and patch is used like minor).
