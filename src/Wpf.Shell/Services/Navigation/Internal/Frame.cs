using CommunityToolkit.Mvvm.ComponentModel;

namespace Wpf.Shell.Services.Navigation.Internal
{
    /// <summary>
    /// One entry in a tab's navigation history.
    /// 
    /// A frame couples a ViewModel instance with the parameters it was pushed with and a
    /// cancellation token source for cooperative cancellation of in-flight async work.
    /// 
    /// Parameters are stored as <see cref="object"/> because the navigation service holds
    /// frames heterogeneously — different frames in the same history carry different parameter
    /// types. The framework casts back to the expected type when dispatching lifecycle hooks.
    /// 
    /// Frames are constructed by the navigation service on push and on bootstrap. They are
    /// removed from the history on discard (branching past them); the service runs the discard
    /// cleanup sequence (cancel CTS, fire <c>OnNavigatedFromAsync(Discarded)</c> if applicable,
    /// dispose if applicable) before dropping the reference.
    /// 
    /// Root frames (the depth-0 frame of each tab) are never discarded; their CTS is created at
    /// frame construction and lives for the application's lifetime.
    /// </summary>
    /// <param name="ViewModel">The ViewModel instance shown when this frame is current.</param>
    /// <param name="Parameters">
    /// The parameters captured at push time. For frames whose ViewModel implements
    /// <c>INavigationAware&lt;TParameters&gt;</c>, this is the typed parameter value; the framework
    /// casts on dispatch. For frames whose ViewModel does not implement that interface, the
    /// parameters are stored but never delivered.
    /// </param>
    /// <param name="Cts">
    /// Cancellation token source for cooperative cancellation of async work owned by this frame.
    /// Cancelled when the frame is discarded; never cancelled while the frame remains in the history.
    /// </param>
    internal sealed record Frame(
        ObservableObject ViewModel,
        object Parameters,
        CancellationTokenSource Cts);
}
