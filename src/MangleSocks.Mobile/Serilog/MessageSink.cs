using System;
using System.IO;
using System.Text;
using MangleSocks.Mobile.Messaging;
using Microsoft.Extensions.ObjectPool;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xamarin.Forms;

namespace MangleSocks.Mobile.Serilog
{
    class MessageSink : ILogEventSink
    {
        static readonly ObjectPool<StringBuilder> s_StringBuilderPool =
            new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());

        readonly IMessagingCenter _messagingCenter;
        readonly MessageTemplateTextFormatter _formatter;

        public MessageSink(string outputTemplate, IMessagingCenter messagingCenter, IFormatProvider formatProvider = null)
        {
            this._messagingCenter = messagingCenter ?? throw new ArgumentNullException(nameof(messagingCenter));
            this._formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
        }

        public void Emit(LogEvent logEvent)
        {
            var stringBuilder = s_StringBuilderPool.Get();
            try
            {
                var stringWriter = new StringWriter(stringBuilder);
                this._formatter.Format(logEvent, stringWriter);
                TrimTrailingNewline(stringWriter);
                var message = stringWriter.ToString();
                this._messagingCenter.Send(
                    Application.Current,
                    nameof(ServiceLogMessage),
                    new ServiceLogMessage(logEvent.Level, message));
            }
            finally
            {
                s_StringBuilderPool.Return(stringBuilder);
            }
        }

        static void TrimTrailingNewline(StringWriter stringWriter)
        {
            var stringBuilder = stringWriter.GetStringBuilder();

            bool endsWithNewLine = true;
            if (stringBuilder.Length <= stringWriter.NewLine.Length)
            {
                return;
            }

            for (int i = 0, j = stringBuilder.Length - stringWriter.NewLine.Length;
                i < stringWriter.NewLine.Length;
                i++, j++)
            {
                endsWithNewLine = stringBuilder[j] == stringWriter.NewLine[i];
                if (!endsWithNewLine)
                {
                    break;
                }
            }

            if (endsWithNewLine)
            {
                stringBuilder.Length -= stringWriter.NewLine.Length;
            }
        }
    }
}