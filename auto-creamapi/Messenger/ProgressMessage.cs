using HttpProgress;
using MvvmCross.Plugin.Messenger;

namespace auto_creamapi.Messenger
{
    public class ProgressMessage : MvxMessage
    {
        private readonly ICopyProgress _progress;
        private double _percentProgress;

        public ProgressMessage(object sender, string info, string filename, ICopyProgress progress) : base(sender)
        {
            Info = info;
            Filename = filename;
            _progress = progress;
        }

        public ProgressMessage(object sender, string info, string filename, double progress) : base(sender)
        {
            _progress = null;
            Info = info;
            Filename = filename;
            PercentComplete = progress;
        }

        public string Info { get; }
        public string Filename { get; }

        public double PercentComplete
        {
            get => _progress?.PercentComplete ?? _percentProgress;
            private set => _percentProgress = value;
        }

        // public long BytesTransferred => _progress.BytesTransferred;
        // public long ExpectedBytes => _progress.ExpectedBytes;
        // public long BytesPerSecond => _progress.BytesPerSecond;
        // public TimeSpan TransferTime => _progress.TransferTime;
    }
}