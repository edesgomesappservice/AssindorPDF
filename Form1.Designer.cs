namespace CloudDocs.AssinadorDigital
{
    partial class frmPrincipal
    {
        /// <summary>
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Windows Form Designer

        /// <summary>
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmPrincipal));
            this.cboCertificados = new System.Windows.Forms.ComboBox();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.cntSiar = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.sairToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lblURL = new System.Windows.Forms.Label();
            this.btnRecarregar = new System.Windows.Forms.Button();
            this.btnConfiguracoes = new System.Windows.Forms.Button();
            this.btnFechar = new System.Windows.Forms.Button();
            this.btnAssinar = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cntSiar.SuspendLayout();
            this.SuspendLayout();
            // 
            // cboCertificados
            // 
            this.cboCertificados.AllowDrop = true;
            this.cboCertificados.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cboCertificados.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cboCertificados.DisplayMember = "Nome";
            this.cboCertificados.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboCertificados.Location = new System.Drawing.Point(15, 24);
            this.cboCertificados.Name = "cboCertificados";
            this.cboCertificados.Size = new System.Drawing.Size(518, 21);
            this.cboCertificados.TabIndex = 49;
            this.cboCertificados.ValueMember = "PessoaId";
            this.cboCertificados.Visible = false;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.cntSiar;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "AppService - Assinador Digital A3";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // cntSiar
            // 
            this.cntSiar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sairToolStripMenuItem});
            this.cntSiar.Name = "cntSiar";
            this.cntSiar.Size = new System.Drawing.Size(94, 26);
            this.cntSiar.Text = "Sair";
            // 
            // sairToolStripMenuItem
            // 
            this.sairToolStripMenuItem.Name = "sairToolStripMenuItem";
            this.sairToolStripMenuItem.Size = new System.Drawing.Size(93, 22);
            this.sairToolStripMenuItem.Text = "Sair";
            this.sairToolStripMenuItem.Click += new System.EventHandler(this.sairToolStripMenuItem_Click);
            // 
            // lblURL
            // 
            this.lblURL.AutoSize = true;
            this.lblURL.Enabled = false;
            this.lblURL.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblURL.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblURL.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblURL.Location = new System.Drawing.Point(40, 104);
            this.lblURL.Name = "lblURL";
            this.lblURL.Size = new System.Drawing.Size(60, 13);
            this.lblURL.TabIndex = 57;
            this.lblURL.Text = "Link-URL";
            // 
            // btnRecarregar
            // 
            this.btnRecarregar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecarregar.Image = global::CloudDocs.AssinadorDigital.Properties.Resources.btnreload21;
            this.btnRecarregar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRecarregar.Location = new System.Drawing.Point(538, 24);
            this.btnRecarregar.Name = "btnRecarregar";
            this.btnRecarregar.Size = new System.Drawing.Size(32, 22);
            this.btnRecarregar.TabIndex = 56;
            this.btnRecarregar.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.btnRecarregar.UseVisualStyleBackColor = true;
            this.btnRecarregar.Visible = false;
            this.btnRecarregar.Click += new System.EventHandler(this.btnRecarregar_Click);
            // 
            // btnConfiguracoes
            // 
            this.btnConfiguracoes.ContextMenuStrip = this.cntSiar;
            this.btnConfiguracoes.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnConfiguracoes.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnConfiguracoes.Image = global::CloudDocs.AssinadorDigital.Properties.Resources.cog;
            this.btnConfiguracoes.Location = new System.Drawing.Point(555, 96);
            this.btnConfiguracoes.Name = "btnConfiguracoes";
            this.btnConfiguracoes.Size = new System.Drawing.Size(25, 23);
            this.btnConfiguracoes.TabIndex = 55;
            this.btnConfiguracoes.UseVisualStyleBackColor = true;
            this.btnConfiguracoes.Click += new System.EventHandler(this.btnConfiguracoes_Click);
            // 
            // btnFechar
            // 
            this.btnFechar.BackColor = System.Drawing.SystemColors.Menu;
            this.btnFechar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFechar.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnFechar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnFechar.Location = new System.Drawing.Point(179, 34);
            this.btnFechar.Name = "btnFechar";
            this.btnFechar.Size = new System.Drawing.Size(202, 35);
            this.btnFechar.TabIndex = 54;
            this.btnFechar.Text = "&Minimizar";
            this.btnFechar.UseVisualStyleBackColor = false;
            this.btnFechar.Click += new System.EventHandler(this.btnFechar_Click);
            // 
            // btnAssinar
            // 
            this.btnAssinar.BackColor = System.Drawing.Color.Transparent;
            this.btnAssinar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAssinar.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAssinar.ForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.btnAssinar.Image = global::CloudDocs.AssinadorDigital.Properties.Resources.btnsign;
            this.btnAssinar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnAssinar.Location = new System.Drawing.Point(6, 10);
            this.btnAssinar.Name = "btnAssinar";
            this.btnAssinar.Size = new System.Drawing.Size(202, 35);
            this.btnAssinar.TabIndex = 52;
            this.btnAssinar.Text = "&Assinar Documento";
            this.btnAssinar.UseVisualStyleBackColor = false;
            this.btnAssinar.Visible = false;
            this.btnAssinar.Click += new System.EventHandler(this.btnAssinar_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Enabled = false;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(3, 104);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 13);
            this.label1.TabIndex = 58;
            this.label1.Text = "URL:";
            // 
            // frmPrincipal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 123);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblURL);
            this.Controls.Add(this.btnRecarregar);
            this.Controls.Add(this.btnConfiguracoes);
            this.Controls.Add(this.btnFechar);
            this.Controls.Add(this.btnAssinar);
            this.Controls.Add(this.cboCertificados);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "frmPrincipal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = " - Assinador Digital - Versão 3.0";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmPrincipal_FormClosing);
            this.Load += new System.EventHandler(this.frmPrincipal_Load);
            this.Resize += new System.EventHandler(this.frmPrincipal_Resize);
            this.cntSiar.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cboCertificados;
        private System.Windows.Forms.Button btnAssinar;
        private System.Windows.Forms.Button btnFechar;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip cntSiar;
        private System.Windows.Forms.ToolStripMenuItem sairToolStripMenuItem;
        private System.Windows.Forms.Button btnConfiguracoes;
        private System.Windows.Forms.Button btnRecarregar;
        private System.Windows.Forms.Label lblURL;
        private System.Windows.Forms.Label label1;
    }
}

