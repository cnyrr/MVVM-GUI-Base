using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Wpf.Shell.Services.Navigation.Contracts;
using Wpf.Shell.ViewModels;

namespace Wpf.Shell.Bench.ViewModels.TestScaling
{
    /// <summary>
    /// A page (transient detail VM, not a tab root) that pushes the layout, theming, and scaling
    /// paths under load: a dense reflowing cell grid, a data table with ellipsizing and wrapping
    /// columns, an unbreakable string in three handling modes, and enough vertical content to
    /// exercise scrolling. Navigated to from the scale-test root; pushed onto the active tab's
    /// history, so it carries its own Back command.
    ///
    /// Content is generated once in the constructor.
    /// </summary>
    public sealed partial class TestStressViewModel : ViewModelBase
    {
        private readonly INavigationFacade _nav;

        public TestStressViewModel(INavigationFacade nav, ILogger<TestStressViewModel> logger)
            : base(logger)
        {
            _nav = nav;
            Cells = Enumerable.Range(1, 200).ToList();
            Rows = BuildRows(40);
        }

        /// <summary>Many small items for the reflowing density grid.</summary>
        public IReadOnlyList<int> Cells { get; }

        /// <summary>Rows for the data table. One row carries a deliberately over-long name.</summary>
        public IReadOnlyList<StressRow> Rows { get; }

        /// <summary>
        /// A long token with no whitespace or hyphen break opportunities. Forces the text engine
        /// into emergency character-wrap / overflow / trim depending on the consuming TextBlock.
        /// </summary>
        public string UnbreakableToken =>
            "https://example.invalid/extremely/long/path/segment?token=abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>A long wrapping paragraph for the constrained reading column.</summary>
        public string LongParagraph =>
            "This block uses TextWrapping and a max content width so its line length stays readable " +
            "no matter how wide the window gets. On an ultrawide screen it should cap out and centre " +
            "rather than stretching edge to edge; on a narrow or portrait screen it should shrink and " +
            "reflow without clipping. If the line length ever runs the full width of a 32:9 display, " +
            "the max-width clamp on this column is not being applied. The text continues here only to " +
            "give the ScrollViewer something to scroll and to confirm vertical growth behaves on the " +
            "shorter portrait and 800x480 sizes where everything cannot fit at once.";

        [RelayCommand]
        private Task GoBackAsync() => _nav.GoBackAsync();

        private static IReadOnlyList<StressRow> BuildRows(int n)
        {
            var statuses = new[] { "Info", "Success", "Warning", "Error" };
            var rows = new List<StressRow>(n);
            for (var i = 1; i <= n; i++)
            {
                var name = i == 7
                    ? "A deliberately very long row name that should ellipsize when the Name column is narrow"
                    : $"Item {i:000}";

                rows.Add(new StressRow(
                    i,
                    name,
                    statuses[i % statuses.Length],
                    $"Wrapping detail for row {i}. This column grows vertically as it wraps, which — " +
                    $"stacked over 40 rows — is what drives the page past one viewport height."));
            }
            return rows;
        }
    }

    /// <summary>One row in the stress-test data table. Co-located with its VM per convention.</summary>
    public sealed record StressRow(int Id, string Name, string Status, string Detail);
}