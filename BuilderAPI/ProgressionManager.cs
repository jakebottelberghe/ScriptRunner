using NuGet.Protocol.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace BuilderAPI
{
    public class ProgressionManager
    {
        private CancellationTokenSource _cancellationSource;
        private static ProgressionManager _global = new ProgressionManager();

        private Dictionary<string, List<Delegate>> _eventCallbacks = new ();
        private Dictionary<string, IMessageErasure> _channels = new();
        private Dictionary<string, Task> _processing = new();

        private ProgressionManager() {
            _cancellationSource = new CancellationTokenSource();
        }
        public static void Write<MessageType>(MessageType message) where MessageType : class
        {
            var messageType = typeof(MessageType).Name;
            if (_global._channels.ContainsKey(messageType)) return;
            _global._channels[messageType].PublishEvent(message);
        }
        public static void CreateChannel<MessageType>() where MessageType : class
        {
            var messageType = typeof(MessageType).Name;
            if (_global._channels.ContainsKey(messageType)) return;
            
            _global._channels[messageType] = new ChannelTypeErasure<MessageType>();
            
            if(!_global._eventCallbacks.ContainsKey(messageType)) _global._eventCallbacks[messageType] = new List<Delegate>();
            
            _global._processing[messageType] = _global.MessageReader<MessageType>(_global._cancellationSource.Token);
        }

        public static void AddMessageHandler<MessageType>(Action<MessageType> messageCallback) where MessageType : class
        {
            var messageType = typeof(MessageType).Name;
            if (!_global._eventCallbacks.ContainsKey(messageType)) _global._eventCallbacks[messageType] = new List<Delegate>();
            _global._eventCallbacks[messageType].Add(messageCallback);
        }

        private async Task MessageReader<MessageType>( CancellationToken token) where MessageType : class
        {
            var messageType = typeof(MessageType).Name;
            var c = _channels[messageType];
            var tc = c as ChannelTypeErasure<MessageType>;
            var q = _eventCallbacks[messageType];
            if (tc == null || q == null || tc.MessageReader == null) return;
            await foreach(var message in tc.MessageReader.ReadAllAsync(token))
            {
                foreach (var action in q)
                {
                    action.DynamicInvoke(message);
                }
            }
        }
    }
}
