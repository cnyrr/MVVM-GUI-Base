using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MVVM_Base.ViewModels
{
    /// <summary>
    /// Universal abstract base for every ViewModel in the application.
    ///
    /// Provides two things, deliberately kept thin:
    /// <list type="bullet">
    ///   <item>An observable <see cref="Name"/> for future breadcrumbs / titles / debug overlays.
    ///   No consumer reads it today; sub-VMs that participate in breadcrumbs will assign it as
    ///   their data loads. Defaults to <c>"unlabeled"</c> — visible if it ever surfaces, easy to
    ///   spot, harmless if not.</item>
    ///   <item>An injected <see cref="Logger"/>. Available everywhere, used selectively. Logging
    ///   is not active beyond what derived VMs explicitly emit — the dependency is in place so
    ///   logging adoption doesn't require touching constructor signatures across the codebase.</item>
    /// </list>
    ///
    /// Note: Tab labels live on <see cref="MVVM_Base.Services.Navigation.Contracts.IRootViewModel.TabLabel"/>,
    /// not here. <see cref="Name"/> and <c>IRootViewModel.Label</c> are independent — a root may
    /// expose the same string for both, but the framework does not assume they're related.
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject
    {
        /// <summary>
        /// Display name for this VM. Observable so breadcrumbs / titles can update as data loads.
        /// Defaults to <c>"unlabeled"</c>.
        /// </summary>
        [ObservableProperty]
        private string _name = "unlabeled";

        /// <summary>
        /// Logger scoped to the derived ViewModel's concrete type. Always non-null.
        /// </summary>
        protected ILogger Logger { get; }

        protected ViewModelBase(ILogger logger)
        {
            Logger = logger;
        }
    }
}