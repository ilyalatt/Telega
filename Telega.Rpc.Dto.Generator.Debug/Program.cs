using System;
using Telega.Rpc.Dto.Generator;

var ctx = FileSyncContext.Extract(Environment.CurrentDirectory);
Generator.Sync(ctx, forceOverwrite: true);
