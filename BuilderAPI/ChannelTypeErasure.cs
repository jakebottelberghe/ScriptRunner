using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace BuilderAPI
{
    internal interface IMessageErasure
    {
        public void PublishEvent(object message);
    }

    internal class ChannelTypeErasure<MessageType> : IMessageErasure where MessageType : class
    {
        private readonly Channel<MessageType> _channel;

        public ChannelReader<MessageType>? MessageReader => _channel?.Reader;

        public ChannelTypeErasure()
        {
            _channel = Channel.CreateUnbounded<MessageType>();
        }

        public void PublishEvent(object message)
        {
            var typedMessage = message as MessageType;
            if (typedMessage != null)
            {
                _channel.Writer.WriteAsync(typedMessage);
            }
        }

    }
}
