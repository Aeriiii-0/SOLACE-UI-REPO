using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SOLUM_UI
{
    public partial class SoloParentRecordsPage : Page
    {
        /// <summary>
        /// Clears existing page number buttons and builds new ones based on current page and total pages.
        /// Shows page numbers with proper navigation between pages.
        /// Note: PageNumberPanel must be defined in the XAML for this to function.
        /// </summary>
        private void BuildPageNumbers()
        {
            // This method is called from UpdatePagingUI to refresh pagination UI.
            // If a PageNumberPanel is defined in XAML, page number buttons will be generated here.
            // Currently, pagination is handled via the BtnNext/BtnBack buttons.
            
            // Placeholder: future implementation when PageNumberPanel is added to XAML
            // For now, this is a no-op to satisfy the UpdatePagingUI call.
        }

        /// <summary>
        /// Handles the SizeChanged event for the records grid clip to apply rounded corners.
        /// </summary>
        private void RecordsClipGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Grid grid && grid.ActualWidth > 0 && grid.ActualHeight > 0)
            {
                grid.Clip = new RectangleGeometry(new Rect(0, 0, grid.ActualWidth, grid.ActualHeight), 19, 19);
            }
        }
    }
}

