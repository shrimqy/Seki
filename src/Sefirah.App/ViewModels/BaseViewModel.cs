using Sefirah.App.Utils;

namespace Sefirah.App.ViewModels;
public abstract class BaseViewModel : ObservableObject
{
    protected readonly ILogger _logger;

    protected BaseViewModel()
    {
        _logger = Ioc.Default.GetRequiredService<ILogger>();
    }
}
