using System.Collections.Generic;

//Helper class to pass objects and data into the view.
namespace Amazon_Product_Surfer.Models
{
    public class ViewModel
    {
        public List<ItemFromAmazon> ItemListForView { get; set; }
        public IEnumerable<string> Currencies { get; set; }
        public string searchTerm = "";
        public int page=-1;
        public int maxPages=-1;
        public int islast=0; 

        public ViewModel(List<ItemFromAmazon> List)
        {
            ItemListForView = List;
        }
    }
}