using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace BuilderAPI
{

    internal record MonitorRecord(string Name, string Path, string OldName, string OldPath, List<string> Tags);

    internal class FileMonitor : IDisposable
    {
        private class MonitorMessage
        {
            public WatcherChangeTypes EventType { get; set; } = WatcherChangeTypes.All;
            public string File { get; set; } = string.Empty;
            public string OldFile { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string OldPath { get; set; } = string.Empty;
        };


        private readonly NotifyFilters _fileFilterEvents = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite;
        private readonly int _workerThreadWaitMS = 500;

        private FileSystemWatcher? _watcher = null;
        private Channel<MonitorMessage>? _messageChannel = null;
        private CancellationTokenSource? _eventCancellationToken = null;
        private Task? _eventWorker = null;

        public List<Action<MonitorRecord>> EventHanderCallback { get; init; } = new ();

        public void StartMonitor(string projectRoot)
        {
            StopMonitor();

            _messageChannel = Channel.CreateUnbounded<MonitorMessage>();

            _eventCancellationToken = new CancellationTokenSource();
            _eventWorker = EventHandler(_eventCancellationToken.Token);

            _watcher = new FileSystemWatcher(projectRoot);
            _watcher.NotifyFilter = _fileFilterEvents;
            _watcher.IncludeSubdirectories = true;
            _watcher.Created += OnStandardEvent;
            _watcher.Changed += OnStandardEvent;
            _watcher.Deleted += OnStandardEvent;
            _watcher.Renamed += OnRenamed;
            _watcher.EnableRaisingEvents = true;
        }
        public void StopMonitor()
        {
            // This order is important:
            // We need to stop monitoring file changes before we stop writing to the channel.
            if(_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnStandardEvent;
                _watcher.Changed -= OnStandardEvent;
                _watcher.Deleted -= OnStandardEvent;
                _watcher.Renamed -= OnRenamed;
                _watcher.Dispose();
            }
            if (_messageChannel != null)
            {
                _messageChannel.Writer.Complete();
            }
            if(_eventWorker != null)
            { 
                _eventWorker?.Wait(); 
            }
            _watcher = null;
            _eventCancellationToken = null;
            _eventWorker = null;
            _messageChannel = null;
        }

        private async Task EventHandler(CancellationToken token)
        {
            if (_messageChannel == null) return;
            await foreach(var mp in _messageChannel.Reader.ReadAllAsync(token))
            {
                var tags = Enum.GetValues<WatcherChangeTypes>()
                    .Where(flag => mp.EventType.HasFlag(flag))
                    .Select(flag => flag.ToString())
                    .ToList();

                var rec = new MonitorRecord(mp.File, mp.Path, mp.OldFile, mp.OldPath, tags);

                EventHanderCallback.ForEach(callback => callback(rec));
            }
        }

        private void OnStandardEvent(object sender, FileSystemEventArgs e)
        {
            if(sender == this && _messageChannel != null)
            {
                _messageChannel.Writer.TryWrite(new MonitorMessage
                {
                    EventType = e.ChangeType,
                    File = e.Name?.Contains('.') == true ? e.Name : "",
                    Path = e.FullPath,
                });
            }
        }

        private void OnRenamed(object sender, FileSystemEventArgs e)
        {
            if (sender == this)
            {
                var renameEvent = e as RenamedEventArgs;
                if (renameEvent != null && _messageChannel != null)
                {
                    _messageChannel.Writer.TryWrite(new MonitorMessage
                    {
                        EventType = e.ChangeType,
                        File = renameEvent.Name?.Contains('.') == true ? renameEvent.Name : "",
                        Path = renameEvent.FullPath,
                        OldFile = renameEvent.OldName?.Contains('.') == true ? renameEvent.OldName : "",
                        OldPath = renameEvent.OldFullPath,
                    });
                }
            }
        }

        public void Dispose()
        {
            StopMonitor();
        }
    }
}
