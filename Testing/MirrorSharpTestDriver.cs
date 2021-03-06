using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MirrorSharp.Internal;
using MirrorSharp.Testing.Internal;
using MirrorSharp.Testing.Internal.Results;
using MirrorSharp.Testing.Results;
using Newtonsoft.Json;

// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.DelegateAllocation
// ReSharper disable HeapView.ObjectAllocation

namespace MirrorSharp.Testing {
    public class MirrorSharpTestDriver {
        private static readonly MirrorSharpOptions DefaultOptions = new MirrorSharpOptions();
        private static readonly ConcurrentDictionary<MirrorSharpOptions, LanguageManager> LanguageManagerCache = new ConcurrentDictionary<MirrorSharpOptions, LanguageManager>();

        private readonly TestMiddleware _middleware;

        private MirrorSharpTestDriver(MirrorSharpOptions? options = null, string languageName = LanguageNames.CSharp) {
            options = options ?? DefaultOptions;

            var language = GetLanguageManager(options).GetLanguage(languageName);
            _middleware = new TestMiddleware(options);
            Session = new WorkSession(language, options);
        }
        
        internal WorkSession Session { get; }

        public static MirrorSharpTestDriver New(MirrorSharpOptions? options = null, string languageName = LanguageNames.CSharp) {
            return new MirrorSharpTestDriver(options, languageName);
        }

        public MirrorSharpTestDriver SetText(string text) {
            Session.ReplaceText(text);
            return this;
        }

        public MirrorSharpTestDriver SetTextWithCursor(string textWithCursor) {
            var parsed = TextWithCursor.Parse(textWithCursor);

            Session.ReplaceText(parsed.Text);
            Session.CursorPosition = parsed.CursorPosition;
            return this;
        }

        public async Task SendTypeCharsAsync(string value) {
            foreach (var @char in value) {
                await SendAsync(CommandIds.TypeChar, @char);
            }
        }

        public Task<SlowUpdateResult<object>> SendSlowUpdateAsync() => SendSlowUpdateAsync<object>();

        public Task<SlowUpdateResult<TExtensionResult>> SendSlowUpdateAsync<TExtensionResult>() {
            return SendAsync<SlowUpdateResult<TExtensionResult>>(CommandIds.SlowUpdate);
        }

        public Task<OptionsEchoResult> SendSetOptionAsync(string name, string value) {
            return SendAsync<OptionsEchoResult>(CommandIds.SetOptions, $"{name}={value}");
        }

        public Task<OptionsEchoResult> SendSetOptionsAsync(IDictionary<string, string> options) {
            return SendAsync<OptionsEchoResult>(CommandIds.SetOptions, string.Join(",", options.Select(o => $"{o.Key}={o.Value}")));
        }

        public Task<InfoTipResult> SendRequestInfoTipAsync(int position) {
            return SendAsync<InfoTipResult>(CommandIds.RequestInfoTip, position);
        }

        internal Task SendReplaceTextAsync(string newText, int start = 0, int length = 0, int newCursorPosition = 0, string reason = "") {
            // ReSharper disable HeapView.BoxingAllocation
            return SendAsync(CommandIds.ReplaceText, $"{start}:{length}:{newCursorPosition}:{reason}:{newText}");
            // ReSharper restore HeapView.BoxingAllocation
        }

        internal Task<CompletionsResult> SendTypeCharAsync(char @char) {
            return SendAsync<CompletionsResult>(CommandIds.TypeChar, @char);
        }

        internal async Task<TResult> SendAsync<TResult>(char commandId, HandlerTestArgument? argument = null)
            where TResult : class?
        {
            var sender = new StubCommandResultSender();
            await _middleware.GetHandler(commandId).ExecuteAsync(argument?.ToAsyncData(commandId) ?? AsyncData.Empty, Session, sender, CancellationToken.None);
            return sender.LastMessageJson != null ? JsonConvert.DeserializeObject<TResult>(sender.LastMessageJson) : null;
        }

        internal Task SendAsync(char commandId, HandlerTestArgument? argument = default(HandlerTestArgument)) {
            return _middleware.GetHandler(commandId).ExecuteAsync(argument?.ToAsyncData(commandId) ?? AsyncData.Empty, Session, new StubCommandResultSender(), CancellationToken.None);
        }

        private static LanguageManager GetLanguageManager(MirrorSharpOptions options) {
            return LanguageManagerCache.GetOrAdd(options, _ => new LanguageManager(options));
        }

        private class TestMiddleware : MiddlewareBase {
            public TestMiddleware(MirrorSharpOptions options) : base(GetLanguageManager(options), options) {
            }
        }
    }
}
