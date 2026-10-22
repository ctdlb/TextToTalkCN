using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using TextToTalk.Events;

namespace TextToTalk.Backends
{
    public abstract class VoiceBackend : IDisposable
    {
        private readonly object synthesisGate = new();
        private readonly Dictionary<TextSource, List<CancellationTokenSource>> pendingSynthesis = new();

        public Vector4 TitleBarColor { get; protected init; }

        /// <summary>
        /// 登记一次即将开始的合成，返回的句柄在合成结束（含异常/取消）后必须释放。
        /// 取消语音时通过它中断仍在请求中的合成，避免取消之后又冒出一句。
        /// </summary>
        protected SynthesisHandle BeginSynthesis(TextSource source)
        {
            var cts = new CancellationTokenSource();
            lock (this.synthesisGate)
            {
                if (!this.pendingSynthesis.TryGetValue(source, out var list))
                {
                    list = new List<CancellationTokenSource>();
                    this.pendingSynthesis[source] = list;
                }

                list.Add(cts);
            }

            return new SynthesisHandle(cts.Token, () => this.EndSynthesis(source, cts));
        }

        /// <summary>
        /// 取消进行中的合成。source 为 null 时取消全部。
        /// </summary>
        protected void CancelPendingSynthesis(TextSource? source = null)
        {
            lock (this.synthesisGate)
            {
                foreach (var (pendingSource, list) in this.pendingSynthesis)
                {
                    if (source != null && pendingSource != source) continue;
                    foreach (var cts in list)
                    {
                        cts.Cancel();
                    }
                }
            }
        }

        private void EndSynthesis(TextSource source, CancellationTokenSource cts)
        {
            lock (this.synthesisGate)
            {
                if (this.pendingSynthesis.TryGetValue(source, out var list))
                {
                    list.Remove(cts);
                    if (list.Count == 0) this.pendingSynthesis.Remove(source);
                }
            }

            cts.Dispose();
        }

        public abstract void Say(SayRequest request);

        public abstract void CancelAllSpeech();

        public abstract void CancelSay(TextSource source);

        public abstract void DrawSettings(IConfigUIDelegates helpers);

        public abstract void DrawStyles(IConfigUIDelegates helpers);

        public abstract TextSource GetCurrentlySpokenTextSource();

        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
        }

        public virtual void OnNpcDialogueSessionEvent(NpcDialogueSessionEvent ev)
        {
        }

        protected abstract void Dispose(bool disposing);

        public void Dispose()
        {
            CancelPendingSynthesis();
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 一次合成的登记句柄，Dispose 时从后端登记表里移除。
    /// </summary>
    public sealed class SynthesisHandle : IDisposable
    {
        private Action? release;

        internal SynthesisHandle(CancellationToken token, Action release)
        {
            this.Token = token;
            this.release = release;
        }

        public CancellationToken Token { get; }

        public void Dispose()
        {
            this.release?.Invoke();
            this.release = null;
        }
    }
}