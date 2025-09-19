using System;
using System.Drawing;
using System.Windows.Forms;

namespace WorkOrderBlender
{
  public partial class SequenceDialog : Form
  {
    public string SequenceId { get; private set; }

    public SequenceDialog()
    {
      InitializeComponent();
    }

    public SequenceDialog(string title, string prompt) : this()
    {
      this.Text = title;
      this.lblPrompt.Text = prompt;
    }

    private void InitializeComponent()
    {
      this.lblPrompt = new Label();
      this.txtSequenceId = new TextBox();
      this.btnOK = new Button();
      this.btnCancel = new Button();
      this.SuspendLayout();

      //
      // lblPrompt
      //
      this.lblPrompt.AutoSize = true;
      this.lblPrompt.Location = new Point(12, 15);
      this.lblPrompt.Name = "lblPrompt";
      this.lblPrompt.Size = new Size(200, 13);
      this.lblPrompt.TabIndex = 0;
      this.lblPrompt.Text = "Enter sequence ID for selected rows:";

      //
      // txtSequenceId
      //
      this.txtSequenceId.Location = new Point(12, 35);
      this.txtSequenceId.Name = "txtSequenceId";
      this.txtSequenceId.Size = new Size(350, 20);
      this.txtSequenceId.TabIndex = 1;
      this.txtSequenceId.TextChanged += new EventHandler(this.txtSequenceId_TextChanged);

      //
      // btnOK
      //
      this.btnOK.Enabled = false;
      this.btnOK.Location = new Point(206, 70);
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new Size(75, 23);
      this.btnOK.TabIndex = 2;
      this.btnOK.Text = "OK";
      this.btnOK.UseVisualStyleBackColor = true;
      this.btnOK.Click += new EventHandler(this.btnOK_Click);

      //
      // btnCancel
      //
      this.btnCancel.DialogResult = DialogResult.Cancel;
      this.btnCancel.Location = new Point(287, 70);
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new Size(75, 23);
      this.btnCancel.TabIndex = 3;
      this.btnCancel.Text = "Cancel";
      this.btnCancel.UseVisualStyleBackColor = true;

      //
      // SequenceDialog
      //
      this.AcceptButton = this.btnOK;
      this.AutoScaleDimensions = new SizeF(6F, 13F);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.CancelButton = this.btnCancel;
      this.ClientSize = new Size(374, 105);
      this.Controls.Add(this.btnCancel);
      this.Controls.Add(this.btnOK);
      this.Controls.Add(this.txtSequenceId);
      this.Controls.Add(this.lblPrompt);
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "SequenceDialog";
      this.StartPosition = FormStartPosition.CenterParent;
      this.Text = "Sequence Selection";
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    private void txtSequenceId_TextChanged(object sender, EventArgs e)
    {
      btnOK.Enabled = !string.IsNullOrWhiteSpace(txtSequenceId.Text);
    }

    protected override void OnShown(EventArgs e)
    {
      base.OnShown(e);
      txtSequenceId.Focus();
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
      SequenceId = txtSequenceId.Text.Trim();
      DialogResult = DialogResult.OK;
    }

    private Label lblPrompt;
    private TextBox txtSequenceId;
    private Button btnOK;
    private Button btnCancel;
  }
}
