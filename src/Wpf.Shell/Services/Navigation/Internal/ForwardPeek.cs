namespace Wpf.Shell.Services.Navigation.Internal
{
    /// <summary>
    /// A read-only snapshot of the next forward frame in the active tab's history.
    /// 
    /// Returned by <see cref="INavigationService.PeekForward"/> for the facade to inspect when
    /// deciding between recovery (the requested navigation matches the existing forward frame,
    /// so go forward) and branching (parameters differ, so push and discard the future).
    /// 
    /// The framework constructs this DTO; consumers compare it against a requested navigation by
    /// matching <see cref="ViewModelType"/> and using value equality on <see cref="Parameters"/>.
    /// </summary>
    /// <param name="ViewModelType">
    /// The runtime type of the ViewModel in the next forward frame.
    /// </param>
    /// <param name="Parameters">
    /// The parameters stored on the next forward frame, captured at the time it was originally
    /// pushed. Boxed as <see cref="object"/> because the navigation service stores frames
    /// heterogeneously (different frames carry different parameter types). The facade casts to
    /// the expected parameter type when comparing.
    /// </param>
    internal sealed record ForwardPeek(Type ViewModelType, object Parameters);
}
