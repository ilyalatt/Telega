using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BigMath;
using Telega.Utils;

namespace Telega.Rpc.Dto {
    static class Yamlifier {
        public sealed record Context(int Indent, bool SkipFirstItemIndent, StringBuilder Output);

        public delegate void Writer(Context ctx);
        public delegate Writer Stringifier<T>(T v);

        public static Context EmptyContext => new(
            Indent: 0,
            SkipFirstItemIndent: true,
            Output: new()
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AppendIndent(Context ctx) {
            const int indentSize = 4;
            ctx.Output.Append(' ', ctx.Indent * indentSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AppendNewLine(Context ctx) {
            var output = ctx.Output;
            while (output.Length > 0 && output[output.Length - 1] == ' ') {
                output.Remove(output.Length - 1, 1);
            }
            output.Append('\n');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(Context ctx, Writer writer) {
            writer(ctx);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer WriteBool(bool v) =>
            ctx => ctx.Output.Append(v ? "true" : "false");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer WriteInt(int v) =>
            ctx => ctx.Output.Append(v.ToString());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer WriteUint(uint v) =>
            ctx => ctx.Output.Append(v.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer WriteLong(long v) =>
            ctx => ctx.Output.Append(v.ToString());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer WriteDouble(double v) =>
            ctx => ctx.Output.Append(v.ToString(CultureInfo.InvariantCulture));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer WriteInt128(Int128 v) =>
            ctx => ctx.Output.Append(v.ToString());
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer WriteInt256(Int256 v) =>
            ctx => ctx.Output.Append(v.ToString());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer WriteBytes(Bytes v) =>
            WriteMapping(
                ("Count", WriteLong(v.Ref.Length))
            );

        static Writer WriteIndentedContent<T>(
            Stringifier<T> stringify,
            IReadOnlyList<T> items
        ) => ctx => {
            items.Iter((x, i) => {
                if (i > 0 || !ctx.SkipFirstItemIndent) {
                    AppendNewLine(ctx);
                    AppendIndent(ctx);
                }
                Write(ctx, stringify(x));
            });
        };

        public static Writer WriteString(string v) =>
            ctx => {
                if (v.Length == 0) {
                    ctx.Output.Append("\"\"");
                    return;
                }

                static Writer WriteItem(string item) => itemCtx => {
                    itemCtx.Output.Append(item);
                };

                var lines = v.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 1) {
                    var line = lines.Single();
                    ctx.Output.Append(line);
                }
                else {
                    ctx.Output.Append("|");
                    Write(
                        ctx with { Indent = ctx.Indent + 1, SkipFirstItemIndent = false },
                        WriteIndentedContent(WriteItem, lines)
                    );
                }
            };

        public static Stringifier<IReadOnlyList<T>> StringifyVector<T>(
            Stringifier<T> stringify
        ) => vector => ctx => {
            if (vector.Count == 0) {
                ctx.Output.Append("[ ]");
                return;
            }

            Writer WriteItem(T item) => itemCtx => {
                itemCtx.Output.Append("- ");
                Write(itemCtx with { SkipFirstItemIndent = true }, stringify(item));
            };

            Write(
                ctx,
                WriteIndentedContent(WriteItem, vector)
            );
        };

        static Writer WriteOption<T>(
            Stringifier<T> stringify,
            T? v
        ) where T : struct => ctx => {
            v.NMatch(
                some: x => {
                    Write(ctx, stringify(x));
                },
                none: () => {
                    ctx.Output.Append("~");
                }
            );
        };
        
        static Writer WriteOption<T>(
            Stringifier<T> stringifier,
            T? v
        ) where T : class => ctx => {
            v.NMatch(
                some: x => {
                    Write(ctx, stringifier(x));
                },
                none: () => {
                    ctx.Output.Append("~");
                }
            );
        };

        public static Func<T?, Writer> StringifyOptionStruct<T>(
            Stringifier<T> stringify
        ) where T : struct => option =>
            WriteOption(stringify, option);
        
        public static Func<T?, Writer> StringifyOptionClass<T>(
            Stringifier<T> stringify
        ) where T : class => option =>
            WriteOption(stringify, option);

        public static Writer WriteMapping(
            params (string?, Writer)[] kvps
        ) => ctx => {
            if (kvps.Length == 0) {
                ctx.Output.Append("{ }");
                return;
            }

            static Writer WriteKvp((string?, Writer) kvp) => itemCtx => {
                var (key, value) = kvp;
                var inline = key == null;
                if (!inline) {
                    itemCtx.Output.Append(key);
                    itemCtx.Output.Append(": ");
                }
                Write(
                    itemCtx with {
                        Indent = itemCtx.Indent + (!inline ? 1 : 0),
                        SkipFirstItemIndent = inline
                    },
                    value
                );
            };

            Write(
                ctx,
                WriteIndentedContent(WriteKvp, kvps)
            );
        };

        public static Writer WriteUnion(
            string? tag,
            Writer writer
        ) =>
            tag != null ? WriteMapping((tag, writer)) : writer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Writer Stringify<T>(Stringifier<T> stringifier, T v) =>
            stringifier(v);
        
        public static string Yamlify(Writer writer) {
            var ctx = EmptyContext;
            Write(ctx, writer);
            return ctx.Output.ToString();
        }
    }
}