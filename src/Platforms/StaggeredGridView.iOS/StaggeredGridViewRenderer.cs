using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using Foundation;
using StaggeredGridView.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Xam.Controls.StaggeredGridView), typeof(StaggeredGridViewRenderer))]

namespace StaggeredGridView.iOS
{
    public class StaggeredGridViewRenderer : ViewRenderer<Xam.Controls.StaggeredGridView, UIView>
    {
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
                {
                    attrs.Add(attr);
                }
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