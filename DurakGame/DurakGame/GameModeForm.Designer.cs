namespace DurakGame
{
    partial class GameModeForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // GameModeForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(579, 417);
            Margin = new Padding(4, 5, 4, 5);
            Name = "GameModeForm";
            Text = "Выбор режима игры";
            ResumeLayout(false);
        }
    }
}