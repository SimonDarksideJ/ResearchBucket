using System.Collections.ObjectModel;
using Avalonia.Threading;
using MonoGameHub.Core.Services;

namespace MonoGameHub.App.ViewModels;

public abstract class LoggableViewModel : ViewModelBase
{
    public ObservableCollection<string> LogLines { get; } = new();

    protected void Log(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        LogBus.Publish(line);

        void AddLine()
        {
            // Keep log bounded.
            if (LogLines.Count > 5000)
                LogLines.RemoveAt(0);

            LogLines.Add(line);
        }

        if (Dispatcher.UIThread.CheckAccess())
            AddLine();
        else
            Dispatcher.UIThread.Post(AddLine);
    }
}

