using System;
using System.Collections.Generic;
using System.Text;

namespace MVVM_Base.Services.Navigation.Contracts
{
    /// <summary>
    /// Marker interface identifying a ViewModel that may serve as a root of a navigation history.
    /// 
    /// Root ViewModels are singletons in the DI container — they live for the application's lifetime
    /// and preserve state across navigation context switches. They cannot be popped from the
    /// navigation history.
    /// 
    /// Detail ViewModels (transient, pushed onto the history above a root) do not implement this
    /// interface. Implementing it should be a deliberate choice indicating "this VM is suitable as
    /// a root."
    /// </summary>
    public interface IRootViewModel
    {
    }
}
