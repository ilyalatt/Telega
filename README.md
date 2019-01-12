<!--- [![NuGet](https://img.shields.io/nuget/v/Telega.svg)](https://nuget.org/packages/Telega) --->

# Introduction

The root of Telega is TLSharp.Core. However, the library is designed completely different. You can play with the library in [Telega.Example](https://github.com/ilyalatt/Telega/tree/master/Telega.Example/Program.cs).

* Telega.Rpc.Dto.Generator - a generator of DTO objects.
* Telega - the main project, contains RPC protocol implementation, TelegramClient, other auxiliary classes.
* Telega.Rpc.Dto - a directory in Telega project that contains generated Types and Functions (they are excluded from git because the size is ~5MB, you need to run generator if you want to build the project). Based on [LanguageExt](https://github.com/louthy/language-ext), you can read an introduction [here](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Introduction). DTO objects handle serialization without reflection.

# Difference from TLSharp

* netstandard support
* layer 82
* a new DTO generator which is based on .tl scheme
* completely redesigned DTOs
* elimination of all reflection usages (even in deserialization)
* RPC backround receive (vs pull after sending in the original library)
* RPC queue (it is needed to fix a simultaneos requests problem)
* session atomic-like updates (with a backup file usage)
* enhanced exceptions (no more InvalidOperationException)
* major refactoring of the whole library, a lot of breaking changes because of it

# Versioning

Telega is being actively developed, it will have breaking changes. After the 1 version it will folow [SemVer](https://semver.org/) except Telega.Rpc.Dto and Telega.Internal changes. Before the 1 version a minor increment can have breaking changes, patch can have a new functionality (so minor is used like major and patch is used like minor).
