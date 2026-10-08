using System;
using System.Collections.Generic;
using System.Windows.Controls;
using SOLUM_UI.Models;

namespace SOLUM_UI
{
    public partial class ProfessionalDashboardPage : Page
    {
        private DashboardChartData _dashboardData;

        public ProfessionalDashboardPage()
        {
            InitializeComponent();
            InitializeData();
            LoadDashboard();
        }

        private void InitializeData()
        {
            // Initialize the dashboard data model with default values from image_1.png
            _dashboardData = new DashboardChartData
            {
                AgeBelow19 = 0,
                Age20To39 = 118,
                Age40To59 = 287,
                Age60Plus = 20,
                CivilStatusSingle = 175,
                CivilStatusMarried = 105,
                CivilStatusWidowed = 137,
                CivilStatusSeparatedAnnulled = 8,
                ChildrenBelow6 = 78,
                Children7To22 = 630,
                ChildrenAbove22 = 71,
                SexFemale = 380,
                SexMale = 45
            };
        }

        private void LoadDashboard()
        {
            // Bind data to UI elements
            // This method will be expanded when charts are implemented
            // For now, the static values in XAML serve as placeholders
        }
    }
}
