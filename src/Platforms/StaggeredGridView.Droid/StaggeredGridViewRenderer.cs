using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Android.Content;
using Android.Content.Res;
using Android.Support.V4.View;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using StaggeredGridView.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Object = Java.Lang.Object;
using PropertyChangingEventArgs = Xamarin.Forms.PropertyChangingEventArgs;
using View = Android.Views.View;

[assembly: ExportRenderer(typeof(Xam.Controls.StaggeredGridView), typeof(StaggeredGridViewRenderer))]

namespace StaggeredGridView.Droid
{
    public class StaggeredGridViewRenderer : ViewRenderer<Xam.Controls.StaggeredGridView, RecyclerView>
    {
        private ViewAdapter _adapter;

        protected override void OnElementChanged(ElementChangedEventArgs<Xam.Controls.StaggeredGridView> e)
        {
            base.OnElementChanged(e);
            if (e.NewElement != null)
            {
                var context = Context;
                var collectionView = new RecyclerView(context);

                var metrics = Resources.DisplayMetrics;
                var spacing = Element.Spacing;
                var width = metrics.WidthPixels;

                collectionView.SetPadding(Convert.ToInt32(Element.Padding.Left), Convert.ToInt32(Element.Padding.Top),
                    Convert.ToInt32(Element.Padding.Right), Convert.ToInt32(Element.Padding.Bottom));

                collectionView.SetBackgroundColor(Element.BackgroundColor.ToAndroid());

                Unbind(e.OldElement);
                Bind(e.NewElement);

                _adapter = new ViewAdapter(context, collectionView, Element);
                collectionView.SetAdapter(_adapter);

                collectionView.SetLayoutManager(new AutoStaggeredGridLayoutManager(ContextHelper.Dp2Px(Context, 100), StaggeredGridLayoutManager.Vertical));

                SetNativeControl(collectionView);
            }
        }

        private void Unbind(Xam.Controls.StaggeredGridView oldElement)
        {
            if (oldElement != null)
            {
                oldElement.PropertyChanging -= ElementPropertyChanging;
                oldElement.PropertyChanged -= ElementPropertyChanged;
                if (oldElement.ItemsSource is INotifyCollectionChanged changed)
                    changed.CollectionChanged -= DataCollectionChanged;
            }
        }

        private void Bind(Xam.Controls.StaggeredGridView newElement)
        {
            if (newElement != null)
            {
                newElement.PropertyChanging += ElementPropertyChanging;
                newElement.PropertyChanged += ElementPropertyChanged;
                if (newElement.ItemsSource is INotifyCollectionChanged changed)
                    changed.CollectionChanged += DataCollectionChanged;
            }
        }

        private void ElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Xam.Controls.StaggeredGridView.ItemsSourceProperty.PropertyName)
            {
                if (Element.ItemsSource is INotifyCollectionChanged changed)
                    changed.CollectionChanged += DataCollectionChanged;
                ReloadData();
            }
        }

        private void ElementPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == Xam.Controls.StaggeredGridView.ItemsSourceProperty.PropertyName)
                if (Element.ItemsSource is INotifyCollectionChanged changed)
                    changed.CollectionChanged -= DataCollectionChanged;
        }

        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ReloadData();
        }

        private void ReloadData()
        {
            _adapter?.NotifyDataSetChanged();
        }
    }


    public class ViewAdapter : RecyclerView.Adapter
    {
        private readonly RecyclerView _aView;

        private readonly Xam.Controls.StaggeredGridView _view;

        private Context _context;

        public ViewAdapter(Context context, RecyclerView aView, Xam.Controls.StaggeredGridView view)
        {
            _context = context;
            _aView = aView;
            _view = view;
        }

        public override int ItemCount => (_view.ItemsSource as ICollection).Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = holder as RecyclerViewHolder;
            UpdateItemView(item, position);
        }

        public override void OnViewRecycled(Object holder)
        {
            base.OnViewRecycled(holder);
            if (holder is RecyclerViewHolder viewHolder)
            {
                viewHolder.ViewCell.SendDisappearing();
            }
        }

        private void UpdateItemView(RecyclerViewHolder viewHolder, int position)
        {
            var dataContext = (_view.ItemsSource as IList)[position];
            if (dataContext != null)
            {
                ViewCell viewCell;
                var template = _view.ItemTemplate;
                if (template is DataTemplateSelector)
                    template = ((DataTemplateSelector)template).SelectTemplate(dataContext, _view);
                if (template != null)
                    viewCell = template.CreateContent() as ViewCell;
                else
                    viewCell = _view.CreateDefaultCell(dataContext) as ViewCell;
                viewCell.SendAppearing();
                viewHolder.UpdateUi(viewCell, dataContext, _view);
                //var dataTemplate = _view.ItemTemplate;
                //ViewCell viewCell;
                //if (dataTemplate is DataTemplateSelector selector)
                //{
                //    var template = selector.SelectTemplate((_view.ItemsSource as IList)[position], _view.Parent);
                //    viewCell = template.CreateContent() as ViewCell;
                //}
                //else
                //{
                //    viewCell = dataTemplate.CreateContent() as ViewCell;
                //}
                //viewCell.SendAppearing();

                //viewHolder._viewCell = viewCell;
                //viewHolder.UpdateUi(viewCell, dataContext, _view);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var contentFrame = new LinearLayout(parent.Context)
            {
                LayoutParameters =
                    new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.WrapContent)
            };
            return new RecyclerViewHolder(contentFrame);
        }
    }

    public class RecyclerViewHolder : RecyclerView.ViewHolder
    {

        public RecyclerViewHolder(View itemView) : base(itemView)
        {
            ItemView = itemView;
        }

        public Cell ViewCell { get; set; }


        public void UpdateUi(ViewCell viewCell, object dataContext, Xam.Controls.StaggeredGridView collectionView)
        { 
            var contentLayout = (LinearLayout)ItemView;
            ViewCell = viewCell;
            viewCell.BindingContext = dataContext;
            viewCell.Parent = collectionView;

            if (Platform.GetRenderer(viewCell.View) == null) {
                Platform.SetRenderer(viewCell.View, Platform.CreateRenderer(viewCell.View));
            }
            var renderer = Platform.GetRenderer(viewCell.View);
            var a = renderer.GetDesiredSize(int.MaxValue, int.MaxValue);
            renderer.UpdateLayout();
            var metrics = Resources.System.DisplayMetrics;
            // Layout and Measure Xamarin Forms View
            var elementSizeRequest = viewCell.View.Measure(100, double.MaxValue);
            
            var height = ContextHelper.Dp2Px(contentLayout.Context,
                (int) elementSizeRequest.Request.Height);
            var width = ContextHelper.Dp2Px(contentLayout.Context,
                (int) elementSizeRequest.Request.Width);

            viewCell.View.Layout(new Rectangle(0, 0, elementSizeRequest.Request.Width, elementSizeRequest.Request.Height));

            // Layout Android View
            var layoutParams = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent) {
                Height = height,
                Width = width
            };

            var viewGroup = renderer.View;
            viewGroup.LayoutParameters = layoutParams;
            viewGroup.Layout(0, 0, width, height);

            contentLayout.RemoveAllViews();
            contentLayout.AddView(renderer.View);
        }
    }

    public class ContextHelper
    {
        public static int Dp2Px(Context context, int dp)
        {
            return (int) TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, context.Resources.DisplayMetrics);
        }
        public static int Px2Dp(Context context, int px)
        {
            float scale = context.Resources.DisplayMetrics.Density;
            return (int) (px * scale + 0.5f);
        }
    }


    public class AutoStaggeredGridLayoutManager : StaggeredGridLayoutManager
    {
        public enum Strategy
        {
            MinSize = 0,
            SuitableSize = 1
        }

        private int _columnSize = -1;
        private bool _columnSizeChanged = true;

        private List<IOnUpdateSpanCountListener> _listeners;
        private Strategy _strategy;

        public AutoStaggeredGridLayoutManager(int columnSize, int orientation) : base(1, orientation)
        {
            SetColumnSize(columnSize);
        }

        public void SetColumnSize(int columnSize)
        {
            if (columnSize == _columnSize)
                return;
            _columnSize = columnSize;
            _columnSizeChanged = true;
        }

        public void SetStrategy(Strategy strategy)
        {
            if (strategy == _strategy)
                return;
            _strategy = strategy;
            _columnSizeChanged = true;
        }

        public static int GetSpanCountForSuitableSize(int total, int single)
        {
            var span = total / single;
            if (span <= 0)
                return 1;
            var span2 = span + 1;
            var deviation = Math.Abs(1 - total / span / (float) single);
            var deviation2 = Math.Abs(1 - total / span2 / (float) single);
            return deviation < deviation2 ? span : span2;
        }

        public static int GetSpanCountForMinSize(int total, int single)
        {
            return Math.Max(1, total / single);
        }

        public override void OnMeasure(RecyclerView.Recycler recycler, RecyclerView.State state, int widthSpec,
            int heightSpec)
        {
            if (_columnSizeChanged && _columnSize > 0)
            {
                int totalSpace;
                if (Orientation == Vertical)
                {
                    if (MeasureSpecMode.Exactly != View.MeasureSpec.GetMode(widthSpec))
                        throw new InvalidOperationException(
                            "RecyclerView need a fixed width for AutoStaggeredGridLayoutManager");
                    totalSpace = View.MeasureSpec.GetSize(widthSpec) - PaddingRight - PaddingLeft;
                }
                else
                {
                    if (MeasureSpecMode.Exactly != View.MeasureSpec.GetMode(heightSpec))
                        throw new InvalidOperationException(
                            "RecyclerView need a fixed height for AutoStaggeredGridLayoutManager");
                    totalSpace = View.MeasureSpec.GetSize(heightSpec) - PaddingTop - PaddingBottom;
                }

                int spanCount;
                switch (_strategy)
                {
                    case Strategy.MinSize:
                        spanCount = GetSpanCountForMinSize(totalSpace, _columnSize);
                        break;
                    case Strategy.SuitableSize:
                        spanCount = GetSpanCountForSuitableSize(totalSpace, _columnSize);
                        break;
                    default:
                        throw new Exception();
                }
                SpanCount = spanCount;
                _columnSizeChanged = false;

                if (null != _listeners)
                    for (int i = 0, n = _listeners.Count; i < n; i++)
                        _listeners[i].OnUpdateSpanCount(spanCount);
            }
            base.OnMeasure(recycler, state, widthSpec, heightSpec);
        }

        public void AddOnUpdateSpanCountListener(IOnUpdateSpanCountListener listener)
        {
            if (null == _listeners)
                _listeners = new List<IOnUpdateSpanCountListener>();
            _listeners.Add(listener);
        }

        public void RemoveOnUpdateSpanCountListener(IOnUpdateSpanCountListener listener)
        {
            _listeners?.Remove(listener);
        }

        public interface IOnUpdateSpanCountListener
        {
            void OnUpdateSpanCount(int spanCount);
        }
    }
}