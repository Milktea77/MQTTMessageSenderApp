using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class ConfigForm : Form
    {
        private Dictionary<string, string> valueMappings;
        private Dictionary<string, CheckBox> checkBoxes = new Dictionary<string, CheckBox>();
        private Dictionary<string, CheckBox> incrementCheckBoxes = new Dictionary<string, CheckBox>();
        private Dictionary<string, TextBox> textBoxes = new Dictionary<string, TextBox>();
        private TableLayoutPanel tableLayout;

        public ConfigForm(Dictionary<string, string> existingValues)
        {
            this.Text = "功能值配置";
            this.ClientSize = new System.Drawing.Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;

            valueMappings = new Dictionary<string, string>(existingValues);
            SetupUI();
        }

        private void SetupUI()
        {
            Button parseButton = new Button { Text = "解析 JSON", Dock = DockStyle.Top };
            parseButton.Click += ParseJson;

            Button saveButton = new Button { Text = "保存", Dock = DockStyle.Bottom };
            saveButton.Click += SaveConfig;

            tableLayout = new TableLayoutPanel
            {
                ColumnCount = 4,
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            tableLayout.Controls.Add(new Label { Text = "选择", AutoSize = true }, 0, 0);
            tableLayout.Controls.Add(new Label { Text = "功能名称", AutoSize = true }, 1, 0);
            tableLayout.Controls.Add(new Label { Text = "值 (可编辑)", AutoSize = true }, 2, 0);
            tableLayout.Controls.Add(new Label { Text = "递增", AutoSize = true }, 3, 0);

            this.Controls.Add(saveButton);
            this.Controls.Add(tableLayout);
            this.Controls.Add(parseButton);
        }

        private void ParseJson(object sender, EventArgs e)
        {
            tableLayout.Controls.Clear();
            checkBoxes.Clear();
            textBoxes.Clear();
            incrementCheckBoxes.Clear();

            tableLayout.Controls.Add(new Label { Text = "选择", AutoSize = true }, 0, 0);
            tableLayout.Controls.Add(new Label { Text = "功能名称", AutoSize = true }, 1, 0);
            tableLayout.Controls.Add(new Label { Text = "值 (可编辑)", AutoSize = true }, 2, 0);
            tableLayout.Controls.Add(new Label { Text = "递增", AutoSize = true }, 3, 0);

            try
            {
                string json = File.ReadAllText("sim_message.txt");
                var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (jsonObj.ContainsKey("devs"))
                {
                    HashSet<string> uniqueFunctions = new HashSet<string>();
                    int row = 1;

                    foreach (var dev in JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonObj["devs"].ToString()))
                    {
                        if (dev.ContainsKey("d"))
                        {
                            foreach (var data in JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dev["d"].ToString()))
                            {
                                if (data.ContainsKey("m") && data.ContainsKey("v"))
                                {
                                    string m = data["m"].ToString();
                                    string v = data["v"].ToString();

                                    if (!uniqueFunctions.Contains(m))
                                    {
                                        uniqueFunctions.Add(m);

                                        CheckBox checkBox = new CheckBox();
                                        CheckBox incrementCheckBox = new CheckBox { Enabled = false };
                                        Label mLabel = new Label { Text = m, AutoSize = true };
                                        TextBox vTextBox = new TextBox { Text = v, Enabled = false, Width = 100 };

                                        checkBox.CheckedChanged += (s, ev) =>
                                        {
                                            vTextBox.Enabled = checkBox.Checked;
                                            incrementCheckBox.Enabled = checkBox.Checked;
                                        };

                                        incrementCheckBox.CheckedChanged += (s, ev) =>
                                        {
                                            if (incrementCheckBox.Checked)
                                            {
                                                vTextBox.Text = "[a-b,c,d]";
                                            }
                                        };

                                        checkBoxes[m] = checkBox;
                                        incrementCheckBoxes[m] = incrementCheckBox;
                                        textBoxes[m] = vTextBox;
                                        valueMappings[m] = v;

                                        tableLayout.Controls.Add(checkBox, 0, row);
                                        tableLayout.Controls.Add(mLabel, 1, row);
                                        tableLayout.Controls.Add(vTextBox, 2, row);
                                        tableLayout.Controls.Add(incrementCheckBox, 3, row);

                                        row++;
                                    }
                                }
                            }
                        }
                    }
                }
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
                string messageFile = "sim_message.txt";
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
                                if (checkBoxes.ContainsKey(m) && checkBoxes[m].Checked)
                                {
                                    data["v"] = textBoxes[m].Text;
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

        public Dictionary<string, string> GetConfiguredValues()
        {
            return valueMappings;
        }
    }
}