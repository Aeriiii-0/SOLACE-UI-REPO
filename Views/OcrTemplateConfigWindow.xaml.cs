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
        private ResizeHandleType _activeHandle = ResizeHandleType.Move;
        private double _currentZoom = 1.0;
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _origX, _origY, _origW, _origH;
        private bool _updatingFromCode = false;

        public bool TemplateSaved { get; private set; } = false;

        public OcrTemplateConfigWindow(string imagePath = null)
        {
            InitializeComponent();
            _imagePath = imagePath;
            Loaded += OcrTemplateConfigWindow_Loaded;
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
            _fields.Add(new TemplateFieldItem { Key = "application_date", Name = "Date of Application", Category = "Header", X = 0.760, Y = 0.108, W = 0.225, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "last_name", Name = "Last Name / Surname", Category = "Section I", X = 0.090, Y = 0.188, W = 0.160, H = 0.032 });
            _fields.Add(new TemplateFieldItem { Key = "first_name", Name = "First Name", Category = "Section I", X = 0.285, Y = 0.188, W = 0.185, H = 0.032 });
            _fields.Add(new TemplateFieldItem { Key = "middle_name", Name = "Middle Name", Category = "Section I", X = 0.515, Y = 0.188, W = 0.150, H = 0.032 });
            _fields.Add(new TemplateFieldItem { Key = "ext_name", Name = "Extension Name", Category = "Section I", X = 0.690, Y = 0.188, W = 0.070, H = 0.032 });
            _fields.Add(new TemplateFieldItem { Key = "civil_status", Name = "Civil Status", Category = "Section I", X = 0.810, Y = 0.188, W = 0.050, H = 0.032 });
            _fields.Add(new TemplateFieldItem { Key = "sex", Name = "Sex", Category = "Section I", X = 0.885, Y = 0.188, W = 0.080, H = 0.032 });
            _fields.Add(new TemplateFieldItem { Key = "birthdate", Name = "Date of Birth", Category = "Section I", X = 0.090, Y = 0.222, W = 0.120, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "birthplace", Name = "Place of Birth", Category = "Section I", X = 0.265, Y = 0.222, W = 0.145, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "age", Name = "Age", Category = "Section I", X = 0.435, Y = 0.222, W = 0.060, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "educational_attainment", Name = "Educational Attainment", Category = "Section I", X = 0.585, Y = 0.222, W = 0.140, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "philsys_number", Name = "PhilSys Card Number", Category = "Section I", X = 0.810, Y = 0.222, W = 0.155, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "religion", Name = "Religion", Category = "Section I", X = 0.075, Y = 0.255, W = 0.138, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "occupation", Name = "Occupation / Source of Income", Category = "Section I", X = 0.325, Y = 0.255, W = 0.125, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "monthly_income", Name = "Monthly Income", Category = "Section I", X = 0.520, Y = 0.255, W = 0.075, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "employment_status", Name = "Employment Status (Checkboxes)", Category = "Section I", X = 0.593, Y = 0.236, W = 0.380, H = 0.035 });
            _fields.Add(new TemplateFieldItem { Key = "is_employed", Name = "Checkbox: Employed", Category = "Section I", X = 0.620, Y = 0.252, W = 0.017, H = 0.012 });
            _fields.Add(new TemplateFieldItem { Key = "is_self_employed", Name = "Checkbox: Self-Employed", Category = "Section I", X = 0.718, Y = 0.252, W = 0.017, H = 0.012 });
            _fields.Add(new TemplateFieldItem { Key = "is_not_employed", Name = "Checkbox: Not Employed", Category = "Section I", X = 0.845, Y = 0.252, W = 0.019, H = 0.012 });
            _fields.Add(new TemplateFieldItem { Key = "address", Name = "Address (House/Street)", Category = "Section I", X = 0.175, Y = 0.285, W = 0.240, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "barangay", Name = "Barangay", Category = "Section I", X = 0.460, Y = 0.285, W = 0.205, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "contact_number", Name = "Contact Number", Category = "Section I", X = 0.740, Y = 0.285, W = 0.225, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "emergency_name", Name = "Emergency Contact Person", Category = "Section I", X = 0.170, Y = 0.315, W = 0.160, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "emergency_relationship", Name = "Emergency Relationship", Category = "Section I", X = 0.395, Y = 0.315, W = 0.100, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "emergency_address", Name = "Emergency Contact Address", Category = "Section I", X = 0.535, Y = 0.315, W = 0.215, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "emergency_number", Name = "Emergency Contact Number", Category = "Section I", X = 0.810, Y = 0.315, W = 0.155, H = 0.028 });

            // Section II: Circumstances
            _fields.Add(new TemplateFieldItem { Key = "circ_a1", Name = "Circumstance A1 (Rape)", Category = "Section II", X = 0.030, Y = 0.345, W = 0.045, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a2", Name = "Circumstance A2 (Death of Spouse)", Category = "Section II", X = 0.030, Y = 0.365, W = 0.045, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a2_cause", Name = "A2 Subfield: Cause of Death", Category = "Section II", X = 0.100, Y = 0.365, W = 0.200, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a2_date", Name = "A2 Subfield: Date of Death", Category = "Section II", X = 0.320, Y = 0.365, W = 0.150, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a3", Name = "Circumstance A3 (Detention)", Category = "Section II", X = 0.030, Y = 0.385, W = 0.045, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a4", Name = "Circumstance A4 (Incapacity)", Category = "Section II", X = 0.030, Y = 0.405, W = 0.045, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a4_disability", Name = "A4 Subfield: Type of Disability", Category = "Section II", X = 0.100, Y = 0.405, W = 0.370, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a5", Name = "Circumstance A5 (Separation)", Category = "Section II", X = 0.030, Y = 0.425, W = 0.045, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a5_period", Name = "A5 Subfield: Period of Separation", Category = "Section II", X = 0.100, Y = 0.425, W = 0.370, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a6", Name = "Circumstance A6 (Nullity / Annulment)", Category = "Section II", X = 0.030, Y = 0.445, W = 0.045, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a6_nullity", Name = "A6 Sub-Checkbox: Declaration of Nullity", Category = "Section II", X = 0.080, Y = 0.445, W = 0.040, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a6_annulment", Name = "A6 Sub-Checkbox: Annulment", Category = "Section II", X = 0.220, Y = 0.445, W = 0.040, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_a7", Name = "Circumstance A7 (Abandonment)", Category = "Section II", X = 0.030, Y = 0.465, W = 0.045, H = 0.018 });
            _fields.Add(new TemplateFieldItem { Key = "circ_b", Name = "Circumstance B (OFW Spouse)", Category = "Section II", X = 0.500, Y = 0.345, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_b_stay", Name = "B Subfield: Stay Abroad", Category = "Section II", X = 0.560, Y = 0.345, W = 0.380, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_c", Name = "Circumstance C (Unmarried Parent)", Category = "Section II", X = 0.500, Y = 0.370, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_d", Name = "Circumstance D (Legal Guardian)", Category = "Section II", X = 0.500, Y = 0.395, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_e", Name = "Circumstance E (Relative 4th Degree)", Category = "Section II", X = 0.500, Y = 0.420, W = 0.045, H = 0.022 });
            _fields.Add(new TemplateFieldItem { Key = "circ_f", Name = "Circumstance F (Pregnant Woman)", Category = "Section II", X = 0.500, Y = 0.445, W = 0.045, H = 0.022 });

            // Section III: Family Composition Row 1
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_name", Name = "Family Row 1: Name", Category = "Section III", X = 0.025, Y = 0.513, W = 0.230, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_sex", Name = "Family Row 1: Sex", Category = "Section III", X = 0.255, Y = 0.513, W = 0.060, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_age", Name = "Family Row 1: Age", Category = "Section III", X = 0.315, Y = 0.513, W = 0.055, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_dob", Name = "Family Row 1: Birthdate", Category = "Section III", X = 0.370, Y = 0.513, W = 0.135, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_civ", Name = "Family Row 1: Civil Status", Category = "Section III", X = 0.505, Y = 0.513, W = 0.095, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_rel", Name = "Family Row 1: Relationship", Category = "Section III", X = 0.600, Y = 0.513, W = 0.120, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_edu", Name = "Family Row 1: Education / Job", Category = "Section III", X = 0.720, Y = 0.513, W = 0.140, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row1_inc", Name = "Family Row 1: Income", Category = "Section III", X = 0.860, Y = 0.513, W = 0.105, H = 0.028 });

            // Section III: Family Composition Row 2
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_name", Name = "Family Row 2: Name", Category = "Section III", X = 0.025, Y = 0.543, W = 0.230, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_sex", Name = "Family Row 2: Sex", Category = "Section III", X = 0.255, Y = 0.543, W = 0.060, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_age", Name = "Family Row 2: Age", Category = "Section III", X = 0.315, Y = 0.543, W = 0.055, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_dob", Name = "Family Row 2: Birthdate", Category = "Section III", X = 0.370, Y = 0.543, W = 0.135, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_civ", Name = "Family Row 2: Civil Status", Category = "Section III", X = 0.505, Y = 0.543, W = 0.095, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_rel", Name = "Family Row 2: Relationship", Category = "Section III", X = 0.600, Y = 0.543, W = 0.120, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_edu", Name = "Family Row 2: Education / Job", Category = "Section III", X = 0.720, Y = 0.543, W = 0.140, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row2_inc", Name = "Family Row 2: Income", Category = "Section III", X = 0.860, Y = 0.543, W = 0.105, H = 0.028 });

            // Section III: Family Composition Row 3
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_name", Name = "Family Row 3: Name", Category = "Section III", X = 0.025, Y = 0.574, W = 0.230, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_sex", Name = "Family Row 3: Sex", Category = "Section III", X = 0.255, Y = 0.574, W = 0.060, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_age", Name = "Family Row 3: Age", Category = "Section III", X = 0.315, Y = 0.574, W = 0.055, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_dob", Name = "Family Row 3: Birthdate", Category = "Section III", X = 0.370, Y = 0.574, W = 0.135, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_civ", Name = "Family Row 3: Civil Status", Category = "Section III", X = 0.505, Y = 0.574, W = 0.095, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_rel", Name = "Family Row 3: Relationship", Category = "Section III", X = 0.600, Y = 0.574, W = 0.120, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_edu", Name = "Family Row 3: Education / Job", Category = "Section III", X = 0.720, Y = 0.574, W = 0.140, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row3_inc", Name = "Family Row 3: Income", Category = "Section III", X = 0.860, Y = 0.574, W = 0.105, H = 0.028 });

            // Section III: Family Composition Row 4
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_name", Name = "Family Row 4: Name", Category = "Section III", X = 0.025, Y = 0.605, W = 0.230, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_sex", Name = "Family Row 4: Sex", Category = "Section III", X = 0.255, Y = 0.605, W = 0.060, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_age", Name = "Family Row 4: Age", Category = "Section III", X = 0.315, Y = 0.605, W = 0.055, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_dob", Name = "Family Row 4: Birthdate", Category = "Section III", X = 0.370, Y = 0.605, W = 0.135, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_civ", Name = "Family Row 4: Civil Status", Category = "Section III", X = 0.505, Y = 0.605, W = 0.095, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_rel", Name = "Family Row 4: Relationship", Category = "Section III", X = 0.600, Y = 0.605, W = 0.120, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_edu", Name = "Family Row 4: Education / Job", Category = "Section III", X = 0.720, Y = 0.605, W = 0.140, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row4_inc", Name = "Family Row 4: Income", Category = "Section III", X = 0.860, Y = 0.605, W = 0.105, H = 0.028 });

            // Section III: Family Composition Row 5
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_name", Name = "Family Row 5: Name", Category = "Section III", X = 0.025, Y = 0.635, W = 0.230, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_sex", Name = "Family Row 5: Sex", Category = "Section III", X = 0.255, Y = 0.635, W = 0.060, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_age", Name = "Family Row 5: Age", Category = "Section III", X = 0.315, Y = 0.635, W = 0.055, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_dob", Name = "Family Row 5: Birthdate", Category = "Section III", X = 0.370, Y = 0.635, W = 0.135, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_civ", Name = "Family Row 5: Civil Status", Category = "Section III", X = 0.505, Y = 0.635, W = 0.095, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_rel", Name = "Family Row 5: Relationship", Category = "Section III", X = 0.600, Y = 0.635, W = 0.120, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_edu", Name = "Family Row 5: Education / Job", Category = "Section III", X = 0.720, Y = 0.635, W = 0.140, H = 0.028 });
            _fields.Add(new TemplateFieldItem { Key = "fam_row5_inc", Name = "Family Row 5: Income", Category = "Section III", X = 0.860, Y = 0.635, W = 0.105, H = 0.028 });

            // Section IV: Needs and Problems
            _fields.Add(new TemplateFieldItem { Key = "needs", Name = "IV. Needs and Problems", Category = "Section IV", X = 0.025, Y = 0.555, W = 0.940, H = 0.030 });

            // Section V: Other Sources of Income
            _fields.Add(new TemplateFieldItem { Key = "other_income", Name = "V. Other Sources of Income", Category = "Section V", X = 0.025, Y = 0.602, W = 0.940, H = 0.025 });
        }

        private void LoadDocumentImage()
        {
            if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(_imagePath, UriKind.Absolute);
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
                RedrawAllCanvasBoxes();
                UpdateUndoRedoButtons();
                TxtStatusMessage.Text = "↪ Redid change.";
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    Undo_Click(this, null);
                    e.Handled = true;
                }
                else if (e.Key == Key.Y)
                {
                    Redo_Click(this, null);
                    e.Handled = true;
                }
            }
        }

        private void PopulateFieldCombo()
        {
            CmbFields.ItemsSource = null;
            CmbFields.ItemsSource = _fields;
            if (_fields.Count > 0)
            {
                CmbFields.SelectedIndex = 0;
            }
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
                bool isSelected = (_selectedField != null && _selectedField.Key == f.Key);

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
                    Fill = isSelected ? new SolidColorBrush(Color.FromArgb(35, 233, 30, 99)) : new SolidColorBrush(Color.FromArgb(12, 33, 150, 243)),
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
        }

        private void CmbFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFields.SelectedItem is TemplateFieldItem item)
            {
                _selectedField = item;
                UpdateSidebarFromField(item);
                RedrawAllCanvasBoxes();
            }
        }

        private void UpdateSidebarFromField(TemplateFieldItem f)
        {
            _updatingFromCode = true;
            TxtFieldKey.Text = f.Key;
            TxtFieldCategory.Text = f.Category;
            SliderX.Value = f.X;
            SliderY.Value = f.Y;
            SliderW.Value = f.W;
            SliderH.Value = f.H;
            _updatingFromCode = false;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_updatingFromCode || _selectedField == null) return;

            _selectedField.X = Math.Round(SliderX.Value, 3);
            _selectedField.Y = Math.Round(SliderY.Value, 3);
            _selectedField.W = Math.Round(SliderW.Value, 3);
            _selectedField.H = Math.Round(SliderH.Value, 3);

            RedrawAllCanvasBoxes();
        }

        private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var hit = e.OriginalSource as FrameworkElement;
            if (hit != null)
            {
                TemplateFieldItem fieldItem = null;
                ResizeHandleType handleType = ResizeHandleType.Move;

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

                if (fieldItem != null)
                {
                    PushUndoState();
                    _selectedField = fieldItem;
                    _activeHandle = handleType;
                    CmbFields.SelectedItem = fieldItem;
                    _isDragging = true;
                    _dragStartPoint = e.GetPosition(OverlayCanvas);
                    _origX = fieldItem.X;
                    _origY = fieldItem.Y;
                    _origW = fieldItem.W;
                    _origH = fieldItem.H;
                    OverlayCanvas.CaptureMouse();
                }
            }
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedField != null)
            {
                var curPos = e.GetPosition(OverlayCanvas);
                double dx = (curPos.X - _dragStartPoint.X) / OverlayCanvas.Width;
                double dy = (curPos.Y - _dragStartPoint.Y) / OverlayCanvas.Height;

                switch (_activeHandle)
                {
                    case ResizeHandleType.Move:
                        _selectedField.X = Math.Max(0, Math.Min(0.98, Math.Round(_origX + dx, 3)));
                        _selectedField.Y = Math.Max(0, Math.Min(0.98, Math.Round(_origY + dy, 3)));
                        break;

                    case ResizeHandleType.BottomRight:
                        _selectedField.W = Math.Max(0.005, Math.Min(1.0 - _selectedField.X, Math.Round(_origW + dx, 3)));
                        _selectedField.H = Math.Max(0.005, Math.Min(1.0 - _selectedField.Y, Math.Round(_origH + dy, 3)));
                        break;

                    case ResizeHandleType.BottomLeft:
                        double newX_bl = Math.Max(0, Math.Min(_origX + _origW - 0.005, Math.Round(_origX + dx, 3)));
                        _selectedField.W = Math.Max(0.005, Math.Round(_origW - (newX_bl - _origX), 3));
                        _selectedField.X = newX_bl;
                        _selectedField.H = Math.Max(0.005, Math.Min(1.0 - _selectedField.Y, Math.Round(_origH + dy, 3)));
                        break;

                    case ResizeHandleType.TopRight:
                        double newY_tr = Math.Max(0, Math.Min(_origY + _origH - 0.005, Math.Round(_origY + dy, 3)));
                        _selectedField.H = Math.Max(0.005, Math.Round(_origH - (newY_tr - _origY), 3));
                        _selectedField.Y = newY_tr;
                        _selectedField.W = Math.Max(0.005, Math.Min(1.0 - _selectedField.X, Math.Round(_origW + dx, 3)));
                        break;

                    case ResizeHandleType.TopLeft:
                        double newX_tl = Math.Max(0, Math.Min(_origX + _origW - 0.005, Math.Round(_origX + dx, 3)));
                        double newY_tl = Math.Max(0, Math.Min(_origY + _origH - 0.005, Math.Round(_origY + dy, 3)));
                        _selectedField.W = Math.Max(0.005, Math.Round(_origW - (newX_tl - _origX), 3));
                        _selectedField.H = Math.Max(0.005, Math.Round(_origH - (newY_tl - _origY), 3));
                        _selectedField.X = newX_tl;
                        _selectedField.Y = newY_tl;
                        break;

                    case ResizeHandleType.Right:
                        _selectedField.W = Math.Max(0.005, Math.Min(1.0 - _selectedField.X, Math.Round(_origW + dx, 3)));
                        break;

                    case ResizeHandleType.Left:
                        double newX_l = Math.Max(0, Math.Min(_origX + _origW - 0.005, Math.Round(_origX + dx, 3)));
                        _selectedField.W = Math.Max(0.005, Math.Round(_origW - (newX_l - _origX), 3));
                        _selectedField.X = newX_l;
                        break;

                    case ResizeHandleType.Bottom:
                        _selectedField.H = Math.Max(0.005, Math.Min(1.0 - _selectedField.Y, Math.Round(_origH + dy, 3)));
                        break;

                    case ResizeHandleType.Top:
                        double newY_t = Math.Max(0, Math.Min(_origY + _origH - 0.005, Math.Round(_origY + dy, 3)));
                        _selectedField.H = Math.Max(0.005, Math.Round(_origH - (newY_t - _origY), 3));
                        _selectedField.Y = newY_t;
                        break;
                }

                UpdateSidebarFromField(_selectedField);
                RedrawAllCanvasBoxes();
            }
        }

        private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                OverlayCanvas.ReleaseMouseCapture();
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
                    _selectedField.H
                );

                if (roiResult != null && roiResult.Status == "success")
                {
                    string extractedVal = string.IsNullOrEmpty(roiResult.Value) ? "(empty)" : roiResult.Value;
                    string timeInfo = roiResult.ExecutionTimeSeconds > 0 ? $" [{roiResult.ExecutionTimeSeconds:F2}s]" : "";
                    TxtLiveOcrResult.Text = $"Extracted{timeInfo}: \"{extractedVal}\" (Conf: {roiResult.Confidence:F2})";
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

        private async void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string json = BuildTemplateJson();
                bool ok = await SOLUM_UI.Services.OcrService.SaveTemplateJsonAsync(json);
                if (ok)
                {
                    TemplateSaved = true;
                    TxtStatusMessage.Text = "✓ Template configuration saved successfully!";
                    MessageBox.Show("Template coordinates saved successfully!", "Template Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save template to server or local disk.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save template: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            var res = MessageBox.Show("Are you sure you want to revert all field bounding boxes to the factory default positions?", "Revert to Default", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                PushUndoState();
                LoadFactoryDefaults();
                PopulateFieldCombo();
                RedrawAllCanvasBoxes();
                TxtStatusMessage.Text = "● Reverted to factory default layout. Click 'Save as Template' to persist.";
            }
        }

        private void ApplyAndReExtract_Click(object sender, RoutedEventArgs e)
        {
            SaveTemplate_Click(sender, e);
            DialogResult = true;
            Close();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Min(3.0, _currentZoom + 0.2);
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

        private void ZoomFit()
        {
            if (CanvasScrollViewer.ActualWidth > 0 && CanvasContainer.Width > 0 && CanvasContainer.Height > 0)
            {
                double fitScaleW = (CanvasScrollViewer.ActualWidth - 40) / CanvasContainer.Width;
                double fitScaleH = (CanvasScrollViewer.ActualHeight - 40) / CanvasContainer.Height;
                double fitScale = (fitScaleH > 0) ? Math.Min(fitScaleW, fitScaleH) : fitScaleW;
                _currentZoom = Math.Max(0.1, Math.Min(1.2, fitScale));
                ApplyZoom();
            }
        }

        private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                _currentZoom = Math.Min(4.0, _currentZoom + 0.12);
            else
                _currentZoom = Math.Max(0.1, _currentZoom - 0.12);

            ApplyZoom();
            e.Handled = true;
        }

        private void ApplyZoom()
        {
            CanvasContainer.LayoutTransform = new ScaleTransform(_currentZoom, _currentZoom);
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
