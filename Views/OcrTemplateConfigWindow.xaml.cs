using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SOLUM_UI.Views.Dialogs;
using SOLUM_UI.Models;

namespace SOLUM_UI
{
    public class TemplateFieldItem
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double W { get; set; }
        public double H { get; set; }
    }

    public enum ResizeHandleType
    {
        Move,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Top,
        Bottom,
        Left,
        Right
    }

    public class HandleTagInfo
    {
        public TemplateFieldItem Field { get; set; }
        public ResizeHandleType HandleType { get; set; }
    }

    public partial class OcrTemplateConfigWindow : Window
    {
        private readonly string _imagePath;
        private readonly List<TemplateFieldItem> _fields = new List<TemplateFieldItem>();
        private readonly Stack<string> _undoStack = new Stack<string>();
        private readonly Stack<string> _redoStack = new Stack<string>();
        private TemplateFieldItem _selectedField;
        private readonly HashSet<TemplateFieldItem> _selectedFields = new HashSet<TemplateFieldItem>();
        private class BoxCoordSnapshot { public double X; public double Y; public double W; public double H; }
        private readonly Dictionary<TemplateFieldItem, BoxCoordSnapshot> _origCoords = new Dictionary<TemplateFieldItem, BoxCoordSnapshot>();
        private ResizeHandleType _activeHandle = ResizeHandleType.Move;
        private double _currentZoom = 1.0;
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private List<OcrWordToken> _activeRoiTokens = new List<OcrWordToken>();

        // Canvas Panning State
        private bool _isPanning = false;
        private Point _panStartMousePoint;
        private double _panStartH;
        private double _panStartV;

        public bool TemplateSaved { get; private set; } = false;
        private readonly string _initialFieldKey;

        public OcrTemplateConfigWindow(string imagePath = null, string initialFieldKey = null)
        {
            InitializeComponent();
            _imagePath = imagePath;
            _initialFieldKey = initialFieldKey;
            Loaded += OcrTemplateConfigWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            double workW = SystemParameters.WorkArea.Width;
            double workH = SystemParameters.WorkArea.Height;
            Width = Math.Min(1340, workW - 30);
            Height = Math.Min(840, workH - 30);
            Left = SystemParameters.WorkArea.Left + (workW - Width) / 2;
            Top = SystemParameters.WorkArea.Top + (workH - Height) / 2;
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void OcrTemplateConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTemplateConfig();
            LoadDocumentImage();
            PopulateFieldCombo();
            RedrawAllCanvasBoxes();
            UpdateUndoRedoButtons();
        }

        private void LoadTemplateConfig()
        {
            _fields.Clear();
            string localPath = SOLUM_UI.Services.OcrService.GetLocalTemplatePath();
            if (File.Exists(localPath))
            {
                try
                {
                    string json = File.ReadAllText(localPath);
                    ParseTemplateJson(json);
                    return;
                }
                catch { }
            }

            // Fallback default coordinates if file is unavailable
            LoadFactoryDefaults();
        }

        private void ParseTemplateJson(string json)
        {
            _fields.Clear();
            try
            {
                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                var root = serializer.Deserialize<Dictionary<string, object>>(json);
                if (root != null && root.TryGetValue("fields", out var fieldsObj) && fieldsObj is Dictionary<string, object> fields)
                {
                    foreach (var kvp in fields)
                    {
                        string key = kvp.Key;
                        if (kvp.Value is Dictionary<string, object> fieldDict)
                        {
                            string name = fieldDict.TryGetValue("name", out var nObj) ? nObj?.ToString() : key;
                            string cat = fieldDict.TryGetValue("category", out var cObj) ? cObj?.ToString() : "general";
                            if (fieldDict.TryGetValue("bbox", out var bboxObj) && bboxObj is System.Collections.IEnumerable list)
                            {
                                var nums = list.Cast<object>().Select(v => Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                                if (nums.Length >= 4)
                                {
                                    _fields.Add(new TemplateFieldItem { Key = key, Name = name, Category = cat, X = nums[0], Y = nums[1], W = nums[2], H = nums[3] });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[OcrTemplateConfigWindow] ParseTemplateJson error: " + ex.Message);
            }

            if (_fields.Count == 0) LoadFactoryDefaults();
        }

        private void LoadFactoryDefaults()
        {
            _fields.Clear();
            _fields.Add(new TemplateFieldItem { Key = "application_date", Name = "Date of Application", Category = "Header", X = 0.720, Y = 0.108, W = 0.250, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "last_name", Name = "Last Name / Surname", Category = "Section I", X = 0.080, Y = 0.185, W = 0.165, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "first_name", Name = "First Name", Category = "Section I", X = 0.320, Y = 0.185, W = 0.165, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "middle_name", Name = "Middle Name", Category = "Section I", X = 0.520, Y = 0.185, W = 0.165, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "ext_name", Name = "Extension Name", Category = "Section I", X = 0.685, Y = 0.185, W = 0.095, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "civil_status", Name = "Civil Status", Category = "Section I", X = 0.785, Y = 0.185, W = 0.095, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "sex", Name = "Sex", Category = "Section I", X = 0.880, Y = 0.185, W = 0.095, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "birthdate", Name = "Date of Birth", Category = "Section I", X = 0.075, Y = 0.209, W = 0.135, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "birthplace", Name = "Place of Birth", Category = "Section I", X = 0.260, Y = 0.209, W = 0.160, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "age", Name = "Age", Category = "Section I", X = 0.435, Y = 0.209, W = 0.075, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "educational_attainment", Name = "Educational Attainment", Category = "Section I", X = 0.535, Y = 0.209, W = 0.215, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "philsys_number", Name = "PhilSys Card Number", Category = "Section I", X = 0.755, Y = 0.209, W = 0.220, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "religion", Name = "Religion", Category = "Section I", X = 0.070, Y = 0.235, W = 0.140, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "occupation", Name = "Occupation / Source of Income", Category = "Section I", X = 0.260, Y = 0.235, W = 0.200, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "monthly_income", Name = "Monthly Income", Category = "Section I", X = 0.480, Y = 0.235, W = 0.125, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "is_employed", Name = "Checkbox: Employed", Category = "Section I", X = 0.6167, Y = 0.2347, W = 0.0084, H = 0.0056 });
            _fields.Add(new TemplateFieldItem { Key = "is_self_employed", Name = "Checkbox: Self-Employed", Category = "Section I", X = 0.7056, Y = 0.2347, W = 0.0084, H = 0.0056 });
            _fields.Add(new TemplateFieldItem { Key = "is_not_employed", Name = "Checkbox: Not Employed", Category = "Section I", X = 0.8256, Y = 0.2347, W = 0.0084, H = 0.0056 });
            _fields.Add(new TemplateFieldItem { Key = "address", Name = "Address (House/Street)", Category = "Section I", X = 0.030, Y = 0.260, W = 0.395, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "barangay", Name = "Barangay", Category = "Section I", X = 0.480, Y = 0.254, W = 0.205, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "contact_number", Name = "Contact Number", Category = "Section I", X = 0.760, Y = 0.260, W = 0.215, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "emergency_name", Name = "Emergency Contact Person", Category = "Section I", X = 0.020, Y = 0.286, W = 0.315, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "emergency_relationship", Name = "Emergency Relationship", Category = "Section I", X = 0.335, Y = 0.286, W = 0.175, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "emergency_address", Name = "Emergency Contact Address", Category = "Section I", X = 0.510, Y = 0.286, W = 0.255, H = 0.016 });
            _fields.Add(new TemplateFieldItem { Key = "emergency_number", Name = "Emergency Contact Number", Category = "Section I", X = 0.770, Y = 0.286, W = 0.205, H = 0.016 });

            // Section II: Circumstances
            _fields.Add(new TemplateFieldItem { Key = "circ_a1", Name = "Circumstance A1 (Rape)", Category = "Section II", X = 0.022, Y = 0.320, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a2", Name = "Circumstance A2 (Death of Spouse)", Category = "Section II", X = 0.022, Y = 0.344, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a2_cause", Name = "A2 Subfield: Cause of Death", Category = "Section II", X = 0.170, Y = 0.350, W = 0.085, H = 0.020 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a2_date", Name = "A2 Subfield: Date of Death", Category = "Section II", X = 0.290, Y = 0.350, W = 0.075, H = 0.020 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a3", Name = "Circumstance A3 (Detention)", Category = "Section II", X = 0.022, Y = 0.370, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a4", Name = "Circumstance A4 (Incapacity)", Category = "Section II", X = 0.022, Y = 0.395, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a4_disability", Name = "A4 Subfield: Type of Disability", Category = "Section II", X = 0.180, Y = 0.400, W = 0.180, H = 0.020 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a5", Name = "Circumstance A5 (Separation)", Category = "Section II", X = 0.370, Y = 0.320, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a5_period", Name = "A5 Subfield: Period of Separation", Category = "Section II", X = 0.550, Y = 0.326, W = 0.150, H = 0.020 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a6", Name = "Circumstance A6 (Nullity / Annulment)", Category = "Section II", X = 0.370, Y = 0.344, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a6_nullity", Name = "A6 Sub-Checkbox: Declaration of Nullity", Category = "Section II", X = 0.420, Y = 0.352, W = 0.035, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a6_annulment", Name = "A6 Sub-Checkbox: Annulment", Category = "Section II", X = 0.520, Y = 0.352, W = 0.035, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a7", Name = "Circumstance A7 (Abandonment)", Category = "Section II", X = 0.370, Y = 0.370, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_b", Name = "Circumstance B (OFW Spouse)", Category = "Section II", X = 0.370, Y = 0.395, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_b_stay", Name = "B Subfield: Stay Abroad", Category = "Section II", X = 0.560, Y = 0.400, W = 0.145, H = 0.020 });
            _fields.Add(new TemplateFieldItem { Key = "circ_c", Name = "Circumstance C (Unmarried Parent)", Category = "Section II", X = 0.700, Y = 0.320, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_d", Name = "Circumstance D (Legal Guardian)", Category = "Section II", X = 0.700, Y = 0.344, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_e", Name = "Circumstance E (Relative 4th Degree)", Category = "Section II", X = 0.700, Y = 0.370, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_f", Name = "Circumstance F (Pregnant Woman)", Category = "Section II", X = 0.700, Y = 0.395, W = 0.045, H = 0.022 });

            // Section III: Family Composition Row 1
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_name", Name = "Family Row 1: Name", Category = "Section III", X = 0.025, Y = 0.462, W = 0.255, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_sex", Name = "Family Row 1: Sex", Category = "Section III", X = 0.285, Y = 0.462, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_age", Name = "Family Row 1: Age", Category = "Section III", X = 0.340, Y = 0.462, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_dob", Name = "Family Row 1: Birthdate", Category = "Section III", X = 0.395, Y = 0.462, W = 0.115, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_civ", Name = "Family Row 1: Civil Status", Category = "Section III", X = 0.510, Y = 0.462, W = 0.100, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_rel", Name = "Family Row 1: Relationship", Category = "Section III", X = 0.615, Y = 0.462, W = 0.120, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_edu", Name = "Family Row 1: Education / Job", Category = "Section III", X = 0.740, Y = 0.462, W = 0.145, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_inc", Name = "Family Row 1: Income", Category = "Section III", X = 0.885, Y = 0.462, W = 0.095, H = 0.015 });

            // Section III: Family Composition Row 2
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_name", Name = "Family Row 2: Name", Category = "Section III", X = 0.025, Y = 0.476, W = 0.255, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_sex", Name = "Family Row 2: Sex", Category = "Section III", X = 0.285, Y = 0.476, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_age", Name = "Family Row 2: Age", Category = "Section III", X = 0.340, Y = 0.476, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_dob", Name = "Family Row 2: Birthdate", Category = "Section III", X = 0.395, Y = 0.476, W = 0.115, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_civ", Name = "Family Row 2: Civil Status", Category = "Section III", X = 0.510, Y = 0.476, W = 0.100, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_rel", Name = "Family Row 2: Relationship", Category = "Section III", X = 0.615, Y = 0.476, W = 0.120, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_edu", Name = "Family Row 2: Education / Job", Category = "Section III", X = 0.740, Y = 0.476, W = 0.145, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_inc", Name = "Family Row 2: Income", Category = "Section III", X = 0.885, Y = 0.476, W = 0.095, H = 0.015 });

            // Section III: Family Composition Row 3
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_name", Name = "Family Row 3: Name", Category = "Section III", X = 0.025, Y = 0.491, W = 0.255, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_sex", Name = "Family Row 3: Sex", Category = "Section III", X = 0.285, Y = 0.491, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_age", Name = "Family Row 3: Age", Category = "Section III", X = 0.340, Y = 0.491, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_dob", Name = "Family Row 3: Birthdate", Category = "Section III", X = 0.395, Y = 0.491, W = 0.115, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_civ", Name = "Family Row 3: Civil Status", Category = "Section III", X = 0.510, Y = 0.491, W = 0.100, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_rel", Name = "Family Row 3: Relationship", Category = "Section III", X = 0.615, Y = 0.491, W = 0.120, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_edu", Name = "Family Row 3: Education / Job", Category = "Section III", X = 0.740, Y = 0.491, W = 0.145, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_inc", Name = "Family Row 3: Income", Category = "Section III", X = 0.885, Y = 0.491, W = 0.095, H = 0.015 });

            // Section III: Family Composition Row 4
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_name", Name = "Family Row 4: Name", Category = "Section III", X = 0.025, Y = 0.505, W = 0.255, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_sex", Name = "Family Row 4: Sex", Category = "Section III", X = 0.285, Y = 0.505, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_age", Name = "Family Row 4: Age", Category = "Section III", X = 0.340, Y = 0.505, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_dob", Name = "Family Row 4: Birthdate", Category = "Section III", X = 0.395, Y = 0.505, W = 0.115, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_civ", Name = "Family Row 4: Civil Status", Category = "Section III", X = 0.510, Y = 0.505, W = 0.100, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_rel", Name = "Family Row 4: Relationship", Category = "Section III", X = 0.615, Y = 0.505, W = 0.120, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_edu", Name = "Family Row 4: Education / Job", Category = "Section III", X = 0.740, Y = 0.505, W = 0.145, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_inc", Name = "Family Row 4: Income", Category = "Section III", X = 0.885, Y = 0.505, W = 0.095, H = 0.015 });

            // Section III: Family Composition Row 5
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_name", Name = "Family Row 5: Name", Category = "Section III", X = 0.025, Y = 0.520, W = 0.255, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_sex", Name = "Family Row 5: Sex", Category = "Section III", X = 0.285, Y = 0.520, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_age", Name = "Family Row 5: Age", Category = "Section III", X = 0.340, Y = 0.520, W = 0.055, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_dob", Name = "Family Row 5: Birthdate", Category = "Section III", X = 0.395, Y = 0.520, W = 0.115, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_civ", Name = "Family Row 5: Civil Status", Category = "Section III", X = 0.510, Y = 0.520, W = 0.100, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_rel", Name = "Family Row 5: Relationship", Category = "Section III", X = 0.615, Y = 0.520, W = 0.120, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_edu", Name = "Family Row 5: Education / Job", Category = "Section III", X = 0.740, Y = 0.520, W = 0.145, H = 0.015 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_inc", Name = "Family Row 5: Income", Category = "Section III", X = 0.885, Y = 0.520, W = 0.095, H = 0.015 });

            // Section IV: Needs and Problems
            _fields.Add(new TemplateFieldItem { Key = "needs", Name = "IV. Needs and Problems", Category = "Section IV", X = 0.025, Y = 0.555, W = 0.940, H = 0.025 });

            // Section V: Other Sources of Income
            _fields.Add(new TemplateFieldItem { Key = "other_income", Name = "V. Other Sources of Income", Category = "Section V", X = 0.025, Y = 0.605, W = 0.940, H = 0.025 });
        }

        private void LoadDocumentImage()
        {
            string imgToLoad = _imagePath;
            if (string.IsNullOrEmpty(imgToLoad) || !File.Exists(imgToLoad))
            {
                string tempDeskewed = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "solace_last_deskewed_preview.jpg");
                if (File.Exists(tempDeskewed))
                {
                    imgToLoad = tempDeskewed;
                }
                else
                {
                    string ocrDir = SOLUM_UI.Services.OcrService.FindOcrDirectory();
                    string samplePath = System.IO.Path.Combine(ocrDir, "sample_form.jpg");
                    if (File.Exists(samplePath))
                    {
                        imgToLoad = samplePath;
                    }
                }
            }

            if (!string.IsNullOrEmpty(imgToLoad) && File.Exists(imgToLoad))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(imgToLoad, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();

                    ImgDocument.Source = bmp;
                    OverlayCanvas.Width = bmp.PixelWidth;
                    OverlayCanvas.Height = bmp.PixelHeight;
                    CanvasContainer.Width = bmp.PixelWidth;
                    CanvasContainer.Height = bmp.PixelHeight;

                    Dispatcher.BeginInvoke(new Action(ZoomFit), System.Windows.Threading.DispatcherPriority.Loaded);
                    return;
                }
                catch { }
            }

            // Fallback placeholder dimension if image isn't loaded
            OverlayCanvas.Width = 1000;
            OverlayCanvas.Height = 1350;
            CanvasContainer.Width = 1000;
            CanvasContainer.Height = 1350;
        }

        private void PushUndoState()
        {
            _undoStack.Push(BuildTemplateJson());
            _redoStack.Clear();
            UpdateUndoRedoButtons();
        }

        private void UpdateUndoRedoButtons()
        {
            BtnUndo.IsEnabled = (_undoStack.Count > 0);
            BtnRedo.IsEnabled = (_redoStack.Count > 0);
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(BuildTemplateJson());
                string prevJson = _undoStack.Pop();
                _fields.Clear();
                ParseTemplateJson(prevJson);
                PopulateFieldCombo();
                _selectedFields.Clear();
                if (_selectedField != null) _selectedFields.Add(_selectedField);
                RedrawAllCanvasBoxes();
                UpdateUndoRedoButtons();
                TxtStatusMessage.Text = "↩ Undid last change.";
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(BuildTemplateJson());
                string nextJson = _redoStack.Pop();
                _fields.Clear();
                ParseTemplateJson(nextJson);
                PopulateFieldCombo();
                _selectedFields.Clear();
                if (_selectedField != null) _selectedFields.Add(_selectedField);
                RedrawAllCanvasBoxes();
                UpdateUndoRedoButtons();
                TxtStatusMessage.Text = "↪ Redid change.";
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Avoid intercepting arrow keys if typing in a textbox
            if (e.OriginalSource is TextBox) return;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    Undo_Click(this, null);
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Y)
                {
                    Redo_Click(this, null);
                    e.Handled = true;
                    return;
                }
            }

            if (_selectedFields.Count > 0 || _selectedField != null)
            {
                double step = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 0.005 : 0.001;

                if (e.Key == Key.Up)
                {
                    ApplyNudge(0, -step);
                    e.Handled = true;
                }
                else if (e.Key == Key.Down)
                {
                    ApplyNudge(0, step);
                    e.Handled = true;
                }
                else if (e.Key == Key.Left)
                {
                    ApplyNudge(-step, 0);
                    e.Handled = true;
                }
                else if (e.Key == Key.Right)
                {
                    ApplyNudge(step, 0);
                    e.Handled = true;
                }
            }
        }

        private void Nudge_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string dir)
            {
                double step = (RbStepNormal?.IsChecked == true) ? 0.005 : 0.001;

                double dx = 0;
                double dy = 0;
                switch (dir)
                {
                    case "Up":    dy = -step; break;
                    case "Down":  dy = +step; break;
                    case "Left":  dx = -step; break;
                    case "Right": dx = +step; break;
                }

                ApplyNudge(dx, dy);
            }
        }

        private void ApplyNudge(double dx, double dy)
        {
            if (_selectedFields.Count == 0 && _selectedField != null) _selectedFields.Add(_selectedField);
            if (_selectedFields.Count == 0) return;

            PushUndoState();

            foreach (var sf in _selectedFields)
            {
                sf.X = Math.Max(0.0, Math.Min(0.98, Math.Round(sf.X + dx, 4)));
                sf.Y = Math.Max(0.0, Math.Min(0.98, Math.Round(sf.Y + dy, 4)));
            }

            if (_selectedField != null) UpdateSidebarFromField(_selectedField);
            RedrawAllCanvasBoxes();
            TxtStatusMessage.Text = $"🎯 Nudged {_selectedFields.Count} box{(_selectedFields.Count > 1 ? "es" : "")}.";
        }

        private void ExpandBox_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFields.Count == 0 && _selectedField != null) _selectedFields.Add(_selectedField);
            if (_selectedFields.Count == 0) return;

            PushUndoState();
            double step = (RbStepNormal?.IsChecked == true) ? 0.004 : 0.001;

            foreach (var sf in _selectedFields)
            {
                sf.X = Math.Max(0.0, Math.Round(sf.X - step / 2.0, 4));
                sf.Y = Math.Max(0.0, Math.Round(sf.Y - step / 2.0, 4));
                sf.W = Math.Min(1.0 - sf.X, Math.Round(sf.W + step, 4));
                sf.H = Math.Min(1.0 - sf.Y, Math.Round(sf.H + step, 4));
            }

            if (_selectedField != null) UpdateSidebarFromField(_selectedField);
            RedrawAllCanvasBoxes();
            TxtStatusMessage.Text = $"➕ Expanded {_selectedFields.Count} box{(_selectedFields.Count > 1 ? "es" : "")}.";
        }

        private void ShrinkBox_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFields.Count == 0 && _selectedField != null) _selectedFields.Add(_selectedField);
            if (_selectedFields.Count == 0) return;

            PushUndoState();
            double step = (RbStepNormal?.IsChecked == true) ? 0.004 : 0.001;

            foreach (var sf in _selectedFields)
            {
                if (sf.W <= 0.004 || sf.H <= 0.004) continue;
                sf.X = Math.Min(0.98, Math.Round(sf.X + step / 2.0, 4));
                sf.Y = Math.Min(0.98, Math.Round(sf.Y + step / 2.0, 4));
                sf.W = Math.Max(0.002, Math.Round(sf.W - step, 4));
                sf.H = Math.Max(0.002, Math.Round(sf.H - step, 4));
            }

            if (_selectedField != null) UpdateSidebarFromField(_selectedField);
            RedrawAllCanvasBoxes();
            TxtStatusMessage.Text = $"➖ Shrunk {_selectedFields.Count} box{(_selectedFields.Count > 1 ? "es" : "")}.";
        }

        private void PopulateFieldCombo()
        {
            CmbFields.ItemsSource = null;
            CmbFields.ItemsSource = _fields;
            if (_fields.Count > 0)
            {
                if (!string.IsNullOrEmpty(_initialFieldKey))
                {
                    var match = _fields.FirstOrDefault(f => string.Equals(f.Key, _initialFieldKey, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        _selectedField = match;
                        _selectedFields.Clear();
                        _selectedFields.Add(match);
                        CmbFields.SelectedItem = match;
                        ScrollToField(match);
                        return;
                    }
                }
                _selectedField = _fields[0];
                _selectedFields.Clear();
                _selectedFields.Add(_selectedField);
                CmbFields.SelectedIndex = 0;
            }
        }

        private void ScrollToField(TemplateFieldItem f)
        {
            if (f == null || CanvasScrollViewer == null) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                double cW = OverlayCanvas.Width;
                double cH = OverlayCanvas.Height;
                if (cW <= 0 || cH <= 0) return;

                double targetCenterX = (f.X + f.W / 2.0) * cW * _currentZoom;
                double targetCenterY = (f.Y + f.H / 2.0) * cH * _currentZoom;

                double scrollX = targetCenterX - (CanvasScrollViewer.ViewportWidth / 2.0);
                double scrollY = targetCenterY - (CanvasScrollViewer.ViewportHeight / 2.0);

                CanvasScrollViewer.ScrollToHorizontalOffset(Math.Max(0, scrollX));
                CanvasScrollViewer.ScrollToVerticalOffset(Math.Max(0, scrollY));
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void RedrawAllCanvasBoxes()
        {
            OverlayCanvas.Children.Clear();
            double cW = OverlayCanvas.Width;
            double cH = OverlayCanvas.Height;

            if (cW <= 0 || cH <= 0) return;

            // Compute zoom-invariant screen dimensions so handles & labels remain compact and consistent at any zoom level
            double z = Math.Max(0.05, _currentZoom);
            double strokeSelected = Math.Max(1.5, 2.5 / z);
            double strokeUnselected = Math.Max(0.8, 1.2 / z);
            double fontSize = Math.Max(8.0, 11.0 / z);
            double labelPadH = Math.Max(3.0, 6.0 / z);
            double labelPadV = Math.Max(1.0, 2.5 / z);
            double labelCorner = Math.Max(2.0, 3.0 / z);
            double labelOffset = Math.Max(14.0, 18.0 / z);
            
            // Compact 10px screen handle size
            double hSize = 10.0 / z;
            double halfH = hSize / 2.0;
            double handleStroke = Math.Max(1.0, 1.5 / z);

            foreach (var f in _fields)
            {
                bool isSelected = _selectedFields.Contains(f) || (_selectedField != null && _selectedField.Key == f.Key);

                double left = f.X * cW;
                double top = f.Y * cH;
                double width = f.W * cW;
                double height = f.H * cH;

                // Rectangle border for field
                var rect = new Rectangle
                {
                    Width = Math.Max(4 / z, width),
                    Height = Math.Max(4 / z, height),
                    Stroke = isSelected ? new SolidColorBrush(Color.FromRgb(233, 30, 99)) : new SolidColorBrush(Color.FromArgb(170, 33, 150, 243)),
                    StrokeThickness = isSelected ? strokeSelected : strokeUnselected,
                    Fill = isSelected ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(12, 33, 150, 243)),
                    Tag = new HandleTagInfo { Field = f, HandleType = ResizeHandleType.Move },
                    Cursor = Cursors.SizeAll
                };

                Canvas.SetLeft(rect, left);
                Canvas.SetTop(rect, top);
                OverlayCanvas.Children.Add(rect);

                // Show Field Name Title ONLY for the selected field to avoid blocking the view of other fields
                if (isSelected)
                {
                    var labelBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(233, 30, 99)),
                        CornerRadius = new CornerRadius(labelCorner),
                        Padding = new Thickness(labelPadH, labelPadV, labelPadH, labelPadV),
                        IsHitTestVisible = false
                    };

                    var lblText = new TextBlock
                    {
                        Text = f.Name,
                        FontSize = fontSize,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White
                    };
                    labelBorder.Child = lblText;

                    Canvas.SetLeft(labelBorder, left);
                    Canvas.SetTop(labelBorder, Math.Max(0, top - labelOffset));
                    OverlayCanvas.Children.Add(labelBorder);

                    // Render 8 compact corner and edge resize handles
                    var handleList = new[]
                    {
                        new { Type = ResizeHandleType.TopLeft,     Cx = left,            Cy = top,             Cursor = Cursors.SizeNWSE },
                        new { Type = ResizeHandleType.TopRight,    Cx = left + width,    Cy = top,             Cursor = Cursors.SizeNESW },
                        new { Type = ResizeHandleType.BottomLeft,  Cx = left,            Cy = top + height,    Cursor = Cursors.SizeNESW },
                        new { Type = ResizeHandleType.BottomRight, Cx = left + width,    Cy = top + height,    Cursor = Cursors.SizeNWSE },
                        new { Type = ResizeHandleType.Top,         Cx = left + width/2,  Cy = top,             Cursor = Cursors.SizeNS },
                        new { Type = ResizeHandleType.Bottom,      Cx = left + width/2,  Cy = top + height,    Cursor = Cursors.SizeNS },
                        new { Type = ResizeHandleType.Left,        Cx = left,            Cy = top + height/2,  Cursor = Cursors.SizeWE },
                        new { Type = ResizeHandleType.Right,       Cx = left + width,    Cy = top + height/2,  Cursor = Cursors.SizeWE }
                    };

                    foreach (var h in handleList)
                    {
                        var handleRect = new Rectangle
                        {
                            Width = hSize,
                            Height = hSize,
                            Fill = Brushes.White,
                            Stroke = new SolidColorBrush(Color.FromRgb(233, 30, 99)),
                            StrokeThickness = handleStroke,
                            Cursor = h.Cursor,
                            Tag = new HandleTagInfo { Field = f, HandleType = h.Type }
                        };

                        Canvas.SetLeft(handleRect, h.Cx - halfH);
                        Canvas.SetTop(handleRect, h.Cy - halfH);
                        OverlayCanvas.Children.Add(handleRect);
                    }
                }
            }

            // Draw any detected AI words/tokens inside the tested box
            if (_activeRoiTokens != null && _activeRoiTokens.Count > 0)
            {
                foreach (var tok in _activeRoiTokens)
                {
                    if (tok.Bbox == null || tok.Bbox.Length < 4) continue;
                    double tX = tok.Bbox[0] * cW;
                    double tY = tok.Bbox[1] * cH;
                    double tW = tok.Bbox[2] * cW;
                    double tH = tok.Bbox[3] * cH;

                    var tokenRect = new Rectangle
                    {
                        Width = Math.Max(4, tW),
                        Height = Math.Max(4, tH),
                        Stroke = tok.IsLabel ? new SolidColorBrush(Color.FromRgb(245, 124, 0)) : new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                        StrokeThickness = 1.5 / z,
                        StrokeDashArray = tok.IsLabel ? new DoubleCollection { 3, 2 } : null,
                        Fill = tok.IsLabel ? new SolidColorBrush(Color.FromArgb(45, 255, 152, 0)) : new SolidColorBrush(Color.FromArgb(40, 76, 175, 80)),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(tokenRect, tX);
                    Canvas.SetTop(tokenRect, tY);
                    OverlayCanvas.Children.Add(tokenRect);

                    var tText = new TextBlock
                    {
                        Text = tok.IsLabel ? $"[LBL] {tok.Text}" : $"{tok.Text} ({tok.Confidence:F2})",
                        FontSize = Math.Max(8.0, 10.0 / z),
                        Foreground = tok.IsLabel ? new SolidColorBrush(Color.FromRgb(230, 81, 0)) : new SolidColorBrush(Color.FromRgb(27, 94, 32)),
                        FontWeight = FontWeights.Bold,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(tText, tX);
                    Canvas.SetTop(tText, Math.Max(0, tY - (12.0 / z)));
                    OverlayCanvas.Children.Add(tText);
                }
            }
        }

        private void CmbFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFields.SelectedItem is TemplateFieldItem item)
            {
                _selectedField = item;
                _selectedFields.Clear();
                _selectedFields.Add(item);
                _activeRoiTokens.Clear();
                UpdateSidebarFromField(item);
                RedrawAllCanvasBoxes();
                // Do not auto-scroll or pan to center the box; preserve current pan/zoom position
            }
        }

        private void UpdateSidebarFromField(TemplateFieldItem f)
        {
            if (f == null) return;
            if (_selectedFields.Count > 1)
            {
                TxtFieldKey.Text = $"{_selectedFields.Count} boxes selected";
                TxtFieldCategory.Text = "Multi-Select (Shift)";
            }
            else
            {
                TxtFieldKey.Text = f.Key;
                TxtFieldCategory.Text = f.Category;
            }
            TxtValX.Text = f.X.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            TxtValY.Text = f.Y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            TxtValW.Text = f.W.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            TxtValH.Text = f.H.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = true;
                _panStartMousePoint = e.GetPosition(CanvasScrollViewer);
                _panStartH = CanvasScrollViewer.HorizontalOffset;
                _panStartV = CanvasScrollViewer.VerticalOffset;
                OverlayCanvas.CaptureMouse();
                OverlayCanvas.Cursor = Cursors.Hand;
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                var hit = e.OriginalSource as FrameworkElement;
                TemplateFieldItem fieldItem = null;
                ResizeHandleType handleType = ResizeHandleType.Move;

                if (hit != null)
                {
                    if (hit.Tag is HandleTagInfo hInfo)
                    {
                        fieldItem = hInfo.Field;
                        handleType = hInfo.HandleType;
                    }
                    else if (hit.Tag is TemplateFieldItem f)
                    {
                        fieldItem = f;
                        handleType = ResizeHandleType.Move;
                    }
                }

                if (fieldItem != null)
                {
                    PushUndoState();
                    bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                    if (isShift)
                    {
                        // Shift+Click: Toggle field in multi-selection
                        if (_selectedFields.Contains(fieldItem))
                        {
                            if (_selectedFields.Count > 1)
                            {
                                _selectedFields.Remove(fieldItem);
                                _selectedField = _selectedFields.FirstOrDefault();
                            }
                        }
                        else
                        {
                            _selectedFields.Add(fieldItem);
                            _selectedField = fieldItem;
                        }
                    }
                    else
                    {
                        // Standard Click: If clicking an unselected box, select only this box.
                        // If clicking an already selected box, retain multi-selection for simultaneous dragging/resizing.
                        if (!_selectedFields.Contains(fieldItem))
                        {
                            _selectedFields.Clear();
                            _selectedFields.Add(fieldItem);
                            _selectedField = fieldItem;
                        }
                        else
                        {
                            _selectedField = fieldItem;
                        }
                    }

                    _activeHandle = handleType;
                    CmbFields.SelectedItem = _selectedField;

                    // Snapshot starting coordinates for all selected fields for simultaneous resizing/dragging
                    _origCoords.Clear();
                    foreach (var sf in _selectedFields)
                    {
                        _origCoords[sf] = new BoxCoordSnapshot { X = sf.X, Y = sf.Y, W = sf.W, H = sf.H };
                    }

                    _isDragging = true;
                    _dragStartPoint = e.GetPosition(OverlayCanvas);
                    OverlayCanvas.CaptureMouse();
                    e.Handled = true;

                    UpdateSidebarFromField(_selectedField);
                    RedrawAllCanvasBoxes();
                    TxtStatusMessage.Text = $"● {_selectedFields.Count} box{(_selectedFields.Count > 1 ? "es" : "")} selected. Hold Shift to add/remove.";
                }
                else
                {
                    // Empty canvas space clicked - initiate panning drag
                    _isPanning = true;
                    _panStartMousePoint = e.GetPosition(CanvasScrollViewer);
                    _panStartH = CanvasScrollViewer.HorizontalOffset;
                    _panStartV = CanvasScrollViewer.VerticalOffset;
                    OverlayCanvas.CaptureMouse();
                    OverlayCanvas.Cursor = Cursors.Hand;
                    e.Handled = true;
                }
            }
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && OverlayCanvas.IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(CanvasScrollViewer);
                double deltaX = _panStartMousePoint.X - currentPoint.X;
                double deltaY = _panStartMousePoint.Y - currentPoint.Y;

                CanvasScrollViewer.ScrollToHorizontalOffset(_panStartH + deltaX);
                CanvasScrollViewer.ScrollToVerticalOffset(_panStartV + deltaY);
                return;
            }

            if (_isDragging && _origCoords.Count > 0)
            {
                var curPos = e.GetPosition(OverlayCanvas);
                double dx = (curPos.X - _dragStartPoint.X) / OverlayCanvas.Width;
                double dy = (curPos.Y - _dragStartPoint.Y) / OverlayCanvas.Height;

                foreach (var kvp in _origCoords)
                {
                    var sf = kvp.Key;
                    var orig = kvp.Value;

                    switch (_activeHandle)
                    {
                        case ResizeHandleType.Move:
                            sf.X = Math.Max(0.0, Math.Min(0.98, Math.Round(orig.X + dx, 4)));
                            sf.Y = Math.Max(0.0, Math.Min(0.98, Math.Round(orig.Y + dy, 4)));
                            break;

                        case ResizeHandleType.BottomRight:
                            sf.W = Math.Max(0.003, Math.Min(1.0 - sf.X, Math.Round(orig.W + dx, 4)));
                            sf.H = Math.Max(0.003, Math.Min(1.0 - sf.Y, Math.Round(orig.H + dy, 4)));
                            break;

                        case ResizeHandleType.BottomLeft:
                            double newX_bl = Math.Max(0.0, Math.Min(orig.X + orig.W - 0.003, Math.Round(orig.X + dx, 4)));
                            sf.W = Math.Max(0.003, Math.Round(orig.W - (newX_bl - orig.X), 4));
                            sf.X = newX_bl;
                            sf.H = Math.Max(0.003, Math.Min(1.0 - sf.Y, Math.Round(orig.H + dy, 4)));
                            break;

                        case ResizeHandleType.TopRight:
                            double newY_tr = Math.Max(0.0, Math.Min(orig.Y + orig.H - 0.003, Math.Round(orig.Y + dy, 4)));
                            sf.H = Math.Max(0.003, Math.Round(orig.H - (newY_tr - orig.Y), 4));
                            sf.Y = newY_tr;
                            sf.W = Math.Max(0.003, Math.Min(1.0 - sf.X, Math.Round(orig.W + dx, 4)));
                            break;

                        case ResizeHandleType.TopLeft:
                            double newX_tl = Math.Max(0.0, Math.Min(orig.X + orig.W - 0.003, Math.Round(orig.X + dx, 4)));
                            double newY_tl = Math.Max(0.0, Math.Min(orig.Y + orig.H - 0.003, Math.Round(orig.Y + dy, 4)));
                            sf.W = Math.Max(0.003, Math.Round(orig.W - (newX_tl - orig.X), 4));
                            sf.H = Math.Max(0.003, Math.Round(orig.H - (newY_tl - orig.Y), 4));
                            sf.X = newX_tl;
                            sf.Y = newY_tl;
                            break;

                        case ResizeHandleType.Right:
                            sf.W = Math.Max(0.003, Math.Min(1.0 - sf.X, Math.Round(orig.W + dx, 4)));
                            break;

                        case ResizeHandleType.Left:
                            double newX_l = Math.Max(0.0, Math.Min(orig.X + orig.W - 0.003, Math.Round(orig.X + dx, 4)));
                            sf.W = Math.Max(0.003, Math.Round(orig.W - (newX_l - orig.X), 4));
                            sf.X = newX_l;
                            break;

                        case ResizeHandleType.Bottom:
                            sf.H = Math.Max(0.003, Math.Min(1.0 - sf.Y, Math.Round(orig.H + dy, 4)));
                            break;

                        case ResizeHandleType.Top:
                            double newY_t = Math.Max(0.0, Math.Min(orig.Y + orig.H - 0.003, Math.Round(orig.Y + dy, 4)));
                            sf.H = Math.Max(0.003, Math.Round(orig.H - (newY_t - orig.Y), 4));
                            sf.Y = newY_t;
                            break;
                    }
                }

                if (_selectedField != null) UpdateSidebarFromField(_selectedField);
                RedrawAllCanvasBoxes();
            }
        }

        private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                OverlayCanvas.ReleaseMouseCapture();
                OverlayCanvas.Cursor = Cursors.Arrow;
            }

            if (_isDragging)
            {
                _isDragging = false;
                OverlayCanvas.ReleaseMouseCapture();
            }
        }

        private void CanvasScrollViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = true;
                _panStartMousePoint = e.GetPosition(CanvasScrollViewer);
                _panStartH = CanvasScrollViewer.HorizontalOffset;
                _panStartV = CanvasScrollViewer.VerticalOffset;
                CanvasScrollViewer.CaptureMouse();
                CanvasScrollViewer.Cursor = Cursors.Hand;
            }
        }

        private void CanvasScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && CanvasScrollViewer.IsMouseCaptured)
            {
                Point currentPoint = e.GetPosition(CanvasScrollViewer);
                double deltaX = _panStartMousePoint.X - currentPoint.X;
                double deltaY = _panStartMousePoint.Y - currentPoint.Y;

                CanvasScrollViewer.ScrollToHorizontalOffset(_panStartH + deltaX);
                CanvasScrollViewer.ScrollToVerticalOffset(_panStartV + deltaY);
            }
        }

        private void CanvasScrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning && CanvasScrollViewer.IsMouseCaptured)
            {
                _isPanning = false;
                CanvasScrollViewer.ReleaseMouseCapture();
                CanvasScrollViewer.Cursor = Cursors.Arrow;
            }
        }

        private async void TestOcr_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedField == null || string.IsNullOrEmpty(_imagePath) || !File.Exists(_imagePath))
            {
                TxtLiveOcrResult.Text = "Please select a field and ensure an image scan is uploaded.";
                return;
            }

            TxtLiveOcrResult.Text = "⚡ Running OCR on this box...";
            BtnTestOcr.IsEnabled = false;

            try
            {
                var roiResult = await SOLUM_UI.Services.OcrService.ExtractRoiAsync(
                    _imagePath,
                    _selectedField.X,
                    _selectedField.Y,
                    _selectedField.W,
                    _selectedField.H,
                    _selectedField.Key
                );

                if (roiResult != null && roiResult.Status == "success")
                {
                    _activeRoiTokens = roiResult.Tokens ?? new List<OcrWordToken>();
                    RedrawAllCanvasBoxes();

                    string timeInfo = roiResult.ExecutionTimeSeconds > 0 ? $" [{roiResult.ExecutionTimeSeconds:F2}s]" : "";
                    var sb = new StringBuilder();

                    bool isCheckbox = _selectedField != null && (
                        _selectedField.Key.StartsWith("is_") ||
                        _selectedField.Key.StartsWith("circ_") ||
                        _selectedField.Name.ToLower().Contains("checkbox") ||
                        roiResult.Value == "True" ||
                        roiResult.Value == "False"
                    );

                    if (isCheckbox && (roiResult.Value == "True" || roiResult.Value == "False" || (!string.IsNullOrEmpty(roiResult.RawValue) && roiResult.RawValue.StartsWith("Checkbox Ink:"))))
                    {
                        bool isMarked = (roiResult.Value == "True");
                        string markPill = isMarked ? "✓ MARKED" : "✗ UNMARKED";
                        sb.AppendLine($"Status: {markPill} ({roiResult.RawValue})");
                        sb.AppendLine($"Confidence: {roiResult.Confidence:F2} ({roiResult.Rating ?? "High"}){timeInfo}");
                    }
                    else if (!string.IsNullOrEmpty(roiResult.Value))
                    {
                        if (!string.IsNullOrEmpty(roiResult.RawValue) && !string.Equals(roiResult.RawValue, roiResult.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            sb.AppendLine($"Extracted{timeInfo}: \"{roiResult.Value}\"");
                            sb.AppendLine($"Raw: \"{roiResult.RawValue}\" (Conf: {roiResult.Confidence:F2})");
                        }
                        else
                        {
                            sb.AppendLine($"Extracted{timeInfo}: \"{roiResult.Value}\" (Conf: {roiResult.Confidence:F2})");
                        }
                    }
                    else if (!string.IsNullOrEmpty(roiResult.RawValue))
                    {
                        sb.AppendLine($"Filtered{timeInfo}: Raw \"{roiResult.RawValue}\" (Ignored: form label header)");
                    }
                    else
                    {
                        sb.AppendLine($"Extracted{timeInfo}: (empty box, no text detected)");
                    }

                    if (roiResult.Tokens != null && roiResult.Tokens.Count > 0)
                    {
                        var wordsList = roiResult.Tokens
                            .Where(t => !t.IsLabel)
                            .Select(t => $"[{t.Text}: {t.Confidence:F2}]");
                        var wordStr = string.Join(" ", wordsList);
                        if (!string.IsNullOrEmpty(wordStr))
                        {
                            sb.AppendLine("Tokens: " + wordStr);
                        }
                    }

                    if (roiResult.FilteredLabels != null && roiResult.FilteredLabels.Count > 0)
                    {
                        sb.AppendLine("Skipped Labels: " + string.Join(", ", roiResult.FilteredLabels.Select(l => $"\"{l}\"")));
                    }

                    TxtLiveOcrResult.Text = sb.ToString().TrimEnd();
                }
                else
                {
                    TxtLiveOcrResult.Text = "OCR Engine returned no text or is offline.";
                }
            }
            catch (Exception ex)
            {
                TxtLiveOcrResult.Text = "Error: " + ex.Message;
            }
            finally
            {
                BtnTestOcr.IsEnabled = true;
            }
        }

        private async Task<bool> SaveTemplateInternalAsync(bool showSuccessDialog = true)
        {
            try
            {
                string json = BuildTemplateJson();
                bool ok = await SOLUM_UI.Services.OcrService.SaveTemplateJsonAsync(json);
                if (ok)
                {
                    TemplateSaved = true;
                    TxtStatusMessage.Text = "✓ Template configuration saved successfully!";
                    if (showSuccessDialog)
                    {
                        ActionConfirmDialog.Success("Template Saved", "Template coordinates saved successfully!", this);
                    }
                    return true;
                }
                else
                {
                    ActionConfirmDialog.Warning("Save Error", "Failed to save template to server or local disk.", this);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ActionConfirmDialog.Error("Save Error", "Failed to save template: " + ex.Message, this);
                return false;
            }
        }

        private async void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            bool isConfirmed = ActionConfirmDialog.Show(
                title: "Save Template Layout",
                message: "Are you sure you want to save these adjusted box coordinates as the active OCR template?",
                confirmText: "Save Template",
                cancelText: "Cancel",
                theme: ConfirmThemeType.Primary,
                owner: this);

            if (!isConfirmed) return;

            await SaveTemplateInternalAsync(showSuccessDialog: true);
        }

        private string BuildTemplateJson()
        {
            string localPath = SOLUM_UI.Services.OcrService.GetLocalTemplatePath();
            Dictionary<string, object> root = null;
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            try
            {
                if (File.Exists(localPath))
                {
                    string existing = File.ReadAllText(localPath);
                    root = serializer.Deserialize<Dictionary<string, object>>(existing);
                }
            }
            catch { }

            if (root == null)
            {
                root = new Dictionary<string, object>
                {
                    { "form_id", "CSWDO-066-2" },
                    { "form_name", "City of Biñan CSWDO Application Form for Solo Parent" },
                    { "version", "2.2" }
                };
            }

            var fieldsDict = new Dictionary<string, object>();
            foreach (var f in _fields)
            {
                string type = (f.Key == "family_composition_table") ? "table" : (f.Key == "circumstances_section" ? "circumstance_group" : "text");
                var fieldData = new Dictionary<string, object>
                {
                    { "name", f.Name },
                    { "category", f.Category },
                    { "bbox", new double[] { Math.Round(f.X, 3), Math.Round(f.Y, 3), Math.Round(f.W, 3), Math.Round(f.H, 3) } },
                    { "type", type }
                };
                fieldsDict[f.Key] = fieldData;
            }
            root["fields"] = fieldsDict;

            return serializer.Serialize(root);
        }

        private void ResetToDefault_Click(object sender, RoutedEventArgs e)
        {
            bool isConfirmed = SOLUM_UI.Views.Dialogs.ActionConfirmDialog.Show(
                title: "Revert to Factory Defaults",
                message: "Are you sure you want to revert all field bounding boxes to the factory default positions?",
                confirmText: "Revert Layout",
                cancelText: "Cancel",
                theme: SOLUM_UI.Views.Dialogs.ConfirmThemeType.Warning,
                owner: this);

            if (isConfirmed)
            {
                PushUndoState();
                LoadFactoryDefaults();
                PopulateFieldCombo();
                RedrawAllCanvasBoxes();
                TxtStatusMessage.Text = "● Reverted to factory default layout. Click 'Save as Template' to persist.";
            }
        }

        private async void ApplyAndReExtract_Click(object sender, RoutedEventArgs e)
        {
            bool ok = await SaveTemplateInternalAsync(showSuccessDialog: false);
            if (ok)
            {
                DialogResult = true;
                Close();
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Min(5.0, _currentZoom + 0.2);
            ApplyZoom();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Max(0.1, _currentZoom - 0.2);
            ApplyZoom();
        }

        private void ZoomFit_Click(object sender, RoutedEventArgs e)
        {
            ZoomFit();
        }

        private void ZoomReset_Click(object sender, MouseButtonEventArgs e)
        {
            _currentZoom = 1.0;
            ApplyZoom();
        }

        private void ZoomReset1to1_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = 1.0;
            ApplyZoom();
        }

        private void ZoomFit()
        {
            if (CanvasScrollViewer.ActualWidth > 0 && CanvasContainer.Width > 0 && CanvasContainer.Height > 0)
            {
                double fitScaleW = (CanvasScrollViewer.ActualWidth - 60) / CanvasContainer.Width;
                double fitScaleH = (CanvasScrollViewer.ActualHeight - 60) / CanvasContainer.Height;
                double fitScale = (fitScaleH > 0) ? Math.Min(fitScaleW, fitScaleH) : fitScaleW;
                _currentZoom = Math.Max(0.1, Math.Min(1.2, fitScale));
                ApplyZoom();
                CanvasScrollViewer.ScrollToHorizontalOffset(0);
                CanvasScrollViewer.ScrollToVerticalOffset(0);
            }
        }

        private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (CanvasContainer.ActualWidth <= 0 || CanvasContainer.ActualHeight <= 0)
                return;

            // Zoom ONLY when Ctrl is held down (cursor-centered zoom)
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Point mousePosInViewer = e.GetPosition(CanvasScrollViewer);

                double oldZoom = _currentZoom;
                double zoomDelta = e.Delta > 0 ? 0.15 : -0.15;
                double newZoom = Math.Max(0.1, Math.Min(5.0, _currentZoom + zoomDelta));

                if (Math.Abs(newZoom - oldZoom) < 0.001)
                {
                    e.Handled = true;
                    return;
                }

                double contentX = (CanvasScrollViewer.HorizontalOffset + mousePosInViewer.X) / oldZoom;
                double contentY = (CanvasScrollViewer.VerticalOffset + mousePosInViewer.Y) / oldZoom;

                _currentZoom = newZoom;
                ApplyZoom();

                double newHorizontalOffset = (contentX * newZoom) - mousePosInViewer.X;
                double newVerticalOffset = (contentY * newZoom) - mousePosInViewer.Y;

                CanvasScrollViewer.ScrollToHorizontalOffset(Math.Max(0, newHorizontalOffset));
                CanvasScrollViewer.ScrollToVerticalOffset(Math.Max(0, newVerticalOffset));

                e.Handled = true;
                return;
            }

            // Normal scroll behavior without Ctrl:
            // Scroll vertically (or Shift+Wheel horizontally) smoothly without zooming
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                double newOffset = CanvasScrollViewer.HorizontalOffset - (e.Delta * 0.8);
                CanvasScrollViewer.ScrollToHorizontalOffset(Math.Max(0, Math.Min(CanvasScrollViewer.ScrollableWidth, newOffset)));
                e.Handled = true;
            }
            else
            {
                double newOffset = CanvasScrollViewer.VerticalOffset - (e.Delta * 0.8);
                CanvasScrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(CanvasScrollViewer.ScrollableHeight, newOffset)));
                e.Handled = true;
            }
        }

        private void ApplyZoom()
        {
            CanvasContainer.LayoutTransform = new ScaleTransform(_currentZoom, _currentZoom);
            if (TxtZoomLevel != null)
            {
                TxtZoomLevel.Text = $"{(int)Math.Round(_currentZoom * 100)}%";
            }
            RedrawAllCanvasBoxes();
        }

        #region Global Nudge and Scale (All Boxes)

        private void GlobalNudgeUp_Click(object sender, RoutedEventArgs e) => GlobalNudge(0.0, -0.005);
        private void GlobalNudgeDown_Click(object sender, RoutedEventArgs e) => GlobalNudge(0.0, 0.005);
        private void GlobalNudgeLeft_Click(object sender, RoutedEventArgs e) => GlobalNudge(-0.005, 0.0);
        private void GlobalNudgeRight_Click(object sender, RoutedEventArgs e) => GlobalNudge(0.005, 0.0);

        private void GlobalScaleExpand_Click(object sender, RoutedEventArgs e) => GlobalScale(1.01, 1.01);
        private void GlobalScaleShrink_Click(object sender, RoutedEventArgs e) => GlobalScale(0.99, 0.99);
        private void GlobalScaleWidth_Click(object sender, RoutedEventArgs e) => GlobalScale(1.01, 1.0);
        private void GlobalScaleHeight_Click(object sender, RoutedEventArgs e) => GlobalScale(1.0, 1.01);

        private void GlobalNudge(double dx, double dy)
        {
            PushUndoState();
            foreach (var f in _fields)
            {
                f.X = Math.Max(0.0, Math.Min(0.98, Math.Round(f.X + dx, 3)));
                f.Y = Math.Max(0.0, Math.Min(0.98, Math.Round(f.Y + dy, 3)));
            }
            if (_selectedField != null) UpdateSidebarFromField(_selectedField);
            RedrawAllCanvasBoxes();
            TxtStatusMessage.Text = $"🌐 Nudged all boxes (dx: {(dx * 100):+0.0;-0.0;0}%, dy: {(dy * 100):+0.0;-0.0;0}%).";
        }

        private void GlobalScale(double scaleX, double scaleY)
        {
            PushUndoState();
            double cx = 0.5;
            double cy = 0.5;
            foreach (var f in _fields)
            {
                double relX = f.X - cx;
                double relY = f.Y - cy;
                f.X = Math.Max(0.0, Math.Min(0.95, Math.Round(cx + relX * scaleX, 3)));
                f.Y = Math.Max(0.0, Math.Min(0.95, Math.Round(cy + relY * scaleY, 3)));
                f.W = Math.Max(0.005, Math.Min(0.95, Math.Round(f.W * scaleX, 3)));
                f.H = Math.Max(0.005, Math.Min(0.50, Math.Round(f.H * scaleY, 3)));
            }
            if (_selectedField != null) UpdateSidebarFromField(_selectedField);
            RedrawAllCanvasBoxes();
            TxtStatusMessage.Text = $"🌐 Scaled all boxes (Sx: {(scaleX * 100):0.0}%, Sy: {(scaleY * 100):0.0}%).";
        }

        #endregion

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (TemplateSaved && DialogResult == null)
            {
                try
                {
                    DialogResult = true;
                }
                catch { }
            }
            base.OnClosing(e);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (TemplateSaved)
            {
                DialogResult = true;
            }
            Close();
        }
    }
}
