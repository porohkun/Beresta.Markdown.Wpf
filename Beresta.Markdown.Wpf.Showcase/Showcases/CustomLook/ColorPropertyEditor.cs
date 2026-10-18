namespace Beresta.Markdown.Wpf.Showcase.Showcases.CustomLook;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using HandyControl.Controls;
using HandyControl.Tools.Converter;

/// <summary>Редактор цвета: образец и код цвета, по щелчку — пикер HandyControl; выбранный цвет ставится сразу.</summary>
public sealed class ColorPropertyEditor : PropertyEditorBase
{
    private readonly ColorPicker _picker = new();

    public override FrameworkElement CreateElement(PropertyItem propertyItem)
    {
        var swatch = new Border
        {
            Width = 16,
            Height = 16,
            CornerRadius = new(3),
            BorderThickness = new(1),
            Margin = new(0, 0, 8, 0)
        };
        swatch.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        swatch.SetBinding(Border.BackgroundProperty, new Binding(nameof(ColorPicker.SelectedBrush)) { Source = _picker });

        var code = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        code.SetBinding(TextBlock.TextProperty, new Binding("SelectedBrush.Color") { Source = _picker });

        var button = new ToggleButton
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            IsEnabled = !propertyItem.IsReadOnly,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { swatch, code }
            }
        };

        var frame = new Border
        {
            Margin = new(8),
            BorderThickness = new(1),
            CornerRadius = new(4),
            Child = _picker
        };
        frame.SetResourceReference(Border.BackgroundProperty, "RegionBrush");
        frame.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        frame.SetResourceReference(UIElement.EffectProperty, "EffectShadow2");

        var popup = new Popup
        {
            PlacementTarget = button,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = frame
        };
        popup.SetBinding(Popup.IsOpenProperty, new Binding(nameof(ToggleButton.IsChecked)) { Source = button, Mode = BindingMode.TwoWay });

        // BYDESIGN: пока пикер открыт, кнопка щелчков не ловит: щелчок по ней закрыл бы пикер и тут же открыл снова.
        button.SetBinding(UIElement.IsHitTestVisibleProperty, new Binding(nameof(Popup.IsOpen))
        {
            Source = popup,
            Converter = new Boolean2BooleanReConverter()
        });

        // BYDESIGN: ширина кнопок пикера в шаблоне HandyControl задана под английские подписи — русская
        // «Подтвердить» в неё не влезает.
        _picker.Loaded += (_, _) =>
        {
            foreach (var choice in Visuals.Descendants<Button>(_picker).Where(button => button.Content is string))
            {
                choice.Width = double.NaN;
                choice.MinWidth = 80;
            }
        };

        SolidColorBrush? opened = null;
        popup.Opened += (_, _) => opened = _picker.SelectedBrush;
        _picker.Confirmed += (_, _) => button.IsChecked = false;
        _picker.Canceled += (_, _) =>
        {
            if (opened is not null)
            {
                _picker.SelectedBrush = opened;
            }

            button.IsChecked = false;
        };

        return new Grid { Children = { button, popup } };
    }

    public override void CreateBinding(PropertyItem propertyItem, DependencyObject element)
        => BindingOperations.SetBinding(
            _picker,
            ColorPicker.SelectedBrushProperty,
            new Binding(propertyItem.PropertyName)
            {
                Source = propertyItem.Value,
                Mode = BindingMode.TwoWay
            });

    public override DependencyProperty GetDependencyProperty() => ColorPicker.SelectedBrushProperty;
}
