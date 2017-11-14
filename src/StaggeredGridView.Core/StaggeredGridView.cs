using System;
using System.Collections;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Xam.Controls
{
    public class StaggeredGridView : ListView
    {
        public new static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource),
            typeof(IEnumerable), typeof(StaggeredGridView));

        public new static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate),
            typeof(DataTemplate), typeof(StaggeredGridView));

        public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing),
            typeof(double), typeof(StaggeredGridView), 0.0);

        public static readonly BindableProperty PaddingProperty =
            BindableProperty.Create(nameof(Padding), typeof(Thickness), typeof(StaggeredGridView), new Thickness(0));

        public StaggeredGridView()
        {
            HasUnevenRows = true;
        }

        public Thickness Padding
        {
            get => (Thickness) GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        public new IEnumerable ItemsSource
        {
            get => (IEnumerable) GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public new DataTemplate ItemTemplate
        {
            get => (DataTemplate) GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public double Spacing
        {
            get => (double) GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }


        public new Cell CreateDefaultCell(object item)
        {
            return CreateDefault(item);
        }

        protected override Cell CreateDefault(object item)
        {
            string text = null;
            if (item != null)
                text = item.ToString();

            return new TextCell {Text = text};
        }

        public new event EventHandler<EventArg<object>> ItemSelected;

        public void InvokeItemSelectedEvent(object sender, object item)
        {
            ItemSelected?.Invoke(sender, new EventArg<object>(item));
        }
    }
}