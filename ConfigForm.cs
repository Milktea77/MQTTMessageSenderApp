using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class ConfigForm : Form
    {
        private Dictionary<string, string> valueMappings;
        private Dictionary<string, CheckBox> checkBoxes = new Dictionary<string, CheckBox>();
        private Dictionary<string, CheckBox> incrementCheckBoxes = new Dictionary<string, CheckBox>();
        private Dictionary<string, TextBox> textBoxes = new Dictionary<string, TextBox>();
        private DataGridView dataGrid;

        // 统一配色方案
        private static readonly Color PrimaryColor = Color.FromArgb(59, 130, 246);
        private static readonly Color SecondaryColor = Color.FromArgb(107, 33, 168);
        private static readonly Color LightGray = Color.FromArgb(248, 250, 252);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private static readonly Color TextColor = Color.FromArgb(51, 65, 85);
        private static readonly Color White = Color.FromArgb(255, 255, 255);
        private static readonly Color HeaderBgColor = Color.FromArgb(238, 242, 255);
        private static readonly Color RowAlternateColor = Color.FromArgb(249, 250, 252);

        public ConfigForm(Dictionary<string, string> existingValues)
        {
            this.Text = "功能值配置";
            this.ClientSize = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = LightGray;
            this.Font = new Font("Segoe UI", 9F);
            this.MinimumSize = new Size(700, 500);

            valueMappings = new Dictionary<string, string>(existingValues);
            SetupUI();
        }

        private void SetupUI()
        {
            // 创建主面板
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(15),
                BackColor = LightGray
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // 按钮栏
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 表格区域
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // 保存按钮

            // 顶部按钮
            var topButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = LightGray
            };

            var parseButton = CreateStyledButton("解析 JSON", PrimaryColor);
            parseButton.Click += ParseJson;
            topButtonPanel.Controls.Add(parseButton);
            mainPanel.Controls.Add(topButtonPanel, 0, 0);

            // 数据表格
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = LightGray,
                BorderStyle = BorderStyle.FixedSingle
            };

            dataGrid = CreateStyledDataGridView();
            scrollPanel.Controls.Add(dataGrid);
            mainPanel.Controls.Add(scrollPanel, 0, 1);

            // 底部保存按钮
            var bottomButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = LightGray
            };

            var saveButton = CreateStyledButton("保存配置", PrimaryColor);
            saveButton.Click += SaveConfig;
            bottomButtonPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(bottomButtonPanel, 0, 2);

            this.Controls.Add(mainPanel);
        }

        private DataGridView CreateStyledDataGridView()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = HeaderBgColor,
                    ForeColor = TextColor,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Padding = new Padding(8)
                },
                EnableHeadersVisualStyles = false,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = RowAlternateColor,
                    ForeColor = TextColor,
                    Padding = new Padding(5)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = White,
                    ForeColor = TextColor,
                    Padding = new Padding(5),
                    Font = new Font("Segoe UI", 9F)
                },
                GridColor = BorderColor
            };

            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            // 添加列
            var checkboxColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "选择",
                Width = 60,
                MinimumWidth = 50,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }
            };

            var nameColumn = new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "功能名称",
                ReadOnly = true,
                MinimumWidth = 150
            };

            var valueColumn = new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "值 (可编辑)",
                MinimumWidth = 120
            };

            var incrementColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Increment",
                HeaderText = "递增",
                Width = 60,
                MinimumWidth = 60,
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }
            };

            grid.Columns.AddRange(checkboxColumn, nameColumn, valueColumn, incrementColumn);

            grid.CellValueChanged += DataGrid_CellValueChanged;
            grid.CurrentCellDirtyStateChanged += DataGrid_CurrentCellDirtyStateChanged;

            return grid;
        }

        private void DataGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dataGrid.RowCount)
            {
                var nameCell = dataGrid.Rows[e.RowIndex].Cells["Name"];
                if (nameCell != null)
                {
                    var name = nameCell.Value?.ToString();
                    if (name != null)
                    {
                        if (e.ColumnIndex == 0) // 选择列
                        {
                            var isSelected = dataGrid.Rows[e.RowIndex].Cells["Selected"].Value is bool selected && selected;
                            EnableRowControls(e.RowIndex, isSelected);
                        }
                        else if (e.ColumnIndex == 3) // 递增列
                        {
                            var isIncrement = dataGrid.Rows[e.RowIndex].Cells["Increment"].Value is bool increment && increment;
                            if (isIncrement)
                            {
                                dataGrid.Rows[e.RowIndex].Cells["Value"].Value = "[a-b,c,d]";
                            }
                        }
                        else if (e.ColumnIndex == 2) // 值列
                        {
                            var value = dataGrid.Rows[e.RowIndex].Cells["Value"].Value?.ToString();
                            if (value != null)
                            {
                                valueMappings[name] = value;
                            }
                        }
                    }
                }
            }
        }

        private void DataGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            dataGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void EnableRowControls(int rowIndex, bool enabled)
        {
            dataGrid.BeginEdit(false);
            dataGrid.Rows[rowIndex].Cells["Value"].ReadOnly = !enabled;
            dataGrid.Rows[rowIndex].Cells["Increment"].ReadOnly = !enabled;
            dataGrid.EndEdit();
        }

        private Button CreateStyledButton(string text, Color bgColor)
        {
            var button = new Button
            {
                Text = text,
                Width = 120,
                Height = 36,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = bgColor,
                ForeColor = White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5)
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ChangeColorBrightness(bgColor, -20);
            button.FlatAppearance.MouseDownBackColor = ChangeColorBrightness(bgColor, -40);

            return button;
        }

        private Color ChangeColorBrightness(Color color, int brightnessChange)
        {
            return Color.FromArgb(
                Math.Max(0, Math.Min(255, color.R + brightnessChange)),
                Math.Max(0, Math.Min(255, color.G + brightnessChange)),
                Math.Max(0, Math.Min(255, color.B + brightnessChange))
            );
        }

        private void ParseJson(object sender, EventArgs e)
        {
            dataGrid.Rows.Clear();
            checkBoxes.Clear();
            textBoxes.Clear();
            incrementCheckBoxes.Clear();

            HashSet<string> uniqueFunctions = new HashSet<string>();

            try
            {
                string json = File.ReadAllText(MessageFileHandler.MessageFilePath);
                var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (jsonObj.ContainsKey("devs"))
                {

                    if (jsonObj["devs"] is JsonElement devicesElement && devicesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var devElement in devicesElement.EnumerateArray())
                        {
                            if (devElement.ValueKind == JsonValueKind.Object && devElement.TryGetProperty("d", out var dElement))
                            {
                                if (dElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var dataElement in dElement.EnumerateArray())
                                    {
                                        if (dataElement.ValueKind == JsonValueKind.Object &&
                                            dataElement.TryGetProperty("m", out var mElement) &&
                                            dataElement.TryGetProperty("v", out var vElement))
                                        {
                                            string m = mElement.ToString();
                                            string v = vElement.ToString();

                                            if (!uniqueFunctions.Contains(m))
                                            {
                                                uniqueFunctions.Add(m);
                                                valueMappings[m] = v;

                                                dataGrid.Rows.Add(false, m, v, false);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                MessageBox.Show($"成功解析 {uniqueFunctions.Count} 个功能项", "解析完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveConfig(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "保存配置将会使更改保存至 sim_message.txt，原文件内容将被更新。\n\n是否继续？",
                "配置修改警告",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.No) return;

            try
            {
                string messageFile = MessageFileHandler.MessageFilePath;
                if (!File.Exists(messageFile))
                {
                    MessageBox.Show($"消息文件 '{messageFile}' 不存在！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string jsonContent = File.ReadAllText(messageFile);
                var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                if (jsonDict.ContainsKey("devs"))
                {
                    List<Dictionary<string, object>> devices = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonDict["devs"].ToString());

                    foreach (var dev in devices)
                    {
                        if (dev.ContainsKey("d"))
                        {
                            List<Dictionary<string, object>> deviceData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dev["d"].ToString());

                            foreach (var data in deviceData)
                            {
                                string m = data["m"].ToString();

                                var cell = FindCellByName(m);
                                if (cell != null && cell.OwningRow.Cells[0].Value is bool selected && selected) // 如果选中
                                {
                                    string valueConfig = cell.Value?.ToString() ?? "";

                                    // 处理递增公式
                                    if (System.Text.RegularExpressions.Regex.IsMatch(valueConfig, @"\[\d+(\.\d+)?-\d+(\.\d+)?,\d+,\d+(\.\d+)?\]"))
                                    {
                                        data["v"] = valueConfig;
                                    }
                                    else if (bool.TryParse(valueConfig, out bool boolValue))
                                    {
                                        data["v"] = boolValue;
                                    }
                                    else if (int.TryParse(valueConfig, out int intValue))
                                    {
                                        data["v"] = intValue;
                                    }
                                    else if (double.TryParse(valueConfig, out double doubleValue))
                                    {
                                        data["v"] = doubleValue;
                                    }
                                    else
                                    {
                                        data["v"] = valueConfig;
                                    }
                                }
                            }

                            dev["d"] = deviceData;
                        }
                    }

                    jsonDict["devs"] = devices;
                }

                File.WriteAllText(messageFile, JsonSerializer.Serialize(jsonDict, new JsonSerializerOptions { WriteIndented = true }));
                MessageBox.Show("配置已保存，sim_message.txt 已更新！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataGridViewCell FindCellByName(string name)
        {
            foreach (DataGridViewRow row in dataGrid.Rows)
            {
                if (row.Cells[1].Value?.ToString() == name) // Name列在索引1
                {
                    return row.Cells[2]; // Value列在索引2
                }
            }
            return null;
        }

        public Dictionary<string, string> GetConfiguredValues()
        {
            return valueMappings;
        }
    }
}
