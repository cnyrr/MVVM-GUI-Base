namespace Wpf.Shell.Services.Navigation.Contracts
{
    /// <summary>
    /// Optional lifecycle hook for snippet ViewModels that need to know when they are actually
    /// visible on a secondary display. Most snippets — static readouts — do not implement this and
    /// pay nothing. Snippets that hold a live resource (a poll, a timer, an animation, a
    /// subscription) implement it to start that resource on show and release it on hide.
    ///
    /// Both methods are synchronous and default to no-ops; a snippet overrides only what it needs.
    /// The owning display shell invokes them and is the sole authority on visibility: a snippet is
    /// "shown" only when its display is in Enrich mode, this snippet is the display's current
    /// selection, and a monitor exists at all. In single-monitor builds, in Mirror or Logo mode, or
    /// while another snippet is selected, these never fire.
    ///
    /// Contract:
    /// <list type="bullet">
    ///   <item>Start resources in <see cref="OnShown"/>; release them in <see cref="OnHidden"/>.</item>
    ///   <item>Do NOT assume <see cref="OnShown"/> ever fires — be inert until it does.</item>
    ///   <item>Neither method may trigger navigation. They run during navigation and mode
    ///   transitions; re-entering navigation from them is a bug.</item>
    /// </list>
    /// </summary>
    public interface IMonitorScreenAware
    {
        /// <summary>Called when this snippet becomes the visible content of a live Enrich display.</summary>
        void OnShown() { }

        /// <summary>
        /// Called when this snippet stops being visible — selection change, navigation away, mode
        /// change off Enrich, or eviction. Release live resources here.
        /// </summary>
        void OnHidden() { }
    }
}