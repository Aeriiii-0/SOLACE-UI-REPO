using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SOLUM_UI.Services
{
    public static partial class OcrService
    {
        public static async Task<string> GetTemplateJsonAsync()
        {
            try
            {
                if (await IsServerOnlineAsync())
                {
                    var resp = await _http.GetAsync(ServerUrl + "/template");
                    if (resp.IsSuccessStatusCode)
                    {
                        return await resp.Content.ReadAsStringAsync();
                    }
                }
            }
            catch { }

            string localPath = GetLocalTemplatePath();
            if (File.Exists(localPath))
            {
                return File.ReadAllText(localPath);
            }
            return string.Empty;
        }

        public static async Task<bool> SaveTemplateJsonAsync(string json)
        {
            bool savedServer = false;
            try
            {
                if (await IsServerOnlineAsync())
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var resp = await _http.PostAsync(ServerUrl + "/template", content);
                    savedServer = resp.IsSuccessStatusCode;
                }
            }
            catch { }

            try
            {
                string localPath = GetLocalTemplatePath();
                string dir = Path.GetDirectoryName(localPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(localPath, json, new System.Text.UTF8Encoding(false));
                return true;
            }
            catch
            {
                return savedServer;
            }
        }

        public static string GetLocalTemplatePath()
        {
            string ocrDir = FindOcrDirectory();
            return Path.Combine(ocrDir, "cswdo_template.json");
        }

        public static Dictionary<string, double[]> GetTemplateBoundingBoxes()
        {
            var dict = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = GetLocalTemplatePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var root = serializer.Deserialize<Dictionary<string, object>>(json);
                    if (root != null && root.TryGetValue("fields", out var fieldsObj) && fieldsObj is Dictionary<string, object> fields)
                    {
                        foreach (var kvp in fields)
                        {
                            if (kvp.Value is Dictionary<string, object> fieldDict &&
                                fieldDict.TryGetValue("bbox", out var bboxObj) &&
                                bboxObj is System.Collections.IEnumerable list)
                            {
                                var nums = list.Cast<object>().Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).ToArray();
                                if (nums.Length >= 4)
                                {
                                    dict[kvp.Key] = new[] { nums[0], nums[1], nums[2], nums[3] };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[OcrService] Error loading template bboxes: " + ex.Message);
            }
            return dict;
        }

        public static void SaveTemplateBoundingBoxes(Dictionary<string, double[]> boxes)
        {
            if (boxes == null) return;
            try
            {
                string path = GetLocalTemplatePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var root = serializer.Deserialize<Dictionary<string, object>>(json);
                    if (root != null && root.TryGetValue("fields", out var fieldsObj) && fieldsObj is Dictionary<string, object> fields)
                    {
                        foreach (var kvp in boxes)
                        {
                            if (fields.TryGetValue(kvp.Key, out var fieldObj) && fieldObj is Dictionary<string, object> fieldDict)
                            {
                                fieldDict["bbox"] = kvp.Value;
                            }
                            else
                            {
                                fields[kvp.Key] = new Dictionary<string, object>
                                {
                                    { "bbox", kvp.Value }
                                };
                            }
                        }
                        string updatedJson = serializer.Serialize(root);
                        File.WriteAllText(path, updatedJson, new System.Text.UTF8Encoding(false));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[OcrService] Error saving template bboxes: " + ex.Message);
            }
        }
    }
}
