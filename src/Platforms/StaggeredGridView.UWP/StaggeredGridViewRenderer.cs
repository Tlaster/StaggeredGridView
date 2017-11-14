using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Marduk.Controls;
using StaggeredGridView.UWP;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.UWP;
using DataTemplate = Windows.UI.Xaml.DataTemplate;
using DataTemplateSelector = Xamarin.Forms.DataTemplateSelector;
using ItemTappedEventArgs = Marduk.Controls.ItemTappedEventArgs;
using ListView = Xam.Controls.StaggeredGridView;
using PropertyChangingEventArgs = Xamarin.Forms.PropertyChangingEventArgs;
using Size = Windows.Foundation.Size;
using WApp = Windows.UI.Xaml.Application;

[assembly: ExportRenderer(typeof(ListView), typeof(StaggeredGridViewRenderer))]

namespace StaggeredGridView.UWP
{
    public class StaggeredGridViewRenderer : ViewRenderer<ListView, ScrollViewer>
    {
        private WaterfallFlowView _control;

        protected override void OnElementChanged(ElementChangedEventArgs<ListView> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
            {
                var a = WApp.Current.Resources["ExCellTemplate"];
                _control = new WaterfallFlowView
                {
                    IsAdaptiveEnable = true,
                    ItemTemplate = (DataTemplate) WApp.Current.Resources["ExCellTemplate"],
                    HeaderTemplate = (DataTemplate) WApp.Current.Resources["View"],
                    FooterTemplate = (DataTemplate) WApp.Current.Resources["View"],
                    StackCount = 3,
                    DelayMeasure = true,
                    Spacing = Element.Spacing
                };


                SetNativeControl(new ScrollViewer());
                Control.Padding = new Windows.UI.Xaml.Thickness(Convert.ToInt32(Element.Padding.Left), Convert.ToInt32(Element.Padding.Top), Convert.ToInt32(Element.Padding.Right), Convert.ToInt32(Element.Padding.Bottom));
                Control.Content = _control;
            }
            Unbind(e.OldElement);
            Bind(e.NewElement);
        }

        private void Unbind(ListView oldElement)
        {
            if (oldElement != null)
            {
                oldElement.PropertyChanging -= ElementPropertyChanging;
                oldElement.PropertyChanged -= ElementPropertyChanged;
                if (oldElement.ItemsSource is INotifyCollectionChanged changed)
                    changed.CollectionChanged -= DataCollectionChanged;
            }
        }

        private void ElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ListView.ItemsSourceProperty.PropertyName)
            {
                if (Element.ItemsSource is INotifyCollectionChanged changed)
                    changed.CollectionChanged += DataCollectionChanged;
                ReloadData();
            }
        }

        private void ElementPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == ListView.ItemsSourceProperty.PropertyName)
                if (Element.ItemsSource is INotifyCollectionChanged changed)
                    changed.CollectionChanged -= DataCollectionChanged;
        }

        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ReloadData();
        }

        private void ReloadData()
        {
            _control.ItemSource = Element.ItemsSource;
            Element.ClearValue(ExCellControl.MeasuredEstimateProperty);
        }

        private void Bind(ListView newElement)
        {
            if (newElement != null)
            {
                newElement.PropertyChanging += ElementPropertyChanging;
                newElement.PropertyChanged += ElementPropertyChanged;
                if (newElement.ItemsSource is INotifyCollectionChanged changed)
                    changed.CollectionChanged += DataCollectionChanged;
                ReloadData();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case nameof(Element.Spacing):
                    _control.Spacing = Element.Spacing;
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            //if (disposing)
                //_control.ItemTapped -= ControlOnItemTapped;
            base.Dispose(disposing);
        }
    }


    public class ExCellControl : ContentControl
    {
        public static readonly DependencyProperty CellProperty = DependencyProperty.Register(nameof(Cell), typeof(object),
            typeof(ExCellControl),
            new PropertyMetadata(null, (o, e) => ((ExCellControl) o).SetSource((Cell) e.OldValue, (Cell) e.NewValue)));

        internal static readonly BindableProperty MeasuredEstimateProperty =
            BindableProperty.Create("MeasuredEstimate", typeof(double), typeof(ListView), -1d);

        private readonly Lazy<ListView> _listView;
        
        private DataTemplate _currentTemplate;
        private bool _isListViewRealized;
        private object _newValue;

        public ExCellControl()
        {
            _listView = new Lazy<ListView>(GetListView);
            DataContextChanged += OnDataContextChanged;
            Unloaded += (sender, args) => { Cell?.SendDisappearing(); };
        }

        public Cell Cell
        {
            get => (Cell) GetValue(CellProperty);
            set => SetValue(CellProperty, value);
        }
        

        protected FrameworkElement CellContent => (FrameworkElement) Content;
        

        protected override Size MeasureOverride(Size availableSize)
        {
            var lv = _listView.Value;
            
            if (_newValue != null)
            {
                SetCell(_newValue);
                _newValue = null;
            }

            if (Content == null)
            {
                if (lv != null)
                {
                    var estimate = (double) lv.GetValue(MeasuredEstimateProperty);
                    if (estimate > -1)
                        return new Size(availableSize.Width, estimate);
                }
                return new Size(0, Cell.DefaultCellHeight);
            }
            var result = base.MeasureOverride(availableSize);
            lv?.SetValue(MeasuredEstimateProperty, result.Height);
            return result;
        }

        private ListView GetListView()
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null)
            {
                if (parent is StaggeredGridViewRenderer lv)
                {
                    _isListViewRealized = true;
                    return lv.Element;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private DataTemplate GetTemplate(Cell cell)
        {
            var renderer = Registrar.Registered.GetHandler<ICellRenderer>(cell.GetType());
            return renderer.GetTemplate(cell);
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            //TODO: Memory leak!
            if (_isListViewRealized || args.NewValue is Cell)
                SetCell(args.NewValue);
            else if (args.NewValue != null)
                _newValue = args.NewValue;
        }
        

        private void SetCell(object newContext)
        {
            var cell = newContext as Cell;

            if (ReferenceEquals(Cell?.BindingContext, newContext))
                return;
            var lv = _listView.Value;
            if (lv != null)
            {
                var template = lv.ItemTemplate;
                var bindingContext = newContext;

                if (template is DataTemplateSelector)
                    template = ((DataTemplateSelector) template).SelectTemplate(bindingContext, lv);

                if (template != null)
                    cell = template.CreateContent() as Cell;
                else
                    cell = lv.CreateDefaultCell(bindingContext);
                
                cell.Parent = lv;
                
                BindableObject.SetInheritedBindingContext(cell, bindingContext);
            }

            Cell = cell;
        }

        private void SetSource(Cell oldCell, Cell newCell)
        {
            if (oldCell != null)
            {
                CellContent.Tapped -= CellContent_Tapped;
                oldCell.SendDisappearing();
            }

            if (newCell != null)
            {
                newCell.SendAppearing();
                UpdateContent(newCell);
                CellContent.Tapped += CellContent_Tapped;
            }
        }

        private void CellContent_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var lv = _listView.Value;
            lv?.InvokeItemSelectedEvent(lv, (CellContent.DataContext as Cell).BindingContext);
        }

        private void UpdateContent(Cell newCell)
        {
            var dt = GetTemplate(newCell);
            if (dt != _currentTemplate || Content == null)
            {
                _currentTemplate = dt;
                Content = dt.LoadContent();
            }
            ((FrameworkElement) Content).DataContext = newCell;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new FrameworkElementAutomationPeer(this);
        }
        
    }
}