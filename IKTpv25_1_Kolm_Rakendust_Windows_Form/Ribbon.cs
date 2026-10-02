using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Naidis_IKTpv25_Windows_Forms
{
    // Tööriistariba nagu Wordis: üleval vahelehed, all valitud lehe nupugrupid ühel real
    public class Ribbon : TabControl
    {
        private readonly List<FlowLayoutPanel> paneelid = new List<FlowLayoutPanel>();

        public Ribbon()
        {
            Dock = DockStyle.Top;
            Multiline = false;
            TabStop = false;
        }

        public void LisaSakk(string pealkiri, params Control[] grupid)
        {
            var paneel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                Padding = new Padding(4),
                BackColor = UiStiil.PaneeliTaust
            };
            paneel.Controls.AddRange(grupid);

            var leht = new TabPage(pealkiri) { BackColor = UiStiil.PaneeliTaust };
            leht.Controls.Add(paneel);
            TabPages.Add(leht);
            paneelid.Add(paneel);
        }

        // Kutsuda pärast kõigi lehtede lisamist: seab kõrguse ja akna minimaalse laiuse,
        // et valitud lehe nupud mahuksid alati ühele reale
        public void Valmis(Form vorm)
        {
            int korgus = 0;
            int laius = 0;
            foreach (FlowLayoutPanel p in paneelid)
            {
                Size s = p.GetPreferredSize(Size.Empty);
                korgus = Math.Max(korgus, s.Height);
                laius = Math.Max(laius, s.Width);
            }
            Height = korgus + 38;

            int raam = vorm.Width - vorm.ClientSize.Width;
            int vajalik = Math.Min(laius + 24, Screen.PrimaryScreen.WorkingArea.Width - raam);
            vorm.MinimumSize = new Size(vajalik + raam, vorm.MinimumSize.Height);
            if (vorm.ClientSize.Width < vajalik)
            {
                vorm.ClientSize = new Size(vajalik, vorm.ClientSize.Height);
            }
        }
    }
}
