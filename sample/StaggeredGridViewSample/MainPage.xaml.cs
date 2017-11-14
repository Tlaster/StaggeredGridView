using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace StaggeredGridViewSample
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            staggeredGridView.ItemsSource = new List<string>
            {
                "dsdsaf",
                "dsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsgfdsgfdsgfdsgfdsgfdgfddsafdsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
                "dsdsaf",
            };
        }
    }
}