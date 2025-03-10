using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace MQTTMessageSenderApp
{
    public class ConfigForm : Form
    {
        private Dictionary<string, string> valueMappings;
        private Dictionary<string, CheckBox> checkBoxes = new Dictionary<string, CheckBox>();
        private Dictionary<string, TextBox> textBoxes = new Dictionary<string, TextBox>();
        private TableLayoutPanel tableLayout;

        public ConfigForm(Dictionary<string, string> existingValues)
        {
            this.Text = "功能值配置";
            this.ClientSize = new System.Drawing.Size(500, 500);
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
                ColumnCount = 3,
                Dock = DockStyle.Fill,
                AutoSize = true
            };

            tableLayout.Controls.Add(new Label { Text = "选择", AutoSize = true }, 0, 0);
            tableLayout.Controls.Add(new Label { Text = "功能名称", AutoSize = true }, 1, 0);
            tableLayout.Controls.Add(new Label { Text = "值 (可编辑)", AutoSize = true }, 2, 0);

            this.Controls.Add(saveButton);
            this.Controls.Add(tableLayout);
            this.Controls.Add(parseButton);
        }

        private void ParseJson(object sender, EventArgs e)
        {
            tableLayout.Controls.Clear();
            checkBoxes.Clear();
            textBoxes.Clear();

            tableLayout.Controls.Add(new Label { Text = "选择", AutoSize = true }, 0, 0);
            tableLayout.Controls.Add(new Label { Text = "功能名称", AutoSize = true }, 1, 0);
            tableLayout.Controls.Add(new Label { Text = "值 (可编辑)", AutoSize = true }, 2, 0);

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
                                        Label mLabel = new Label { Text = m, AutoSize = true };
                                        TextBox vTextBox = new TextBox { Text = v, Enabled = false, Width = 100 };

                                        checkBox.CheckedChanged += (s, ev) => vTextBox.Enabled = checkBox.Checked;

                                        checkBoxes[m] = checkBox;
                                        textBoxes[m] = vTextBox;
                                        valueMappings[m] = v;

                                        tableLayout.Controls.Add(checkBox, 0, row);
                                        tableLayout.Controls.Add(mLabel, 1, row);
                                        tableLayout.Controls.Add(vTextBox, 2, row);

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
            foreach (var m in checkBoxes.Keys)
            {
                if (checkBoxes[m].Checked)
                {
                    valueMappings[m] = textBoxes[m].Text;
                }
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public Dictionary<string, string> GetConfiguredValues()
        {
            return valueMappings;
        }
    }
}
