using System;
using System.Collections.Generic;
using System.Text;
using StaggeredGridView.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(Xam.Controls.StaggeredGridView), typeof(StaggeredGridViewRenderer))]

namespace StaggeredGridView.iOS
{
    public class StaggeredGridViewRenderer : ViewRenderer<Xam.Controls.StaggeredGridView, UIView>
    {
    }
}
