namespace Beresta.Markdown.Wpf.Presenters;

using System.Globalization;
using System.Windows.Controls;
using Kit;

/// <summary>The date picker of a value in the properties card: a picked date is written as <c>yyyy-MM-dd</c>.</summary>
public sealed class MdPropertyDatePicker : DatePicker
{
    /// <inheritdoc/>
    protected override void OnSelectedDateChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectedDateChanged(e);
        if (SelectedDate is { } date && DataContext is PropertyVm property && date.Date != property.Date)
        {
            MdPropertiesPresenter.SetValueCommand.Execute(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), this);
        }
    }
}
