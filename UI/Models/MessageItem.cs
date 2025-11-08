using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UI.Models
{
    public class MessageItem : INotifyPropertyChanged
    {
        private string _text = "";
        private MessageRole _role = MessageRole.Program;

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public MessageRole Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPlayer)); }
        }

        public bool IsPlayer => _role == MessageRole.Player;

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string p = null)
          => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}