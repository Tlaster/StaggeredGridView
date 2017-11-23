using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CoreGraphics;
using Foundation;
using StaggeredGridView.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;
using PropertyChangingEventArgs = Xamarin.Forms.PropertyChangingEventArgs;

[assembly: ExportRenderer(typeof(Xam.Controls.StaggeredGridView), typeof(StaggeredGridViewRenderer))]

namespace StaggeredGridView.iOS
{
    public class StaggeredGridViewRenderer : ViewRenderer<Xam.Controls.StaggeredGridView, UICollectionView>
    {
        private ViewDataSource _dataSource;

        private ViewDataSource DataSource =>
            _dataSource ?? (_dataSource = new ViewDataSource(GetCell, RowsInSection, ItemSelected));

        protected override void OnElementChanged(ElementChangedEventArgs<Xam.Controls.StaggeredGridView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
                Unbind(e.OldElement);
            if (e.NewElement != null)
                if (Control == null)
                {
                    var collectionView = new UICollectionView(default(CGRect), new WaterfallLayout())
                    {
                        AllowsMultipleSelection = false,
                        ContentInset = new UIEdgeInsets((float) Element.Padding.Top, (float) Element.Padding.Left,
                            (float) Element.Padding.Bottom, (float) Element.Padding.Right),
                        BackgroundColor = Element.BackgroundColor.ToUIColor()
                    };

                    Bind(e.NewElement);

                    collectionView.Source = DataSource;
                    //collectionView.Delegate = this.GridViewDelegate;

                    SetNativeControl(collectionView);
                }
        }

        public void ItemSelected(UICollectionView tableView, NSIndexPath indexPath)
        {
            var item = Element.ItemsSource.Cast<object>().ElementAt(indexPath.Row);
            Element.InvokeItemSelectedEvent(this, item);
        }

        public int RowsInSection(UICollectionView collectionView, nint section)
        {
            return ((ICollection) Element.ItemsSource).Count;
        }

        public UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = Element.ItemsSource.Cast<object>().ElementAt(indexPath.Row);
            if (!(Element.ItemTemplate.CreateContent() is ViewCell viewCellBinded)) return null;

            viewCellBinded.BindingContext = item;
            return GetCell(collectionView, viewCellBinded, indexPath);
        }

        protected virtual UICollectionViewCell GetCell(UICollectionView collectionView, ViewCell item,
            NSIndexPath indexPath)
        {
            if (!(collectionView.DequeueReusableCell(new NSString(CustomViewCell.Key), indexPath) is CustomViewCell
                collectionCell)) return null;

            collectionCell.ViewCell = item;

            return collectionCell;
        }


        private void Unbind(Xam.Controls.StaggeredGridView oldElement)
        {
            if (oldElement == null) return;

            oldElement.PropertyChanging -= ElementPropertyChanging;
            oldElement.PropertyChanged -= ElementPropertyChanged;

            if (oldElement.ItemsSource is INotifyCollectionChanged itemsSource)
                itemsSource.CollectionChanged -= DataCollectionChanged;
        }

        private void Bind(Xam.Controls.StaggeredGridView newElement)
        {
            if (newElement == null) return;

            newElement.PropertyChanging += ElementPropertyChanging;
            newElement.PropertyChanged += ElementPropertyChanged;

            if (newElement.ItemsSource is INotifyCollectionChanged source)
                source.CollectionChanged += DataCollectionChanged;
        }

        private void ElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Xam.Controls.StaggeredGridView.ItemsSourceProperty.PropertyName)
            {
                if (Element.ItemsSource is INotifyCollectionChanged newItemsSource)
                {
                    newItemsSource.CollectionChanged += DataCollectionChanged;
                    Control.ReloadData();
                }
            }
//            else if (e.PropertyName == "ItemWidth" || e.PropertyName == "ItemHeight")
//            {
//                Control.ItemSize = new CGSize((float) Element.ItemWidth, (float) Element.ItemHeight);
//            }
        }

        private void ElementPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == "ItemsSource")
                if (Element.ItemsSource is INotifyCollectionChanged oldItemsSource)
                    oldItemsSource.CollectionChanged -= DataCollectionChanged;
        }

        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvokeOnMainThread(() =>
            {
                try
                {
                    if (Control == null)
                        return;

                    Control.ReloadData();
                }
                catch
                {
                    // ignored
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && _dataSource != null)
            {
                Unbind(Element);
                _dataSource.Dispose();
                _dataSource = null;
            }
        }
    }

    public class CustomViewCell : UICollectionViewCell
    {
        public const string Key = "StaggeredGridViewCell";
        private UIView _view;
        private ViewCell _viewCell;

        [Export("initWithFrame:")]
        public CustomViewCell(CGRect frame) : base(frame)
        {
            // SelectedBackgroundView = new GridItemSelectedViewOverlay (frame);
            // this.BringSubviewToFront (SelectedBackgroundView);
        }

        public ViewCell ViewCell
        {
            get => _viewCell;
            set
            {
                if (_viewCell != value)
                    UpdateCell(value);
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            var frame = ContentView.Frame;
            frame.X = (Bounds.Width - frame.Width) / 2;
            frame.Y = (Bounds.Height - frame.Height) / 2;
            ViewCell.View.Layout(frame.ToRectangle());
            _view.Frame = frame;
        }

        private void UpdateCell(ViewCell cell)
        {
            if (_viewCell != null)
                _viewCell.PropertyChanged -= HandlePropertyChanged;

            _viewCell = cell;
            _viewCell.PropertyChanged += HandlePropertyChanged;
            //this.viewCell.SendAppearing ();
            UpdateView();
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateView();
        }


        private void UpdateView()
        {
            _view?.RemoveFromSuperview();

            _view = Platform.CreateRenderer(_viewCell.View).NativeView;
            _view.AutoresizingMask = UIViewAutoresizing.All;
            _view.ContentMode = UIViewContentMode.ScaleToFill;

            AddSubview(_view);
        }
    }

    public class ViewDataSource : UICollectionViewSource
    {
        public delegate UICollectionViewCell OnGetCell(UICollectionView collectionView, NSIndexPath indexPath);

        public delegate void OnItemSelected(UICollectionView collectionView, NSIndexPath indexPath);

        public delegate int OnRowsInSection(UICollectionView collectionView, nint section);

        private readonly OnGetCell _onGetCell;
        private readonly OnItemSelected _onItemSelected;

        private readonly OnRowsInSection _onRowsInSection;

        public ViewDataSource(OnGetCell onGetCell, OnRowsInSection onRowsInSection, OnItemSelected onItemSelected)
        {
            _onGetCell = onGetCell;
            _onRowsInSection = onRowsInSection;
            _onItemSelected = onItemSelected;
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            return _onRowsInSection(collectionView, section);
        }

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            _onItemSelected(collectionView, indexPath);
        }


        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = _onGetCell(collectionView, indexPath);

            cell.AddGestureRecognizer(new UITapGestureRecognizer(v => { ItemSelected(collectionView, indexPath); }));

            return cell;
        }
    }

    public interface IWaterfallLayoutDelegate : IUICollectionViewDelegate
    {
        double HeaderHeight { get; }
        double FooterHeight { get; }
        UIEdgeInsets Insets { get; }
        double MinimumColumnSpacing { get; }
        int ColumnCount { get; }
        CGSize GetItemSize(NSIndexPath indexPath);
    }

    public enum WaterfallDirection
    {
        ShortestFirst,
        LeftToRight,
        RightToLeft
    }


    public class WaterfallLayout : UICollectionViewLayout
    {
        public const string ElementKindSectionHeader = "CHTCollectionElementKindSectionHeader";
        public const string ElementKindSectionFooter = "CHTCollectionElementKindSectionFooter";
        private const int UnionSize = 20;
        private List<UICollectionViewLayoutAttributes> _allItemAttributes;
        private int _columnCount;

        private List<List<double>> _columnHeights;
        private WaterfallDirection _direction;
        private double _footerHeight;
        private Dictionary<int, UICollectionViewLayoutAttributes> _footersAttributes;
        private double _headerHeight;
        private Dictionary<int, UICollectionViewLayoutAttributes> _headersAttributes;
        private double _minimumColumnSpacing;
        private double _minimumInteritemSpacing;
        private UIEdgeInsets _sectionInset;
        private List<List<UICollectionViewLayoutAttributes>> _sectionItemAttributes;
        private List<NSValue> _unionRects;

        public WaterfallLayout()
        {
            HeaderHeight = 0.0;
            FooterHeight = 0.0;
            ColumnCount = 2;
            MinimumInteritemSpacing = 10;
            MinimumColumnSpacing = 10;
            SectionInset = UIEdgeInsets.Zero;
            Direction = WaterfallDirection.ShortestFirst;

            _headersAttributes = new Dictionary<int, UICollectionViewLayoutAttributes>();
            _footersAttributes = new Dictionary<int, UICollectionViewLayoutAttributes>();
            _unionRects = new List<NSValue>();
            _columnHeights = new List<List<double>>();
            _allItemAttributes = new List<UICollectionViewLayoutAttributes>();
            _sectionItemAttributes = new List<List<UICollectionViewLayoutAttributes>>();
        }

        public int ColumnCount
        {
            get => _columnCount;
            set
            {
                _columnCount = value;
                InvalidateLayout();
            }
        }

        public double MinimumColumnSpacing
        {
            get => _minimumColumnSpacing;
            set
            {
                _minimumColumnSpacing = value;
                InvalidateLayout();
            }
        }

        public double MinimumInteritemSpacing
        {
            get => _minimumInteritemSpacing;
            set
            {
                _minimumInteritemSpacing = value;
                InvalidateLayout();
            }
        }

        public double HeaderHeight
        {
            get => _headerHeight;
            set
            {
                _headerHeight = value;
                InvalidateLayout();
            }
        }

        public double FooterHeight
        {
            get => _footerHeight;
            set
            {
                _footerHeight = value;
                InvalidateLayout();
            }
        }

        public UIEdgeInsets SectionInset
        {
            get => _sectionInset;
            set
            {
                _sectionInset = value;
                InvalidateLayout();
            }
        }

        public WaterfallDirection Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                InvalidateLayout();
            }
        }

        public IWaterfallLayoutDelegate Delegate => CollectionView?.Delegate as IWaterfallLayoutDelegate;

        public override CGSize CollectionViewContentSize
        {
            get
            {
                var numberOfSections = CollectionView.NumberOfSections();
                if (numberOfSections == 0)
                    return CGSize.Empty;

                var contentSize = CollectionView.Bounds.Size;

                if (_columnHeights.Count > 0)
                {
                    var height = _columnHeights.LastOrDefault()?.FirstOrDefault();
                    if (height != null)
                    {
                        contentSize.Height = new nfloat(height.Value);
                        return contentSize;
                    }
                }
                return CGSize.Empty;
            }
        }


        public int ColumnCountForSection(int section)
        {
            return Delegate?.ColumnCount ?? _columnCount;
        }

        public double ItemWidthInSectionAtIndex(int section)
        {
            var insets = Delegate?.Insets ?? _sectionInset;
            var width = CollectionView.Bounds.Size.Width - insets.Left - insets.Right;
            var columnCount = ColumnCountForSection(section);
            var spaceColumCount = columnCount - 2d;
            return Math.Floor((width - spaceColumCount * MinimumColumnSpacing) / (columnCount * 1d));
        }

        public override void PrepareLayout()
        {
            base.PrepareLayout();
            var numberOfSections = CollectionView.NumberOfSections();
            if (numberOfSections == 0)
                return;

            _headersAttributes = new Dictionary<int, UICollectionViewLayoutAttributes>();
            _footersAttributes = new Dictionary<int, UICollectionViewLayoutAttributes>();
            _unionRects = new List<NSValue>();
            _columnHeights = new List<List<double>>();
            _allItemAttributes = new List<UICollectionViewLayoutAttributes>();
            _sectionItemAttributes = new List<List<UICollectionViewLayoutAttributes>>();
            Enumerable.Range(0, Convert.ToInt32(numberOfSections)).ForEach(num =>
                _columnHeights.Add(Enumerable.Range(0, ColumnCountForSection(num)).Select(item => (double) item)
                    .ToList()));

            var top = 0d;

            for (var i = 0; i < numberOfSections; i++)
            {
                var minimumInteritemSpacing = Delegate?.MinimumColumnSpacing ?? MinimumColumnSpacing;
                var insets = Delegate?.Insets ?? SectionInset;
                var width = CollectionView.Bounds.Size.Width - insets.Left - insets.Right;
                var columnCount = ColumnCountForSection(i);
                var spaceColumCount = columnCount - 1d;
                var itemWidth = Math.Floor((width - spaceColumCount * MinimumColumnSpacing) / (columnCount * 1d));
                var headerHeight = Delegate?.HeaderHeight ?? HeaderHeight;
                if (headerHeight > 0)
                    using (var attributes =
                        UICollectionViewLayoutAttributes.CreateForSupplementaryView(
                            new NSString(ElementKindSectionHeader), NSIndexPath.FromItemSection(0, i)))
                    {
                        attributes.Frame = new CGRect(0, top, CollectionView.Bounds.Size.Width, headerHeight);
                        _headersAttributes.Add(i, attributes);
                        _allItemAttributes.Add(attributes);
                        top = attributes.Frame.GetMaxY();
                    }
                top += insets.Top;
                for (var j = 0; j < columnCount; j++)
                    _columnHeights[i][j] = top;
                var itemCount = CollectionView.NumberOfItemsInSection(i);
                var itemAttributes = new List<UICollectionViewLayoutAttributes>();
                for (var j = 0; j < itemCount; j++)
                    using (var indexPath = NSIndexPath.FromItemSection(j, i))
                    {
                        var columnIndex = NextColumnIndexForItem(j, i);
                        var xOffset = insets.Left + itemWidth + _minimumColumnSpacing * (columnIndex * 1d);
                        var yOffset = _columnHeights[i][columnIndex];
                        var itemSize = Delegate?.GetItemSize(indexPath);
                        var itemHeight = 0d;
                        if (itemSize?.Height > 0 && itemSize?.Width > 0)
                            itemHeight = Math.Floor(itemSize.Value.Height * itemWidth / itemSize.Value.Width);
                        using (var attributes = UICollectionViewLayoutAttributes.CreateForCell(indexPath))
                        {
                            attributes.Frame = new CGRect(xOffset, yOffset, itemWidth, itemHeight);
                            itemAttributes.Add(attributes);
                            _allItemAttributes.Add(attributes);
                            _columnHeights[i][columnIndex] = attributes.Frame.GetMaxY() + minimumInteritemSpacing;
                        }
                    }

                _sectionItemAttributes.Add(itemAttributes);

                var footerColumnIndex = LongestColumnIndexInSection(i);
                top = _columnHeights[i][footerColumnIndex] - minimumInteritemSpacing + insets.Bottom;
                var footerHeight = Delegate?.FooterHeight ?? _footerHeight;
                if (footerHeight > 0)
                    using (var attributes =
                        UICollectionViewLayoutAttributes.CreateForSupplementaryView(
                            new NSString(ElementKindSectionFooter), NSIndexPath.FromItemSection(0, i)))
                    {
                        attributes.Frame = new CGRect(0, top, CollectionView.Bounds.Size.Width, footerHeight);
                        _footersAttributes.Add(i, attributes);
                        _allItemAttributes.Add(attributes);
                        top = attributes.Frame.GetMaxY();
                    }
                for (var j = 0; j < columnCount; j++)
                    _columnHeights[i][j] = top;
            }
            var idx = 0;
            var itemCounts = _allItemAttributes.Count;
            while (idx < itemCounts)
            {
                var rect1 = _allItemAttributes[idx].Frame;
                idx = Math.Min(idx + UnionSize, itemCounts) - 1;
                var rect2 = _allItemAttributes[idx].Frame;
                _unionRects.Add(NSValue.FromCGRect(rect1.UnionWith(rect2)));
                idx += 1;
            }
        }

        public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
        {
            if (indexPath.Section >= _sectionItemAttributes.Count)
                return null;
            var list = _sectionItemAttributes[indexPath.Section];
            if (indexPath.Item >= list.Count)
                return null;
            return list[Convert.ToInt32(indexPath.Item)];
        }

        public override UICollectionViewLayoutAttributes LayoutAttributesForSupplementaryView(NSString kind,
            NSIndexPath indexPath)
        {
            switch (kind)
            {
                case ElementKindSectionHeader:
                    return _headersAttributes[indexPath.Section];
                    break;
                case ElementKindSectionFooter:
                    return _footersAttributes[indexPath.Section];
                    break;
                default:
                    return new UICollectionViewLayoutAttributes();
            }
        }

        public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
        {
            var begin = 0;
            var end = _unionRects.Count;
            var attrs = new List<UICollectionViewLayoutAttributes>();
            for (var i = 0; i < end; i++)
            {
                var unionRect = _unionRects[i];
                if (rect.IntersectsWith(unionRect.CGRectValue))
                {
                    begin = i * UnionSize;
                    break;
                }
            }
            for (var i = _unionRects.Count - 1; i >= 0; i--)
            {
                var unionRect = _unionRects[i];
                if (rect.IntersectsWith(unionRect.CGRectValue))
                {
                    end = Math.Min((i + 1) * UnionSize, _allItemAttributes.Count);
                    break;
                }
            }

            for (var i = begin; i < end; i++)
            {
                var attr = _allItemAttributes[i];
                if (rect.IntersectsWith(attr.Frame))
                    attrs.Add(attr);
            }
            return attrs.ToArray();
        }

        public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
        {
            return newBounds.Width != CollectionView.Bounds.Width;
        }

        public int ShortestColumnIndexInSection(int section)
        {
            var index = 0;
            var shorestHeight = double.MaxValue;
            for (var i = 0; i < _columnHeights[section].Count; i++)
            {
                if (!(_columnHeights[section][i] < shorestHeight)) continue;
                shorestHeight = _columnHeights[section][i];
                index = i;
            }
            return index;
        }

        public int LongestColumnIndexInSection(int section)
        {
            var index = 0;
            var longestHeight = 0.0;
            for (var i = 0; i < _columnHeights[section].Count; i++)
            {
                if (!(_columnHeights[section][i] > longestHeight)) continue;
                longestHeight = _columnHeights[section][i];
                index = i;
            }
            return index;
        }

        public int NextColumnIndexForItem(int item, int section)
        {
            var index = 0;
            var columnCount = ColumnCountForSection(section);
            switch (Direction)
            {
                case WaterfallDirection.ShortestFirst:
                    index = ShortestColumnIndexInSection(section);
                    break;
                case WaterfallDirection.LeftToRight:
                    index = item % columnCount;
                    break;
                case WaterfallDirection.RightToLeft:
                    index = columnCount - 1 - item % columnCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return index;
        }
    }
}